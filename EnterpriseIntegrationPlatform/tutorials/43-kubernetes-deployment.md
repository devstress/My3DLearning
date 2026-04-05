# Tutorial 43 — Kubernetes Deployment

## What You'll Learn

- How to deploy the EIP platform on Kubernetes
- Helm chart structure and configuration via `deploy/helm/eip/`
- Kustomize overlays for dev, staging, and production environments
- Container image management with GitHub Container Registry (GHCR)
- Horizontal Pod Autoscaler configuration for elastic scaling
- Transitioning from .NET Aspire local development to Kubernetes production

## From .NET Aspire to Kubernetes

During development, .NET Aspire orchestrates services locally. For production, we
transition to Kubernetes for resilience, scaling, and infrastructure-as-code.

```
┌─────────────────────────────────────────────────────┐
│               Development (.NET Aspire)             │
│  AppHost → orchestrates Gateway, Workers, Infra     │
└──────────────────────┬──────────────────────────────┘
                       │  Transition
                       ▼
┌─────────────────────────────────────────────────────┐
│              Production (Kubernetes)                 │
│  Helm/Kustomize → K8s manifests → eip namespace     │
│  GHCR images → Pods → Services → Ingress            │
└─────────────────────────────────────────────────────┘
```

## Helm Chart Structure

The Helm chart lives at `deploy/helm/eip/`:

```
deploy/helm/eip/
├── Chart.yaml          # Chart metadata and version
├── values.yaml         # Default configuration values
└── templates/
    ├── _helpers.tpl
    ├── admin-api.yaml
    ├── configmap.yaml
    ├── demo-pipeline.yaml
    ├── grafana-dashboards-configmap.yaml
    ├── hpa.yaml
    ├── ingestion-kafka.yaml
    ├── namespace.yaml
    ├── networkpolicy.yaml
    ├── openclaw-web.yaml
    ├── serviceaccount.yaml
    └── workflow-temporal.yaml
```

**Chart.yaml** declares the chart:

```yaml
apiVersion: v2
name: eip
description: Enterprise Integration Platform
version: 1.0.0
appVersion: "1.0.0"
```

**values.yaml** provides defaults:

```yaml
replicaCount: 2
image:
  repository: ghcr.io/your-org/eip-gateway
  tag: latest
  pullPolicy: IfNotPresent
namespace: eip
resources:
  requests:
    cpu: 250m
    memory: 256Mi
  limits:
    cpu: 1000m
    memory: 1Gi
autoscaling:
  enabled: true
  minReplicas: 2
  maxReplicas: 10
  targetCPUUtilizationPercentage: 70
```

## Kustomize Overlays

Kustomize overlays at `deploy/kustomize/` customize per environment:

```
deploy/kustomize/
├── base/
│   ├── kustomization.yaml
│   ├── namespace.yaml
│   ├── admin-api/
│   │   ├── deployment.yaml
│   │   └── service.yaml
│   └── openclaw-web/
│       ├── deployment.yaml
│       └── service.yaml
├── overlays/
│   ├── dev/
│   │   └── kustomization.yaml        # 1 replica, debug logging
│   ├── staging/
│   │   └── kustomization.yaml        # 2 replicas, info logging
│   └── prod/
│       ├── kustomization.yaml        # 3+ replicas, warn logging
│       ├── pdb-admin-api.yaml        # PodDisruptionBudget
│       └── pdb-openclaw-web.yaml     # PodDisruptionBudget
```

Apply an overlay:

```bash
kubectl apply -k deploy/kustomize/overlays/prod/
```

## Namespace and Image Registry

All resources deploy into the `eip` namespace:

```bash
kubectl create namespace eip
kubectl config set-context --current --namespace=eip
```

Images are pushed to GHCR during CI:

```bash
docker build -t ghcr.io/your-org/eip-gateway:v1.0.0 .
docker push ghcr.io/your-org/eip-gateway:v1.0.0
```

## Horizontal Pod Autoscaler

The HPA scales pods based on CPU utilization:

```
          ┌──────────┐
          │   HPA    │  target: 70% CPU
          └────┬─────┘
               │ scales
     ┌─────────┼──────────┐
     ▼         ▼          ▼
 ┌───────┐ ┌───────┐ ┌───────┐
 │ Pod 1 │ │ Pod 2 │ │ Pod N │   min: 2, max: 10
 └───────┘ └───────┘ └───────┘
```

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: gateway-api-hpa
  namespace: eip
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: gateway-api
  minReplicas: 2
  maxReplicas: 10
  metrics:
    - type: Resource
      resource:
        name: cpu
        target:
          type: Utilization
          averageUtilization: 70
```

## Deployment Validation

Run `deploy/validate.sh` to verify deployments:

```bash
#!/bin/bash
set -e
echo "Validating EIP deployment..."
kubectl get pods -n eip -o wide
kubectl rollout status deployment/gateway-api -n eip --timeout=120s
kubectl rollout status deployment/pipeline-worker -n eip --timeout=120s
echo "All deployments healthy."
```

## Scalability Dimension

Kubernetes provides **horizontal scaling** through the HPA, adding or removing
pod replicas based on CPU and memory metrics. Combined with Competing Consumers
on the broker side, this enables elastic throughput that matches demand.

## Atomicity Dimension

Rolling deployments ensure **zero-downtime releases**. Kubernetes maintains the
old pods until new ones pass readiness probes, preserving message processing
continuity. Liveness probes automatically restart unhealthy pods.

## Lab

**Objective:** Configure Kubernetes HPA for auto-scaling integration workers, analyze graceful shutdown for **atomic** in-flight message handling, and design Kustomize overlays for multi-environment deployment.

### Step 1: Configure HPA with Multi-Metric Scaling

Modify the HPA to scale on both CPU and memory utilization:

```yaml
spec:
  minReplicas: 2
  maxReplicas: 20
  metrics:
    - type: Resource
      resource:
        name: cpu
        target:
          type: Utilization
          averageUtilization: 70
    - type: Resource
      resource:
        name: memory
        target:
          type: Utilization
          averageUtilization: 80
```

Why set memory threshold higher than CPU? (hint: pipeline workers are more likely CPU-bound from JSON processing; memory scaling catches enrichment cache growth)

### Step 2: Analyze Graceful Shutdown Atomicity

During a rolling update, a pod is terminated while processing a message. Trace the shutdown sequence:

```
1. Kubernetes sends SIGTERM → pod enters termination grace period
2. Worker stops accepting new messages (unsubscribes from broker)
3. In-flight messages complete processing (up to terminationGracePeriodSeconds)
4. Worker sends Ack for completed messages, Nack for incomplete ones
5. Pod terminates → broker redelivers Nack'd messages to other consumers
```

What happens if `terminationGracePeriodSeconds` is too short? How does this affect **message atomicity**?

### Step 3: Design Kustomize Overlays

Create a Kustomize patch that overrides the image tag for staging:

```yaml
# overlays/staging/kustomization.yaml
resources:
  - ../../base
patches:
  - target:
      kind: Deployment
      name: pipeline-worker
    patch: |
      - op: replace
        path: /spec/template/spec/containers/0/image
        value: registry.example.com/eip/worker:staging-latest
```

How does Kustomize differ from Helm for multi-environment deployment? Which is more **scalable** for a platform with 10+ microservices across 3 environments?

## Exam

1. What happens to in-flight messages when a pod is terminated during a rolling update?
   - A) Messages are lost
   - B) The pod completes processing in-flight messages during the termination grace period, Acks completed work, and Nacks incomplete messages — the broker redelivers Nack'd messages to healthy pods, ensuring **zero message loss**
   - C) All messages are automatically retried from the beginning
   - D) The broker waits for the pod to restart

2. Why should the memory HPA threshold be set higher than CPU for integration workers?
   - A) Memory is always less constrained than CPU
   - B) Pipeline workers are typically CPU-bound from JSON parsing and regex evaluation; memory growth is gradual (from caching and enrichment) — setting memory threshold higher prevents premature scaling while still catching memory-intensive workload changes
   - C) Kubernetes requires different thresholds
   - D) Memory scaling is faster than CPU scaling

3. How does Kubernetes auto-scaling support **integration platform scalability**?
   - A) HPA only works with web servers
   - B) HPA automatically adjusts the number of pipeline worker pods based on actual load — during peak hours, more workers process messages in parallel; during off-peak, resources are released, optimizing cost while maintaining throughput SLAs
   - C) Auto-scaling requires manual approval
   - D) The broker handles scaling internally

**Previous: [← Tutorial 42](42-configuration.md)** | **Next: [Tutorial 44 →](44-disaster-recovery.md)**

# Tutorial 43 — Kubernetes Deployment

Deploy the platform to Kubernetes with Helm charts and Kustomize overlays.

## Learning Objectives

After completing this tutorial you will be able to:

1. Resolve environment-specific configuration overrides
2. Fall back to default values when no environment override exists
3. Resolve multiple keys at once and publish results
4. Build a dev → staging → prod configuration cascade
5. Resolve configuration from environment variables

---

## Lab — Guided Practice

> 💻 Run the lab tests to see each concept demonstrated in isolation.
> Each test targets a single behaviour so you can study one idea at a time.

| # | Test Name | Concept |
|---|-----------|---------|
| 1 | `EnvironmentOverride_ResolvesSpecificEnvironment` | Resolve environment-specific override |
| 2 | `EnvironmentOverride_FallsBackToDefault` | Fall back to default value |
| 3 | `EnvironmentOverride_ReturnsNull_WhenNotFound` | Returns null when key not found |
| 4 | `EnvironmentOverride_ResolveMany_PublishResults` | Resolve many keys and publish results |
| 5 | `ConfigCascade_DevStagingProd_PublishResolved` | Dev → staging → prod cascade |
| 6 | `EnvironmentVariable_ResolveFromEnvVar` | Resolve from environment variable |

> 💻 [`tests/TutorialLabs/Tutorial43/Lab.cs`](../tests/TutorialLabs/Tutorial43/Lab.cs)

```bash
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial43.Lab"
```

---

## Exam — Fill in the Blanks

> 🎯 Open `Exam.cs` and fill in the `// TODO:` blanks. Tests will **fail** until you write the missing code.
> After attempting each challenge, check your work against `Exam.Answers.cs`.

| # | Challenge | Difficulty | What You Fill In |
|---|-----------|------------|------------------|
| 1 | `Challenge1_FullConfigCascade_WithNatsBrokerEndpoint` | 🟢 Starter | Full config cascade with NatsBrokerEndpoint |
| 2 | `Challenge2_MultiKeyResolution_AcrossEnvironments` | 🟡 Intermediate | Multi-key resolution across environments |
| 3 | `Challenge3_DeploymentConfigScenario_PublishAllResolved` | 🔴 Advanced | Deployment config scenario — publish all resolved |

> 💻 [`tests/TutorialLabs/Tutorial43/Exam.cs`](../tests/TutorialLabs/Tutorial43/Exam.cs)

```bash
# Run exam (will fail until you fill in the blanks):
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial43.Exam" --filter "FullyQualifiedName!~ExamAnswers"

# Run answer key to verify expected behaviour:
dotnet test --filter "FullyQualifiedName~TutorialLabs.Tutorial43.ExamAnswers"
```

---

**Previous: [← Tutorial 42](42-configuration.md)** | **Next: [Tutorial 44 →](44-disaster-recovery.md)**

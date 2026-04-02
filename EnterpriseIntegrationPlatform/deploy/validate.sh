#!/usr/bin/env bash
# Enterprise Integration Platform - Kubernetes manifest validation script
# Validates Helm charts and Kustomize overlays

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ERRORS=0

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

info()  { echo -e "${GREEN}[INFO]${NC}  $*"; }
warn()  { echo -e "${YELLOW}[WARN]${NC}  $*"; }
error() { echo -e "${RED}[ERROR]${NC} $*"; ERRORS=$((ERRORS + 1)); }

# ── 1. Validate YAML syntax ───────────────────────────────────────────

info "Validating YAML syntax..."

yaml_files=$(find "$SCRIPT_DIR" -name '*.yaml' -o -name '*.yml' | grep -v '_helpers.tpl' | sort)

for f in $yaml_files; do
    relative="${f#"$SCRIPT_DIR"/}"
    # Skip files with Helm template syntax ({{ }})
    if grep -q '{{' "$f" 2>/dev/null; then
        info "  Skipping Helm template: $relative"
        continue
    fi
    if python3 -c "import yaml, sys; yaml.safe_load_all(open(sys.argv[1]))" "$f" 2>/dev/null; then
        info "  ✓ $relative"
    else
        error "  ✗ Invalid YAML: $relative"
    fi
done

# ── 2. Helm chart validation ──────────────────────────────────────────

HELM_CHART="$SCRIPT_DIR/helm/eip"

if command -v helm &>/dev/null; then
    info ""
    info "Validating Helm chart..."

    if helm lint "$HELM_CHART" 2>&1; then
        info "  ✓ Helm lint passed"
    else
        error "  ✗ Helm lint failed"
    fi

    info ""
    info "Rendering Helm templates (dry-run)..."
    if helm template eip-release "$HELM_CHART" > /dev/null 2>&1; then
        info "  ✓ Helm template rendering succeeded"
        info ""
        info "Resources that would be created:"
        helm template eip-release "$HELM_CHART" | grep -E '^kind:|^  name:' | paste - - | sed 's/kind: //;s/  name: /  /' | sort | while read -r line; do
            info "    $line"
        done
    else
        error "  ✗ Helm template rendering failed"
        helm template eip-release "$HELM_CHART" 2>&1 | head -20
    fi
else
    warn "helm not found — skipping Helm validation"
fi

# ── 3. Kustomize validation ───────────────────────────────────────────

KUSTOMIZE_BASE="$SCRIPT_DIR/kustomize"

if command -v kubectl &>/dev/null; then
    info ""
    info "Validating Kustomize overlays..."

    for overlay in base overlays/dev overlays/staging overlays/prod; do
        overlay_path="$KUSTOMIZE_BASE/$overlay"
        if [ -d "$overlay_path" ]; then
            if kubectl kustomize "$overlay_path" > /dev/null 2>&1; then
                info "  ✓ $overlay"
                info "    Resources:"
                kubectl kustomize "$overlay_path" | grep -E '^kind:|^  name:' | paste - - | sed 's/kind: //;s/  name: /  /' | sort | while read -r line; do
                    info "      $line"
                done
            else
                error "  ✗ $overlay failed"
                kubectl kustomize "$overlay_path" 2>&1 | head -10
            fi
        else
            warn "  Overlay not found: $overlay"
        fi
    done
elif command -v kustomize &>/dev/null; then
    info ""
    info "Validating Kustomize overlays..."

    for overlay in base overlays/dev overlays/staging overlays/prod; do
        overlay_path="$KUSTOMIZE_BASE/$overlay"
        if [ -d "$overlay_path" ]; then
            if kustomize build "$overlay_path" > /dev/null 2>&1; then
                info "  ✓ $overlay"
            else
                error "  ✗ $overlay failed"
                kustomize build "$overlay_path" 2>&1 | head -10
            fi
        fi
    done
else
    warn "Neither kubectl nor kustomize found — skipping Kustomize validation"
fi

# ── Summary ───────────────────────────────────────────────────────────

echo ""
if [ "$ERRORS" -eq 0 ]; then
    info "All validations passed ✓"
    exit 0
else
    error "$ERRORS validation error(s) found"
    exit 1
fi

#!/bin/bash
set -e
OWNER="devstress"
REPO="My3DLearning"
BRANCH="copilot/continue-terrane-chunks"
MSG=$(git log -1 --format=%B)

push_file() {
  local filepath="$1"
  local content=$(base64 -w0 "$filepath")
  
  # Try to get existing SHA (file may be new)
  local sha=$(gh api "repos/$OWNER/$REPO/contents/$filepath?ref=$BRANCH" --jq '.sha' 2>/dev/null || echo "")
  
  if [ -n "$sha" ]; then
    # Update existing file
    gh api "repos/$OWNER/$REPO/contents/$filepath" -X PUT \
      -f message="$MSG" \
      -f content="$content" \
      -f sha="$sha" \
      -f branch="$BRANCH" \
      --jq '.commit.sha' 2>&1
  else
    # Create new file
    gh api "repos/$OWNER/$REPO/contents/$filepath" -X PUT \
      -f message="$MSG" \
      -f content="$content" \
      -f branch="$BRANCH" \
      --jq '.commit.sha' 2>&1
  fi
  echo "  ✓ $filepath"
}

FILES=$(git diff HEAD~1 --name-only)
while IFS= read -r f; do
  push_file "$f"
done <<< "$FILES"
echo "All files pushed!"

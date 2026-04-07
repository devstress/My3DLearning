#!/bin/bash
set -e
OWNER="devstress"
REPO="My3DLearning"
BRANCH="copilot/redo-integrations-real-lab-exam"
BASE="EnterpriseIntegrationPlatform/tests/TutorialLabs"

# Get the current commit SHA on the remote branch
REMOTE_SHA=$(gh api "repos/$OWNER/$REPO/git/ref/heads/$BRANCH" --jq '.object.sha')
echo "Remote HEAD: $REMOTE_SHA"

# Get the base tree
BASE_TREE=$(gh api "repos/$OWNER/$REPO/git/commits/$REMOTE_SHA" --jq '.tree.sha')
echo "Base tree: $BASE_TREE"

# Create blobs for each changed file
declare -A BLOB_SHAS
FILES=(
  "Tutorial36/Lab.cs"
  "Tutorial36/Exam.cs"
  "Tutorial37/Lab.cs"
  "Tutorial37/Exam.cs"
  "Tutorial38/Lab.cs"
  "Tutorial38/Exam.cs"
  "Tutorial39/Lab.cs"
  "Tutorial39/Exam.cs"
  "Tutorial40/Lab.cs"
  "Tutorial40/Exam.cs"
)

for f in "${FILES[@]}"; do
  FULL="$BASE/$f"
  CONTENT=$(base64 -w0 "$FULL")
  BLOB_SHA=$(gh api "repos/$OWNER/$REPO/git/blobs" -X POST \
    -f content="$CONTENT" -f encoding="base64" --jq '.sha')
  BLOB_SHAS["$FULL"]="$BLOB_SHA"
  echo "Blob for $f: $BLOB_SHA"
done

# Build tree JSON
TREE_JSON="["
FIRST=true
for f in "${FILES[@]}"; do
  FULL="$BASE/$f"
  if [ "$FIRST" = true ]; then FIRST=false; else TREE_JSON+=","; fi
  TREE_JSON+="{\"path\":\"$FULL\",\"mode\":\"100644\",\"type\":\"blob\",\"sha\":\"${BLOB_SHAS[$FULL]}\"}"
done
TREE_JSON+="]"

# Create tree
NEW_TREE=$(echo "$TREE_JSON" | gh api "repos/$OWNER/$REPO/git/trees" -X POST \
  --input - -f base_tree="$BASE_TREE" --jq '.sha' \
  --field "tree=$TREE_JSON")
echo "New tree: $NEW_TREE"

# Create commit
LOCAL_MSG=$(git log -1 --format=%B)
NEW_COMMIT=$(gh api "repos/$OWNER/$REPO/git/commits" -X POST \
  -f message="$LOCAL_MSG" \
  -f "tree=$NEW_TREE" \
  -f "parents[]=$REMOTE_SHA" \
  --jq '.sha')
echo "New commit: $NEW_COMMIT"

# Update the branch ref
gh api "repos/$OWNER/$REPO/git/refs/heads/$BRANCH" -X PATCH \
  -f sha="$NEW_COMMIT" -F force=true --jq '.object.sha'
echo "Branch updated!"

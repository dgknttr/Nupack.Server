#!/bin/bash

# GitHub Branch Protection Setup Script
# Usage: ./setup-branch-protection.sh YOUR_GITHUB_TOKEN

if [ -z "$1" ]; then
    echo "Usage: $0 <github_token>"
    echo "Get your token from: https://github.com/settings/tokens"
    exit 1
fi

GITHUB_TOKEN="$1"
REPO_OWNER="dgknttr"
REPO_NAME="Nupack.Server"
BRANCH="main"

echo "Setting up branch protection for $REPO_OWNER/$REPO_NAME:$BRANCH"

curl -X PUT \
  -H "Authorization: token $GITHUB_TOKEN" \
  -H "Accept: application/vnd.github.v3+json" \
  "https://api.github.com/repos/$REPO_OWNER/$REPO_NAME/branches/$BRANCH/protection" \
  -d '{
    "required_status_checks": {
      "strict": true,
      "contexts": [
        "Test",
        "Security Scan",
        "CodeQL Analysis (csharp)"
      ]
    },
    "enforce_admins": true,
    "required_pull_request_reviews": {
      "required_approving_review_count": 1,
      "dismiss_stale_reviews": true,
      "require_code_owner_reviews": true,
      "require_last_push_approval": true
    },
    "restrictions": null,
    "required_linear_history": true,
    "allow_force_pushes": false,
    "allow_deletions": false,
    "block_creations": false
  }'

echo ""
echo "Branch protection setup complete!"
echo "Check: https://github.com/$REPO_OWNER/$REPO_NAME/settings/branches"

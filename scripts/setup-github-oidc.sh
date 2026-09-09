#!/usr/bin/env bash
# One-time setup: creates an Azure AD app registration with a federated
# credential trusting GitHub Actions OIDC tokens from this repo's main
# branch, and grants it the roles needed by the
# .github/workflows/azure-kubernetes-service.yml workflow (ACR push,
# AKS credential fetch).
#
# Run manually against your own `az login` session — this is not
# invoked by CI. Fill in the variables below before running.

set -euo pipefail

# ---- 1. Fill these in ----
SUBSCRIPTION_ID="<your-subscription-id>"
TENANT_ID="<your-tenant-id>"
GH_ORG="org-drake"
GH_REPO="dot-net-core-microservice"

ACR_RESOURCE_GROUP="<acr-resource-group>"
ACR_NAME="<acr-name>"                # short name, no .azurecr.io suffix

AKS_RESOURCE_GROUP="<aks-resource-group>"
AKS_CLUSTER_NAME="<aks-cluster-name>"

APP_NAME="gh-oidc-${GH_REPO}"

# ---- 2. Log in and select subscription ----
az login
az account set --subscription "$SUBSCRIPTION_ID"

# ---- 3. Create a dedicated app registration + service principal for GitHub OIDC ----
APP_ID=$(az ad app create --display-name "$APP_NAME" --query appId -o tsv)
az ad sp create --id "$APP_ID"
SP_OBJECT_ID=$(az ad sp show --id "$APP_ID" --query id -o tsv)

echo "AZURE_CLIENT_ID:       $APP_ID"
echo "AZURE_TENANT_ID:       $TENANT_ID"
echo "AZURE_SUBSCRIPTION_ID: $SUBSCRIPTION_ID"

# ---- 4. Federated credential trusting pushes to main on this repo ----
# One credential covers both the "build" and "deploy" jobs — the
# subject is scoped to repo+branch, not job name, and both jobs only
# run on push to main.
az ad app federated-credential create \
  --id "$APP_ID" \
  --parameters '{
    "name": "github-main-branch",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:'"$GH_ORG"'/'"$GH_REPO"':ref:refs/heads/main",
    "audiences": ["api://AzureADTokenExchange"]
  }'

# ---- 5. Grant least-privilege roles ----
# ACR: allow pushing images (build job)
ACR_ID=$(az acr show --name "$ACR_NAME" --resource-group "$ACR_RESOURCE_GROUP" --query id -o tsv)
az role assignment create \
  --assignee-object-id "$SP_OBJECT_ID" \
  --assignee-principal-type ServicePrincipal \
  --role "AcrPush" \
  --scope "$ACR_ID"

# AKS: allow fetching cluster credentials (deploy job)
AKS_ID=$(az aks show --name "$AKS_CLUSTER_NAME" --resource-group "$AKS_RESOURCE_GROUP" --query id -o tsv)
az role assignment create \
  --assignee-object-id "$SP_OBJECT_ID" \
  --assignee-principal-type ServicePrincipal \
  --role "Azure Kubernetes Service Cluster User Role" \
  --scope "$AKS_ID"

# Only if the cluster has Azure RBAC for Kubernetes Authorization enabled
# (check: az aks show -n $AKS_CLUSTER_NAME -g $AKS_RESOURCE_GROUP --query aadProfile.enableAzureRbac)
# — otherwise `kubectl apply` will 403 even with a valid kubeconfig:
# az role assignment create \
#   --assignee-object-id "$SP_OBJECT_ID" \
#   --assignee-principal-type ServicePrincipal \
#   --role "Azure Kubernetes Service RBAC Writer" \
#   --scope "$AKS_ID"

# ---- 6. Push the three values into GitHub repo secrets ----
# Requires `gh` CLI authenticated, or set them manually in
# Settings > Secrets and variables > Actions
gh secret set AZURE_CLIENT_ID --repo "$GH_ORG/$GH_REPO" --body "$APP_ID"
gh secret set AZURE_TENANT_ID --repo "$GH_ORG/$GH_REPO" --body "$TENANT_ID"
gh secret set AZURE_SUBSCRIPTION_ID --repo "$GH_ORG/$GH_REPO" --body "$SUBSCRIPTION_ID"

# ---- 7. Once a workflow run succeeds end-to-end, remove the old secrets ----
# gh secret delete AZURE_CREDENTIALS --repo "$GH_ORG/$GH_REPO"
# gh secret delete ACR_USERNAME --repo "$GH_ORG/$GH_REPO"
# gh secret delete ACR_PASSWORD --repo "$GH_ORG/$GH_REPO"

#!/usr/bin/env bash
# One-time setup: registers this API as an App Registration in Microsoft
# Entra ID so it can validate Bearer tokens (AzureAd:TenantId /
# AzureAd:Audience, see src/dot-net-core-microservice/Program.cs). Exposes an
# app role so service principals (e.g. another workload's managed identity)
# can be granted access via the client-credentials flow - there's no signed-in
# user for service-to-service calls, so this uses app roles rather than
# delegated (user) scopes.
#
# Run manually against your own `az login` session - this is not invoked by
# CI. Fill in the variables below before running.

set -euo pipefail

# ---- 1. Fill these in ----
SUBSCRIPTION_ID="<your-subscription-id>"
TENANT_ID="<your-tenant-id>"
APP_NAME="dot-net-core-microservice-api"
APP_ROLE_VALUE="Products.ReadWrite"

# ---- 2. Log in and select subscription ----
az login
az account set --subscription "$SUBSCRIPTION_ID"

# ---- 3. Create the App Registration ----
APP_ID=$(az ad app create --display-name "$APP_NAME" --query appId -o tsv)
az ad sp create --id "$APP_ID" >/dev/null

# Application ID URI - this is what callers request a token "for", and what
# the API sets as AzureAd:Audience.
az ad app update --id "$APP_ID" --identifier-uris "api://$APP_ID"

# ---- 4. Expose an app role for service-to-service callers ----
ROLE_ID=$(uuidgen 2>/dev/null || python3 -c "import uuid; print(uuid.uuid4())")
az ad app update --id "$APP_ID" --app-roles "[{
  \"allowedMemberTypes\": [\"Application\"],
  \"description\": \"Full access to the Products API\",
  \"displayName\": \"$APP_ROLE_VALUE\",
  \"id\": \"$ROLE_ID\",
  \"isEnabled\": true,
  \"value\": \"$APP_ROLE_VALUE\"
}]"

echo
echo "AzureAd:TenantId = $TENANT_ID"
echo "AzureAd:Audience = api://$APP_ID"
echo "-> set these via dotnet user-secrets locally, or AzureAd__TenantId /"
echo "   AzureAd__Audience in k8s/all.yaml for the deployed app."
echo
echo "To let a service principal (e.g. a workload identity's app ID, from"
echo "scripts/setup-workload-identity.sh) call this API:"
echo "  az ad app permission add --id <caller-app-id> --api $APP_ID --api-permissions ${ROLE_ID}=Role"
echo "  az ad app permission admin-consent --id <caller-app-id>"
echo "It can then request a token with:"
echo "  az account get-access-token --resource api://$APP_ID"

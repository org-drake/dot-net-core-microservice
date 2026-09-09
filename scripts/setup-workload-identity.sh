#!/usr/bin/env bash
# One-time setup: creates a user-assigned managed identity, federates it with
# this AKS cluster's OIDC issuer via Microsoft Entra Workload ID, and prints
# what to paste into k8s/all.yaml and run against Azure SQL Database so the
# app can authenticate with zero stored SQL credentials (Sql:AuthenticationMode
# = "ManagedIdentity", see src/dot-net-core-microservice.Infrastructure/DependencyInjection.cs).
#
# Run manually against your own `az login` session - this is not invoked by
# CI. Fill in the variables below before running. Requires the target
# database to actually be Azure SQL Database or Managed Instance - Workload
# ID cannot authenticate to a self-hosted SQL Server.

set -euo pipefail

# ---- 1. Fill these in ----
SUBSCRIPTION_ID="<your-subscription-id>"
RESOURCE_GROUP="<resource-group>"

AKS_RESOURCE_GROUP="<aks-resource-group>"
AKS_CLUSTER_NAME="<aks-cluster-name>"

K8S_NAMESPACE="dotnet-demo"
K8S_SERVICE_ACCOUNT="dot-net-core-microservice"

SQL_SERVER_NAME="<your-sql-server>"                 # without .database.windows.net
SQL_DATABASE_NAME="ProductCatalogDb"

IDENTITY_NAME="wi-dot-net-core-microservice"

# ---- 2. Log in and select subscription ----
az login
az account set --subscription "$SUBSCRIPTION_ID"

# ---- 3. Ensure the AKS cluster has an OIDC issuer and Workload ID enabled ----
az aks update \
  --resource-group "$AKS_RESOURCE_GROUP" \
  --name "$AKS_CLUSTER_NAME" \
  --enable-oidc-issuer \
  --enable-workload-identity

AKS_OIDC_ISSUER=$(az aks show \
  --resource-group "$AKS_RESOURCE_GROUP" \
  --name "$AKS_CLUSTER_NAME" \
  --query "oidcIssuerProfile.issuerUrl" -o tsv)

# ---- 4. Create the user-assigned managed identity ----
az identity create \
  --resource-group "$RESOURCE_GROUP" \
  --name "$IDENTITY_NAME"

IDENTITY_CLIENT_ID=$(az identity show \
  --resource-group "$RESOURCE_GROUP" \
  --name "$IDENTITY_NAME" \
  --query "clientId" -o tsv)

echo "Managed identity client ID: $IDENTITY_CLIENT_ID"
echo "-> paste this into k8s/all.yaml as the ServiceAccount's"
echo "   azure.workload.identity/client-id annotation."

# ---- 5. Federated credential trusting the AKS ServiceAccount's OIDC token ----
az identity federated-credential create \
  --name "aks-${K8S_NAMESPACE}-${K8S_SERVICE_ACCOUNT}" \
  --identity-name "$IDENTITY_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --issuer "$AKS_OIDC_ISSUER" \
  --subject "system:serviceaccount:${K8S_NAMESPACE}:${K8S_SERVICE_ACCOUNT}" \
  --audiences "api://AzureADTokenExchange"

# ---- 6. Grant the identity access on the Azure SQL Database ----
# Contained database users for a managed identity can only be created by
# connecting as the server's Microsoft Entra admin - this cannot be scripted
# with `az` alone. Connect (e.g. via `sqlcmd` or Azure Data Studio, using
# Entra auth) to $SQL_DATABASE_NAME on $SQL_SERVER_NAME.database.windows.net
# and run:
cat <<SQL

Run this against ${SQL_DATABASE_NAME} on ${SQL_SERVER_NAME}.database.windows.net,
connected as the server's Microsoft Entra admin:

  CREATE USER [${IDENTITY_NAME}] FROM EXTERNAL PROVIDER;
  ALTER ROLE db_datareader ADD MEMBER [${IDENTITY_NAME}];
  ALTER ROLE db_datawriter ADD MEMBER [${IDENTITY_NAME}];

SQL

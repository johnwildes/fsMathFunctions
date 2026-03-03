# Deployment Guide — Azure Container Apps

## Prerequisites

- Azure CLI (`az`) — logged in: `az login`
- Azure Developer CLI (`azd`) — optional but recommended
- Docker
- An Azure subscription with a resource group

---

## First-time deploy

### 1. Create resource group

```bash
az group create --name fsmathfn-rg --location eastus
```

### 2. Provision infrastructure

```bash
az deployment group create \
  --resource-group fsmathfn-rg \
  --template-file infra/main.bicep \
  --parameters @infra/main.parameters.json \
  --parameters dbAdminPassword='<strong-password>'
```

Save the outputs — you'll need `acrLoginServer` and `keyVaultName`.

### 3. Add the JWT secret to Key Vault

```bash
az keyvault secret set \
  --vault-name <keyVaultName> \
  --name jwt-secret \
  --value "$(openssl rand -base64 48)"
```

### 4. Build and push Docker images

Replace `<acr>` with the `acrLoginServer` output (e.g. `fsmathfnprodabc123acr.azurecr.io`).

```bash
ACR=<acr>
PORTAL_URL=<portal-url-from-deployment-output>
TAG=latest

az acr login --name $ACR

# Finance API
docker build -t $ACR/finance-api:$TAG .
docker push $ACR/finance-api:$TAG

# Portal backend
docker build -t $ACR/portal:$TAG -f FsMathFunctions.Portal/Dockerfile .
docker push $ACR/portal:$TAG

# Portal UI — embed the backend URL at build time
docker build \
  --build-arg VITE_PORTAL_API_URL=$PORTAL_URL \
  -t $ACR/portal-ui:$TAG \
  FsMathFunctions.Portal.UI
docker push $ACR/portal-ui:$TAG
```

### 5. Update container apps to use the new images

```bash
RG=fsmathfn-rg
PREFIX=fsmathfn-prod-<uniqueSuffix>   # from deployment

az containerapp update --name $PREFIX-finance-api --resource-group $RG --image $ACR/finance-api:$TAG
az containerapp update --name $PREFIX-portal      --resource-group $RG --image $ACR/portal:$TAG
az containerapp update --name $PREFIX-portal-ui   --resource-group $RG --image $ACR/portal-ui:$TAG
```

---

## Using `azd` (alternative)

```bash
azd auth login
azd up          # provisions infra + builds + deploys all services
```

> **Note**: First run will prompt for `dbAdminPassword`. Add the JWT secret to Key Vault manually after `azd provision`.

---

## First admin account

After deployment, register via the Portal UI. The first registered user gets the `User` role. To promote to `Admin`, connect to the database and run:

```sql
UPDATE users SET role = 'Admin' WHERE email = 'your@email.com';
```

---

## Local development

```bash
cp .env.example .env
# Edit .env — set a real JWT_SECRET
docker compose up
```

Services:
- Portal UI: http://localhost:3000
- Portal API: http://localhost:5001/swagger
- Finance API: http://localhost:5000

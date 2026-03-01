// FsMathFunctions — Main Bicep Template
// Provisions all Azure resources for the API key management portal on ACA
//
// Deploy with:
//   az deployment group create \
//     --resource-group <rg> \
//     --template-file infra/main.bicep \
//     --parameters prefix=fsmathfn env=prod location=eastus \
//                  dbAdminPassword=<secret> \
//     --parameters @infra/main.parameters.json

@description('Short prefix for all resource names (e.g. fsmathfn)')
@minLength(3)
@maxLength(12)
param prefix string

@description('Environment tag (dev | staging | prod)')
@allowed(['dev', 'staging', 'prod'])
param env string = 'prod'

@description('Azure region for all resources')
param location string = resourceGroup().location

@description('PostgreSQL administrator username')
param dbAdminUsername string = 'pgadmin'

@description('PostgreSQL administrator password')
@secure()
param dbAdminPassword string

@description('Docker image tag to deploy across all services')
param imageTag string = 'latest'

var tags = {
  project: 'FsMathFunctions'
  environment: env
  managedBy: 'bicep'
}

// Unique suffix derived from the resource group ID to avoid global naming conflicts
var uniqueSuffix = take(uniqueString(resourceGroup().id), 6)
var fullPrefix = '${prefix}-${env}-${uniqueSuffix}'

// ─── Container Registry ──────────────────────────────────────────────────────

module acr 'modules/acr.bicep' = {
  name: 'acr'
  params: {
    location: location
    prefix: fullPrefix
    tags: tags
  }
}

// ─── PostgreSQL ───────────────────────────────────────────────────────────────

module postgres 'modules/postgres.bicep' = {
  name: 'postgres'
  params: {
    location: location
    prefix: fullPrefix
    tags: tags
    adminUsername: dbAdminUsername
    adminPassword: dbAdminPassword
  }
}

var dbConnectionString = 'Host=${postgres.outputs.host};Database=${postgres.outputs.databaseName};Username=${postgres.outputs.adminUsername};Password=${dbAdminPassword};SSL Mode=Require;Trust Server Certificate=true'

// ─── Container Apps Environment ───────────────────────────────────────────────

module acaEnvironment 'modules/aca-environment.bicep' = {
  name: 'acaEnvironment'
  params: {
    location: location
    prefix: fullPrefix
    tags: tags
  }
}

// ─── Portal Backend (deployed first so we get its URL for the UI build) ───────

module portal 'modules/portal.bicep' = {
  name: 'portal'
  params: {
    location: location
    prefix: fullPrefix
    tags: tags
    environmentId: acaEnvironment.outputs.id
    acrLoginServer: acr.outputs.loginServer
    acrId: acr.outputs.id
    imageTag: imageTag
    dbConnectionString: dbConnectionString
    // Key Vault URI populated after keyvault module runs — see ordering below
    keyVaultUri: keyVault.outputs.vaultUri
    corsOrigin: portalUi.outputs.url
  }
}

// ─── Key Vault (depends on portal identity principal ID) ─────────────────────

module keyVault 'modules/keyvault.bicep' = {
  name: 'keyVault'
  params: {
    location: location
    prefix: fullPrefix
    tags: tags
    portalPrincipalId: portal.outputs.principalId
  }
}

// ─── Finance API ──────────────────────────────────────────────────────────────

module financeApi 'modules/finance-api.bicep' = {
  name: 'financeApi'
  params: {
    location: location
    prefix: fullPrefix
    tags: tags
    environmentId: acaEnvironment.outputs.id
    acrLoginServer: acr.outputs.loginServer
    acrId: acr.outputs.id
    imageTag: imageTag
    dbConnectionString: dbConnectionString
  }
}

// ─── Portal UI ────────────────────────────────────────────────────────────────

module portalUi 'modules/portal-ui.bicep' = {
  name: 'portalUi'
  params: {
    location: location
    prefix: fullPrefix
    tags: tags
    environmentId: acaEnvironment.outputs.id
    acrLoginServer: acr.outputs.loginServer
    acrId: acr.outputs.id
    imageTag: imageTag
    portalApiUrl: portal.outputs.url
  }
}

// ─── Outputs ──────────────────────────────────────────────────────────────────

@description('Portal UI public URL')
output portalUiUrl string = portalUi.outputs.url

@description('Portal backend public URL')
output portalUrl string = portal.outputs.url

@description('Finance API internal FQDN')
output financeApiInternalFqdn string = financeApi.outputs.internalFqdn

@description('ACR login server — use this when pushing images')
output acrLoginServer string = acr.outputs.loginServer

@description('Key Vault name — manually add the jwt-secret after first deploy')
output keyVaultName string = keyVault.outputs.name

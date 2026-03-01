// Azure Key Vault
// Stores the JWT signing secret; accessed by the portal via managed identity

@description('Location for all resources')
param location string

@description('Resource name prefix')
param prefix string

@description('Tags to apply to resources')
param tags object

@description('Object ID of the Container Apps managed identity that needs secret read access')
param portalPrincipalId string

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: '${prefix}-kv'
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    // Disable legacy access policies; use RBAC instead
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    enabledForDeployment: false
    enabledForTemplateDeployment: false
    enabledForDiskEncryption: false
    publicNetworkAccess: 'Enabled'
  }
}

// Key Vault Secrets User — allows reading secret values
var secretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

resource secretRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(vault.id, portalPrincipalId, secretsUserRoleId)
  scope: vault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', secretsUserRoleId)
    principalId: portalPrincipalId
    principalType: 'ServicePrincipal'
  }
}

@description('Key Vault URI (e.g. https://my-kv.vault.azure.net/)')
output vaultUri string = vault.properties.vaultUri

@description('Key Vault name')
output name string = vault.name

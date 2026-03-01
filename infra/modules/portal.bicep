// Portal Backend — Container App with external ingress
// Handles auth and API key management; publicly accessible

@description('Location for all resources')
param location string

@description('Resource name prefix')
param prefix string

@description('Tags to apply to resources')
param tags object

@description('Container Apps Environment resource ID')
param environmentId string

@description('ACR login server')
param acrLoginServer string

@description('ACR resource ID (for AcrPull role assignment)')
param acrId string

@description('Docker image tag to deploy')
param imageTag string = 'latest'

@description('PostgreSQL connection string')
@secure()
param dbConnectionString string

@description('Key Vault URI')
param keyVaultUri string

@description('Portal UI origin for CORS (e.g. https://portal-ui.nicecliff-abc123.eastus.azurecontainerapps.io)')
param corsOrigin string

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-07-31-preview' = {
  name: '${prefix}-portal-id'
  location: location
  tags: tags
}

var acrPullRoleId = '7f951dda-4ed3-4680-a7ca-43fe172d538d'

resource acrPullAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acrId, identity.id, acrPullRoleId)
  scope: resourceGroup()
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', acrPullRoleId)
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource app 'Microsoft.App/containerApps@2024-03-01' = {
  name: '${prefix}-portal'
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identity.id}': {}
    }
  }
  properties: {
    environmentId: environmentId
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
        corsPolicy: {
          allowedOrigins: [corsOrigin]
          allowedMethods: ['GET', 'POST', 'DELETE', 'OPTIONS']
          allowedHeaders: ['Authorization', 'Content-Type']
          allowCredentials: false
        }
      }
      registries: [
        {
          server: acrLoginServer
          identity: identity.id
        }
      ]
      secrets: [
        {
          name: 'db-connection-string'
          value: dbConnectionString
        }
        {
          // JWT secret fetched from Key Vault via managed identity at deployment time
          name: 'jwt-secret'
          keyVaultUrl: '${keyVaultUri}secrets/jwt-secret'
          identity: identity.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'portal'
          image: '${acrLoginServer}/portal:${imageTag}'
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            {
              name: 'ConnectionStrings__Default'
              secretRef: 'db-connection-string'
            }
            {
              name: 'JWT__Secret'
              secretRef: 'jwt-secret'
            }
            {
              name: 'JWT__Issuer'
              value: 'FsMathFunctions.Portal'
            }
            {
              name: 'JWT__Audience'
              value: 'FsMathFunctions.Portal'
            }
            {
              name: 'JWT__ExpiryHours'
              value: '24'
            }
            {
              name: 'CORS__Origins'
              value: corsOrigin
            }
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 3
        rules: [
          {
            name: 'http-scaling'
            http: {
              metadata: {
                concurrentRequests: '50'
              }
            }
          }
        ]
      }
    }
  }
}

@description('External URL of the Portal backend')
output url string = 'https://${app.properties.configuration.ingress.fqdn}'

@description('Portal managed identity principal ID (needed for Key Vault role assignment)')
output principalId string = identity.properties.principalId

@description('Portal resource name')
output name string = app.name

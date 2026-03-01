// Finance API — Container App with internal-only ingress
// Only reachable from other apps in the same environment

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

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-07-31-preview' = {
  name: '${prefix}-finance-api-id'
  location: location
  tags: tags
}

// AcrPull role — allows the managed identity to pull images
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
  name: '${prefix}-finance-api'
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
        // Internal ingress — not reachable from the internet
        external: false
        targetPort: 8080
        transport: 'http'
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
      ]
    }
    template: {
      containers: [
        {
          name: 'finance-api'
          image: '${acrLoginServer}/finance-api:${imageTag}'
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

@description('Internal FQDN of the Finance API (accessible within the environment)')
output internalFqdn string = app.properties.configuration.ingress.fqdn

@description('Finance API resource name')
output name string = app.name

// Portal UI — Container App serving the static React bundle via nginx
// Publicly accessible; proxies API calls to the Portal backend

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

@description('Portal backend URL embedded into the React bundle at build time')
param portalApiUrl string

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-07-31-preview' = {
  name: '${prefix}-portal-ui-id'
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
  name: '${prefix}-portal-ui'
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
        targetPort: 80
        transport: 'http'
      }
      registries: [
        {
          server: acrLoginServer
          identity: identity.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'portal-ui'
          // NOTE: The VITE_PORTAL_API_URL arg must be baked in at docker build time.
          // Pass --build-arg VITE_PORTAL_API_URL=<portal-url> when pushing to ACR.
          image: '${acrLoginServer}/portal-ui:${imageTag}'
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            // Runtime hint only — the actual value is embedded in the JS bundle
            {
              name: 'PORTAL_API_URL'
              value: portalApiUrl
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 5
        rules: [
          {
            name: 'http-scaling'
            http: {
              metadata: {
                concurrentRequests: '100'
              }
            }
          }
        ]
      }
    }
  }
}

@description('External URL of the Portal UI')
output url string = 'https://${app.properties.configuration.ingress.fqdn}'

@description('Portal UI resource name')
output name string = app.name

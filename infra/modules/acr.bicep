// Azure Container Registry
// Stores Docker images for all three services

@description('Location for all resources')
param location string

@description('Resource name prefix')
param prefix string

@description('Tags to apply to resources')
param tags object

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: '${replace(prefix, '-', '')}acr'
  location: location
  tags: tags
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: false
    anonymousPullEnabled: false
  }
}

@description('ACR login server (e.g. myacr.azurecr.io)')
output loginServer string = acr.properties.loginServer

@description('ACR resource ID (used for role assignment)')
output id string = acr.id

@description('ACR name')
output name string = acr.name

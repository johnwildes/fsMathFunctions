// Azure Database for PostgreSQL Flexible Server
// Shared between the Finance API and Portal backend

@description('Location for all resources')
param location string

@description('Resource name prefix')
param prefix string

@description('Tags to apply to resources')
param tags object

@description('PostgreSQL administrator username')
param adminUsername string

@description('PostgreSQL administrator password (store in Key Vault in production)')
@secure()
param adminPassword string

@description('Name of the initial database to create')
param databaseName string = 'fsmathdb'

resource server 'Microsoft.DBforPostgreSQL/flexibleServers@2023-06-01-preview' = {
  name: '${prefix}-pg'
  location: location
  tags: tags
  sku: {
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }
  properties: {
    administratorLogin: adminUsername
    administratorLoginPassword: adminPassword
    version: '16'
    storage: {
      storageSizeGB: 32
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: {
      mode: 'Disabled'
    }
    // Allow Azure services to connect (Container Apps uses VNET or public + firewall)
    network: {
      publicNetworkAccess: 'Enabled'
    }
  }
}

// Allow all Azure-internal traffic (0.0.0.0 → 0.0.0.0 is the "Allow Azure services" rule)
resource firewallAzureServices 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-06-01-preview' = {
  parent: server
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource database 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-06-01-preview' = {
  parent: server
  name: databaseName
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

@description('Full PostgreSQL hostname')
output host string = server.properties.fullyQualifiedDomainName

@description('Database name')
output databaseName string = database.name

@description('Admin username')
output adminUsername string = adminUsername

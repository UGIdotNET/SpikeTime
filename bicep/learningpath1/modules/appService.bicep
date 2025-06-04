param location string
param appServiceName string

@allowed([
  'dev'
  'staging'
  'prod'
])
param env string

var appServicePlanName = 'spiketime-toy-asp'

resource appServicePlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'F1'
    capacity: 1
  }
}

resource webApplication 'Microsoft.Web/sites@2024-04-01' = {
  name: appServiceName
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
  }
}

output appServiceAppHostName string = webApplication.properties.defaultHostName

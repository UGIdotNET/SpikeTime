param location string = resourceGroup().location

@minLength(3)
@maxLength(24)
param storageAccountName string = 'toysa${uniqueString(resourceGroup().id)}'

param appServiceName string = 'toy-webapp-${uniqueString(resourceGroup().id)}'

@allowed([
  'dev'
  'staging'
  'prod'
])
param env string

var storageAccountSku = env == 'dev' ? 'Standard_LRS' : 'Premium_LRS'

module storageAccount './modules/storageAccount.bicep' = {
  params: {
    location: location
    sku: storageAccountSku
    storageAccountName: storageAccountName
  }
}
 
module appService './modules/appService.bicep' = {
  params: {
    appServiceName: appServiceName
    location: location
    env: env
  }
}

output storageAccountName string = storageAccount.outputs.storageAccountName
output webApplicationHostName string = appService.outputs.appServiceAppHostName


param storageAccountName string
param location string
param sku string

resource storageaccount 'Microsoft.Storage/storageAccounts@2024-01-01' = {
  name: storageAccountName
  location: location
  kind: 'StorageV2'
  sku: {
    name: sku
  }
  properties: {
    accessTier: 'Hot'
  }
}

output storageAccountName string = storageaccount.name

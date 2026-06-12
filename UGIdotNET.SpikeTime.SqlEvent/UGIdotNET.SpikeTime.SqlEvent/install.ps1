function Generate-SasToken {
    $subscriptionId = ""
    $tenantId = ""
    $resourceGroupName = "spiketime-sqles-rg"
    $namespaceName = "spiketime-es-hub"
    $eventHubName = "sqlserverhub"
    $policyName = "spiketimesqles-policy"
    $tokenExpireInDays = "2"

    # Modifying the rest of the script is not necessary.

    # Login to Azure and set Azure Subscription.
    Connect-AzAccount -TenantId $tenantId

    # Get current context and check subscription
    $currentContext = Get-AzContext
    if ($currentContext.Subscription.Id -ne $subscriptionId) {
        Write-Host "Current subscription is $($currentContext.Subscription.Id), switching to $subscriptionId..."
        Set-AzContext -SubscriptionId $subscriptionId | Out-Null
    } else {
        Write-Host "Already using subscription $subscriptionId."
    }

    # Try to get the authorization policy (it should have Send rights)
    $rights = @("Send")
    $policy = Get-AzEventHubAuthorizationRule -ResourceGroupName $resourceGroupName -NamespaceName $namespaceName -EventHubName $eventHubName -AuthorizationRuleName $policyName -ErrorAction SilentlyContinue

    # If the policy does not exist, create it
    if (-not $policy) {
        Write-Output "Policy '$policyName' does not exist. Creating it now..."

    # Create a new policy with the Manage, Send and Listen rights
        $policy = New-AzEventHubAuthorizationRule -ResourceGroupName $resourceGroupName -NamespaceName $namespaceName -EventHubName $eventHubName -AuthorizationRuleName $policyName -Rights $rights
        if (-not $policy) {
            throw "Error. Policy was not created."
        }
        Write-Output "Policy '$policyName' created successfully."
    } else {
        Write-Output "Policy '$policyName' already exists."
    }

    if ("Send" -in $policy.Rights) {
        Write-Host "Authorization rule has required right: Send."
    } else {
        throw "Authorization rule is missing Send right."
    }

    $keys = Get-AzEventHubKey -ResourceGroupName $resourceGroupName -NamespaceName $namespaceName -EventHubName $eventHubName -AuthorizationRuleName $policyName

    if (-not $keys) {
        throw "Could not obtain Azure Event Hub Key. Script failed and will end now."
    }
    if (-not $keys.PrimaryKey) {
        throw "Could not obtain Primary Key. Script failed and will end now."
    }

    # Get the Primary Key of the Shared Access Policy
    $primaryKey = ($keys.PrimaryKey)
    Write-Host $primaryKey

    ## Check that the primary key is not empty.

    # Define a function to create a SAS token (similar to the C# code provided)
    function Create-SasToken {
        param (
            [string]$resourceUri, [string]$keyName, [string]$key
        )

    $sinceEpoch = [datetime]::UtcNow - [datetime]"1970-01-01"
        $expiry = [int]$sinceEpoch.TotalSeconds + ((60 * 60 * 24) * [int]$tokenExpireInDays) # seconds since Unix epoch
        $stringToSign = [System.Web.HttpUtility]::UrlEncode($resourceUri) + "`n" + $expiry
        $hmac = New-Object System.Security.Cryptography.HMACSHA256
        $hmac.Key = [Text.Encoding]::UTF8.GetBytes($key)
        $signature = [Convert]::ToBase64String($hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($stringToSign)))
        $sasToken = "SharedAccessSignature sr=$([System.Web.HttpUtility]::UrlEncode($resourceUri))&sig=$([System.Web.HttpUtility]::UrlEncode($signature))&se=$expiry&skn=$keyName"
        return $sasToken
    }

    # Construct the resource URI for the SAS token
    $resourceUri = "https://$namespaceName.servicebus.windows.net/$eventHubName"

    # Generate the SAS token using the primary key from the new policy
    $sasToken = Create-SasToken -resourceUri $resourceUri -keyName $policyName -key $primaryKey

    # Output the SAS token
    Write-Output @"
    -- Generated SAS Token --
    $sasToken
    -- End of generated SAS Token --
"@
}

Generate-SasToken
param([string]$Base = 'http://127.0.0.1:5289', [string]$WebhookSecret = 'whsec_local_test')
$ErrorActionPreference = 'Stop'
function Api($Method, $Path, $Body = $null, $Token = $null) {
    $args = @{ Method = $Method; Uri = "$Base$Path"; ContentType = 'application/json' }
    if ($Token) { $args.Headers = @{ Authorization = "Bearer $Token" } }
    if ($null -ne $Body) { $args.Body = ($Body | ConvertTo-Json -Depth 12 -Compress) }
    Invoke-RestMethod @args
}
function Check($Condition, $Message) { if (-not $Condition) { throw $Message } }

$suffix = [guid]::NewGuid().ToString('N').Substring(0, 8)
$user = (Api POST '/api/v1/auth/register' @{ email="stripe-$suffix@example.com"; password='Buyer123!'; fullName='Stripe Test' }).accessToken
$address = Api POST '/api/v1/addresses' @{ recipient='Stripe Test'; phone='0900000000'; line1='1 Test'; ward='Ward'; district='District'; province='HCMC'; isDefault=$true } $user
$product = (Api GET '/api/v1/products').items | Where-Object slug -eq 'rx-78-2-hg' | Select-Object -First 1
$variant = (Api GET "/api/v1/products/$($product.id)/variants").items | Select-Object -First 1
$null = Api POST '/api/v1/cart/items' @{ variantId=$variant.id; quantity=1 } $user
$order = Api POST '/api/v1/orders' @{ addressId=$address.id; voucherCode=$null } $user
$reservedStock = (Api GET "/api/v1/variants/$($variant.id)").stock
$sessionId = "cs_test_local_$suffix"
$sqlResult = & sqlcmd -S '(localdb)\MSSQLLocalDB' -d GundamShopDb -b -I -Q "UPDATE [Payments] SET [ExternalSessionId] = '$sessionId' WHERE [OrderId] = '$($order.id)'" 2>&1
Check ($LASTEXITCODE -eq 0) "Could not prepare local webhook fixture: $sqlResult"

$payload = @{ id="evt_local_$suffix"; type='checkout.session.completed'; data=@{ object=@{ id=$sessionId; payment_status='paid' } } } | ConvertTo-Json -Depth 8 -Compress
$stamp = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
$bytes = [Text.Encoding]::UTF8.GetBytes("$stamp.$payload")
$hash = [Security.Cryptography.HMACSHA256]::HashData([Text.Encoding]::UTF8.GetBytes($WebhookSecret), $bytes)
$signature = [Convert]::ToHexString($hash).ToLowerInvariant()
$headers = @{ 'Stripe-Signature' = "t=$stamp,v1=$signature" }
$first = Invoke-RestMethod -Method Post -Uri "$Base/api/v1/payments/stripe/webhook" -Headers $headers -ContentType 'application/json' -Body $payload
$second = Invoke-RestMethod -Method Post -Uri "$Base/api/v1/payments/stripe/webhook" -Headers $headers -ContentType 'application/json' -Body $payload
Check ($first.processed -and -not $second.processed) 'Webhook idempotency failed'
Check (((Api GET "/api/v1/orders/$($order.id)" $null $user).paymentStatus) -eq 'Paid') 'Webhook did not pay order'
Check (((Api GET "/api/v1/variants/$($variant.id)").stock) -eq $reservedStock) 'Duplicate webhook changed stock'
$bad = (Invoke-WebRequest -Method Post -Uri "$Base/api/v1/payments/stripe/webhook" -Headers @{ 'Stripe-Signature'="t=$stamp,v1=bad" } -ContentType 'application/json' -Body $payload -SkipHttpErrorCheck).StatusCode
Check ($bad -eq 400) 'Invalid webhook signature was accepted'
Write-Output 'PASS: signed Stripe webhook, duplicate event, invalid signature, stock state'

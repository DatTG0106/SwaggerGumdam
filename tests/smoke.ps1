param([string]$Base = 'http://127.0.0.1:5288')
$ErrorActionPreference = 'Stop'
function Api($Method, $Path, $Body = $null, $Token = $null) {
    $args = @{ Method = $Method; Uri = "$Base$Path"; ContentType = 'application/json' }
    if ($Token) { $args.Headers = @{ Authorization = "Bearer $Token" } }
    if ($null -ne $Body) { $args.Body = ($Body | ConvertTo-Json -Depth 12 -Compress) }
    Invoke-RestMethod @args
}
function Check($Condition, $Message) { if (-not $Condition) { throw $Message } }

$suffix = [guid]::NewGuid().ToString('N').Substring(0, 8)
$admin = (Api POST '/api/v1/auth/login' @{ email='admin@gundam.local'; password='Admin123!' }).accessToken
$user = (Api POST '/api/v1/auth/register' @{ email="buyer-$suffix@example.com"; password='Buyer123!'; fullName='Smoke Buyer' }).accessToken
Check $admin 'Admin login failed'
Check $user 'Registration failed'
$withoutToken = (Invoke-WebRequest -Uri "$Base/api/v1/vouchers" -SkipHttpErrorCheck).StatusCode
$customerToken = (Invoke-WebRequest -Uri "$Base/api/v1/vouchers" -Headers @{ Authorization = "Bearer $user" } -SkipHttpErrorCheck).StatusCode
Check ($withoutToken -eq 401 -and $customerToken -eq 403) 'JWT authorization failed'

$address = Api POST '/api/v1/addresses' @{ recipient='Smoke Buyer'; phone='0900000000'; line1='1 Test Street'; ward='Ward 1'; district='District 1'; province='HCMC'; isDefault=$true } $user
Check ((Api GET "/api/v1/addresses/$($address.id)" $null $user).id -eq $address.id) 'Address read failed'
$address2 = Api PUT "/api/v1/addresses/$($address.id)" @{ recipient='Smoke Buyer'; phone='0900000000'; line1='2 Test Street'; ward='Ward 1'; district='District 1'; province='HCMC'; isDefault=$true } $user
Check ($address2.line1 -eq '2 Test Street') 'Address update failed'

$category = Api POST '/api/v1/categories' @{ name="Test $suffix"; slug="test-$suffix"; description='Smoke' } $admin
$category = Api PUT "/api/v1/categories/$($category.id)" @{ name="Updated $suffix"; slug="test-$suffix"; description='Smoke' } $admin
$product = Api POST '/api/v1/products' @{ categoryId=$category.id; name="Smoke Gundam $suffix"; slug="smoke-$suffix"; grade='HG'; scale='1/144'; description='Smoke'; imageUrl=$null } $admin
$product = Api PUT "/api/v1/products/$($product.id)" @{ categoryId=$category.id; name="Updated Gundam $suffix"; slug="smoke-$suffix"; grade='HG'; scale='1/144'; description='Smoke'; imageUrl=$null } $admin
$variant = Api POST '/api/v1/variants' @{ productId=$product.id; sku="SMOKE-$suffix"; label='Standard'; priceVnd=120000; initialStock=5 } $admin
$invalid = (Invoke-WebRequest -Method Post -Uri "$Base/api/v1/variants" -Headers @{ Authorization = "Bearer $admin" } -ContentType 'application/json' -Body (@{ productId=$product.id; sku='BAD'; label='Bad'; priceVnd=-1; initialStock=0 } | ConvertTo-Json) -SkipHttpErrorCheck).StatusCode
Check ($invalid -eq 400) 'Request validation failed'
$variant = Api PUT "/api/v1/variants/$($variant.id)" @{ label='Limited'; priceVnd=125000; isActive=$true } $admin
$variant = Api POST "/api/v1/variants/$($variant.id)/stock-adjustments" @{ delta=2; reason='Smoke restock' } $admin
Check ($variant.stock -eq 7) 'Stock adjustment failed'

$voucher = Api POST '/api/v1/vouchers' @{ code="SALE-$suffix"; discountVnd=10000; minSubtotalVnd=100000; maxUses=10; startsAtUtc=(Get-Date).AddDays(-1).ToUniversalTime().ToString('o'); endsAtUtc=(Get-Date).AddDays(1).ToUniversalTime().ToString('o') } $admin
$voucher = Api PUT "/api/v1/vouchers/$($voucher.id)" @{ code="SALE-$suffix"; discountVnd=15000; minSubtotalVnd=100000; maxUses=10; startsAtUtc=(Get-Date).AddDays(-1).ToUniversalTime().ToString('o'); endsAtUtc=(Get-Date).AddDays(1).ToUniversalTime().ToString('o') } $admin
$cart = Api POST '/api/v1/cart/items' @{ variantId=$variant.id; quantity=1 } $user
$cart = Api PUT "/api/v1/cart/items/$($cart.id)" @{ quantity=2 } $user
Check ($cart.quantity -eq 2) 'Cart update failed'
$order = Api POST '/api/v1/orders' @{ addressId=$address.id; voucherCode="SALE-$suffix" } $user
Check ($order.totalVnd -eq 235000) 'Checkout total failed'
Check ($order.status -eq 'PendingPayment') 'Order not pending'
$null = Api POST "/api/v1/payments/mock/orders/$($order.id)/complete" @{ succeeded=$true } $user
$paid = Api GET "/api/v1/orders/$($order.id)" $null $user
Check ($paid.paymentStatus -eq 'Paid') 'Mock success failed'
$null = Api PUT "/api/v1/orders/$($order.id)/status" @{ status='Shipped' } $admin
$null = Api PUT "/api/v1/orders/$($order.id)/status" @{ status='Delivered' } $admin
$review = Api POST '/api/v1/reviews' @{ productId=$product.id; rating=5; comment='Good kit' } $user
$review = Api PUT "/api/v1/reviews/$($review.id)" @{ rating=4; comment='Updated review' } $user
Check ($review.rating -eq 4) 'Review update failed'

$cart2 = Api POST '/api/v1/cart/items' @{ variantId=$variant.id; quantity=1 } $user
$failedOrder = Api POST '/api/v1/orders' @{ addressId=$address.id; voucherCode=$null } $user
$before = (Api GET "/api/v1/variants/$($variant.id)").stock
$null = Api POST "/api/v1/payments/mock/orders/$($failedOrder.id)/complete" @{ succeeded=$false } $user
$after = (Api GET "/api/v1/variants/$($variant.id)").stock
Check ($after -eq ($before + 1)) 'Stock not restored after payment failure'

$odata = Invoke-RestMethod "$Base/odata/Products?`$filter=Grade eq 'HG'&`$top=5"
Check ($odata.value.Count -ge 1) 'OData failed'
$blockedOData = (Invoke-WebRequest -Uri "$Base/odata/Products?`$expand=Variants" -SkipHttpErrorCheck).StatusCode
Check ($blockedOData -eq 400) 'Unsupported OData option was not blocked'
$null = Api DELETE "/api/v1/reviews/$($review.id)" $null $user
$null = Api DELETE "/api/v1/vouchers/$($voucher.id)" $null $admin
$null = Api DELETE "/api/v1/variants/$($variant.id)" $null $admin
$null = Api DELETE "/api/v1/products/$($product.id)" $null $admin
$null = Api DELETE "/api/v1/categories/$($category.id)" $null $admin
$null = Api DELETE "/api/v1/addresses/$($address.id)" $null $user
Write-Output "PASS: 5 CRUD modules, checkout, voucher, mock payment, stock restore, review, OData"

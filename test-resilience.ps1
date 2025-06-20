# Function to make HTTP requests with timing
function Invoke-TestRequest {
    param (
        [string]$Name,
        [string]$Url
    )
    Write-Host "`n=== Testing $Name ===" -ForegroundColor Cyan
    try {
        $start = Get-Date
        $response = Invoke-WebRequest -Uri $Url -TimeoutSec 20 -ErrorAction Stop
        $duration = ((Get-Date) - $start).TotalSeconds
        Write-Host "Status: $($response.StatusCode) - Duration: $($duration)s" -ForegroundColor Green
        Write-Host "Response: $($response.Content)" -ForegroundColor Gray
    }
    catch {
        $duration = ((Get-Date) - $start).TotalSeconds
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "Duration: $($duration)s" -ForegroundColor Yellow
    }
    Start-Sleep -Seconds 1
}

# Function to test circuit breaker
function Test-CircuitBreaker {
    param (
        [string]$Service
    )
    Write-Host "`n=== Testing Circuit Breaker for $Service ===" -ForegroundColor Magenta
    for ($i = 1; $i -le 7; $i++) {
        Write-Host "`nRequest #$i" -ForegroundColor Yellow
        Invoke-TestRequest -Name "$Service Circuit Breaker" -Url "http://localhost:7000/test/$Service/circuit-breaker"
        Start-Sleep -Seconds 1
    }
}

# Test Health Checks
Write-Host "`n=== Testing Health Checks ===" -ForegroundColor Green
Invoke-TestRequest -Name "Products Health" -Url "http://localhost:7000/products/health"
Invoke-TestRequest -Name "Orders Health" -Url "http://localhost:7000/order/health"

# Test Timeouts
Write-Host "`n=== Testing Timeouts ===" -ForegroundColor Yellow
Invoke-TestRequest -Name "Products Timeout" -Url "http://localhost:7000/test/products/timeout"
Invoke-TestRequest -Name "Orders Timeout" -Url "http://localhost:7000/test/orders/timeout"

# Test Circuit Breakers
Test-CircuitBreaker -Service "products"
Test-CircuitBreaker -Service "orders"

# Reset Circuit Breakers
Write-Host "`n=== Resetting Circuit Breakers ===" -ForegroundColor Cyan
Invoke-TestRequest -Name "Reset Products" -Url "http://localhost:7000/test/products/reset"
Invoke-TestRequest -Name "Reset Orders" -Url "http://localhost:7000/test/orders/reset"

# Test Random Failures (with retry policy)
Write-Host "`n=== Testing Random Failures (with Retry Policy) ===" -ForegroundColor Yellow
for ($i = 1; $i -le 5; $i++) {
    Write-Host "`nTest #$i" -ForegroundColor Cyan
    Invoke-TestRequest -Name "Products Random Failure" -Url "http://localhost:7000/test/products/random-failure"
    Invoke-TestRequest -Name "Orders Random Failure" -Url "http://localhost:7000/test/orders/random-failure"
    Start-Sleep -Seconds 2
}

# Test Slow Responses
Write-Host "`n=== Testing Slow Responses ===" -ForegroundColor Magenta
for ($i = 1; $i -le 3; $i++) {
    Write-Host "`nTest #$i" -ForegroundColor Cyan
    Invoke-TestRequest -Name "Products Slow Response" -Url "http://localhost:7000/test/products/slow-response"
    Invoke-TestRequest -Name "Orders Slow Response" -Url "http://localhost:7000/test/orders/slow-response"
    Start-Sleep -Seconds 2
}

Write-Host "`n=== All Tests Completed ===" -ForegroundColor Green 
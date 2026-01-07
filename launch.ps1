# PowerShell script to launch all microservices
Write-Host "Starting Microservices E-Commerce Platform..." -ForegroundColor Green

$services = @(
    "src\Identity\Identity.API",
    "src\Microservices\Product.API",
    "src\Microservices\Coupon.API",
    "src\Microservices\ShoppingCart.API",
    "src\Microservices\Order.API",
    "src\Microservices\Email.API",
    "src\Microservices\Payment.API",
    "src\Gateway\Gateway.API"
)

foreach ($service in $services) {
    Write-Host "Starting $service..." -ForegroundColor Yellow
    Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$service'; dotnet run"
    Start-Sleep -Seconds 2
}

Write-Host "All services are starting. Check individual windows for status." -ForegroundColor Green

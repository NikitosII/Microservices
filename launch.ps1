# Microservices E-Commerce Platform Launch Script (Windows)
# This script launches all microservices and the frontend dashboard

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Microservices E-Commerce Platform    " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if Docker is running
Write-Host "[1/4] Checking Docker..." -ForegroundColor Yellow
$dockerCheck = docker info 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "X Docker is not running. Please start Docker Desktop first." -ForegroundColor Red
    Write-Host ""
    Write-Host "Steps to start Docker Desktop:" -ForegroundColor Yellow
    Write-Host "  1. Open Docker Desktop application" -ForegroundColor Gray
    Write-Host "  2. Wait for Docker to fully start (system tray icon should be green)" -ForegroundColor Gray
    Write-Host "  3. Run this script again" -ForegroundColor Gray
    Write-Host ""
    exit 1
}
Write-Host "OK Docker is running" -ForegroundColor Green

# Check if docker-compose file exists
if (-Not (Test-Path "docker-compose.yml")) {
    Write-Host "X docker-compose.yml not found!" -ForegroundColor Red
    exit 1
}

# Start microservices with Docker Compose
Write-Host ""
Write-Host "[2/4] Starting microservices..." -ForegroundColor Yellow
Write-Host "This may take a few minutes on first run (downloading images)..." -ForegroundColor Gray
docker-compose up -d

if ($LASTEXITCODE -ne 0) {
    Write-Host "X Failed to start microservices" -ForegroundColor Red
    exit 1
}

Write-Host "OK Microservices containers started" -ForegroundColor Green

# Wait for services to be healthy
Write-Host ""
Write-Host "[3/4] Waiting for services to be ready..." -ForegroundColor Yellow
Write-Host "This may take 30-60 seconds..." -ForegroundColor Gray

$maxAttempts = 60
$attempt = 0
$servicesReady = $false

while ($attempt -lt $maxAttempts -and -not $servicesReady) {
    Start-Sleep -Seconds 2
    $attempt++

    # Check Gateway health
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5000/api/health" -TimeoutSec 2 -UseBasicParsing -ErrorAction SilentlyContinue
        if ($response.StatusCode -eq 200) {
            $servicesReady = $true
            Write-Host "OK Services are ready!" -ForegroundColor Green
        }
    } catch {
        Write-Host "." -NoNewline -ForegroundColor Gray
    }
}

if (-not $servicesReady) {
    Write-Host ""
    Write-Host "! Services are taking longer than expected to start." -ForegroundColor Yellow
    Write-Host "  Continuing anyway... Check the dashboard for status." -ForegroundColor Yellow
}

# Start frontend
Write-Host ""
Write-Host "[4/4] Starting frontend dashboard..." -ForegroundColor Yellow

# Check if node_modules exists
if (-Not (Test-Path "client-app\node_modules")) {
    Write-Host "Installing frontend dependencies..." -ForegroundColor Gray
    Set-Location client-app
    npm install
    Set-Location ..
}

Write-Host "OK Launching frontend..." -ForegroundColor Green
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "     Platform is starting!              " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Dashboard URL: " -NoNewline
Write-Host "http://localhost:55585" -ForegroundColor Green
Write-Host ""
Write-Host "Microservices:" -ForegroundColor Yellow
Write-Host "  - Gateway API:        http://localhost:5000" -ForegroundColor Gray
Write-Host "  - Identity API:       http://localhost:5001" -ForegroundColor Gray
Write-Host "  - Product API:        http://localhost:5002" -ForegroundColor Gray
Write-Host "  - Coupon API:         http://localhost:5003" -ForegroundColor Gray
Write-Host "  - Shopping Cart API:  http://localhost:5004" -ForegroundColor Gray
Write-Host "  - Order API:          http://localhost:5005" -ForegroundColor Gray
Write-Host "  - Payment API:        http://localhost:5007" -ForegroundColor Gray
Write-Host ""
Write-Host "RabbitMQ Management:  http://localhost:15672 (admin/admin)" -ForegroundColor Gray
Write-Host ""
Write-Host "Press Ctrl+C to stop the frontend (microservices will keep running)" -ForegroundColor Yellow
Write-Host "To stop everything: " -NoNewline
Write-Host "docker-compose down" -ForegroundColor Cyan
Write-Host ""

# Start frontend in foreground
Set-Location client-app
npm run dev

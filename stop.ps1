# Stop Microservices Platform (Windows)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Stopping Microservices Platform      " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$choice = Read-Host "Do you want to remove data volumes? (y/N)"

if ($choice -eq "y" -or $choice -eq "Y") {
    Write-Host "Stopping containers and removing volumes..." -ForegroundColor Yellow
    docker-compose down -v
    Write-Host "✓ All containers stopped and data removed" -ForegroundColor Green
} else {
    Write-Host "Stopping containers (keeping data)..." -ForegroundColor Yellow
    docker-compose down
    Write-Host "✓ All containers stopped (data preserved)" -ForegroundColor Green
}

Write-Host ""
Write-Host "Platform stopped successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "To start again, run: " -NoNewline
Write-Host ".\launch.ps1" -ForegroundColor Cyan

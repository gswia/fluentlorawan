# Complete VS Code Removal Script
# Run this as Administrator for best results

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  VS Code Complete Removal Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Stop VS Code processes
Write-Host "[1/6] Stopping VS Code processes..." -ForegroundColor Yellow
Get-Process -Name "Code" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2
Write-Host "      Done!" -ForegroundColor Green

# Remove AppData\Roaming\Code (settings, extensions, etc.)
Write-Host "[2/6] Removing user settings and data..." -ForegroundColor Yellow
$paths = @(
    "$env:APPDATA\Code",
    "$env:APPDATA\Code - Insiders"
)
foreach ($path in $paths) {
    if (Test-Path $path) {
        Remove-Item -Recurse -Force $path -ErrorAction SilentlyContinue
        Write-Host "      Removed: $path" -ForegroundColor Gray
    }
}
Write-Host "      Done!" -ForegroundColor Green

# Remove .vscode folders
Write-Host "[3/6] Removing extensions and cache..." -ForegroundColor Yellow
$vscodePaths = @(
    "$env:USERPROFILE\.vscode",
    "$env:USERPROFILE\.vscode-extensions",
    "$env:USERPROFILE\.vscode-server"
)
foreach ($path in $vscodePaths) {
    if (Test-Path $path) {
        Remove-Item -Recurse -Force $path -ErrorAction SilentlyContinue
        Write-Host "      Removed: $path" -ForegroundColor Gray
    }
}
Write-Host "      Done!" -ForegroundColor Green

# Remove LocalAppData
Write-Host "[4/6] Removing local installation files..." -ForegroundColor Yellow
$localPaths = @(
    "$env:LOCALAPPDATA\Programs\Microsoft VS Code",
    "$env:LOCALAPPDATA\Microsoft\vscode-cpptools"
)
foreach ($path in $localPaths) {
    if (Test-Path $path) {
        Remove-Item -Recurse -Force $path -ErrorAction SilentlyContinue
        Write-Host "      Removed: $path" -ForegroundColor Gray
    }
}
Write-Host "      Done!" -ForegroundColor Green

# Remove temp files
Write-Host "[5/6] Cleaning temp files..." -ForegroundColor Yellow
Get-ChildItem "$env:TEMP\vscode-*" -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "      Done!" -ForegroundColor Green

# Uninstall via winget
Write-Host "[6/6] Attempting to uninstall via winget..." -ForegroundColor Yellow
winget uninstall "Microsoft.VisualStudioCode" --silent 2>$null
Start-Sleep -Seconds 2
Write-Host "      Done!" -ForegroundColor Green

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Cleanup Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "FINAL STEPS (Manual):" -ForegroundColor Cyan
Write-Host "1. Open Settings (Win + I)" -ForegroundColor White
Write-Host "2. Go to Apps > Installed Apps" -ForegroundColor White
Write-Host "3. Search for 'Visual Studio Code'" -ForegroundColor White
Write-Host "4. If found, click ... > Uninstall" -ForegroundColor White
Write-Host ""
Write-Host "After this, VS Code will be completely removed!" -ForegroundColor Green
Write-Host ""

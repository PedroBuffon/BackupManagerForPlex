# Test Release Build Script
# This script simulates the GitHub Actions release process locally

Write-Host "Testing Plex Backup Manager Release Build Process" -ForegroundColor Green

# Clean previous test builds
if (Test-Path "test-release") {
    Remove-Item -Recurse -Force "test-release"
}
New-Item -ItemType Directory -Path "test-release" | Out-Null

# Test build process
Write-Host "Building for win-x64..." -ForegroundColor Yellow
dotnet publish PlexBackup.csproj -r win-x64 -c Release -o "test-release/bin/win-x64" --self-contained false
if ($LASTEXITCODE -eq 0) {
    Write-Host "win-x64 build successful" -ForegroundColor Green
} else {
    Write-Host "win-x64 build failed" -ForegroundColor Red
    exit 1
}

Write-Host "Building for win-x86..." -ForegroundColor Yellow
dotnet publish PlexBackup.csproj -r win-x86 -c Release -o "test-release/bin/win-x86" --self-contained false
if ($LASTEXITCODE -eq 0) {
    Write-Host "win-x86 build successful" -ForegroundColor Green
} else {
    Write-Host "win-x86 build failed" -ForegroundColor Red
    exit 1
}

Write-Host "Building for win-arm64..." -ForegroundColor Yellow
dotnet publish PlexBackup.csproj -r win-arm64 -c Release -o "test-release/bin/win-arm64" --self-contained false
if ($LASTEXITCODE -eq 0) {
    Write-Host "win-arm64 build successful" -ForegroundColor Green
} else {
    Write-Host "win-arm64 build failed" -ForegroundColor Red
    exit 1
}

# Copy additional files
Write-Host "Copying additional files..." -ForegroundColor Yellow
$architectures = @("win-x64", "win-x86", "win-arm64")

foreach ($arch in $architectures) {
    $targetDir = "test-release/bin/$arch"
    
    if (Test-Path "config.json.example") {
        Copy-Item "config.json.example" "$targetDir/" -ErrorAction SilentlyContinue
        Write-Host "  Copied config.json.example to $arch" -ForegroundColor Green
    }
    
    if (Test-Path "README.md") {
        Copy-Item "README.md" "$targetDir/" -ErrorAction SilentlyContinue
        Write-Host "  Copied README.md to $arch" -ForegroundColor Green
    }
    
    if (Test-Path "ROLLBACK.md") {
        Copy-Item "ROLLBACK.md" "$targetDir/" -ErrorAction SilentlyContinue
        Write-Host "  Copied ROLLBACK.md to $arch" -ForegroundColor Green
    }
}

# Create ZIP packages
Write-Host "Creating ZIP packages..." -ForegroundColor Yellow

foreach ($arch in $architectures) {
    $sourceDir = "test-release/bin/$arch"
    $zipFile = "test-release/PlexBackup-$arch.zip"
    
    Compress-Archive -Path "$sourceDir/*" -DestinationPath $zipFile -Force
    
    if (Test-Path $zipFile) {
        $zipSize = [math]::Round((Get-Item $zipFile).Length / 1MB, 2)
        Write-Host "  Created $zipFile ($zipSize MB)" -ForegroundColor Green
    } else {
        Write-Host "  Failed to create $zipFile" -ForegroundColor Red
    }
}

# Summary
Write-Host "`nRelease Build Summary:" -ForegroundColor Cyan
Write-Host "=====================" -ForegroundColor Cyan

if (Test-Path "test-release") {
    Get-ChildItem "test-release" -Recurse | Format-Table Name, Length, LastWriteTime
}

Write-Host "`nRelease build test completed successfully!" -ForegroundColor Green
Write-Host "Files are ready in test-release/ directory" -ForegroundColor Yellow
Write-Host "Clean up with: Remove-Item -Recurse -Force test-release" -ForegroundColor Gray

# Publish Package Script
# Pushes .yak package to Rhino Package Manager (test or production)

param(
    [string]$PackageFile,          # Path to .yak file (auto-detect if not provided)
    [switch]$TestServer,           # Push to test.yak.rhino3d.com (wiped nightly)
    [switch]$Force,                # Skip confirmation prompts
    [string]$YakPath = "$env:TEMP\yak-standalone.exe"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Vessel Studio Package Publishing ===" -ForegroundColor Cyan
Write-Host ""

# ============================================================================
# Step 1: Find Yak CLI
# ============================================================================
Write-Host "Step 1: Locating Yak CLI..." -ForegroundColor Yellow

$yakPaths = @(
    $YakPath,
    "C:\Program Files\Rhino 8\System\Yak.exe",
    "C:\Program Files\Rhino 7\System\Yak.exe"
)

$yakExe = $null
foreach ($path in $yakPaths) {
    if (Test-Path $path) {
        $yakExe = $path
        Write-Host "✓ Found Yak CLI: $yakExe" -ForegroundColor Green
        break
    }
}

if (-not $yakExe) {
    Write-Host "✗ Yak CLI not found" -ForegroundColor Red
    Write-Host ""
    Write-Host "Download standalone Yak CLI:" -ForegroundColor Yellow
    Write-Host "  https://files.mcneel.com/yak/tools/0.13.0/yak.exe" -ForegroundColor White
    Write-Host ""
    Write-Host "Save to: $YakPath" -ForegroundColor Gray
    Write-Host "Or use: -YakPath parameter" -ForegroundColor Gray
    exit 1
}

# ============================================================================
# Step 2: Find Package File
# ============================================================================
Write-Host ""
Write-Host "Step 2: Locating package file..." -ForegroundColor Yellow

if ($PackageFile) {
    if (-not (Test-Path $PackageFile)) {
        Write-Host "✗ Package file not found: $PackageFile" -ForegroundColor Red
        exit 1
    }
    Write-Host "✓ Using specified package: $PackageFile" -ForegroundColor Green
}
else {
    # Auto-detect latest .yak file
    $yakFiles = Get-ChildItem "$PSScriptRoot\VesselStudio-*.yak" -ErrorAction SilentlyContinue
    
    if ($yakFiles.Count -eq 0) {
        Write-Host "✗ No .yak packages found in $PSScriptRoot" -ForegroundColor Red
        Write-Host "  Run: .\create-package.ps1" -ForegroundColor Yellow
        exit 1
    }
    
    if ($yakFiles.Count -gt 1) {
        # Use most recent
        $PackageFile = ($yakFiles | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName
        Write-Host "✓ Found multiple packages, using latest: $(Split-Path $PackageFile -Leaf)" -ForegroundColor Green
    }
    else {
        $PackageFile = $yakFiles[0].FullName
        Write-Host "✓ Found package: $(Split-Path $PackageFile -Leaf)" -ForegroundColor Green
    }
}

$packageSize = (Get-Item $PackageFile).Length
Write-Host "  Size: $([math]::Round($packageSize/1KB, 2)) KB" -ForegroundColor Gray

# Extract version from filename
$packageName = Split-Path $PackageFile -Leaf
if ($packageName -match 'VesselStudio-(\d+\.\d+\.\d+)-rh8_0-win\.yak') {
    $packageVersion = $matches[1]
    Write-Host "  Version: $packageVersion" -ForegroundColor Gray
}
else {
    Write-Host "⚠ Could not parse version from filename" -ForegroundColor Yellow
}

# ============================================================================
# Step 3: Check Authentication
# ============================================================================
Write-Host ""
Write-Host "Step 3: Checking authentication..." -ForegroundColor Yellow

$tokenPath = "$env:APPDATA\McNeel\yak.yml"
if (-not (Test-Path $tokenPath)) {
    Write-Host "✗ Not authenticated with Yak" -ForegroundColor Red
    Write-Host ""
    Write-Host "Run authentication:" -ForegroundColor Yellow
    Write-Host "  & `"$yakExe`" login" -ForegroundColor White
    Write-Host ""
    Write-Host "This will open a browser to log in with Rhino Accounts" -ForegroundColor Gray
    exit 1
}

# Check token age
$tokenAge = (Get-Date) - (Get-Item $tokenPath).LastWriteTime
if ($tokenAge.TotalDays -gt 25) {
    Write-Host "⚠ Token is $([math]::Round($tokenAge.TotalDays)) days old (expires ~30 days)" -ForegroundColor Yellow
    Write-Host "  Consider re-authenticating soon: & `"$yakExe`" login" -ForegroundColor Gray
}
else {
    Write-Host "✓ Authenticated ($([math]::Round($tokenAge.TotalDays)) days ago)" -ForegroundColor Green
}

# ============================================================================
# Step 4: Confirm Publishing
# ============================================================================
Write-Host ""
Write-Host "Step 4: Publishing confirmation" -ForegroundColor Yellow

if ($TestServer) {
    $targetServer = "test.yak.rhino3d.com"
    $serverColor = "Yellow"
    Write-Host "  Target: TEST SERVER (wiped nightly)" -ForegroundColor $serverColor
}
else {
    $targetServer = "yak.rhino3d.com"
    $serverColor = "Red"
    Write-Host "  Target: PRODUCTION SERVER" -ForegroundColor $serverColor
}

Write-Host "  Package: $(Split-Path $PackageFile -Leaf)" -ForegroundColor Gray
Write-Host ""

if (-not $Force) {
    if ($TestServer) {
        $response = Read-Host "Push to TEST server? (Y/n)"
    }
    else {
        Write-Host "⚠ THIS WILL PUBLISH TO PRODUCTION!" -ForegroundColor Red
        Write-Host "  All Rhino 8 users worldwide will see this package" -ForegroundColor Yellow
        Write-Host ""
        $response = Read-Host "Are you sure you want to publish to PRODUCTION? (yes/N)"
        if ($response -ne 'yes') {
            Write-Host "Publish cancelled (type 'yes' to confirm production publish)" -ForegroundColor Yellow
            exit 0
        }
    }
    
    if ($response -eq 'n' -or $response -eq 'N') {
        Write-Host "Publish cancelled" -ForegroundColor Yellow
        exit 0
    }
}

# ============================================================================
# Step 5: Push Package
# ============================================================================
Write-Host ""
Write-Host "Step 5: Pushing package to $targetServer..." -ForegroundColor Yellow

if ($TestServer) {
    $pushCommand = "& `"$yakExe`" push `"$PackageFile`" --source https://test.yak.rhino3d.com"
}
else {
    $pushCommand = "& `"$yakExe`" push `"$PackageFile`""
}

Write-Host "  Command: $pushCommand" -ForegroundColor Gray
Write-Host ""

try {
    if ($TestServer) {
        $output = & "$yakExe" push "$PackageFile" --source "https://test.yak.rhino3d.com" 2>&1
    }
    else {
        $output = & "$yakExe" push "$PackageFile" 2>&1
    }
    
    $output | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "✓ Package pushed successfully" -ForegroundColor Green
    }
    else {
        Write-Host ""
        Write-Host "✗ Push failed with exit code $LASTEXITCODE" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host ""
    Write-Host "✗ Push failed: $_" -ForegroundColor Red
    exit 1
}

# ============================================================================
# Step 6: Verify Package is Searchable
# ============================================================================
Write-Host ""
Write-Host "Step 6: Verifying package is searchable..." -ForegroundColor Yellow

Start-Sleep -Seconds 2

try {
    $searchOutput = & "$yakExe" search "VesselStudio" 2>&1
    
    if ($searchOutput -match "VesselStudio") {
        Write-Host "✓ Package found in search results" -ForegroundColor Green
        Write-Host ""
        $searchOutput | Where-Object { $_ -match "VesselStudio" } | ForEach-Object {
            Write-Host "  $_" -ForegroundColor Cyan
        }
    }
    else {
        Write-Host "⚠ Package not found in search yet (may take a few seconds)" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "⚠ Could not verify search (package may still be indexed)" -ForegroundColor Yellow
}

# ============================================================================
# Summary
# ============================================================================
Write-Host ""
Write-Host "=== Publishing Complete ===" -ForegroundColor Cyan
Write-Host ""

if ($TestServer) {
    Write-Host "Published to TEST server: https://test.yak.rhino3d.com" -ForegroundColor Yellow
    Write-Host "⚠ This server is wiped nightly - for testing only" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "To publish to production:" -ForegroundColor Gray
    Write-Host "  .\publish-package.ps1" -ForegroundColor White
}
else {
    Write-Host "Published to PRODUCTION: https://yak.rhino3d.com" -ForegroundColor Green
    Write-Host ""
    Write-Host "Package details:" -ForegroundColor Gray
    Write-Host "  Name: VesselStudio" -ForegroundColor White
    if ($packageVersion) {
        Write-Host "  Version: $packageVersion" -ForegroundColor White
    }
    Write-Host "  Search: https://www.rhino3d.com/packages/search/?q=vesselstudio" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Users can now install via:" -ForegroundColor Gray
    Write-Host "  - Rhino 8 → Tools → Options → Package Manager → Search: VesselStudio" -ForegroundColor White
    Write-Host "  - Command line: _PackageManager" -ForegroundColor White
    Write-Host ""
    Write-Host "To yank this version (if needed):" -ForegroundColor Gray
    if ($packageVersion) {
        Write-Host "  & `"$yakExe`" yank VesselStudio $packageVersion" -ForegroundColor White
    }
    else {
        Write-Host "  & `"$yakExe`" yank VesselStudio <version>" -ForegroundColor White
    }
}

Write-Host ""

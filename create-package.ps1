# Create Package Script
# Prepares dist/ folder, copies files, and creates .yak package

param(
    [string]$Version,              # Version to package (defaults to AssemblyInfo.cs)
    [switch]$SkipBuild,            # Skip build step (use existing binaries)
    [string]$Configuration = "Release",
    [string]$OutputDir = "$PSScriptRoot\dist",
    [string]$BinDir = "$PSScriptRoot\VesselStudioSimplePlugin\bin\$Configuration\net48",
    [string]$ResourcesDir = "$PSScriptRoot\VesselStudioSimplePlugin\Resources",
    [string]$AssemblyInfoPath = "$PSScriptRoot\VesselStudioSimplePlugin\Properties\AssemblyInfo.cs",
    [string]$ManifestTemplate = "$PSScriptRoot\specs\002-rhino-package-manager\contracts\manifest-template.yml"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Vessel Studio Package Creation ===" -ForegroundColor Cyan
Write-Host ""

# ============================================================================
# Step 1: Determine Version
# ============================================================================
Write-Host "Step 1: Determining version..." -ForegroundColor Yellow

if ($Version) {
    Write-Host "✓ Using explicit version: $Version" -ForegroundColor Green
}
else {
    # Read from AssemblyInfo.cs
    if (-not (Test-Path $AssemblyInfoPath)) {
        Write-Host "✗ AssemblyInfo.cs not found at: $AssemblyInfoPath" -ForegroundColor Red
        exit 1
    }
    
    $assemblyContent = Get-Content $AssemblyInfoPath -Raw
    if ($assemblyContent -match '\[assembly: AssemblyVersion\("(\d+)\.(\d+)\.(\d+)\.(\d+)"\)\]') {
        $Version = "$($matches[1]).$($matches[2]).$($matches[3])"
        Write-Host "✓ Read version from AssemblyInfo.cs: $Version" -ForegroundColor Green
    }
    else {
        Write-Host "✗ Could not parse version from AssemblyInfo.cs" -ForegroundColor Red
        exit 1
    }
}

# ============================================================================
# Step 2: Build Plugin (if not skipped)
# ============================================================================
if (-not $SkipBuild) {
    Write-Host ""
    Write-Host "Step 2: Building plugin..." -ForegroundColor Yellow
    
    if (-not (Test-Path "$PSScriptRoot\build.ps1")) {
        Write-Host "✗ build.ps1 not found" -ForegroundColor Red
        exit 1
    }
    
    $buildResult = & "$PSScriptRoot\build.ps1" -Configuration $Configuration -SkipChangelog
    if ($LASTEXITCODE -ne 0) {
        Write-Host "✗ Build failed" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "✓ Build completed successfully" -ForegroundColor Green
}
else {
    Write-Host ""
    Write-Host "Step 2: Skipping build (using existing binaries)" -ForegroundColor Gray
}

# ============================================================================
# Step 3: Prepare dist/ Folder
# ============================================================================
Write-Host ""
Write-Host "Step 3: Preparing dist/ folder..." -ForegroundColor Yellow

# Create or clean dist/ folder
if (Test-Path $OutputDir) {
    Write-Host "  Cleaning existing dist/ folder..." -ForegroundColor Gray
    Remove-Item "$OutputDir\*" -Force -Recurse -ErrorAction SilentlyContinue
}
else {
    Write-Host "  Creating dist/ folder..." -ForegroundColor Gray
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
}

Write-Host "✓ dist/ folder ready" -ForegroundColor Green

# ============================================================================
# Step 4: Copy Required Files
# ============================================================================
Write-Host ""
Write-Host "Step 4: Copying files to dist/..." -ForegroundColor Yellow

# 4.1: Copy .rhp file
$rhpFile = "$BinDir\VesselStudioSimplePlugin.rhp"
if (-not (Test-Path $rhpFile)) {
    Write-Host "✗ VesselStudioSimplePlugin.rhp not found at: $rhpFile" -ForegroundColor Red
    Write-Host "  Run build.ps1 first or use -Configuration to specify build type" -ForegroundColor Yellow
    exit 1
}

Copy-Item $rhpFile $OutputDir
$rhpSize = (Get-Item "$OutputDir\VesselStudioSimplePlugin.rhp").Length
Write-Host "  ✓ VesselStudioSimplePlugin.rhp ($([math]::Round($rhpSize/1KB, 2)) KB)" -ForegroundColor Green

# 4.2: Copy Newtonsoft.Json.dll
$jsonDll = "$BinDir\Newtonsoft.Json.dll"
if (-not (Test-Path $jsonDll)) {
    Write-Host "✗ Newtonsoft.Json.dll not found at: $jsonDll" -ForegroundColor Red
    exit 1
}

Copy-Item $jsonDll $OutputDir
$jsonSize = (Get-Item "$OutputDir\Newtonsoft.Json.dll").Length
Write-Host "  ✓ Newtonsoft.Json.dll ($([math]::Round($jsonSize/1KB, 2)) KB)" -ForegroundColor Green

# 4.3: Copy icon
$iconSource = "$ResourcesDir\icon_48.png"
if (-not (Test-Path $iconSource)) {
    Write-Host "⚠ icon_48.png not found at: $iconSource" -ForegroundColor Yellow
    Write-Host "  Package will be created without icon" -ForegroundColor Yellow
}
else {
    Copy-Item $iconSource "$OutputDir\icon.png"
    $iconSize = (Get-Item "$OutputDir\icon.png").Length
    Write-Host "  ✓ icon.png ($iconSize bytes)" -ForegroundColor Green
}

# ============================================================================
# Step 5: Create manifest.yml
# ============================================================================
Write-Host ""
Write-Host "Step 5: Creating manifest.yml..." -ForegroundColor Yellow

$manifestContent = @"
name: VesselStudio
version: $Version
authors:
- Creata Collective Limited
description: >
  Send design views straight from your Rhino viewport into your Vessel Studio project.
  Visualise designs in a fraction of a second, make amendments and refine.
url: https://vesselstudio.io
icon: icon.png
keywords:
- vessel
- studio
- yacht
- marine
- design
- capture
- viewport
- screenshot
- upload
- visualization
- guid:A1B2C3D4-E5F6-7A8B-9C0D-1E2F3A4B5C6D
"@

Set-Content "$OutputDir\manifest.yml" -Value $manifestContent
Write-Host "✓ manifest.yml created with version $Version" -ForegroundColor Green

# ============================================================================
# Step 6: Create .yak Package
# ============================================================================
Write-Host ""
Write-Host "Step 6: Creating .yak package..." -ForegroundColor Yellow

$packageName = "VesselStudio-$Version-rh8_0-win.yak"
$packagePath = "$PSScriptRoot\$packageName"

# Remove existing package if it exists
if (Test-Path $packagePath) {
    Write-Host "  Removing existing package..." -ForegroundColor Gray
    Remove-Item $packagePath -Force
}

# Create package as ZIP archive
try {
    Compress-Archive -Path "$OutputDir\*" -DestinationPath $packagePath -Force
    $packageSize = (Get-Item $packagePath).Length
    Write-Host "✓ Created $packageName ($([math]::Round($packageSize/1KB, 2)) KB)" -ForegroundColor Green
}
catch {
    Write-Host "✗ Failed to create package: $_" -ForegroundColor Red
    exit 1
}

# ============================================================================
# Step 7: Verify Package Contents
# ============================================================================
Write-Host ""
Write-Host "Step 7: Verifying package contents..." -ForegroundColor Yellow

$tempExtractDir = "$env:TEMP\vessel-package-verify"
if (Test-Path $tempExtractDir) {
    Remove-Item $tempExtractDir -Recurse -Force
}

Expand-Archive -Path $packagePath -DestinationPath $tempExtractDir

$requiredFiles = @(
    "VesselStudioSimplePlugin.rhp",
    "Newtonsoft.Json.dll",
    "manifest.yml"
)

$allPresent = $true
foreach ($file in $requiredFiles) {
    if (Test-Path "$tempExtractDir\$file") {
        Write-Host "  ✓ $file" -ForegroundColor Green
    }
    else {
        Write-Host "  ✗ $file MISSING" -ForegroundColor Red
        $allPresent = $false
    }
}

if (Test-Path "$tempExtractDir\icon.png") {
    Write-Host "  ✓ icon.png (optional)" -ForegroundColor Green
}

# Cleanup
Remove-Item $tempExtractDir -Recurse -Force

if (-not $allPresent) {
    Write-Host ""
    Write-Host "✗ Package verification failed - missing required files" -ForegroundColor Red
    exit 1
}

# ============================================================================
# Summary
# ============================================================================
Write-Host ""
Write-Host "=== Package Creation Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Package: $packageName" -ForegroundColor Green
Write-Host "Location: $packagePath" -ForegroundColor Gray
Write-Host "Version: $Version" -ForegroundColor Gray
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  Test server: .\publish-package.ps1 -TestServer" -ForegroundColor White
Write-Host "  Production:  .\publish-package.ps1" -ForegroundColor White
Write-Host ""

# Vessel Studio Plugin Build Script
# Integrates changelog analysis and version management into the build process

param(
    [string]$Configuration = "Release",
    [switch]$SkipChangelog,
    [switch]$SkipVersionCheck,
    [switch]$Clean,
    [string]$Since = "HEAD~10"
)

$ErrorActionPreference = "Stop"
$scriptRoot = $PSScriptRoot

Write-Host "=== Vessel Studio Plugin Build ===" -ForegroundColor Cyan
Write-Host ""

# ============================================================================
# Step 1: Changelog Analysis (if not skipped)
# ============================================================================
if (-not $SkipChangelog) {
    Write-Host "Step 1: Analyzing changes for changelog..." -ForegroundColor Yellow
    
    if (Test-Path "$scriptRoot\update-changelog.ps1") {
        try {
            & "$scriptRoot\update-changelog.ps1" -Suggest -Since $Since
            Write-Host ""
            Write-Host "Changelog analysis complete. Review suggestions above." -ForegroundColor Green
            Write-Host ""
            
            # Pause for review
            $response = Read-Host "Continue with build? (Y/n)"
            if ($response -eq 'n' -or $response -eq 'N') {
                Write-Host "Build cancelled by user." -ForegroundColor Red
                exit 1
            }
        }
        catch {
            Write-Host "Warning: Changelog analysis failed: $_" -ForegroundColor Yellow
            Write-Host "Continuing with build..." -ForegroundColor Yellow
        }
    }
    else {
        Write-Host "update-changelog.ps1 not found, skipping changelog analysis" -ForegroundColor Yellow
    }
}
else {
    Write-Host "Step 1: Skipping changelog analysis (--SkipChangelog)" -ForegroundColor Gray
}

Write-Host ""

# ============================================================================
# Step 2: Version Check
# ============================================================================
if (-not $SkipVersionCheck) {
    Write-Host "Step 2: Checking version information..." -ForegroundColor Yellow
    
    $versionFile = "$scriptRoot\VesselStudioPlugin\Models\PluginVersion.cs"
    if (Test-Path $versionFile) {
        $content = Get-Content $versionFile -Raw
        
        # Extract version numbers
        if ($content -match 'public const int Major = (\d+);') { $major = $Matches[1] }
        if ($content -match 'public const int Minor = (\d+);') { $minor = $Matches[1] }
        if ($content -match 'public const int Patch = (\d+);') { $patch = $Matches[1] }
        
        Write-Host "  Current Version: $major.$minor.$patch" -ForegroundColor Cyan
        
        # Check CHANGELOG.md for matching version
        $changelogFile = "$scriptRoot\CHANGELOG.md"
        if (Test-Path $changelogFile) {
            $changelog = Get-Content $changelogFile -Raw
            if ($changelog -match "## \[($major\.$minor\.$patch)\]") {
                Write-Host "  ✓ Version documented in CHANGELOG.md" -ForegroundColor Green
            }
            else {
                Write-Host "  ⚠ Version $major.$minor.$patch not found in CHANGELOG.md" -ForegroundColor Yellow
                Write-Host "    Consider updating the changelog before building" -ForegroundColor Yellow
            }
        }
    }
    else {
        Write-Host "  Warning: PluginVersion.cs not found" -ForegroundColor Yellow
    }
}
else {
    Write-Host "Step 2: Skipping version check (--SkipVersionCheck)" -ForegroundColor Gray
}

Write-Host ""

# ============================================================================
# Step 3: Clean (if requested)
# ============================================================================
if ($Clean) {
    Write-Host "Step 3: Cleaning previous build..." -ForegroundColor Yellow
    
    $binDirs = Get-ChildItem -Path $scriptRoot -Recurse -Directory -Filter "bin" | Where-Object { $_.FullName -notlike "*\.specify*" }
    $objDirs = Get-ChildItem -Path $scriptRoot -Recurse -Directory -Filter "obj" | Where-Object { $_.FullName -notlike "*\.specify*" }
    
    $binDirs + $objDirs | ForEach-Object {
        Write-Host "  Removing $($_.FullName)" -ForegroundColor Gray
        Remove-Item $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
    }
    
    Write-Host "  ✓ Clean complete" -ForegroundColor Green
}
else {
    Write-Host "Step 3: Skipping clean (use -Clean to clean before build)" -ForegroundColor Gray
}

Write-Host ""

# ============================================================================
# Step 4: Build Solution
# ============================================================================
Write-Host "Step 4: Building solution..." -ForegroundColor Yellow

$solutionFile = "$scriptRoot\VesselStudioPlugin.sln"

if (-not (Test-Path $solutionFile)) {
    Write-Host "Error: Solution file not found at $solutionFile" -ForegroundColor Red
    exit 1
}

Write-Host "  Configuration: $Configuration" -ForegroundColor Cyan
Write-Host "  Solution: VesselStudioPlugin.sln" -ForegroundColor Cyan
Write-Host ""

$buildArgs = @(
    "build",
    $solutionFile,
    "--configuration", $Configuration,
    "--no-incremental"
)

if ($Clean) {
    $buildArgs += "--no-restore"
}

try {
    & dotnet @buildArgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "Build failed with exit code $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }
    
    Write-Host ""
    Write-Host "✓ Build completed successfully!" -ForegroundColor Green
}
catch {
    Write-Host ""
    Write-Host "Build failed: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# ============================================================================
# Step 5: Build Summary
# ============================================================================
Write-Host "=== Build Summary ===" -ForegroundColor Cyan
Write-Host ""

# Find generated .rhp files
$rhpFiles = Get-ChildItem -Path "$scriptRoot\VesselStudioPlugin\bin\$Configuration" -Filter "*.rhp" -Recurse -ErrorAction SilentlyContinue

if ($rhpFiles) {
    Write-Host "Plugin files generated:" -ForegroundColor Green
    foreach ($rhp in $rhpFiles) {
        $size = [math]::Round($rhp.Length / 1KB, 2)
        $framework = $rhp.Directory.Name
        Write-Host "  • $framework\$($rhp.Name) ($size KB)" -ForegroundColor Cyan
    }
    
    Write-Host ""
    Write-Host "Installation:" -ForegroundColor Yellow
    Write-Host "  1. Close Rhino" -ForegroundColor Gray
    Write-Host "  2. Copy .rhp file to Rhino plugin directory or drag-drop into Rhino" -ForegroundColor Gray
    Write-Host "  3. Start Rhino and test commands:" -ForegroundColor Gray
    Write-Host "     - VesselAbout" -ForegroundColor Gray
    Write-Host "     - VesselSetApiKey" -ForegroundColor Gray
    Write-Host "     - VesselStatus" -ForegroundColor Gray
    Write-Host "     - VesselCapture" -ForegroundColor Gray
}
else {
    Write-Host "Warning: No .rhp files found in build output" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Build completed at $(Get-Date -Format 'HH:mm:ss')" -ForegroundColor Cyan
Write-Host ""

# ============================================================================
# Quick reload option for development
# ============================================================================
if (Test-Path "$scriptRoot\reload-plugin.ps1") {
    $reload = Read-Host "Reload plugin in Rhino? (y/N)"
    if ($reload -eq 'y' -or $reload -eq 'Y') {
        Write-Host ""
        & "$scriptRoot\reload-plugin.ps1"
    }
}

# Release Script - Complete Automation
# Orchestrates the entire release workflow from version detection to publishing

param(
    [string]$Version,              # Explicit version (e.g., "1.2.0") - overrides auto-detection
    [switch]$TestServer,           # Publish to test server instead of production
    [switch]$SkipVersionUpdate,    # Skip version update (use current version)
    [switch]$SkipBuild,            # Skip build step (use existing binaries)
    [switch]$SkipPackage,          # Skip package creation (use existing .yak)
    [switch]$SkipPublish,          # Skip publishing (create package only)
    [switch]$DryRun,               # Show what would happen without making changes
    [switch]$Force,                # Skip all confirmation prompts
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

# Timing
$startTime = Get-Date

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                                                                ║" -ForegroundColor Cyan
Write-Host "║        VESSEL STUDIO RHINO PLUGIN - AUTOMATED RELEASE         ║" -ForegroundColor Cyan
Write-Host "║                                                                ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

if ($DryRun) {
    Write-Host "🔍 DRY RUN MODE - No changes will be made" -ForegroundColor Magenta
    Write-Host ""
}

# ============================================================================
# PHASE 1: Version Management
# ============================================================================
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "PHASE 1: VERSION MANAGEMENT" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

if ($SkipVersionUpdate) {
    Write-Host "⊘ Skipping version update (using current version)" -ForegroundColor Gray
    
    # Read current version
    $assemblyInfoPath = "$PSScriptRoot\VesselStudioSimplePlugin\Properties\AssemblyInfo.cs"
    $assemblyContent = Get-Content $assemblyInfoPath -Raw
    if ($assemblyContent -match '\[assembly: AssemblyVersion\("(\d+)\.(\d+)\.(\d+)\.(\d+)"\)\]') {
        $Version = "$($matches[1]).$($matches[2]).$($matches[3])"
        Write-Host "  Current version: $Version" -ForegroundColor White
    }
    else {
        Write-Host "✗ Could not read current version from AssemblyInfo.cs" -ForegroundColor Red
        exit 1
    }
}
else {
    Write-Host "→ Running update-version.ps1..." -ForegroundColor Yellow
    Write-Host ""
    
    $updateArgs = @{}
    if ($Version) { $updateArgs['NewVersion'] = $Version }
    if ($DryRun) { $updateArgs['DryRun'] = $true }
    if ($Force) { $updateArgs['Force'] = $true }
    
    & "$PSScriptRoot\update-version.ps1" @updateArgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "✗ Version update failed" -ForegroundColor Red
        exit 1
    }
    
    if ($DryRun) {
        Write-Host ""
        Write-Host "Dry run complete - no changes made" -ForegroundColor Magenta
        exit 0
    }
    
    # Read the updated version
    $assemblyInfoPath = "$PSScriptRoot\VesselStudioSimplePlugin\Properties\AssemblyInfo.cs"
    $assemblyContent = Get-Content $assemblyInfoPath -Raw
    if ($assemblyContent -match '\[assembly: AssemblyVersion\("(\d+)\.(\d+)\.(\d+)\.(\d+)"\)\]') {
        $Version = "$($matches[1]).$($matches[2]).$($matches[3])"
    }
}

Write-Host ""
Write-Host "✓ Phase 1 Complete - Version: $Version" -ForegroundColor Green
Write-Host ""

# ============================================================================
# PHASE 2: Build
# ============================================================================
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "PHASE 2: BUILD" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

if ($SkipBuild) {
    Write-Host "⊘ Skipping build (using existing binaries)" -ForegroundColor Gray
}
else {
    Write-Host "→ Running build.ps1..." -ForegroundColor Yellow
    Write-Host ""
    
    & "$PSScriptRoot\build.ps1" -Configuration $Configuration -SkipChangelog
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "✗ Build failed" -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "✓ Phase 2 Complete - Build successful" -ForegroundColor Green
Write-Host ""

# ============================================================================
# PHASE 3: Package Creation
# ============================================================================
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "PHASE 3: PACKAGE CREATION" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

if ($SkipPackage) {
    Write-Host "⊘ Skipping package creation (using existing .yak)" -ForegroundColor Gray
}
else {
    Write-Host "→ Running create-package.ps1..." -ForegroundColor Yellow
    Write-Host ""
    
    & "$PSScriptRoot\create-package.ps1" -Version $Version -SkipBuild -Configuration $Configuration
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "✗ Package creation failed" -ForegroundColor Red
        exit 1
    }
}

$packageFile = "$PSScriptRoot\VesselStudio-$Version-rh8_0-win.yak"
if (-not (Test-Path $packageFile)) {
    Write-Host ""
    Write-Host "✗ Package file not found: $packageFile" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "✓ Phase 3 Complete - Package created" -ForegroundColor Green
Write-Host ""

# ============================================================================
# PHASE 4: Publishing
# ============================================================================
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "PHASE 4: PUBLISHING" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

if ($SkipPublish) {
    Write-Host "⊘ Skipping publishing (package created only)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "To publish manually:" -ForegroundColor Yellow
    Write-Host "  Test:       .\publish-package.ps1 -TestServer" -ForegroundColor White
    Write-Host "  Production: .\publish-package.ps1" -ForegroundColor White
}
else {
    Write-Host "→ Running publish-package.ps1..." -ForegroundColor Yellow
    Write-Host ""
    
    $publishArgs = @{}
    $publishArgs['PackageFile'] = $packageFile
    $publishArgs['Force'] = $true  # Always force - user already consented by running release.ps1
    if ($TestServer) { $publishArgs['TestServer'] = $true }
    
    & "$PSScriptRoot\publish-package.ps1" @publishArgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "✗ Publishing failed" -ForegroundColor Red
        Write-Host ""
        Write-Host "Package was created successfully but not published." -ForegroundColor Yellow
        Write-Host "You can retry publishing:" -ForegroundColor Yellow
        if ($TestServer) {
            Write-Host "  .\publish-package.ps1 -PackageFile `"$packageFile`" -TestServer" -ForegroundColor White
        }
        else {
            Write-Host "  .\publish-package.ps1 -PackageFile `"$packageFile`"" -ForegroundColor White
        }
        exit 1
    }
}

Write-Host ""
Write-Host "✓ Phase 4 Complete - Publishing successful" -ForegroundColor Green
Write-Host ""

# ============================================================================
# COMPLETION SUMMARY
# ============================================================================
$endTime = Get-Date
$duration = $endTime - $startTime

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║                                                                ║" -ForegroundColor Green
Write-Host "║                    RELEASE COMPLETE ✓                          ║" -ForegroundColor Green
Write-Host "║                                                                ║" -ForegroundColor Green
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""
Write-Host "Release Details:" -ForegroundColor Cyan
Write-Host "  Version:  $Version" -ForegroundColor White
Write-Host "  Package:  VesselStudio-$Version-rh8_0-win.yak" -ForegroundColor White
Write-Host "  Target:   $(if ($TestServer) { 'TEST SERVER' } else { 'PRODUCTION' })" -ForegroundColor $(if ($TestServer) { 'Yellow' } else { 'Green' })
Write-Host "  Duration: $($duration.ToString('mm\:ss'))" -ForegroundColor White
Write-Host ""

if (-not $SkipPublish) {
    if ($TestServer) {
        Write-Host "Test Server:" -ForegroundColor Yellow
        Write-Host "  URL: https://test.yak.rhino3d.com" -ForegroundColor White
        Write-Host "  ⚠ Server is wiped nightly - for testing only" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "To publish to production:" -ForegroundColor Gray
        Write-Host "  .\release.ps1 -Version $Version -SkipVersionUpdate -SkipBuild -SkipPackage" -ForegroundColor White
    }
    else {
        Write-Host "Production Server:" -ForegroundColor Green
        Write-Host "  URL: https://yak.rhino3d.com" -ForegroundColor White
        Write-Host "  Search: https://www.rhino3d.com/packages/search/?q=vesselstudio" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "Users can install via:" -ForegroundColor Gray
        Write-Host "  Rhino 8 → Tools → Options → Package Manager → Search: VesselStudio" -ForegroundColor White
        Write-Host ""
        Write-Host "To yank this version if needed:" -ForegroundColor Gray
        Write-Host "  & `"$env:TEMP\yak-standalone.exe`" yank VesselStudio $Version" -ForegroundColor White
    }
}

Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Git commit: git add -A && git commit -m `"chore: Release v$Version`"" -ForegroundColor White
Write-Host "  2. Git tag:    git tag -a v$Version -m `"Release v$Version`"" -ForegroundColor White
Write-Host "  3. Git push:   git push && git push --tags" -ForegroundColor White
Write-Host ""

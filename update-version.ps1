# Update Version Script
# Analyzes CHANGELOG.md [Unreleased] section and determines next semantic version
# Updates version across all project files

param(
    [string]$NewVersion,           # Explicit version (e.g., "1.2.0") - overrides auto-detection
    [switch]$DryRun,               # Show what would be updated without making changes
    [switch]$Force,                # Skip confirmation prompts
    [string]$ChangelogPath = "$PSScriptRoot\docs\reference\CHANGELOG.md",
    [string]$AssemblyInfoPath = "$PSScriptRoot\VesselStudioSimplePlugin\Properties\AssemblyInfo.cs",
    [string]$CsprojPath = "$PSScriptRoot\VesselStudioSimplePlugin\VesselStudioSimplePlugin.csproj"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Vessel Studio Version Update ===" -ForegroundColor Cyan
Write-Host ""

# ============================================================================
# Step 1: Read Current Version
# ============================================================================
Write-Host "Step 1: Reading current version..." -ForegroundColor Yellow

if (-not (Test-Path $AssemblyInfoPath)) {
    Write-Host "✗ AssemblyInfo.cs not found at: $AssemblyInfoPath" -ForegroundColor Red
    exit 1
}

$assemblyContent = Get-Content $AssemblyInfoPath -Raw
if ($assemblyContent -match '\[assembly: AssemblyVersion\("(\d+)\.(\d+)\.(\d+)\.(\d+)"\)\]') {
    $currentMajor = [int]$matches[1]
    $currentMinor = [int]$matches[2]
    $currentPatch = [int]$matches[3]
    $currentVersion = "$currentMajor.$currentMinor.$currentPatch"
    Write-Host "✓ Current version: $currentVersion" -ForegroundColor Green
}
else {
    Write-Host "✗ Could not parse version from AssemblyInfo.cs" -ForegroundColor Red
    exit 1
}

# ============================================================================
# Step 2: Determine Next Version
# ============================================================================
Write-Host ""
Write-Host "Step 2: Determining next version..." -ForegroundColor Yellow

if ($NewVersion) {
    # Explicit version provided
    if ($NewVersion -notmatch '^\d+\.\d+\.\d+$') {
        Write-Host "✗ Invalid version format: $NewVersion (expected X.Y.Z)" -ForegroundColor Red
        exit 1
    }
    
    $nextVersion = $NewVersion
    Write-Host "✓ Using explicit version: $nextVersion" -ForegroundColor Green
}
else {
    # Auto-detect from CHANGELOG.md
    if (-not (Test-Path $ChangelogPath)) {
        Write-Host "✗ CHANGELOG.md not found at: $ChangelogPath" -ForegroundColor Red
        Write-Host "  Use -NewVersion parameter to set version manually" -ForegroundColor Yellow
        exit 1
    }
    
    $changelogContent = Get-Content $ChangelogPath -Raw
    
    # Extract [Unreleased] section
    if ($changelogContent -match '(?s)##\s*\[Unreleased\](.*?)(?=\n##\s+\[|\z)') {
        $unreleasedSection = $matches[1]
        
        # Check for version bump indicators
        $hasBreaking = $unreleasedSection -match '###\s+(BREAKING|Breaking Changes)'
        $hasAdded = $unreleasedSection -match '###\s+Added'
        $hasChanged = $unreleasedSection -match '###\s+Changed'
        $hasFixed = $unreleasedSection -match '###\s+Fixed'
        $hasRemoved = $unreleasedSection -match '###\s+Removed'
        $hasDeprecated = $unreleasedSection -match '###\s+Deprecated'
        $hasSecurity = $unreleasedSection -match '###\s+Security'
        
        # Determine version bump type
        if ($hasBreaking) {
            $bumpType = "MAJOR"
            $nextMajor = $currentMajor + 1
            $nextMinor = 0
            $nextPatch = 0
        }
        elseif ($hasAdded -or $hasChanged -or $hasDeprecated) {
            $bumpType = "MINOR"
            $nextMajor = $currentMajor
            $nextMinor = $currentMinor + 1
            $nextPatch = 0
        }
        elseif ($hasFixed -or $hasSecurity -or $hasRemoved) {
            $bumpType = "PATCH"
            $nextMajor = $currentMajor
            $nextMinor = $currentMinor
            $nextPatch = $currentPatch + 1
        }
        else {
            Write-Host "✗ No changes detected in [Unreleased] section" -ForegroundColor Red
            Write-Host "  CHANGELOG.md should have at least one of: Added, Changed, Fixed, BREAKING" -ForegroundColor Yellow
            Write-Host "  Use -NewVersion parameter to set version manually" -ForegroundColor Yellow
            exit 1
        }
        
        $nextVersion = "$nextMajor.$nextMinor.$nextPatch"
        
        Write-Host "✓ Detected changes:" -ForegroundColor Green
        if ($hasBreaking) { Write-Host "  - BREAKING CHANGES" -ForegroundColor Red }
        if ($hasAdded) { Write-Host "  - Added" -ForegroundColor Cyan }
        if ($hasChanged) { Write-Host "  - Changed" -ForegroundColor Cyan }
        if ($hasFixed) { Write-Host "  - Fixed" -ForegroundColor Cyan }
        if ($hasRemoved) { Write-Host "  - Removed" -ForegroundColor Cyan }
        if ($hasDeprecated) { Write-Host "  - Deprecated" -ForegroundColor Cyan }
        if ($hasSecurity) { Write-Host "  - Security" -ForegroundColor Cyan }
        Write-Host "  → Bump type: $bumpType" -ForegroundColor Yellow
        Write-Host "  → Next version: $nextVersion" -ForegroundColor Green
    }
    else {
        Write-Host "✗ Could not find [Unreleased] section in CHANGELOG.md" -ForegroundColor Red
        Write-Host "  Use -NewVersion parameter to set version manually" -ForegroundColor Yellow
        exit 1
    }
}

# ============================================================================
# Step 3: Confirm Update
# ============================================================================
Write-Host ""
Write-Host "Step 3: Version update plan" -ForegroundColor Yellow
Write-Host "  Current: $currentVersion" -ForegroundColor Gray
Write-Host "  Next:    $nextVersion" -ForegroundColor Green
Write-Host ""

if ($DryRun) {
    Write-Host "[DRY RUN] Would update the following files:" -ForegroundColor Magenta
    Write-Host "  - $AssemblyInfoPath" -ForegroundColor Gray
    Write-Host "  - $CsprojPath" -ForegroundColor Gray
    Write-Host "  - $ChangelogPath" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Run without -DryRun to apply changes" -ForegroundColor Yellow
    exit 0
}

if (-not $Force) {
    $response = Read-Host "Continue with version update? (Y/n)"
    if ($response -eq 'n' -or $response -eq 'N') {
        Write-Host "Version update cancelled" -ForegroundColor Yellow
        exit 0
    }
}

# ============================================================================
# Step 4: Update AssemblyInfo.cs
# ============================================================================
Write-Host ""
Write-Host "Step 4: Updating AssemblyInfo.cs..." -ForegroundColor Yellow

$assemblyContent = Get-Content $AssemblyInfoPath -Raw

# Update AssemblyVersion
$assemblyContent = $assemblyContent -replace `
    '\[assembly: AssemblyVersion\("\d+\.\d+\.\d+\.\d+"\)\]', `
    "[assembly: AssemblyVersion(`"$nextVersion.0`")]"

# Update AssemblyFileVersion
$assemblyContent = $assemblyContent -replace `
    '\[assembly: AssemblyFileVersion\("\d+\.\d+\.\d+\.\d+"\)\]', `
    "[assembly: AssemblyFileVersion(`"$nextVersion.0`")]"

Set-Content $AssemblyInfoPath -Value $assemblyContent -NoNewline
Write-Host "✓ Updated AssemblyInfo.cs → $nextVersion.0" -ForegroundColor Green

# ============================================================================
# Step 5: Update .csproj
# ============================================================================
Write-Host ""
Write-Host "Step 5: Updating .csproj..." -ForegroundColor Yellow

if (-not (Test-Path $CsprojPath)) {
    Write-Host "⚠ .csproj not found at: $CsprojPath" -ForegroundColor Yellow
}
else {
    $csprojContent = Get-Content $CsprojPath -Raw
    $csprojContent = $csprojContent -replace '<Version>\d+\.\d+\.\d+</Version>', "<Version>$nextVersion</Version>"
    Set-Content $CsprojPath -Value $csprojContent -NoNewline
    Write-Host "✓ Updated .csproj → $nextVersion" -ForegroundColor Green
}

# ============================================================================
# Step 6: Update CHANGELOG.md
# ============================================================================
Write-Host ""
Write-Host "Step 6: Updating CHANGELOG.md..." -ForegroundColor Yellow

$changelogContent = Get-Content $ChangelogPath -Raw
$today = Get-Date -Format "yyyy-MM-dd"

# Replace [Unreleased] with versioned release
$changelogContent = $changelogContent -replace `
    '## \[Unreleased\]', `
    "## [Unreleased]`n`n## [$nextVersion] - $today"

Set-Content $ChangelogPath -Value $changelogContent -NoNewline
Write-Host "✓ Updated CHANGELOG.md → [$nextVersion] - $today" -ForegroundColor Green

# ============================================================================
# Summary
# ============================================================================
Write-Host ""
Write-Host "=== Version Update Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Updated version: $currentVersion → $nextVersion" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Run: .\build.ps1 -Configuration Release" -ForegroundColor White
Write-Host "  2. Run: .\create-package.ps1" -ForegroundColor White
Write-Host "  3. Run: .\publish-package.ps1" -ForegroundColor White
Write-Host ""
Write-Host "Or use master script: .\release.ps1" -ForegroundColor Cyan
Write-Host ""

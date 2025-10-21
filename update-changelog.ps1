# Changelog Update Helper Script
# This script helps determine if changelog updates are needed based on git changes

param(
    [switch]$Analyze,
    [switch]$Suggest,
    [string]$Since = "HEAD~10"
)

function Get-GitChanges {
    param([string]$Since)
    
    $changes = git log $Since..HEAD --pretty=format:"%h|%s|%an|%ad" --date=short
    return $changes | ForEach-Object {
        $parts = $_ -split '\|'
        [PSCustomObject]@{
            Hash = $parts[0]
            Message = $parts[1]
            Author = $parts[2]
            Date = $parts[3]
        }
    }
}

function Analyze-ChangeImpact {
    param([array]$Changes)
    
    $categories = @{
        MajorFeature = @()
        MinorFeature = @()
        BugFix = @()
        Documentation = @()
        Refactor = @()
        Test = @()
    }
    
    # Keywords for categorization
    $majorKeywords = @('breaking', 'major', 'new feature', 'added.*command', 'added.*ui', 'integration')
    $minorKeywords = @('added', 'feature', 'enhancement', 'improved', 'update')
    $bugKeywords = @('fix', 'bug', 'issue', 'error', 'crash', 'resolve')
    $docKeywords = @('doc', 'readme', 'comment', 'changelog')
    $refactorKeywords = @('refactor', 'cleanup', 'reorganize', 'rename')
    $testKeywords = @('test', 'spec', 'coverage')
    
    foreach ($change in $Changes) {
        $msg = $change.Message.ToLower()
        
        if ($majorKeywords | Where-Object { $msg -match $_ }) {
            $categories.MajorFeature += $change
        }
        elseif ($bugKeywords | Where-Object { $msg -match $_ }) {
            $categories.BugFix += $change
        }
        elseif ($minorKeywords | Where-Object { $msg -match $_ }) {
            $categories.MinorFeature += $change
        }
        elseif ($docKeywords | Where-Object { $msg -match $_ }) {
            $categories.Documentation += $change
        }
        elseif ($refactorKeywords | Where-Object { $msg -match $_ }) {
            $categories.Refactor += $change
        }
        elseif ($testKeywords | Where-Object { $msg -match $_ }) {
            $categories.Test += $change
        }
    }
    
    return $categories
}

function Suggest-ChangelogUpdate {
    param([hashtable]$Categories)
    
    $suggestions = @()
    $requiresUpdate = $false
    
    if ($Categories.MajorFeature.Count -gt 0) {
        $requiresUpdate = $true
        $suggestions += "`n### Added (Major Features)"
        foreach ($change in $Categories.MajorFeature) {
            $suggestions += "- $($change.Message) ($($change.Hash))"
        }
    }
    
    if ($Categories.MinorFeature.Count -gt 0) {
        $requiresUpdate = $true
        $suggestions += "`n### Added"
        foreach ($change in $Categories.MinorFeature | Select-Object -First 5) {
            $suggestions += "- $($change.Message) ($($change.Hash))"
        }
    }
    
    if ($Categories.BugFix.Count -gt 0) {
        $requiresUpdate = $true
        $suggestions += "`n### Fixed"
        foreach ($change in $Categories.BugFix | Select-Object -First 5) {
            $suggestions += "- $($change.Message) ($($change.Hash))"
        }
    }
    
    return @{
        RequiresUpdate = $requiresUpdate
        Suggestions = $suggestions
    }
}

function Get-CurrentVersion {
    $versionFile = Join-Path $PSScriptRoot "VesselStudioSimplePlugin\Properties\AssemblyInfo.cs"
    if (Test-Path $versionFile) {
        $content = Get-Content $versionFile -Raw
        if ($content -match 'AssemblyVersion\("(\d+)\.(\d+)\.(\d+)\.(\d+)"\)') {
            return "$($matches[1]).$($matches[2]).$($matches[3])"
        }
    }
    return "Unknown"
}

# Main execution
Write-Host "=== Vessel Studio Plugin - Changelog Helper ===" -ForegroundColor Cyan
Write-Host "Current Version: $(Get-CurrentVersion)" -ForegroundColor Green
Write-Host ""

if ($Analyze -or $Suggest) {
    Write-Host "Analyzing git changes since: $Since" -ForegroundColor Yellow
    Write-Host ""
    
    $changes = Get-GitChanges -Since $Since
    
    if ($changes.Count -eq 0) {
        Write-Host "No changes found since $Since" -ForegroundColor Gray
        exit 0
    }
    
    Write-Host "Found $($changes.Count) commits" -ForegroundColor White
    Write-Host ""
    
    $categories = Analyze-ChangeImpact -Changes $changes
    
    Write-Host "Change Breakdown:" -ForegroundColor Cyan
    Write-Host "  Major Features: $($categories.MajorFeature.Count)" -ForegroundColor $(if ($categories.MajorFeature.Count -gt 0) { "Green" } else { "Gray" })
    Write-Host "  Minor Features: $($categories.MinorFeature.Count)" -ForegroundColor $(if ($categories.MinorFeature.Count -gt 0) { "Green" } else { "Gray" })
    Write-Host "  Bug Fixes: $($categories.BugFix.Count)" -ForegroundColor $(if ($categories.BugFix.Count -gt 0) { "Yellow" } else { "Gray" })
    Write-Host "  Documentation: $($categories.Documentation.Count)" -ForegroundColor Gray
    Write-Host "  Refactoring: $($categories.Refactor.Count)" -ForegroundColor Gray
    Write-Host "  Tests: $($categories.Test.Count)" -ForegroundColor Gray
    Write-Host ""
    
    if ($Suggest) {
        $result = Suggest-ChangelogUpdate -Categories $categories
        
        if ($result.RequiresUpdate) {
            Write-Host "RECOMMENDATION: Changelog update needed" -ForegroundColor Red
            Write-Host ""
            Write-Host "Suggested version bump:" -ForegroundColor Yellow
            
            if ($categories.MajorFeature.Count -gt 0) {
                Write-Host "  MINOR version (new features added)" -ForegroundColor Green
            }
            elseif ($categories.BugFix.Count -gt 0) {
                Write-Host "  PATCH version (bug fixes)" -ForegroundColor Yellow
            }
            
            Write-Host ""
            Write-Host "Suggested Changelog Entries:" -ForegroundColor Cyan
            Write-Host "----------------------------"
            $result.Suggestions | ForEach-Object { Write-Host $_ }
            Write-Host ""
            Write-Host "Add these to CHANGELOG.md under [Unreleased]" -ForegroundColor Yellow
        }
        else {
            Write-Host "No significant changes requiring changelog update" -ForegroundColor Green
        }
    }
}
else {
    Write-Host "Usage:" -ForegroundColor White
    Write-Host "  .\update-changelog.ps1 -Analyze          # Analyze recent changes"
    Write-Host "  .\update-changelog.ps1 -Suggest          # Get changelog suggestions"
    Write-Host "  .\update-changelog.ps1 -Suggest -Since HEAD~20  # Check last 20 commits"
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor White
    Write-Host "  .\update-changelog.ps1 -Suggest -Since v1.0.0    # Changes since tag"
    Write-Host "  .\update-changelog.ps1 -Suggest -Since 2025-10-01  # Changes since date"
}

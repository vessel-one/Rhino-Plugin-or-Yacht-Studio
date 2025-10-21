# Quick Build Script - No Changelog Analysis
# Use this for rapid development builds

param(
    [string]$Configuration = "Release",
    [switch]$Clean
)

Write-Host "=== Quick Build ===" -ForegroundColor Cyan

$scriptPath = Join-Path $PSScriptRoot "build.ps1"
$buildArgs = @{
    Configuration = $Configuration
    SkipChangelog = $true
    SkipVersionCheck = $true
}

if ($Clean) {
    $buildArgs.Clean = $true
}

& $scriptPath @buildArgs

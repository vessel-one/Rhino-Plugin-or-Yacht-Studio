# Quick Build Script - No Changelog Analysis
# Use this for rapid development builds

param(
    [string]$Configuration = "Release",
    [switch]$Clean
)

Write-Host "=== Quick Build ===" -ForegroundColor Cyan

$buildArgs = @(".\build.ps1", "-Configuration", $Configuration, "-SkipChangelog", "-SkipVersionCheck")

if ($Clean) {
    $buildArgs += "-Clean"
}

& @buildArgs

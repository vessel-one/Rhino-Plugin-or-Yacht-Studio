<#
.SYNOPSIS
    Build and install DEV version of Vessel Studio plugin

.DESCRIPTION
    Builds the plugin in Debug configuration (with DEV mode enabled) and
    optionally installs it to Rhino for testing. DEV builds can run
    side-by-side with RELEASE versions.

.PARAMETER Install
    Install the plugin to Rhino after building

.PARAMETER Clean
    Clean before building

.EXAMPLE
    .\dev-build.ps1
    Build DEV version

.EXAMPLE
    .\dev-build.ps1 -Install
    Build and install DEV version to Rhino
#>

[CmdletBinding()]
param(
    [switch]$Install,
    [switch]$Clean
)

Write-Host "`n=== Vessel Studio DEV Build ===" -ForegroundColor Cyan
Write-Host "Building plugin in DEBUG mode (DEV configuration)`n" -ForegroundColor Yellow

$projectPath = "VesselStudioSimplePlugin\VesselStudioSimplePlugin.csproj"
$configuration = "Debug"

# Clean if requested
if ($Clean) {
    Write-Host "Cleaning build output..." -ForegroundColor Yellow
    dotnet clean $projectPath -c $configuration
}

# Build
Write-Host "Building DEV version..." -ForegroundColor Yellow
dotnet build $projectPath -c $configuration --nologo

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n❌ Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "`n✅ DEV Build successful!" -ForegroundColor Green

# Show output location
$outputDir = "VesselStudioSimplePlugin\bin\Debug\net48"
$rhpFile = Join-Path $outputDir "VesselStudioSimplePlugin-DEV.rhp"

if (Test-Path $rhpFile) {
    $fileInfo = Get-Item $rhpFile
    Write-Host "`nDEV Plugin file:" -ForegroundColor Cyan
    Write-Host "  📦 $rhpFile" -ForegroundColor White
    Write-Host "  📏 Size: $([math]::Round($fileInfo.Length / 1KB, 1)) KB" -ForegroundColor Gray
    Write-Host "  🕐 Modified: $($fileInfo.LastWriteTime)" -ForegroundColor Gray
}

# Install if requested
if ($Install) {
    Write-Host "`n=== Installing DEV Plugin to Rhino ===" -ForegroundColor Cyan
    
    # Check if Rhino is running
    $rhinoProcess = Get-Process -Name "Rhino" -ErrorAction SilentlyContinue
    if ($rhinoProcess) {
        Write-Host "⚠️  WARNING: Rhino is currently running!" -ForegroundColor Yellow
        Write-Host "   Close Rhino before installing the plugin." -ForegroundColor Yellow
        
        $response = Read-Host "`nClose Rhino and continue? (y/N)"
        if ($response -ne 'y' -and $response -ne 'Y') {
            Write-Host "`nInstallation cancelled." -ForegroundColor Yellow
            exit 0
        }
        
        # Wait for Rhino to close
        Write-Host "Waiting for Rhino to close..." -ForegroundColor Yellow
        Start-Sleep -Seconds 2
    }
    
    # Copy to Rhino plugins directory
    $pluginDir = "$env:APPDATA\McNeel\Rhinoceros\8.0\Plug-ins\Vessel Studio DEV"
    
    if (-not (Test-Path $pluginDir)) {
        New-Item -Path $pluginDir -ItemType Directory -Force | Out-Null
        Write-Host "Created plugin directory: $pluginDir" -ForegroundColor Gray
    }
    
    # Copy files
    Copy-Item $rhpFile -Destination $pluginDir -Force
    
    # Copy Newtonsoft.Json.dll if it exists
    $newtonsoftDll = Join-Path $outputDir "Newtonsoft.Json.dll"
    if (Test-Path $newtonsoftDll) {
        Copy-Item $newtonsoftDll -Destination $pluginDir -Force
        Write-Host "  ✓ Copied Newtonsoft.Json.dll" -ForegroundColor Green
    }
    
    Write-Host "`n✅ DEV Plugin installed!" -ForegroundColor Green
    Write-Host "`nInstallation location:" -ForegroundColor Cyan
    Write-Host "  $pluginDir" -ForegroundColor White
    
    Write-Host "`n=== Next Steps ===" -ForegroundColor Cyan
    Write-Host "1. Start Rhino" -ForegroundColor White
    Write-Host "2. The DEV plugin will load automatically" -ForegroundColor White
    Write-Host "3. Use DEV commands:" -ForegroundColor White
    Write-Host "   • DevVesselStudioShowToolbar" -ForegroundColor Yellow
    Write-Host "   • DevVesselSetApiKey" -ForegroundColor Yellow
    Write-Host "   • DevVesselCapture" -ForegroundColor Yellow
    Write-Host "   • DevVesselQuickCapture" -ForegroundColor Yellow
    Write-Host "`n4. DEV plugin uses separate settings in:" -ForegroundColor White
    Write-Host "   $env:APPDATA\VesselStudioDEV\" -ForegroundColor Gray
    Write-Host "`n5. You can run both DEV and RELEASE plugins simultaneously!" -ForegroundColor Green
    
} else {
    Write-Host "`n=== Manual Installation ===" -ForegroundColor Cyan
    Write-Host "To install the DEV plugin:" -ForegroundColor White
    Write-Host "  1. Close Rhino if it's running" -ForegroundColor Gray
    Write-Host "  2. Run: .\dev-build.ps1 -Install" -ForegroundColor Yellow
    Write-Host "  Or drag-drop the .rhp file into Rhino" -ForegroundColor Gray
}

Write-Host "`n=== DEV vs RELEASE ===" -ForegroundColor Cyan
Write-Host "DEV Build:" -ForegroundColor Yellow
Write-Host "  • Different plugin GUID (no conflicts)" -ForegroundColor Gray
Write-Host "  • Commands prefixed with 'Dev'" -ForegroundColor Gray
Write-Host "  • Separate settings folder (VesselStudioDEV)" -ForegroundColor Gray
Write-Host "  • Can run alongside RELEASE version" -ForegroundColor Gray
Write-Host "`nRELEASE Build:" -ForegroundColor Green
Write-Host "  • Use: .\quick-build.ps1 or .\build.ps1" -ForegroundColor Gray
Write-Host "  • Standard commands (no prefix)" -ForegroundColor Gray
Write-Host "  • Production settings folder (VesselStudio)" -ForegroundColor Gray
Write-Host "  • For package manager distribution" -ForegroundColor Gray

Write-Host ""

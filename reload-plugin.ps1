# 🚀 Vessel Studio Plugin - Quick Reload Script
# Closes Rhino, rebuilds plugin, installs fresh copy, opens Rhino

param(
    [switch]$SkipBuild,
    [switch]$KeepRhinoOpen
)

Write-Host ""
Write-Host "════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🚀 Vessel Studio Plugin - Quick Reload" -ForegroundColor Cyan
Write-Host "════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Step 1: Close Rhino
Write-Host "1️⃣  Closing Rhino..." -ForegroundColor Yellow
$rhinoProcesses = Get-Process -Name "Rhino" -ErrorAction SilentlyContinue

if ($rhinoProcesses) {
    Write-Host "   Found $($rhinoProcesses.Count) Rhino process(es)" -ForegroundColor Gray
    Stop-Process -Name "Rhino" -Force
    Start-Sleep -Seconds 3
    Write-Host "   ✅ Rhino closed" -ForegroundColor Green
} else {
    Write-Host "   ℹ️  Rhino was not running" -ForegroundColor Gray
}

# Step 2: Build plugin
if (-not $SkipBuild) {
    Write-Host ""
    Write-Host "2️⃣  Building plugin..." -ForegroundColor Yellow
    
    Push-Location "VesselStudioSimplePlugin"
    $buildOutput = dotnet build -c Release 2>&1
    $buildResult = $LASTEXITCODE
    Pop-Location
    
    if ($buildResult -ne 0) {
        Write-Host "   ❌ Build failed!" -ForegroundColor Red
        Write-Host ""
        Write-Host "Build output:" -ForegroundColor Gray
        Write-Host $buildOutput -ForegroundColor Gray
        exit 1
    }
    
    # Check if .rhp file was created
    $rhpFile = Get-Item "VesselStudioSimplePlugin\bin\Release\net48\VesselStudioSimplePlugin.rhp" -ErrorAction SilentlyContinue
    if (-not $rhpFile) {
        Write-Host "   ❌ .rhp file not found after build!" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "   ✅ Build succeeded" -ForegroundColor Green
    Write-Host "   📦 Plugin: $($rhpFile.Length) bytes" -ForegroundColor Gray
    Write-Host "   📅 Built: $($rhpFile.LastWriteTime)" -ForegroundColor Gray
} else {
    Write-Host ""
    Write-Host "2️⃣  Skipping build (using existing .rhp)" -ForegroundColor Yellow
}

# Step 3: Remove old plugin from Rhino
Write-Host ""
Write-Host "3️⃣  Removing old plugin from Rhino..." -ForegroundColor Yellow

$pluginFolder = "$env:APPDATA\McNeel\Rhinoceros\8.0\Plug-ins"
$oldPlugin = Join-Path $pluginFolder "VesselStudioSimplePlugin.rhp"

if (Test-Path $oldPlugin) {
    Remove-Item $oldPlugin -Force
    Write-Host "   ✅ Removed old plugin" -ForegroundColor Green
} else {
    Write-Host "   ℹ️  No old plugin found" -ForegroundColor Gray
}

# Step 4: Check for and remove conflicting DLLs
Write-Host ""
Write-Host "4️⃣  Checking for conflicting DLLs..." -ForegroundColor Yellow

$conflictingFiles = @(
    "RhinoCommon.dll",
    "Eto.dll",
    "Rhino.UI.dll",
    "Ed.Eto.dll"
)

$removed = @()
foreach ($file in $conflictingFiles) {
    $fullPath = Join-Path $pluginFolder $file
    if (Test-Path $fullPath) {
        Remove-Item $fullPath -Force -ErrorAction SilentlyContinue
        $removed += $file
    }
}

if ($removed.Count -gt 0) {
    Write-Host "   ⚠️  Removed conflicting files: $($removed -join ', ')" -ForegroundColor Yellow
} else {
    Write-Host "   ✅ No conflicting DLLs found" -ForegroundColor Green
}

# Step 5: Install new plugin
Write-Host ""
Write-Host "5️⃣  Installing new plugin..." -ForegroundColor Yellow

$sourcePath = "VesselStudioSimplePlugin\bin\Release\net48\VesselStudioSimplePlugin.rhp"
$destPath = Join-Path $pluginFolder "VesselStudioSimplePlugin.rhp"

if (-not (Test-Path $sourcePath)) {
    Write-Host "   ❌ Source plugin not found: $sourcePath" -ForegroundColor Red
    Write-Host "   Run without -SkipBuild or build manually first" -ForegroundColor Yellow
    exit 1
}

# Ensure plugin folder exists
if (-not (Test-Path $pluginFolder)) {
    New-Item -ItemType Directory -Path $pluginFolder -Force | Out-Null
}

Copy-Item $sourcePath $destPath -Force

$installedPlugin = Get-Item $destPath
Write-Host "   ✅ Plugin installed" -ForegroundColor Green
Write-Host "   📍 $destPath" -ForegroundColor Gray
Write-Host "   📦 Size: $($installedPlugin.Length) bytes" -ForegroundColor Gray

# Step 6: Verify installation
Write-Host ""
Write-Host "6️⃣  Verifying installation..." -ForegroundColor Yellow

# Check only .rhp file exists (no DLLs)
$pluginFiles = Get-ChildItem $pluginFolder -Filter "*VesselStudio*"

if ($pluginFiles.Count -eq 1 -and $pluginFiles[0].Name -eq "VesselStudioSimplePlugin.rhp") {
    Write-Host "   ✅ Clean installation (only .rhp file)" -ForegroundColor Green
} elseif ($pluginFiles.Count -gt 1) {
    Write-Host "   ⚠️  Multiple Vessel Studio files found:" -ForegroundColor Yellow
    foreach ($file in $pluginFiles) {
        Write-Host "      - $($file.Name)" -ForegroundColor Yellow
    }
} else {
    Write-Host "   ❌ Plugin not found after installation!" -ForegroundColor Red
    exit 1
}

# Step 7: Start Rhino
if (-not $KeepRhinoOpen) {
    Write-Host ""
    Write-Host "7️⃣  Starting Rhino..." -ForegroundColor Yellow
    
    $rhinoPath = "C:\Program Files\Rhino 8\System\Rhino.exe"
    
    if (Test-Path $rhinoPath) {
        Start-Process $rhinoPath
        Start-Sleep -Seconds 2
        Write-Host "   ✅ Rhino started" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️  Rhino not found at: $rhinoPath" -ForegroundColor Yellow
        Write-Host "   Start Rhino manually" -ForegroundColor Gray
    }
}

# Success summary
Write-Host ""
Write-Host "════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✅ PLUGIN RELOADED SUCCESSFULLY!" -ForegroundColor Green
Write-Host "════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "📝 Next steps in Rhino:" -ForegroundColor White
Write-Host ""
Write-Host "   Type: " -NoNewline -ForegroundColor Gray
Write-Host "VesselStudioShowToolbar" -ForegroundColor Yellow
Write-Host "   Or go to: Panels → Vessel Studio" -ForegroundColor Gray
Write-Host ""
Write-Host "   Available commands:" -ForegroundColor Gray
Write-Host "   • VesselSetApiKey         - Configure API key" -ForegroundColor White
Write-Host "   • VesselCapture           - Capture screenshot" -ForegroundColor White
Write-Host "   • VesselQuickCapture      - Quick capture to last project" -ForegroundColor White
Write-Host "   • VesselStudioStatus      - Check connection" -ForegroundColor White
Write-Host "   • VesselStudioHelp        - Show help" -ForegroundColor White
Write-Host ""
Write-Host "💡 Tip: Use " -NoNewline -ForegroundColor Gray
Write-Host ".\reload-plugin.ps1 -SkipBuild" -NoNewline -ForegroundColor Yellow
Write-Host " to skip rebuild" -ForegroundColor Gray
Write-Host ""

# 🔄 Quick Plugin Update Script

Write-Host "🔄 Updating Vessel Studio Rhino Plugin..." -ForegroundColor Cyan
Write-Host ""

# Step 1: Close Rhino
Write-Host "1️⃣  Checking for running Rhino processes..." -ForegroundColor Yellow
$rhinoProcesses = Get-Process -Name "Rhino" -ErrorAction SilentlyContinue

if ($rhinoProcesses) {
    Write-Host "   Found $($rhinoProcesses.Count) Rhino process(es) running" -ForegroundColor Yellow
    Write-Host "   Closing Rhino..." -ForegroundColor Yellow
    Stop-Process -Name "Rhino" -Force
    Start-Sleep -Seconds 3
    Write-Host "   ✅ Rhino closed" -ForegroundColor Green
} else {
    Write-Host "   ✅ Rhino is not running" -ForegroundColor Green
}

# Step 2: Find and delete old plugin
Write-Host ""
Write-Host "2️⃣  Removing old plugin..." -ForegroundColor Yellow

$pluginLocations = @(
    "$env:APPDATA\McNeel\Rhinoceros\8.0\Plug-ins\",
    "C:\Program Files\Rhino 8\Plug-ins\"
)

$found = $false
foreach ($location in $pluginLocations) {
    $oldPlugin = Get-ChildItem "$location*VesselStudio*.rhp" -Recurse -ErrorAction SilentlyContinue
    if ($oldPlugin) {
        Write-Host "   Found old plugin: $($oldPlugin.FullName)" -ForegroundColor Gray
        Remove-Item $oldPlugin.FullName -Force
        Write-Host "   ✅ Deleted old plugin" -ForegroundColor Green
        $found = $true
    }
}

if (-not $found) {
    Write-Host "   ℹ️  No old plugin found" -ForegroundColor Gray
}

# Step 3: Copy new plugin
Write-Host ""
Write-Host "3️⃣  Installing new plugin..." -ForegroundColor Yellow

$sourcePath = "VesselStudioSimplePlugin\bin\Release\net48\VesselStudioSimplePlugin.rhp"
$destPath = "$env:APPDATA\McNeel\Rhinoceros\8.0\Plug-ins\VesselStudioSimplePlugin.rhp"

if (Test-Path $sourcePath) {
    # Ensure destination directory exists
    $destDir = Split-Path $destPath
    if (-not (Test-Path $destDir)) {
        New-Item -ItemType Directory -Path $destDir -Force | Out-Null
    }
    
    Copy-Item $sourcePath $destPath -Force
    $newPlugin = Get-Item $destPath
    Write-Host "   ✅ Plugin installed: $destPath" -ForegroundColor Green
    Write-Host "   📦 Size: $($newPlugin.Length) bytes" -ForegroundColor Gray
    Write-Host "   📅 Date: $($newPlugin.LastWriteTime)" -ForegroundColor Gray
} else {
    Write-Host "   ❌ Source plugin not found: $sourcePath" -ForegroundColor Red
    Write-Host "   Build the plugin first: cd VesselStudioSimplePlugin ; dotnet build -c Release" -ForegroundColor Yellow
    exit 1
}

# Step 4: Start Rhino
Write-Host ""
Write-Host "4️⃣  Starting Rhino..." -ForegroundColor Yellow
Start-Process "C:\Program Files\Rhino 8\System\Rhino.exe"
Start-Sleep -Seconds 2
Write-Host "   ✅ Rhino started" -ForegroundColor Green

Write-Host ""
Write-Host "✅ Update complete!" -ForegroundColor Green
Write-Host ""
Write-Host "📝 Next steps in Rhino:" -ForegroundColor Cyan
Write-Host "   1. Type: VesselStudioShowToolbar" -ForegroundColor White
Write-Host "   2. Test the toolbar panel" -ForegroundColor White
Write-Host "   3. Check status display" -ForegroundColor White
Write-Host ""

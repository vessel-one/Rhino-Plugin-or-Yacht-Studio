# 🔍 Plugin Cleanup - Force Remove All Versions

Write-Host "🔍 Vessel Studio Plugin - Complete Cleanup" -ForegroundColor Cyan
Write-Host ""

# Close Rhino completely
Write-Host "1️⃣  Force closing all Rhino processes..." -ForegroundColor Yellow
$rhinoProcesses = Get-Process -Name "Rhino" -ErrorAction SilentlyContinue
if ($rhinoProcesses) {
    Write-Host "   Killing $($rhinoProcesses.Count) Rhino process(es)..." -ForegroundColor Yellow
    Stop-Process -Name "Rhino" -Force
    Start-Sleep -Seconds 3
}

# Also check for Rhino child processes
$rhinoChildren = Get-Process | Where-Object { $_.ProcessName -like "*Rhino*" }
if ($rhinoChildren) {
    Write-Host "   Killing additional Rhino processes..." -ForegroundColor Yellow
    $rhinoChildren | Stop-Process -Force
    Start-Sleep -Seconds 2
}

Write-Host "   ✅ All Rhino processes closed" -ForegroundColor Green

# Find and remove ALL instances
Write-Host ""
Write-Host "2️⃣  Searching for ALL plugin instances..." -ForegroundColor Yellow

$locations = @(
    "$env:APPDATA\McNeel\Rhinoceros\8.0\Plug-ins\",
    "$env:APPDATA\McNeel\Rhinoceros\packages\",
    "C:\Program Files\Rhino 8\Plug-ins\",
    "C:\Program Files\Common Files\McNeel\",
    "$env:LOCALAPPDATA\McNeel\"
)

$foundFiles = @()

foreach ($location in $locations) {
    if (Test-Path $location) {
        $files = Get-ChildItem "$location" -Filter "*VesselStudio*" -Recurse -ErrorAction SilentlyContinue
        if ($files) {
            $foundFiles += $files
            foreach ($file in $files) {
                Write-Host "   Found: $($file.FullName)" -ForegroundColor Gray
            }
        }
    }
}

if ($foundFiles.Count -eq 0) {
    Write-Host "   ℹ️  No existing plugins found" -ForegroundColor Gray
} else {
    Write-Host ""
    Write-Host "3️⃣  Removing $($foundFiles.Count) old file(s)..." -ForegroundColor Yellow
    foreach ($file in $foundFiles) {
        try {
            Remove-Item $file.FullName -Force -Recurse -ErrorAction Stop
            Write-Host "   ✅ Deleted: $($file.Name)" -ForegroundColor Green
        } catch {
            Write-Host "   ❌ Failed to delete: $($file.Name)" -ForegroundColor Red
            Write-Host "      Error: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

# Wait a moment
Write-Host ""
Write-Host "⏳ Waiting for file system to update..." -ForegroundColor Yellow
Start-Sleep -Seconds 2

# Install fresh copy
Write-Host ""
Write-Host "4️⃣  Installing fresh plugin copy..." -ForegroundColor Yellow

$sourcePath = "VesselStudioSimplePlugin\bin\Release\net48\VesselStudioSimplePlugin.rhp"
$destPath = "$env:APPDATA\McNeel\Rhinoceros\8.0\Plug-ins\VesselStudioSimplePlugin.rhp"

if (Test-Path $sourcePath) {
    Copy-Item $sourcePath $destPath -Force
    $newPlugin = Get-Item $destPath
    Write-Host "   ✅ Plugin installed" -ForegroundColor Green
    Write-Host "   📦 Path: $destPath" -ForegroundColor Gray
    Write-Host "   📦 Size: $($newPlugin.Length) bytes" -ForegroundColor Gray
    Write-Host "   📅 Built: $($newPlugin.LastWriteTime)" -ForegroundColor Gray
} else {
    Write-Host "   ❌ Source not found: $sourcePath" -ForegroundColor Red
    exit 1
}

# Verify only one copy exists
Write-Host ""
Write-Host "5️⃣  Verifying installation..." -ForegroundColor Yellow
$allPlugins = @()
foreach ($location in $locations) {
    if (Test-Path $location) {
        $found = Get-ChildItem "$location" -Filter "*VesselStudio*" -Recurse -ErrorAction SilentlyContinue
        if ($found) {
            $allPlugins += $found
        }
    }
}

if ($allPlugins.Count -eq 1) {
    Write-Host "   ✅ Only one plugin copy found (correct!)" -ForegroundColor Green
    Write-Host "   📍 $($allPlugins[0].FullName)" -ForegroundColor Gray
} elseif ($allPlugins.Count -eq 0) {
    Write-Host "   ❌ No plugins found!" -ForegroundColor Red
} else {
    Write-Host "   ⚠️  Multiple copies found:" -ForegroundColor Yellow
    foreach ($p in $allPlugins) {
        Write-Host "      - $($p.FullName)" -ForegroundColor Yellow
    }
}

# Start Rhino
Write-Host ""
Write-Host "6️⃣  Starting Rhino..." -ForegroundColor Yellow
Start-Process "C:\Program Files\Rhino 8\System\Rhino.exe"
Start-Sleep -Seconds 2
Write-Host "   ✅ Rhino started" -ForegroundColor Green

Write-Host ""
Write-Host "════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✅ CLEANUP COMPLETE!" -ForegroundColor Green
Write-Host "════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "📝 In Rhino, type:" -ForegroundColor White
Write-Host "   PlugInManager" -ForegroundColor Yellow
Write-Host ""
Write-Host "   Look for 'Vessel Studio Simple Plugin'" -ForegroundColor White
Write-Host "   It should show as loaded with today's date" -ForegroundColor White
Write-Host ""
Write-Host "   Then type:" -ForegroundColor White
Write-Host "   VesselStudioShowToolbar" -ForegroundColor Yellow
Write-Host ""

# Yacht Studio Rhino Plugin Development Guidelines

Auto-generated from all feature plans. Last updated: 2025-10-21

## Active Technologies
- C# (.NET Framework 4.8 or .NET 6+) + RhinoCommon SDK, Eto.Forms (cross-platform UI), System.Net.Http (001-rhino-viewport-sync)
- PowerShell 5.1+ + Yak CLI (Rhino Package Manager), YAML (manifest format), McNeel Package Server API (002-rhino-package-manager)
- C# .NET Framework 4.8 (Rhino 7/8 compatibility) + RhinoCommon SDK 8.x, System.Windows.Forms (UI), System.Drawing (image handling), Newtonsoft.Json (API communication) (003-queued-batch-capture)
- In-memory queue (session-only, cleared on plugin unload) (003-queued-batch-capture)

## Project Structure
```
VesselStudioSimplePlugin/     # C# Rhino plugin source
├── bin/Release/net48/        # Build output
├── Resources/                # Icon files
└── [plugin source files]

dist/                         # Package staging folder (002)
├── VesselStudioSimplePlugin.rhp
├── Newtonsoft.Json.dll
├── icon.png
└── manifest.yml

specs/                        # Feature specifications
├── 001-rhino-viewport-sync/
└── 002-rhino-package-manager/

docs/                         # Documentation
└── guides/

build.ps1                     # Build automation
quick-build.ps1               # Fast build
update-changelog.ps1          # Git analysis
```

## Commands

### PowerShell Build Commands
```powershell
.\build.ps1 -Configuration Release          # Full build with verification
.\quick-build.ps1                           # Fast build (no checks)
.\update-changelog.ps1 -Since "7 days ago"  # Analyze commits
```

### Yak CLI Commands (002-rhino-package-manager)
```powershell
# Authentication
& "C:\Program Files\Rhino 8\System\Yak.exe" login

# Package creation
& "C:\Program Files\Rhino 8\System\Yak.exe" spec    # Generate manifest
& "C:\Program Files\Rhino 8\System\Yak.exe" build   # Build .yak package

# Publishing
& "C:\Program Files\Rhino 8\System\Yak.exe" push <package.yak> --source https://test.yak.rhino3d.com
& "C:\Program Files\Rhino 8\System\Yak.exe" push <package.yak>  # Production

# Search and management
& "C:\Program Files\Rhino 8\System\Yak.exe" search <query>
& "C:\Program Files\Rhino 8\System\Yak.exe" yank <package> <version>
```

## Code Style
- C# (.NET Framework 4.8): Follow standard conventions, use RhinoCommon patterns
- PowerShell: Use approved verbs (Get-, New-, Set-), PascalCase for functions

## Recent Changes
- 003-queued-batch-capture: Added C# .NET Framework 4.8 (Rhino 7/8 compatibility) + RhinoCommon SDK 8.x, System.Windows.Forms (UI), System.Drawing (image handling), Newtonsoft.Json (API communication)
- 002-rhino-package-manager: Added PowerShell 5.1+ scripting, Yak CLI integration, YAML manifest handling, McNeel Package Server distribution
- 001-rhino-viewport-sync: Added C# (.NET Framework 4.8 or .NET 6+) + RhinoCommon SDK, Eto.Forms (cross-platform UI), System.Net.Http

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->

# BLOCKER: Yak CLI Dependency Issue

**Status**: 🚨 **BLOCKED** - Cannot proceed with MVP (User Story 1: Package Building)  
**Created**: October 21, 2025  
**Impact**: High - Blocks package creation and publication

---

## Problem Statement

The Yak CLI (`C:\Program Files\Rhino 8\System\Yak.exe`) is missing the `DocoptNet.dll` dependency, causing all yak commands to fail:

```
Unhandled exception. System.IO.FileNotFoundException: Could not load file or assembly 'DocoptNet, 
Version=0.0.0.0, Culture=neutral, PublicKeyToken=7a38c71da49a547e'. 
The system cannot find the file specified.
File name: 'DocoptNet, Version=0.0.0.0, Culture=neutral, PublicKeyToken=7a38c71da49a547e'
   at Yak.Cli.Program.Main(String[] args)
```

### Commands Affected
- `yak spec` - Generate manifest.yml (T009)
- `yak build` - Create .yak package (T017)
- `yak push` - Publish package (T022, T027)
- `yak search` - Verify publication (T024, T029)
- `yak login` - OAuth authentication (T020)

---

## Root Cause

**Missing Dependency**: DocoptNet.dll (command-line argument parser library)

**Current Yak Installation**:
```
C:\Program Files\Rhino 8\System\
├── Yak.exe (166,016 bytes) ✅
├── Yak.dll (74,368 bytes) ✅
├── Yak.Core.dll (123,520 bytes) ✅
├── Yak.deps.json (17,703 bytes) ✅
├── Yak.runtimeconfig.json (407 bytes) ✅
└── DocoptNet.dll ❌ MISSING
```

**Expected**: DocoptNet.dll should be bundled with Rhino 8 in the same directory.

---

## Impact Assessment

### Blocked Tasks (MVP Critical)
- ✅ T001-T016: File preparation and manifest creation (COMPLETED manually)
- 🚨 T017: Build .yak package (BLOCKED - requires `yak build`)
- 🚨 T018-T019: Verify package creation (BLOCKED - depends on T017)
- 🚨 T020-T021: OAuth authentication (BLOCKED - requires `yak login`)
- 🚨 T022-T030: Test and production publishing (BLOCKED - requires `yak push`, `yak search`)

### User Stories Impact
- **US1 (P1)**: Initial Package Publication - **BLOCKED at 53% complete (16/30 tasks)**
- **US2 (P1)**: User Installation - **BLOCKED** (depends on US1 completion)
- **US3 (P2)**: Publishing Updates - **BLOCKED** (depends on US1)
- **US4 (P3)**: Automated Build Integration - **BLOCKED** (depends on US1)

---

## Completed Work

### ✅ Successfully Implemented (T001-T016)

1. **Distribution Folder Setup**
   - Created `dist/` folder
   - Added `dist/` to `.gitignore`
   - Verified Yak CLI executable exists (but not functional)

2. **File Preparation**
   - Copied `VesselStudioSimplePlugin.rhp` (57,856 bytes) to dist/
   - Copied `Newtonsoft.Json.dll` (711,952 bytes) to dist/
   - Copied `icon_48.png` → `icon.png` (1,078 bytes) to dist/
   - Verified no excluded DLLs present

3. **Manifest Creation (Manual)**
   - Created `dist/manifest.yml` with all required fields:
     - name: vesselstudio
     - version: 1.0.0
     - authors: [Creata Collective Limited]
     - description: Marketing copy
     - url: https://vesselstudio.io
     - icon: icon.png
     - keywords: vessel, studio, yacht, marine, design, capture, viewport, screenshot, upload, visualization, guid:A1B2C3D4-E5F6-7A8B-9C0D-1E2F3A4B5C6D

### 📁 Current dist/ Contents
```
dist/
├── VesselStudioSimplePlugin.rhp (57,856 bytes)
├── Newtonsoft.Json.dll (711,952 bytes)
├── icon.png (1,078 bytes)
└── manifest.yml (complete YAML manifest)
```

**Ready for**: Package building (once Yak CLI is fixed)

---

## Resolution Paths

### Option 1: Fix Yak CLI Installation (RECOMMENDED)

**Action**: Reinstall or repair Rhino 8 to restore missing DocoptNet.dll

**Steps**:
1. Close Rhino 8 completely
2. Open "Programs and Features" (Windows)
3. Find "Rhino 8" → Right-click → "Repair"
4. OR: Download latest Rhino 8 installer from McNeel website
5. Verify `DocoptNet.dll` appears in `C:\Program Files\Rhino 8\System\`
6. Test: `& "C:\Program Files\Rhino 8\System\Yak.exe" --version`

**Timeline**: 15-30 minutes  
**Risk**: Low - Standard repair operation  
**Blocker Resolution**: Complete - Unblocks all yak commands

---

### Option 2: Manual Package Creation (WORKAROUND)

**Action**: Manually create .yak file (ZIP archive with specific structure)

**Steps**:
1. ✅ Already have dist/ folder with all files ready
2. Create ZIP archive: `Compress-Archive -Path dist\* -DestinationPath vesselstudio-1.0.0-rh8_0-win.yak`
3. Manually upload to McNeel's package server via web interface (if available)
4. OR: Contact McNeel support to manually publish package

**Timeline**: Unknown (depends on McNeel support response)  
**Risk**: Medium - Non-standard workflow, may not work  
**Blocker Resolution**: Partial - Unblocks T017-T019 only, still blocked on OAuth/publishing

**Limitations**:
- Cannot authenticate with `yak login` (OAuth still broken)
- Cannot push to servers with `yak push` (publishing blocked)
- Cannot search/verify with `yak search` (validation blocked)
- Update workflow (US3) still requires working Yak CLI

---

### Option 3: Use Alternative Rhino Instance (IF AVAILABLE)

**Action**: If another machine has working Rhino 8 + Yak CLI, copy dist/ folder and run commands there

**Steps**:
1. Copy entire `dist/` folder to machine with working Yak CLI
2. Complete MVP tasks (T017-T030) on that machine
3. Document process for future updates

**Timeline**: 1-2 hours (assuming access to alternative machine)  
**Risk**: Low - Standard workflow on working installation  
**Blocker Resolution**: Complete - Unblocks all tasks

---

### Option 4: Contact McNeel Support (ESCALATION)

**Action**: Report bug to McNeel and request assistance

**Contact**:
- Email: support@mcneel.com
- Forum: https://discourse.mcneel.com/
- Include: Rhino version, yak.exe details, error message, DocoptNet.dll missing

**Timeline**: Days to weeks (depends on support response)  
**Risk**: High - No control over timeline  
**Blocker Resolution**: Complete - McNeel may provide DocoptNet.dll or fixed Yak.exe

---

## Recommended Next Steps

1. **IMMEDIATE (15-30 min)**: Try Option 1 - Repair Rhino 8 installation
   - This is most likely to succeed quickly
   - Standard procedure, low risk

2. **IF OPTION 1 FAILS (1-2 hours)**: Try Option 3 - Use alternative machine
   - Ask team if anyone else has working Rhino 8 + Yak CLI
   - Complete MVP publishing from their machine
   - Document process for reproducibility

3. **IF OPTIONS 1-3 FAIL (async)**: Escalate to Option 4 - McNeel Support
   - While waiting for support, document manual workflow
   - Consider if manual ZIP creation (Option 2) is worth testing
   - Update roadmap with revised timeline

4. **PARALLEL WORK**: Continue with non-blocked tasks
   - Begin documentation tasks (Phase 6: T129-T142)
   - Draft PACKAGE_MANAGER_GUIDE.md with manual process
   - Update BUILD_GUIDE.md with Yak CLI troubleshooting

---

## Testing Checklist for Yak CLI Fix

After Yak CLI is repaired, verify these commands work:

```powershell
# Test 1: Version check
& "C:\Program Files\Rhino 8\System\Yak.exe" --version
# Expected: Yak version number displayed

# Test 2: Spec generation (dry run)
cd dist
& "C:\Program Files\Rhino 8\System\Yak.exe" spec
# Expected: No DocoptNet error, manifest.yml questions appear

# Test 3: Build package
& "C:\Program Files\Rhino 8\System\Yak.exe" build
# Expected: vesselstudio-1.0.0-rh8_0-win.yak created

# Test 4: Login (will open browser)
& "C:\Program Files\Rhino 8\System\Yak.exe" login
# Expected: Browser opens to Rhino Accounts OAuth page

# Test 5: Search (no auth required)
& "C:\Program Files\Rhino 8\System\Yak.exe" search --all grasshopper
# Expected: Search results displayed
```

---

## Files Modified

- ✅ `.gitignore` - Added `dist/` entry
- ✅ `dist/manifest.yml` - Created complete manifest
- ✅ `dist/VesselStudioSimplePlugin.rhp` - Copied from bin/Release
- ✅ `dist/Newtonsoft.Json.dll` - Copied from bin/Release
- ✅ `dist/icon.png` - Copied from Resources
- ✅ `specs/002-rhino-package-manager/tasks.md` - Marked T001-T016 complete

---

## Communication to Stakeholders

**For User/Client**:
> We've completed 53% of the MVP work (package file preparation). We're currently blocked by a missing dependency in the Rhino 8 installation (DocoptNet.dll). We're attempting to repair the installation, which should take 15-30 minutes. If that doesn't work, we can complete the publishing on another machine with a working Rhino 8 installation. No code changes needed - this is purely an environment issue.

**For Development Team**:
> Yak CLI is missing DocoptNet.dll. dist/ folder is fully prepared and ready for packaging (rhp, dll, manifest.yml, icon all in place). Need working yak CLI to proceed with `yak build`, `yak login`, `yak push`. Try repair install first, escalate to McNeel support if needed. Can also complete on different dev machine with working Yak.

---

**Last Updated**: October 21, 2025  
**Next Review**: After Yak CLI repair attempt  
**Owner**: Development Team  
**Priority**: 🔥 Critical - Blocks MVP delivery

# Vessel Studio Rhino Plugin - Quick Reference

## Version 1.0.0

### Available Commands

#### 🎯 VesselCapture
**Purpose:** Capture current viewport and upload to Vessel Studio  
**Usage:** Type `VesselCapture` in Rhino command line  
**Features:**
- Captures active viewport screenshot
- Includes metadata (viewport name, display mode, camera position)
- Auto-selects or prompts for project
- Shows upload progress
- Displays success message with link

#### 🔑 VesselSetApiKey
**Purpose:** Configure your Vessel Studio API key  
**Usage:** Type `VesselSetApiKey` in Rhino command line  
**Features:**
- Opens settings dialog
- Secure credential storage
- Validates API key format
- Tests connection to Vessel Studio

#### ℹ️ VesselStatus
**Purpose:** View plugin connection status  
**Usage:** Type `VesselStatus` in Rhino command line  
**Shows:**
- API key configuration status
- Current user information
- Connection health
- Last sync time

#### ℹ️ VesselAbout
**Purpose:** View version info, changelog, and help  
**Usage:** Type `VesselAbout` in Rhino command line  
**Tabs:**
- **About**: Version info, release date, links
- **Changelog**: Full version history
- **Commands**: Detailed command help

---

## First-Time Setup

1. **Get API Key**
   - Log into Vessel Studio at https://vesselstudio.io
   - Navigate to Settings → Rhino Plugin
   - Generate a new API key
   - Copy the key

2. **Configure Plugin**
   - In Rhino, type `VesselSetApiKey`
   - Paste your API key
   - Click "Save"
   - Plugin will test connection

3. **Capture Your First Screenshot**
   - Open or create a Rhino model
   - Set up your viewport view
   - Type `VesselCapture`
   - Select your project
   - Screenshot uploads automatically

---

## Current Features (v1.0.0)

### ✅ Core Features
- Viewport screenshot capture
- API key authentication
- Project selection
- Automatic upload
- Metadata capture
- Settings management
- Multi-format support (PNG/JPEG)
- Cross-platform UI (Windows/Mac)

### 📊 Metadata Captured
- Viewport name
- Display mode (Wireframe, Shaded, Rendered, etc.)
- Camera position and direction
- Camera target point
- Viewport dimensions
- Rhino version
- Capture timestamp

### 🔒 Security
- Secure credential storage using OS encryption
- API key validation
- HTTPS communication
- No password storage

---

## Planned Features (Roadmap)

### 🚀 Coming Soon
- Chat UI for AI-assisted design
- Real-time viewport sync across team
- Batch capture for multiple viewports
- Screenshot history and management
- Custom metadata tags
- Annotation tools

### 💡 Under Consideration
- Automatic capture on view changes
- Version comparison views
- Integration with Rhino's SaveSmall
- Export to various formats
- Custom upload triggers

---

## Troubleshooting

### "API Key not configured"
- Run `VesselSetApiKey` to configure
- Make sure you copied the full key
- Check for extra spaces

### "Connection failed"
- Check internet connection
- Verify API key is still valid
- Try `VesselStatus` to diagnose

### "No active viewport"
- Make sure you have a Rhino document open
- Click into a viewport before capturing

### Plugin not loading
- Check Rhino version (requires Rhino 8+)
- Verify plugin is installed: `PlugInManager`
- Try uninstalling and reinstalling

---

## Support

- **Email:** support@vesselstudio.com
- **Website:** https://vesselstudio.io
- **Version:** 1.0.0
- **Released:** October 21, 2025

---

## Version Management

### For Developers

**Changelog Script:**
```powershell
# Analyze recent changes
.\update-changelog.ps1 -Analyze

# Get changelog suggestions
.\update-changelog.ps1 -Suggest

# Check specific range
.\update-changelog.ps1 -Suggest -Since HEAD~20
.\update-changelog.ps1 -Suggest -Since v1.0.0
```

**Version Update Process:**
1. Make changes and commit
2. Run `.\update-changelog.ps1 -Suggest` to check if update needed
3. Update `CHANGELOG.md` with significant changes
4. Update version in `VesselStudioPlugin\Models\PluginVersion.cs`
5. Update version in `VesselStudioPlugin.csproj`
6. Commit with version tag: `git tag v1.x.x`

**Version Numbering:**
- **Major (1.x.x)**: Breaking changes, major new features
- **Minor (x.1.x)**: New features, backwards compatible
- **Patch (x.x.1)**: Bug fixes, small improvements

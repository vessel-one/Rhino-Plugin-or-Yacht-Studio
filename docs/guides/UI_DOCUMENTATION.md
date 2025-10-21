# Vessel Studio Rhino Plugin - UI Documentation

## Overview

The Vessel Studio Rhino plugin now includes two user-friendly interfaces for users who prefer GUI over command-line:

1. **Top Menu Bar** - "Vessel Studio" menu alongside File, Edit, View, etc.
2. **Toolbar Panel** - Dockable panel with large buttons and status display

---

## 1. Menu Bar Integration

### Location
The **"Vessel Studio"** menu appears in Rhino's top menu bar, inserted before the "Help" menu.

### Menu Structure
```
Vessel Studio
├── Set API Key...           (VesselSetApiKey command)
├── ─────────────────
├── Capture Screenshot...    (VesselCapture command)
├── Quick Capture            (VesselQuickCapture command)
├── ─────────────────
├── Status                   (VesselStudioStatus command)
├── ─────────────────
└── Help & Documentation     (VesselStudioHelp command)
```

### Menu Items

| Menu Item | Description | Shortcut Command |
|-----------|-------------|------------------|
| **Set API Key...** | Configure your Vessel One API key | `VesselSetApiKey` |
| **Capture Screenshot...** | Open dialog to capture and upload | `VesselCapture` |
| **Quick Capture** | Instant capture to last project | `VesselQuickCapture` |
| **Status** | Check connection and configuration | `VesselStudioStatus` |
| **Help & Documentation** | Open docs or show command list | `VesselStudioHelp` |

### Visual Design
```
┌─────────────────────────────────────────────────┐
│ File  Edit  View  Curve  Surface  Vessel Studio  Help │
│                                      └─────────┘      │
│                                           │           │
│              ┌────────────────────────────┼──┐        │
│              │ Vessel Studio              │  │        │
│              ├────────────────────────────┤  │        │
│              │ ⚙ Set API Key...           │  │        │
│              ├────────────────────────────┤  │        │
│              │ 📷 Capture Screenshot...    │  │        │
│              │ ⚡ Quick Capture            │  │        │
│              ├────────────────────────────┤  │        │
│              │ ✓ Status                   │  │        │
│              ├────────────────────────────┤  │        │
│              │ 📖 Help & Documentation     │  │        │
│              └────────────────────────────┘  │        │
└─────────────────────────────────────────────────┘
```

---

## 2. Toolbar Panel

### Accessing the Panel

**Method 1:** Command line
```
VesselStudioShowToolbar
```

**Method 2:** Rhino Panels Menu
```
Panels → Vessel Studio
```

### Panel Layout

```
┌─────────────────────────────────┐
│  Vessel Studio                  │
├─────────────────────────────────┤
│                                 │
│  ┌─────────────────────────┐   │
│  │ ✓ Connected             │   │
│  │ Last project: MyProject │   │
│  └─────────────────────────┘   │
│                                 │
│  ┌─────────────────────────┐   │
│  │   ⚙ Set API Key         │   │ ← Blue button
│  └─────────────────────────┘   │
│                                 │
│  ┌─────────────────────────┐   │
│  │   📷 Capture Screenshot  │   │ ← Green button
│  └─────────────────────────┘   │
│                                 │
│  ┌─────────────────────────┐   │
│  │   ⚡ Quick Capture       │   │ ← Orange button
│  └─────────────────────────┘   │
│                                 │
│  Quick Capture saves to        │
│  the last used project         │
│                                 │
│  📖 View Documentation          │
│                                 │
└─────────────────────────────────┘
```

### Panel Features

#### Status Display
Shows current configuration status with visual indicators:

| Status | Icon | Color | Description |
|--------|------|-------|-------------|
| **Not configured** | ❌ | Red | No API key set yet |
| **API key configured** | ✓ | Blue | Ready to capture, no project yet |
| **Connected** | ✓ | Green | Ready, shows last project name |
| **Error** | ❌ | Red | Error reading configuration |

#### Button States

**Set API Key Button** (Blue)
- Always enabled
- Opens API key configuration dialog
- Updates status after successful configuration

**Capture Screenshot Button** (Green)
- Disabled if no API key configured
- Opens project selection dialog
- Captures active viewport
- Uploads to selected project

**Quick Capture Button** (Orange)
- Disabled if no API key configured
- Disabled if no project has been used yet
- Instant capture without dialogs
- Uses last selected project

### Button Colors

```css
Set API Key:        #4682B4 (Steel Blue)
Capture Screenshot: #4CAF50 (Green)
Quick Capture:      #FF9800 (Orange)
```

---

## 3. User Workflows

### First-Time Setup Workflow

```
┌─────────────────────────────────────────────────┐
│  1. User installs plugin                        │
│     ↓                                            │
│  2. Menu "Vessel Studio" appears in menu bar    │
│     ↓                                            │
│  3. User clicks "Set API Key..." from menu      │
│     OR clicks blue button in toolbar panel      │
│     ↓                                            │
│  4. Dialog prompts for API key                  │
│     ↓                                            │
│  5. Plugin validates key with backend           │
│     ↓                                            │
│  6. Status shows "✓ API key configured"         │
│     ↓                                            │
│  7. Capture buttons now enabled                 │
└─────────────────────────────────────────────────┘
```

### Regular Capture Workflow

**Option A: Using Menu**
```
Menu → Vessel Studio → Capture Screenshot...
  ↓
Dialog opens with project dropdown
  ↓
Select project, enter name
  ↓
Click "Capture & Upload"
  ↓
Success! Image in gallery
```

**Option B: Using Toolbar**
```
Click 📷 Capture Screenshot button
  ↓
Dialog opens with project dropdown
  ↓
Select project, enter name
  ↓
Click "Capture & Upload"
  ↓
Success! Image in gallery
```

**Option C: Quick Capture (Power Users)**
```
Click ⚡ Quick Capture button
  ↓
Instant capture, no dialog
  ↓
Auto-uploads to last project
  ↓
Success notification
```

---

## 4. Status Indicators

### Panel Status Examples

#### Not Configured
```
┌─────────────────────────┐
│ ❌ Not configured        │
│ Set your API key to     │
│ get started             │
└─────────────────────────┘
```

#### Ready (No Project Yet)
```
┌─────────────────────────┐
│ ✓ API key configured    │
│ Ready to capture        │
└─────────────────────────┘
```

#### Connected (Active Project)
```
┌─────────────────────────┐
│ ✓ Connected             │
│ Last project:           │
│ Superyacht Design 2025  │
└─────────────────────────┘
```

---

## 5. Error Handling

### No API Key Set

When user clicks **Capture Screenshot** without API key:
```
┌─────────────────────────────────────┐
│  API Key Required                   │
├─────────────────────────────────────┤
│  Please set your API key first      │
│  using the '⚙ Set API Key' button.  │
│                                      │
│              [ OK ]                  │
└─────────────────────────────────────┘
```

### No Project Selected (Quick Capture)

When user clicks **Quick Capture** before using regular capture:
```
┌─────────────────────────────────────┐
│  No Project Selected                │
├─────────────────────────────────────┤
│  Please use 'Capture Screenshot'    │
│  at least once to select a project. │
│                                      │
│              [ OK ]                  │
└─────────────────────────────────────┘
```

---

## 6. Technical Implementation

### Files Added

| File | Purpose |
|------|---------|
| `VesselStudioMenu.cs` | Menu bar integration |
| `VesselStudioToolbar.cs` | Toolbar registration |
| `VesselStudioToolbarPanel.cs` | Panel UI with buttons |

### Commands Added

| Command | Description |
|---------|-------------|
| `VesselStudioShowToolbar` | Opens the toolbar panel |
| `VesselStudioHelp` | Opens documentation or shows help |

### Integration Points

1. **OnLoad()**: Menu and toolbar are registered when plugin loads
2. **OnShutdown()**: Menu and toolbar are cleaned up when plugin unloads
3. **Panels System**: Uses Rhino's built-in panel docking system
4. **MainMenu()**: Integrates with Rhino's native menu bar

---

## 7. Customization Options

### Panel Position
Users can dock the toolbar panel:
- **Left side** (default)
- **Right side**
- **Floating window**
- **Auto-hide**

### Keyboard Shortcuts
Users can assign custom shortcuts to commands:
```
Tools → Options → Keyboard → Commands → VesselStudio*
```

Suggested shortcuts:
- `Ctrl+Shift+V` → VesselCapture
- `Ctrl+Shift+Q` → VesselQuickCapture
- `Ctrl+Shift+K` → VesselSetApiKey

---

## 8. Accessibility Features

### Color Blind Friendly
- Uses icons with text labels
- Status indicated by ✓/❌ symbols, not just color
- High contrast button designs

### Screen Reader Support
- All buttons have descriptive text
- Status labels are readable
- Tooltips on hover (future enhancement)

### Keyboard Navigation
- Tab order follows logical flow
- Enter key activates focused button
- Esc closes dialogs

---

## 9. Future Enhancements

### Planned Features
- [ ] Toolbar button tooltips on hover
- [ ] Recent projects dropdown in panel
- [ ] Thumbnail preview of last capture
- [ ] Upload progress indicator
- [ ] Keyboard shortcuts displayed in menu
- [ ] Custom icon set for buttons
- [ ] Dark mode theme support
- [ ] Multi-viewport capture option

---

## 10. User Documentation

### Quick Start Guide

**For GUI Users:**
1. Install plugin
2. Click **Vessel Studio** menu → **Set API Key**
3. Paste your API key from Vessel One
4. Click **Capture Screenshot** from menu or toolbar
5. Select your project
6. Done! 🎉

**For Power Users:**
1. Setup API key once
2. Use **Quick Capture** (⚡) for rapid-fire screenshots
3. All captures go to your last-used project
4. No dialogs, just instant uploads

### Tips & Tricks

- **Dock the panel** on your right side for easy access
- **Use Quick Capture** when iterating on designs
- **Menu access** is great for first-time or occasional users
- **Check status** indicator to verify connection
- **Last project name** reminds you where captures will go

---

## Support

For issues or questions:
- Documentation: https://vessel.one/docs/rhino-plugin
- Support: support@vessel.one
- GitHub: https://github.com/vessel-one/rhino-plugin

---

**Version:** 1.0.0  
**Last Updated:** October 20, 2025  
**Compatibility:** Rhino 8+

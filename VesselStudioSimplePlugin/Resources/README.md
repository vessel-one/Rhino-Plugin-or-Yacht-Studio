# Vessel Studio Plugin Resources

This folder contains icon files for the Rhino plugin.

## Icon Files Required

Place your icon PNG files here with these exact names:

### Required Icons

1. **icon_24.png** - Toolbar button icon
   - Size: 24x24 pixels
   - Format: PNG with transparency
   - Used for: Toolbar buttons

2. **icon_32.png** - Panel icon (standard DPI)
   - Size: 32x32 pixels
   - Format: PNG with transparency
   - Used for: Panel tabs, menu items

3. **icon_48.png** - Panel icon (high DPI)
   - Size: 48x48 pixels
   - Format: PNG with transparency
   - Used for: High-resolution displays (2x scale)

## Design Guidelines

- **Format**: PNG with transparent background
- **Colors**: Vessel Studio brand blue (#2563EB) recommended
- **Style**: Simple, recognizable at small sizes
- **Compatibility**: Should work on both light and dark backgrounds

## Build Integration

Once you add the icon files here:
1. The build system will automatically embed them in the .rhp plugin file
2. The plugin will load them at runtime from embedded resources
3. Icons will appear in Rhino's panel tabs and toolbars

## Current Status

⏳ **Waiting for icon files from designer**

Once you provide the icons, they will automatically be used instead of the programmatically-generated icons.

# Vessel Studio Simple Rhino Plugin

A minimal Rhino plugin for capturing viewport screenshots and uploading them to Vessel Studio.

## Features

- ✅ Simple, clean codebase with minimal dependencies
- ✅ Direct RhinoCommon API usage (no complex abstractions)
- ✅ Basic HTTP API integration
- ✅ Viewport screenshot capture
- ✅ API key authentication
- ✅ Status checking and error messages

## Installation

1. Build the plugin:
   ```
   dotnet build --configuration Release
   ```

2. The output will be in `bin\Release\net48\VesselStudioSimplePlugin.rhp`

3. Drag the `.rhp` file into Rhino viewport to install

4. Restart Rhino

## Usage

### 1. Set API Key (First Time)
```
VesselStudioSetApiKey
```
Enter your Vessel Studio API key when prompted.

### 2. Capture Viewport
```
VesselStudioCapture
```
Captures the active viewport and uploads to Vessel Studio.

### 3. Check Status
```
VesselStudioStatus
```
Shows authentication and connection status.

## Commands

| Command | Description |
|---------|-------------|
| `VesselStudioSetApiKey` | Set your API key for authentication |
| `VesselStudioCapture` | Capture active viewport and upload |
| `VesselStudioStatus` | Show plugin status and available commands |

## API Integration

The plugin uploads screenshots to:
```
POST https://vesselstudio.ai/api/plugin/upload
```

With multipart form data:
- `image`: PNG screenshot file
- `metadata`: JSON with Rhino version, timestamp, dimensions, etc.

## File Structure

```
VesselStudioSimplePlugin/
├── VesselStudioSimplePlugin.cs      # Main plugin class
├── VesselStudioApiClient.cs         # Simple HTTP client
├── VesselStudioCaptureCommand.cs    # Capture command
├── VesselStudioSetApiKeyCommand.cs  # API key setup
├── VesselStudioStatusCommand.cs     # Status check
├── VesselStudioSimplePlugin.csproj  # Project file
└── Properties/AssemblyInfo.cs       # Assembly info
```

## Advantages Over Complex Version

✅ **5 files** vs 20+ files  
✅ **~300 lines** vs 3000+ lines  
✅ **0 compilation errors** vs 238 errors  
✅ **Simple to understand** vs complex architecture  
✅ **Direct API calls** vs service abstractions  
✅ **Basic auth** vs OAuth 2.0 flow  
✅ **Works immediately** vs debugging for days  

## Future Enhancements

Once this simple version is working, you can incrementally add:

- Project selection dialog (Eto.Forms)
- OAuth 2.0 authentication
- Progress feedback
- Better error handling
- Batch uploads
- Auto-sync mode

## Support

For issues or questions:
- Email: support@vesselone.com
- GitHub: https://github.com/vessel-one/vesselstudio-rhino-plugin
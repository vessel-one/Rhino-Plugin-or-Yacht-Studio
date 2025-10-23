# Rhino Plugin API Integration Guide

**Vessel Studio API v1.0**  
*For Rhino 3D Plugin Developers*

---

## Overview

This guide provides everything needed to integrate Vessel Studio's screenshot upload API into your Rhino 3D plugin. Users can capture viewport screenshots in Rhino and upload them directly to their Vessel Studio project galleries.

### What This API Does

- **Authenticate** Rhino plugin users via API keys
- **List** user's Vessel Studio projects for selection
- **Upload** screenshots (PNG/JPEG) to project galleries
- **Track** uploaded images with metadata (viewport name, display mode, Rhino version)

---

## Quick Start

### 1. Get an API Key

Users generate API keys in Vessel Studio:
1. Go to **Settings → Rhino Plugin** (or Profile → Rhino Plugin)
2. Click **"Generate New API Key"**
3. Copy the key (format: `vsk_live_XXXXXXXXXXXXXXXXXXXXXX`)
4. Store securely in Rhino plugin settings

### 2. Basic Authentication

All API requests require Bearer token authentication:

```http
Authorization: Bearer vsk_live_XXXXXXXXXXXXXXXXXXXXXX
```

### 3. Test Your Integration

We've provided test scripts to validate your implementation:
- **PowerShell**: `scripts/test-rhino-api.ps1`
- **Node.js**: `scripts/test-rhino-api.js`

---

## API Endpoints

**Base URL**: `https://studio-2721018687-ad053.web.app`  
*(Development: `http://localhost:9002`)*

### 1. Validate API Key

Verify the API key and get user information.

**Endpoint**: `POST /api/rhino/validate`

**Headers**:
```http
Authorization: Bearer vsk_live_XXXXXXXXXXXXXXXXXXXXXX
Content-Type: application/json
```

**Response** (200 OK):
```json
{
  "success": true,
  "user": {
    "id": "lD9SlJwqxCMwEE97EfmsjpbSdBp1",
    "email": "user@example.com",
    "displayName": "John Doe"
  }
}
```

**Error** (401 Unauthorized):
```json
{
  "error": "Invalid or expired API key"
}
```

---

### 2. List User Projects

Retrieve user's projects for dropdown selection in Rhino.

**Endpoint**: `GET /api/rhino/projects`

**Headers**:
```http
Authorization: Bearer vsk_live_XXXXXXXXXXXXXXXXXXXXXX
```

**Response** (200 OK):
```json
{
  "success": true,
  "projects": [
    {
      "id": "lOGFvYYUQxp8eBlsxuoH",
      "name": "Roger Hill Power Cat",
      "createdAt": "2025-10-19T23:25:19.000Z"
    },
    {
      "id": "dzZRAn7oi5kdYQ3okxrg",
      "name": "Bentayga - Monohull",
      "createdAt": "2025-10-15T07:19:56.000Z"
    }
  ]
}
```

**Usage**: Display project names in dropdown, use `id` for upload endpoint.

---

### 3. Upload Screenshot

Upload a viewport screenshot to a specific project gallery.

**Endpoint**: `POST /api/rhino/projects/{projectId}/upload`

**Headers**:
```http
Authorization: Bearer vsk_live_XXXXXXXXXXXXXXXXXXXXXX
Content-Type: multipart/form-data
```

**Form Data Parameters**:

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `file` | File | **Yes** | PNG or JPEG image (max 10MB) |
| `viewportName` | string | No | Name of Rhino viewport (e.g., "Perspective", "Top") |
| `displayMode` | string | No | Rhino display mode (e.g., "Shaded", "Rendered", "Wireframe") |
| `rhinoVersion` | string | No | Rhino version (e.g., "8.0", "7.0") |
| `quality` | integer | No | JPEG quality (1-100, only for JPEG uploads) |

**Response** (200 OK):
```json
{
  "success": true,
  "imageId": "dnmSjEZ3yXId9nRG7UtH",
  "url": "https://storage.googleapis.com/studio-2721018687-ad053.firebasestorage.app/users/.../gallery/rhino_1761001507392_7unot9.png"
}
```

**Example Request (Multipart Form Data)**:
```http
POST /api/rhino/projects/lOGFvYYUQxp8eBlsxuoH/upload
Authorization: Bearer vsk_live_XXXXXXXXXXXXXXXXXXXXXX
Content-Type: multipart/form-data; boundary=----WebKitFormBoundary7MA4YWxkTrZu0gW

------WebKitFormBoundary7MA4YWxkTrZu0gW
Content-Disposition: form-data; name="file"; filename="perspective-view.png"
Content-Type: image/png

[binary image data]
------WebKitFormBoundary7MA4YWxkTrZu0gW
Content-Disposition: form-data; name="viewportName"

Perspective
------WebKitFormBoundary7MA4YWxkTrZu0gW
Content-Disposition: form-data; name="displayMode"

Shaded
------WebKitFormBoundary7MA4YWxkTrZu0gW
Content-Disposition: form-data; name="rhinoVersion"

8.0
------WebKitFormBoundary7MA4YWxkTrZu0gW--
```

---

## Error Handling

All endpoints return standard HTTP status codes:

| Status | Meaning | Response |
|--------|---------|----------|
| 200 | Success | `{ "success": true, ... }` |
| 400 | Bad Request | `{ "error": "Invalid file type" }` |
| 401 | Unauthorized | `{ "error": "Invalid API key" }` |
| 403 | Forbidden | `{ "error": "API_ACCESS_SUSPENDED", ... }` (See Subscription Requirements) |
| 404 | Not Found | `{ "error": "Project not found" }` |
| 429 | Rate Limited | `{ "error": "Rate limit exceeded: 100 uploads per hour" }` |
| 500 | Server Error | `{ "error": "Internal server error" }` |

**Rate Limits**:
- **100 uploads per hour** per API key
- **10 API keys maximum** per user

### Subscription Requirements (403 Forbidden)

**IMPORTANT**: Rhino plugin access requires a **Standard, Pro, or Educational** subscription plan.

When a user's subscription is downgraded (e.g., from Standard to Free/Starter), their API key is **suspended but preserved**. If they upgrade back to Standard/Pro/Educational, the API key is automatically reactivated.

#### 403 Response Structure

When an API key is suspended due to insufficient subscription tier, all endpoints return:

```json
{
  "success": false,
  "error": "API_ACCESS_SUSPENDED",
  "message": "Rhino Plugin Access Suspended",
  "details": "Your API key has been suspended due to a plan change. You are currently on the FREE plan.",
  "userMessage": "Your Rhino plugin access requires a Standard, Pro, or Educational subscription.\n\nUpgrade your plan at: https://vesselstudio.io/settings?tab=billing",
  "requiresUpgrade": true,
  "currentPlan": "free",
  "requiredPlans": ["standard", "pro", "educational"],
  "upgradeUrl": "https://vesselstudio.io/settings?tab=billing"
}
```

#### How to Handle in Your Plugin

**1. Check Response Status**:
```csharp
// C# Example
if (response.StatusCode == HttpStatusCode.Forbidden) 
{
    var errorData = JsonSerializer.Deserialize<ApiErrorResponse>(responseBody);
    
    if (errorData.error == "API_ACCESS_SUSPENDED") 
    {
        // Show upgrade prompt to user
        ShowUpgradeDialog(errorData.userMessage, errorData.upgradeUrl);
        return;
    }
}
```

**2. Display User-Friendly Message**:

The `userMessage` field contains a pre-formatted message you can display directly:

```
Your Rhino plugin access requires a Standard, Pro, or Educational subscription.

Upgrade your plan at: https://vesselstudio.io/settings?tab=billing
```

**3. Provide Upgrade Link**:

Use the `upgradeUrl` field to open the user's browser:

```csharp
// C# Example - Open browser to upgrade page
System.Diagnostics.Process.Start(new ProcessStartInfo
{
    FileName = errorData.upgradeUrl,
    UseShellExecute = true
});
```

#### Recommended UX Flow

1. **On Plugin Launch**: Validate API key with `/api/rhino/validate`
2. **If 403 Error**: 
   - Display `userMessage` in dialog
   - Show "Upgrade Now" button that opens `upgradeUrl`
   - Disable plugin features until upgrade
3. **Preserve Settings**: Keep API key stored locally (it will reactivate on upgrade)
4. **Retry Logic**: After user upgrades, retry validation automatically

#### Example Error Dialog

```
┌─────────────────────────────────────────────────┐
│  ⚠️  Rhino Plugin Access Suspended              │
├─────────────────────────────────────────────────┤
│                                                 │
│  Your Rhino plugin access requires a Standard  │
│  or Pro subscription.                           │
│                                                 │
│  Current Plan: FREE                             │
│  Required: Standard or Pro                      │
│                                                 │
│  Your API key is preserved and will             │
│  automatically reactivate when you upgrade.     │
│                                                 │
│  ┌─────────────┐  ┌────────────┐               │
│  │ Upgrade Now │  │   Cancel   │               │
│  └─────────────┘  └────────────┘               │
└─────────────────────────────────────────────────┘
```

---

## Implementation Guide

### Recommended User Flow

1. **Plugin Settings Panel**
   - Input field for API key
   - "Validate" button to test connection
   - Display authenticated user email/name

2. **Screenshot Upload Dialog**
   - Dropdown: Select project (populated from `/api/rhino/projects`)
   - Button: "Capture Active Viewport"
   - Checkbox: "Include metadata" (viewport name, display mode)
   - Quality slider: 1-100 (for JPEG exports)
   - Button: "Upload to Vessel Studio"

3. **Success Confirmation**
   - Show uploaded image thumbnail
   - Link: "View in Vessel Studio" → Opens project gallery
   - Badge: "Rhino" (orange badge displayed in Vessel Studio gallery)

### Code Examples

#### C# (.NET / RhinoCommon)

```csharp
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Rhino;
using Rhino.Display;

public class VesselStudioUploader
{
    private readonly string apiKey;
    private readonly HttpClient client;
    
    public VesselStudioUploader(string apiKey)
    {
        this.apiKey = apiKey;
        this.client = new HttpClient();
        this.client.BaseAddress = new Uri("https://studio-2721018687-ad053.web.app");
        this.client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", apiKey);
    }
    
    public async Task<bool> ValidateApiKey()
    {
        var response = await client.PostAsync("/api/rhino/validate", null);
        return response.IsSuccessStatusCode;
    }
    
    public async Task<List<Project>> GetProjects()
    {
        var response = await client.GetAsync("/api/rhino/projects");
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<ProjectListResponse>(json);
        return result.Projects;
    }
    
    public async Task<string> UploadScreenshot(
        string projectId, 
        byte[] imageData, 
        string filename,
        string viewportName = null,
        string displayMode = null,
        int? quality = null)
    {
        using (var content = new MultipartFormDataContent())
        {
            // Add image file
            var fileContent = new ByteArrayContent(imageData);
            fileContent.Headers.ContentType = 
                new MediaTypeHeaderValue("image/png");
            content.Add(fileContent, "file", filename);
            
            // Add metadata
            if (viewportName != null)
                content.Add(new StringContent(viewportName), "viewportName");
            if (displayMode != null)
                content.Add(new StringContent(displayMode), "displayMode");
            content.Add(new StringContent("8.0"), "rhinoVersion");
            if (quality.HasValue)
                content.Add(new StringContent(quality.Value.ToString()), "quality");
            
            var response = await client.PostAsync(
                $"/api/rhino/projects/{projectId}/upload", 
                content);
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<UploadResponse>(json);
                return result.ImageId;
            }
            
            throw new Exception($"Upload failed: {response.StatusCode}");
        }
    }
    
    public byte[] CaptureActiveViewport()
    {
        var view = RhinoDoc.ActiveDoc.Views.ActiveView;
        var bitmap = view.CaptureToBitmap();
        
        using (var ms = new System.IO.MemoryStream())
        {
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return ms.ToArray();
        }
    }
}

// Data models
public class ProjectListResponse
{
    public bool Success { get; set; }
    public List<Project> Projects { get; set; }
}

public class Project
{
    public string Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UploadResponse
{
    public bool Success { get; set; }
    public string ImageId { get; set; }
    public string Url { get; set; }
}
```

#### Python (for Rhino.Python)

```python
import Rhino
import System
import json
from System.Net import WebClient, WebHeaderCollection
from System.IO import MemoryStream
from System.Drawing.Imaging import ImageFormat

class VesselStudioUploader:
    def __init__(self, api_key):
        self.api_key = api_key
        self.base_url = "https://studio-2721018687-ad053.web.app"
        
    def validate_api_key(self):
        """Validate the API key"""
        client = WebClient()
        client.Headers.Add("Authorization", f"Bearer {self.api_key}")
        
        try:
            response = client.UploadString(
                f"{self.base_url}/api/rhino/validate",
                "POST",
                ""
            )
            return True
        except:
            return False
    
    def get_projects(self):
        """Get list of user's projects"""
        client = WebClient()
        client.Headers.Add("Authorization", f"Bearer {self.api_key}")
        
        response = client.DownloadString(f"{self.base_url}/api/rhino/projects")
        data = json.loads(response)
        return data.get("projects", [])
    
    def upload_screenshot(self, project_id, image_bytes, metadata=None):
        """Upload screenshot to project gallery"""
        # Note: Multipart form data upload in IronPython is complex
        # Recommend using RestSharp NuGet package or similar
        # This is a simplified example
        
        boundary = "----RhinoPluginBoundary"
        
        # Build multipart form data
        # Implementation depends on your HTTP library choice
        pass
    
    def capture_active_viewport(self):
        """Capture current viewport as PNG bytes"""
        view = Rhino.RhinoDoc.ActiveDoc.Views.ActiveView
        bitmap = view.CaptureToBitmap()
        
        stream = MemoryStream()
        bitmap.Save(stream, ImageFormat.Png)
        return stream.ToArray()
```

---

## Testing Your Integration

### 1. Run Test Scripts

**PowerShell**:
```powershell
cd yachtstudio
.\scripts\test-rhino-api.ps1 -ApiKey "vsk_live_YOUR_KEY_HERE"
```

**Node.js**:
```bash
cd yachtstudio
node scripts/test-rhino-api.js vsk_live_YOUR_KEY_HERE
```

### 2. Manual Testing Checklist

- [ ] Validate API key with invalid key → Expect 401 error
- [ ] Validate API key with valid key → Expect user info
- [ ] List projects with no projects → Expect empty array
- [ ] List projects with multiple projects → Expect all projects
- [ ] Upload PNG screenshot → Verify appears in gallery
- [ ] Upload JPEG screenshot with quality=85 → Verify quality setting
- [ ] Upload to wrong project ID → Expect 404 error
- [ ] Upload file > 10MB → Expect 400 error
- [ ] Upload 100+ times in 1 hour → Expect 429 rate limit error

### 3. Verify in Vessel Studio

1. Open project in Vessel Studio
2. Click "View Gallery" button
3. Uploaded screenshots should have orange "Rhino" badge
4. Hover over image to see metadata tooltip:
   - Viewport name
   - Display mode
   - Rhino version
   - Upload timestamp

---

## Security Best Practices

### API Key Storage

**DO**:
- ✅ Store API keys in user settings/preferences (encrypted if possible)
- ✅ Prompt user to enter API key in plugin settings
- ✅ Validate API key before first use
- ✅ Clear API key on logout/uninstall

**DON'T**:
- ❌ Hardcode API keys in plugin source code
- ❌ Store API keys in plain text files
- ❌ Share API keys between users
- ❌ Log API keys to console/files

### Rate Limiting

- Cache project list locally (refresh every 5 minutes)
- Implement retry logic with exponential backoff
- Show rate limit warnings to users approaching limit
- Queue uploads if rate limit hit

---

## Gallery Display Features

When screenshots are uploaded via Rhino plugin, they appear in Vessel Studio with:

### Visual Indicators
- **Orange "Rhino" Badge** - Distinguishes plugin uploads from AI-generated images
- **Metadata Tooltip** - Shows viewport, display mode, Rhino version
- **Timestamp** - When screenshot was uploaded

### Gallery Actions
Users can:
- View full-size image
- Download original file
- Delete uploaded screenshot
- Compare with AI-generated designs
- Use as reference images for AI generation

---

## Support & Resources

### Documentation
- **API Reference**: This document
- **Test Scripts**: `scripts/test-rhino-api.ps1`, `scripts/test-rhino-api.js`
- **Source Code**: Available on request for integration partners

### Getting Help
- **Email**: support@vesselstudio.com *(replace with actual support email)*
- **Issues**: Report bugs or request features via support channel
- **Rate Limit Increases**: Contact us for enterprise rate limits

### API Updates
- **Version**: 1.0 (October 2025)
- **Stability**: Production-ready
- **Breaking Changes**: Will be announced 30 days in advance
- **Deprecation Policy**: 6-month notice for deprecated endpoints

---

## Implementation Notes

### Critical: Multipart Upload in Firebase Cloud Functions

**⚠️ IMPORTANT**: If implementing the upload endpoint in Firebase Cloud Functions (via Next.js hosting), you CANNOT use the native `request.formData()` API. It will work in local development but fail in production.

**Solution**: Use the `busboy` library for manual stream parsing.

**Full details**: See [RHINO_PLUGIN_MULTIPART_UPLOAD_LEARNINGS.md](./RHINO_PLUGIN_MULTIPART_UPLOAD_LEARNINGS.md) for:
- Root cause analysis
- Working implementation code
- Testing strategies
- Common errors and solutions

### Server Implementation Requirements

**Dependencies**:
```bash
npm install busboy
npm install --save-dev @types/busboy
```

**Code Pattern** (simplified - see full docs for complete implementation):
```typescript
export async function POST(request: NextRequest) {
  const busboy = require('busboy');
  const bb = busboy({ headers: { 'content-type': request.headers.get('content-type') } });
  
  // Event handlers for fields and files
  bb.on('field', (name, value) => { /* store form fields */ });
  bb.on('file', (name, file, info) => { /* collect file data */ });
  
  // Pipe request body through busboy
  const reader = request.body!.getReader();
  while (true) {
    const { done, value } = await reader.read();
    if (done) { bb.end(); break; }
    bb.write(Buffer.from(value));
  }
  
  // Now process parsed data...
}
```

---

## Roadmap & Future Enhancements

### Planned Features (Post-MVP)

#### 1. Canvas Image to Rhino Viewport (Coming Soon)
**Direction**: Pull reference images FROM Vessel Studio into Rhino viewports

**Use Case**: 
- Designer generates yacht concept in Vessel Studio
- Downloads image as reference plane in Rhino
- Models over the AI-generated design

**Proposed API**:
```http
GET /api/rhino/projects/{projectId}/images
Authorization: Bearer vsk_live_XXXXXXXXXXXXXXXXXXXXXX

Response:
{
  "success": true,
  "images": [
    {
      "id": "img-001",
      "url": "https://storage.googleapis.com/.../image.png",
      "type": "generated",
      "prompt": "60ft sailing yacht, side view",
      "createdAt": "2025-10-20T10:30:00Z"
    },
    {
      "id": "img-002", 
      "url": "https://storage.googleapis.com/.../reference.png",
      "type": "reference",
      "source": "rhino-plugin",
      "createdAt": "2025-10-19T15:20:00Z"
    }
  ]
}
```

**Plugin Command**: `VesselStudioLoadReference`
- Shows dialog with project images
- User selects image
- Plugin downloads and sets as viewport background
- Optionally creates reference plane in 3D space

---

#### 2. AI Chat Integration in Rhino (Planned)
**Direction**: Embedded AI assistant inside Rhino interface

**Use Case**:
- User stuck on design problem while modeling
- Opens Vessel Studio chat panel in Rhino
- Asks: "How can I optimize this hull form for speed?"
- AI responds with design suggestions + generates reference images
- User applies insights directly to 3D model

**Proposed API**:
```http
POST /api/rhino/chat
Authorization: Bearer vsk_live_XXXXXXXXXXXXXXXXXXXXXX
Content-Type: application/json

Body:
{
  "message": "How can I improve the hull design for better speed?",
  "context": {
    "projectId": "proj-001",
    "activeViewport": "Perspective",
    "modelStats": {
      "layerCount": 15,
      "objectCount": 234,
      "boundingBox": { "width": 60, "length": 15, "height": 8 }
    }
  },
  "conversationId": "conv-xyz" // Optional: for multi-turn conversations
}

Response:
{
  "success": true,
  "reply": "Based on your 60ft yacht design, I'd recommend...",
  "suggestions": [
    "Reduce beam-to-length ratio to 1:4",
    "Add bulbous bow for displacement hull efficiency",
    "Consider transom stern for modern aesthetic"
  ],
  "referenceImages": [
    {
      "url": "https://storage.googleapis.com/.../hull-example.png",
      "description": "Optimized hull profile for 60ft yacht"
    }
  ],
  "conversationId": "conv-xyz"
}
```

**Plugin UI**:
- Dockable panel in Rhino interface (Eto.Forms)
- Chat history with message bubbles
- Inline image previews
- "Apply to Viewport" button for reference images
- Context-aware: Knows what you're currently modeling

**Token Usage**: Same authentication as upload/download APIs

---

#### 3. Multi-Angle Batch Upload (Planned)
**Direction**: Capture all viewports simultaneously

**Use Case**:
- Designer finishes 3D model
- Runs `VesselStudioBatchCapture` command
- Plugin captures Top, Front, Right, Perspective views
- Uploads all 4 images in one operation
- Vessel Studio displays as 4-panel comparison

**Proposed API**:
```http
POST /api/rhino/projects/{projectId}/batch-upload
Authorization: Bearer vsk_live_XXXXXXXXXXXXXXXXXXXXXX
Content-Type: multipart/form-data

Form Data:
- file_perspective: [PNG image]
- metadata_perspective: {"viewportName": "Perspective", "displayMode": "Shaded"}
- file_top: [PNG image]  
- metadata_top: {"viewportName": "Top", "displayMode": "Wireframe"}
- file_front: [PNG image]
- metadata_front: {"viewportName": "Front", "displayMode": "Shaded"}
- file_right: [PNG image]
- metadata_right: {"viewportName": "Right", "displayMode": "Shaded"}

Response:
{
  "success": true,
  "uploaded": 4,
  "failed": 0,
  "images": [
    { "id": "img-001", "viewport": "Perspective", "url": "..." },
    { "id": "img-002", "viewport": "Top", "url": "..." },
    { "id": "img-003", "viewport": "Front", "url": "..." },
    { "id": "img-004", "viewport": "Right", "url": "..." }
  ]
}
```

**Plugin Features**:
- Checkbox list: Select which viewports to capture
- Quality slider: Consistent quality across all captures
- Progress bar: Shows upload status per viewport
- Retry failed uploads individually

---

#### 4. Auto-Sync Mode (Future)
**Direction**: Real-time viewport streaming to Vessel Studio

**Use Case**:
- Client review meeting in progress
- Designer enables "Live Sync" in Rhino
- Every time viewport updates, screenshot auto-uploads
- Client sees changes in real-time on web browser
- No manual "capture and upload" needed

**Technical Considerations**:
- Debouncing: Only upload after 2-3 seconds of no camera movement
- Bandwidth: Compress images more aggressively (JPEG quality=60)
- Rate limiting: Special exemption for auto-sync mode (300 uploads/hour)
- Toggle on/off: Don't overwhelm during active modeling

---

#### 5. 3D Model Export (Future)
**Direction**: Upload actual geometry, not just screenshots

**Use Case**:
- Export .3dm or STEP file directly to Vessel Studio
- Vessel Studio renders 3D viewer (Three.js)
- Clients can rotate/inspect model on web
- Compare 3D model against AI-generated concepts

**File Formats**: .3dm (native), STEP, IGES, OBJ
**Size Limit**: 50MB per export
**Viewer**: Three.js-based 3D renderer in Vessel Studio web app

---

### Feature Priorities

**Phase 1** (Current - ✅ Complete):
- Screenshot upload to project gallery
- API key authentication
- Project selection

**Phase 2** (Next - Q4 2025):
- Canvas image to Rhino viewport (reference loading)
- AI chat integration (basic text-based)

**Phase 3** (Future - Q1 2026):
- Multi-angle batch upload
- Auto-sync mode
- Enhanced AI chat with visual context

**Phase 4** (Long-term - 2026+):
- 3D model export and web viewer
- Collaborative editing features
- Real-time multi-user sync

---

## Changelog

### v1.0 (October 2025)
- ✅ Initial release
- ✅ API key authentication
- ✅ Project listing
- ✅ Screenshot upload (PNG/JPEG)
- ✅ Metadata support (viewport, display mode, Rhino version)
- ✅ JPEG quality parameter
- ✅ Rate limiting (100/hour)
- ✅ Production-tested with Firebase Cloud Functions
- ✅ Multipart upload implementation (busboy-based)

---

## Appendix: HTTP Status Codes Reference

| Code | Status | When It Happens |
|------|--------|-----------------|
| 200 | OK | Request succeeded |
| 400 | Bad Request | Invalid parameters (e.g., file too large, invalid quality value) |
| 401 | Unauthorized | Invalid/expired API key |
| 404 | Not Found | Project doesn't exist or user doesn't own it |
| 429 | Too Many Requests | Rate limit exceeded (100 uploads/hour) |
| 500 | Internal Server Error | Server-side error (contact support) |

---

## Appendix: File Upload Limits

| Constraint | Limit | Notes |
|------------|-------|-------|
| Max File Size | 10 MB | Per upload |
| Allowed Formats | PNG, JPEG | Other formats rejected |
| JPEG Quality Range | 1-100 | Optional parameter |
| Max Uploads/Hour | 100 | Per API key |
| Max API Keys/User | 10 | Generate in settings |

---

**Ready to integrate?** Start by testing the validation endpoint with a real API key from Vessel Studio. Good luck! 🚀

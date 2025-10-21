# Rhino Plugin Upload Implementation

## Current Implementation (As of Oct 21, 2025)

### Overview
The Rhino plugin uploads viewport screenshots to Vessel Studio using `multipart/form-data` format with proper boundary handling.

---

## HTTP Request Details

### Endpoint
```
POST https://vesselstudio.io/api/rhino/projects/{projectId}/upload
```

### Headers
```
Authorization: Bearer {apiKey}
Content-Type: multipart/form-data; boundary=----WebKitFormBoundary... (AUTO-GENERATED)
```

**CRITICAL:** We do NOT manually set the `Content-Type` header. The `MultipartFormDataContent` class automatically generates it with the correct boundary parameter.

---

## Implementation Code

### File: `VesselStudioApiClient.cs`
**Method:** `UploadScreenshotAsync()`

```csharp
public async Task<UploadResult> UploadScreenshotAsync(
    string projectId,
    byte[] imageBytes,
    string imageName,
    Dictionary<string, object> metadata)
{
    try
    {
        // Create multipart form data - HttpClient auto-sets Content-Type with boundary
        using (var formData = new MultipartFormDataContent())
        {
            // 1. Add file - REQUIRED field
            var fileContent = new ByteArrayContent(imageBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            
            var filename = string.IsNullOrWhiteSpace(imageName) 
                ? "capture.png" 
                : $"{imageName}.png";
            
            formData.Add(fileContent, "file", filename);

            // 2. Add metadata fields as individual string fields (all optional)
            if (metadata != null && metadata.Count > 0)
            {
                foreach (var kvp in metadata)
                {
                    var value = kvp.Value?.ToString() ?? "";
                    formData.Add(new StringContent(value), kvp.Key);
                }
            }

            // 3. POST - Content-Type automatically set by MultipartFormDataContent
            var uploadUrl = $"/api/rhino/projects/{projectId}/upload";
            var response = await _httpClient.PostAsync(uploadUrl, formData);
            var responseText = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonConvert.DeserializeObject<dynamic>(responseText);
                return new UploadResult 
                { 
                    Success = true, 
                    Message = "Upload successful",
                    Url = result.url?.ToString() ?? ""
                };
            }
            else
            {
                return new UploadResult 
                { 
                    Success = false, 
                    Message = $"{response.StatusCode}: {responseText}" 
                };
            }
        }
    }
    catch (Exception ex)
    {
        return new UploadResult 
        { 
            Success = false, 
            Message = $"Exception: {ex.Message}" 
        };
    }
}
```

---

## Form Data Structure Sent

### Required Field

| Field Name | Type | Content-Type | Description |
|------------|------|--------------|-------------|
| `file` | ByteArrayContent | `image/png` | PNG image data (max 10MB) |

### Optional Metadata Fields (sent as StringContent)

| Field Name | Example Value | Description |
|------------|---------------|-------------|
| `width` | `"1920"` | Viewport width in pixels |
| `height` | `"1080"` | Viewport height in pixels |
| `viewportName` | `"Perspective"` | Rhino viewport name |
| `displayMode` | `"Shaded"` | Rhino display mode |
| `rhinoVersion` | `"8.10.24228.13001"` | Full Rhino version string |
| `captureTime` | `"2025-10-21T03:42:16Z"` | ISO 8601 UTC timestamp |

### Example Metadata Dictionary (from VesselCaptureCommand.cs)
```csharp
var metadata = new Dictionary<string, object>
{
    ["width"] = viewCapture.ScreenPortSize.Width.ToString(),
    ["height"] = viewCapture.ScreenPortSize.Height.ToString(),
    ["viewportName"] = viewport.Name,
    ["displayMode"] = viewport.DisplayMode.EnglishName,
    ["rhinoVersion"] = RhinoApp.Version.ToString(),
    ["captureTime"] = DateTime.UtcNow.ToString("o")
};
```

---

## Authentication Setup

### HttpClient Configuration
```csharp
public VesselStudioApiClient()
{
    _httpClient = new HttpClient();
    _httpClient.BaseAddress = new Uri("https://vesselstudio.io");
    _httpClient.Timeout = TimeSpan.FromSeconds(30);
}

public void SetApiKey(string apiKey)
{
    _apiKey = apiKey;
    _httpClient.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", apiKey);
}
```

**API Key Format:** `vsk_live_XXXXXXXXXXXXXXXXXXXXXX`

---

## Expected Server Responses

### Success (200 OK)
```json
{
  "success": true,
  "imageId": "rhino_1729481536901_7unot9",
  "url": "https://storage.googleapis.com/studio-2721018687-ad053.firebasestorage.app/users/.../gallery/rhino_1729481536901_7unot9.png"
}
```

### Error (400 Bad Request)
```json
{
  "error": "Failed to parse multipart/form-data. Ensure Content-Type is set correctly."
}
```

### Error (401 Unauthorized)
```json
{
  "error": "Invalid or missing API key"
}
```

### Error (404 Not Found)
```json
{
  "error": "Project not found"
}
```

---

## Logging Output (Console)

The plugin logs detailed information to Rhino's command line:

```
[Upload] Starting upload to project: XjHAytNpQBQs16oKU2DO
[Upload] Image size: 245.73 KB
[Upload] Image filename: Test Capture.png
[Upload] ✓ Added file field: Test Capture.png (251624 bytes, image/png)
[Upload] Adding 6 metadata fields:
[Upload]   ✓ width = 1920
[Upload]   ✓ height = 1080
[Upload]   ✓ viewportName = Perspective
[Upload]   ✓ displayMode = Shaded
[Upload]   ✓ rhinoVersion = 8.10.24228.13001
[Upload]   ✓ captureTime = 2025-10-21T03:42:16.7890123Z
[Upload] POST https://vesselstudio.io/api/rhino/projects/XjHAytNpQBQs16oKU2DO/upload
[Upload] Authorization: Bearer vsk_live_S7Q...
[Upload] Response: 200 OK
[Upload] ✅ Success: {"success":true,"imageId":"rhino_...","url":"https://..."}
```

---

## Key Implementation Points

### ✅ What We Do Right

1. **No Manual Content-Type Header**
   - We let `MultipartFormDataContent` auto-generate the Content-Type with boundary
   - This is the #1 requirement from the backend team

2. **Correct Field Name**
   - File field is named `"file"` (not "image" or "screenshot")

3. **Proper File Content-Type**
   - Set to `image/png` using `MediaTypeHeaderValue`

4. **Individual Metadata Fields**
   - Each metadata key-value pair is sent as a separate form field
   - All values are converted to strings using `ToString()`

5. **Bearer Token Auth**
   - Authorization header properly formatted: `Bearer {apiKey}`

6. **Comprehensive Logging**
   - All form fields logged before upload
   - Full response status and body logged
   - Makes debugging easy

### ❌ What We Avoid

1. **Manual Content-Type Setting**
   - Never use: `request.Headers.Add("Content-Type", "multipart/form-data")`
   - This breaks boundary detection

2. **JSON Metadata**
   - Don't send metadata as a single JSON field
   - Backend expects individual string fields

3. **Wrong Field Names**
   - Don't use "image" or "data" - must be "file"

---

## Testing Checklist

- [x] Remove any manual Content-Type header setting
- [x] Use MultipartFormDataContent class
- [x] Set Authorization: Bearer {apiKey} header
- [x] Set file ContentType to image/png
- [x] Use field name "file" for the upload
- [x] Send metadata as individual string fields
- [x] Log all fields before upload
- [x] Log response status and body

---

## Troubleshooting

### If Upload Still Fails After These Changes:

1. **Check Rhino Command Line Output**
   - Look for [Upload] logs showing exact fields sent
   - Verify boundary is present in Content-Type header

2. **Verify API Key**
   - Format: `vsk_live_XXXXXXXXXXXXXXXXXXXXXX`
   - Test with `/api/rhino/validate` endpoint first

3. **Check Project ID**
   - Ensure project exists and belongs to the authenticated user
   - Test with `/api/rhino/projects` endpoint to list valid IDs

4. **Test with Curl (Backend Team Can Try)**
   ```bash
   curl -X POST https://vesselstudio.io/api/rhino/projects/XjHAytNpQBQs16oKU2DO/upload \
     -H "Authorization: Bearer vsk_live_S7QpHFS0k_lKWHeYL-JyLGZg7CgXtc4h" \
     -F "file=@screenshot.png" \
     -F "width=1920" \
     -F "height=1080" \
     -F "viewportName=Perspective" \
     -F "displayMode=Shaded" \
     -F "rhinoVersion=8.10.24228.13001" \
     -F "captureTime=2025-10-21T03:42:16Z"
   ```

5. **Compare HTTP Logs**
   - Plugin logs show exactly what we're sending
   - Backend should log what it's receiving
   - Compare field names, content types, and structure

---

## Related Files

- **VesselStudioApiClient.cs** (lines 197-275): Upload implementation
- **VesselCaptureCommand.cs** (lines 19-228): Capture logic and metadata creation
- **VesselStudioSettings.cs**: API key storage/retrieval

---

## Contact Info for Backend Team

If issues persist, provide this info:

1. Full [Upload] log output from Rhino command line
2. HTTP status code and response body
3. Project ID being used
4. API key format (first 12 chars: `vsk_live_S7Q...`)
5. Image size in KB
6. Metadata field count and names

---

**Last Updated:** October 21, 2025  
**Plugin Version:** 1.0.0.0  
**Implementation Status:** ✅ Matches backend requirements exactly

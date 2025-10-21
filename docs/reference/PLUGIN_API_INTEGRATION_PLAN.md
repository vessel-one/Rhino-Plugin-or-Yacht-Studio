# Rhino Plugin - Vessel Studio API Integration Plan

**Date:** October 21, 2025  
**Status:** Ready to Implement  
**Estimated Time:** 4-6 hours

---

## Overview

The Vessel Studio backend and UI are working and tested. We need to update the Rhino plugin to send data in the exact format the backend expects.

### Current State
- ✅ Backend deployed: `https://vesselstudio.io`
- ✅ Backend endpoints tested and working
- ✅ UI gallery view displaying uploads with Rhino badge
- ❌ Plugin using wrong endpoint paths
- ❌ Plugin using JWT auth instead of API keys
- ❌ Plugin sending complex metadata JSON instead of simple fields

---

## Key Differences: Current Plugin vs Backend Expectations

| Aspect | Current Plugin | Backend Expects |
|--------|---------------|-----------------|
| **Base URL** | Configurable | `https://vesselstudio.io/api` |
| **Auth Method** | JWT tokens | API key: `Bearer vsk_live_...` |
| **Validation** | N/A | `POST /api/rhino/validate` |
| **Projects List** | `GET /api/projects` | `GET /api/rhino/projects` |
| **Upload** | `POST /api/projects/{id}/screenshots` | `POST /api/rhino/projects/{id}/upload` |
| **Upload Fields** | `metadata` (JSON), `screenshotId`, `compressionType`, `quality`, `image` | `file`, `viewportName`, `displayMode`, `rhinoVersion`, `quality` |
| **Response** | `{ imageId, imageUrl }` | `{ success, imageId, url }` |

---

## Implementation Tasks

### Task 1: Update API Endpoints and Base URL
**Time:** 30 minutes  
**Priority:** HIGH  
**Files:** `Services/ApiClient.cs`

#### Changes Needed:
```csharp
// Change base URL
private readonly string _baseUrl = "https://vesselstudio.io/api";

// Update endpoint paths
- "/api/projects" 
+ "/api/rhino/projects"

- $"/api/projects/{projectId}/screenshots"
+ $"/api/rhino/projects/{projectId}/upload"
```

#### Implementation:
1. Update `_baseUrl` constant in `ApiClient.cs` constructor
2. Change `GetProjectsAsync()` endpoint from `/api/projects` to `/api/rhino/projects`
3. Change `UploadScreenshotAsync()` endpoint from `/api/projects/{id}/screenshots` to `/api/rhino/projects/{id}/upload`
4. Update `GetProjectDetailsAsync()` if needed (verify if backend has this endpoint)

---

### Task 2: Implement API Key Authentication
**Time:** 1.5 hours  
**Priority:** HIGH  
**Files:** `Services/AuthenticationService.cs`, `Services/ApiClient.cs`, `Models/AuthenticationSession.cs`

#### Changes Needed:

**A. Authentication Method**
- Remove JWT token flow
- Add API key validation
- Store API key in plugin settings

**B. New Methods:**
```csharp
// AuthenticationService.cs
public async Task<bool> ValidateApiKeyAsync(string apiKey)
{
    var response = await _apiClient.PostAsync("/api/rhino/validate", null);
    return response.IsSuccessStatusCode;
}

public void SetApiKey(string apiKey)
{
    // Store in settings
    Settings.ApiKey = apiKey;
    Settings.Save();
    
    // Set in API client
    _apiClient.SetAuthenticationToken(apiKey);
}

public string GetApiKey()
{
    return Settings.ApiKey;
}
```

**C. Update `SetAuthenticationToken` method:**
```csharp
public void SetAuthenticationToken(string token)
{
    // Token should be format: vsk_live_XXXXXXXXX
    if (string.IsNullOrEmpty(token))
        throw new ArgumentNullException(nameof(token));
    
    if (!token.StartsWith("vsk_"))
        throw new ArgumentException("Invalid API key format. Expected 'vsk_live_...'");
        
    _httpClient.DefaultRequestHeaders.Authorization = 
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
}
```

#### Implementation Steps:
1. Add `ApiKey` property to `VesselStudioSettings` class
2. Update `AuthenticationService` to use API keys instead of JWT
3. Add `ValidateApiKeyAsync` method to test API key
4. Update `IsAuthenticated` property to check for valid API key
5. Remove JWT token refresh logic (not needed for API keys)

---

### Task 3: Update Upload Form Data Format
**Time:** 1 hour  
**Priority:** HIGH  
**Files:** `Services/ApiClient.cs`

#### Current Implementation:
```csharp
using var form = new MultipartFormDataContent();

// Complex metadata JSON
var metadataJson = JsonSerializer.Serialize(screenshot.Metadata, _jsonOptions);
form.Add(new StringContent(metadataJson, Encoding.UTF8, "application/json"), "metadata");

// Additional fields
form.Add(new StringContent(screenshot.Id), "screenshotId");
form.Add(new StringContent(screenshot.CompressionType.ToString()), "compressionType");
form.Add(new StringContent(screenshot.Quality.ToString()), "quality");

// Image
var imageContent = new ByteArrayContent(screenshot.ImageData);
form.Add(imageContent, "image", screenshot.Filename);
```

#### Required Implementation:
```csharp
using var form = new MultipartFormDataContent();

// Simple string fields extracted from metadata
form.Add(new StringContent(screenshot.Metadata.ViewportName ?? "Perspective"), "viewportName");
form.Add(new StringContent(screenshot.Metadata.DisplayModeName ?? "Shaded"), "displayMode");
form.Add(new StringContent(screenshot.Metadata.RhinoVersion ?? "8.0"), "rhinoVersion");

// Quality (only for JPEG)
if (screenshot.CompressionType == CompressionType.Jpeg && screenshot.Quality.HasValue)
{
    form.Add(new StringContent(screenshot.Quality.Value.ToString()), "quality");
}

// Image file (field name changed from "image" to "file")
var imageContent = new ByteArrayContent(screenshot.ImageData);
imageContent.Headers.ContentType = 
    new System.Net.Http.Headers.MediaTypeHeaderValue(GetContentType(screenshot));
form.Add(imageContent, "file", screenshot.Filename ?? "capture.png");
```

#### Key Changes:
1. **Remove:** `metadata` JSON field
2. **Remove:** `screenshotId` field (backend generates its own)
3. **Remove:** `compressionType` field (backend detects from file)
4. **Add:** `viewportName` as simple string
5. **Add:** `displayMode` as simple string
6. **Add:** `rhinoVersion` as simple string
7. **Change:** `image` field name → `file`
8. **Keep:** `quality` (optional, for JPEG only)

---

### Task 4: Update Response Models
**Time:** 30 minutes  
**Priority:** MEDIUM  
**Files:** `Models/UploadResponse.cs`

#### Current Model:
```csharp
public class UploadResponse
{
    public string ImageId { get; set; }
    public string ImageUrl { get; set; }
}
```

#### Required Model:
```csharp
public class UploadResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("imageId")]
    public string ImageId { get; set; }
    
    [JsonPropertyName("url")]  // Changed from "imageUrl"
    public string Url { get; set; }
    
    // Convenience property for compatibility
    public string ImageUrl => Url;
}
```

#### Update Upload Handler:
```csharp
var response = await SendAuthenticatedRequestAsync<UploadResponse>(
    HttpMethod.Post,
    $"/api/rhino/projects/{projectId}/upload",
    content: progressContent,
    cancellationToken: cancellationToken);

if (response != null && response.Success)  // Check success flag
{
    transaction.State = UploadState.Completed;
    transaction.ServerImageId = response.ImageId;
    transaction.ServerUrl = response.Url;  // Use Url property
    // ...
}
```

---

### Task 5: Update Project Response Models
**Time:** 30 minutes  
**Priority:** MEDIUM  
**Files:** `Models/ProjectInfo.cs`, `Services/ApiClient.cs`

#### Backend Response Format:
```json
{
  "success": true,
  "projects": [
    {
      "id": "lOGFvYYUQxp8eBlsxuoH",
      "name": "Roger Hill Power Cat",
      "createdAt": "2025-10-19T23:25:19.000Z"
    }
  ]
}
```

#### Update Model:
```csharp
public class ProjectListResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("projects")]
    public List<ProjectInfo> Projects { get; set; }
}

public class ProjectInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}
```

#### Update API Client:
```csharp
public async Task<IEnumerable<ProjectInfo>?> GetProjectsAsync(
    CancellationToken cancellationToken = default)
{
    try
    {
        var response = await SendAuthenticatedRequestAsync<ProjectListResponse>(
            HttpMethod.Get, 
            "/api/rhino/projects",  // Updated endpoint
            cancellationToken: cancellationToken);
        
        return response?.Success == true ? response.Projects : null;
    }
    catch (Exception ex)
    {
        OnApiError($"Failed to get projects: {ex.Message}");
        return null;
    }
}
```

---

### Task 6: Add API Key Settings UI
**Time:** 1.5 hours  
**Priority:** HIGH  
**Files:** `UI/SettingsDialog.cs` (new), `Commands/VesselStudioSettingsCommand.cs` (new)

#### Create Settings Dialog:
```csharp
public class VesselStudioSettingsDialog : Dialog<bool>
{
    private TextBox _apiKeyTextBox;
    private Label _statusLabel;
    private Button _validateButton;
    
    public VesselStudioSettingsDialog()
    {
        Title = "Vessel Studio Settings";
        MinimumSize = new Size(500, 200);
        
        InitializeComponents();
    }
    
    private void InitializeComponents()
    {
        var layout = new DynamicLayout { Padding = 10, Spacing = new Size(5, 5) };
        
        // API Key section
        layout.BeginVertical();
        layout.AddRow(new Label { Text = "API Key:", Font = Fonts.Sans(10, FontStyle.Bold) });
        
        _apiKeyTextBox = new TextBox 
        { 
            PlaceholderText = "vsk_live_XXXXXXXXXXXXXXXX",
            Text = VesselStudioPlugin.Instance?.AuthService?.GetApiKey() ?? ""
        };
        layout.AddRow(_apiKeyTextBox);
        
        layout.AddRow(new Label 
        { 
            Text = "Get your API key from Vessel Studio → Settings → Rhino Plugin",
            Font = Fonts.Sans(8),
            TextColor = Colors.Gray
        });
        
        _validateButton = new Button 
        { 
            Text = "Validate API Key"
        };
        _validateButton.Click += OnValidateClick;
        
        _statusLabel = new Label 
        { 
            Text = "",
            Font = Fonts.Sans(9)
        };
        
        layout.AddRow(_validateButton, _statusLabel);
        layout.EndVertical();
        
        // Action buttons
        var okButton = new Button { Text = "Save" };
        okButton.Click += OnSaveClick;
        
        var cancelButton = new Button { Text = "Cancel" };
        cancelButton.Click += (s, e) => Close(false);
        
        layout.AddSeparateRow(null, okButton, cancelButton);
        
        Content = layout;
    }
    
    private async void OnValidateClick(object sender, EventArgs e)
    {
        var apiKey = _apiKeyTextBox.Text?.Trim();
        
        if (string.IsNullOrEmpty(apiKey))
        {
            _statusLabel.Text = "❌ Please enter an API key";
            _statusLabel.TextColor = Colors.Red;
            return;
        }
        
        _validateButton.Enabled = false;
        _statusLabel.Text = "Validating...";
        _statusLabel.TextColor = Colors.Gray;
        
        try
        {
            var plugin = VesselStudioPlugin.Instance;
            plugin.AuthService.SetApiKey(apiKey);
            
            var isValid = await plugin.AuthService.ValidateApiKeyAsync(apiKey);
            
            if (isValid)
            {
                _statusLabel.Text = "✓ Valid API key";
                _statusLabel.TextColor = Colors.Green;
            }
            else
            {
                _statusLabel.Text = "❌ Invalid API key";
                _statusLabel.TextColor = Colors.Red;
            }
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"❌ Error: {ex.Message}";
            _statusLabel.TextColor = Colors.Red;
        }
        finally
        {
            _validateButton.Enabled = true;
        }
    }
    
    private void OnSaveClick(object sender, EventArgs e)
    {
        var apiKey = _apiKeyTextBox.Text?.Trim();
        
        if (!string.IsNullOrEmpty(apiKey))
        {
            VesselStudioPlugin.Instance?.AuthService?.SetApiKey(apiKey);
        }
        
        Close(true);
    }
}
```

#### Create Settings Command:
```csharp
public class VesselStudioSettingsCommand : Command
{
    public override string EnglishName => "VesselStudioSettings";
    
    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        var dialog = new VesselStudioSettingsDialog();
        var result = dialog.ShowModal(RhinoEtoApp.MainWindow);
        
        return result ? Result.Success : Result.Cancel;
    }
}
```

---

### Task 7: Update Command Names (Optional)
**Time:** 15 minutes  
**Priority:** LOW  
**Files:** `Commands/*.cs`

Backend documentation references these command names:
- `VesselSetApiKey` - Set/validate API key
- `VesselCapture` - Capture and upload screenshot
- `VesselStatus` - Show connection status

Current plugin has:
- `VesselStudioAuth` → Rename to `VesselSetApiKey`
- `CaptureToVesselStudio` → Rename to `VesselCapture`
- Add new `VesselStatus` command

---

### Task 8: End-to-End Integration Testing
**Time:** 1 hour  
**Priority:** HIGH  

#### Test Checklist:

**Setup:**
1. [ ] Generate API key in Vessel Studio
2. [ ] Copy API key to clipboard
3. [ ] Open Rhino 8
4. [ ] Run `VesselStudioSettings` (or `VesselSetApiKey`)
5. [ ] Paste API key
6. [ ] Click "Validate" → Expect success message

**Upload Flow:**
7. [ ] Open a Rhino file with geometry
8. [ ] Run `VesselCapture` (or `CaptureToVesselStudio`)
9. [ ] Select a project from dropdown → Expect to see user's projects
10. [ ] Click "Capture & Upload" → Expect progress messages
11. [ ] Wait for completion → Expect success message with URL

**Verification:**
12. [ ] Open Vessel Studio in browser
13. [ ] Navigate to the selected project
14. [ ] Click "View Gallery" button
15. [ ] Verify uploaded screenshot appears with:
    - [ ] Orange "Rhino" badge
    - [ ] Correct viewport name in tooltip
    - [ ] Correct display mode in tooltip
    - [ ] Rhino version shown
    - [ ] Upload timestamp shown

**Error Cases:**
16. [ ] Try invalid API key → Expect 401 error
17. [ ] Upload to non-existent project → Expect 404 error
18. [ ] Upload very large file (>10MB) → Expect 400 error
19. [ ] Test with no internet connection → Expect network error

---

## File Changes Summary

### Files to Modify:
1. **Services/ApiClient.cs**
   - Update base URL
   - Change endpoint paths (3 locations)
   - Update upload form data fields
   - Update response parsing

2. **Services/AuthenticationService.cs**
   - Add API key validation method
   - Add API key storage methods
   - Update authentication logic

3. **Models/UploadResponse.cs**
   - Update properties to match backend response

4. **Models/ProjectInfo.cs**
   - Add `ProjectListResponse` wrapper class

5. **Models/VesselStudioSettings.cs**
   - Add `ApiKey` property

### Files to Create:
1. **UI/VesselStudioSettingsDialog.cs**
   - Settings dialog with API key input
   - Validation button
   - Save/Cancel actions

2. **Commands/VesselStudioSettingsCommand.cs**
   - Command to open settings dialog

### Files to Test:
1. All updated files
2. Upload flow end-to-end
3. Error handling

---

## Implementation Order

**Phase 1: Core API Changes (2 hours)**
1. Task 1: Update endpoints and base URL
2. Task 4: Update response models
3. Task 5: Update project models
4. Task 3: Update upload form data

**Phase 2: Authentication (1.5 hours)**
5. Task 2: Implement API key authentication
6. Task 6: Add settings UI

**Phase 3: Testing & Polish (1.5 hours)**
7. Task 8: Integration testing
8. Task 7: Command name updates (optional)
9. Fix any bugs found during testing

---

## Success Criteria

✅ **API Key Validation**
- User can enter API key in settings
- Validation endpoint returns success
- Invalid key shows error message

✅ **Project Listing**
- Projects dropdown shows user's projects
- Project names and IDs correct
- Empty state handled gracefully

✅ **Screenshot Upload**
- Viewport capture works
- Upload progress shown
- Success message with URL displayed

✅ **Gallery Verification**
- Screenshot appears in Vessel Studio gallery
- Orange "Rhino" badge visible
- Metadata tooltip shows correct info

✅ **Error Handling**
- Invalid API key → 401 error
- Network errors → Clear message
- File too large → 400 error

---

## Rollback Plan

If issues occur during implementation:
1. Create git branch before starting: `git checkout -b fix/vessel-studio-api-integration`
2. Commit after each task completion
3. If problems: `git checkout main` to revert
4. Keep old code commented out for reference

---

## Post-Implementation

**Documentation:**
- Update README.md with new setup instructions
- Document API key generation process
- Add troubleshooting guide

**Future Enhancements:**
- Add batch upload support
- Add upload history panel
- Add automatic retry on failure
- Add offline queueing

---

**Ready to begin implementation!** 🚀

Start with Phase 1 (Core API Changes) to get the basic connectivity working, then move to authentication and UI.

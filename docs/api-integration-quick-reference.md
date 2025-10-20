# Simplified API Integration - Quick Reference

**Last Updated:** October 20, 2025

---

## 🎯 Simplified Design

All Rhino captures go **directly to project gallery**. No per-image visibility choice.

---

## 📸 Capture Dialog (Simplified)

```
┌────────────────────────────────────┐
│ 📸 Capture to Vessel One           │
│                                    │
│ Send to project:                   │
│ [▼ Luxury Yacht 45ft]              │
│                                    │
│ Image name (optional):             │
│ [Hull Profile View]                │
│                                    │
│ [Cancel]  [Capture & Upload]       │
└────────────────────────────────────┘
```

**That's it!** Just 2 inputs:
1. Which project?
2. What name?

---

## 🏗️ Data Storage

### Firebase Storage
```
/projects/
  {projectId}/
    gallery/
      img001.png  ← Rhino capture 1
      img002.png  ← Rhino capture 2
      img003.png  ← Rhino capture 3
```

### Firestore
```
projects/
  {projectId}/
    gallery/
      img001: {
        name: "Hull Profile View",
        source: "rhino-plugin",
        uploadedBy: "user123",
        uploadedAt: timestamp,
        metadata: {
          viewportName: "Perspective",
          displayMode: "Shaded",
          rhinoVersion: "8.1.23325"
        }
      }
```

---

## 🔄 User Flow

### Standard Capture
```
1. User types: VesselCapture
2. Dialog shows all their projects
3. User selects project from dropdown
4. User names the image (or uses default)
5. Click "Capture & Upload"
6. ✅ Image appears in project gallery (browser updates in real-time)
```

### Quick Capture (Power Users)
```
1. User types: VesselQuickCapture
2. ✅ Instantly captures to last-used project
3. ✅ Auto-names with timestamp
4. ✅ No dialog interruption
```

---

## 🔌 API Endpoints (Simplified)

### 1. Validate API Key
```
POST /api/rhino/validate
Authorization: Bearer vsk_live_abc123...

Response:
{
  "valid": true,
  "userId": "user123",
  "userName": "John Doe"
}
```

### 2. List Projects
```
GET /api/rhino/projects
Authorization: Bearer vsk_live_abc123...

Response:
{
  "projects": [
    {
      "id": "proj_001",
      "name": "Luxury Yacht 45ft",
      "type": "yacht",
      "thumbnailUrl": "..."
    },
    ...
  ]
}
```

### 3. Upload Screenshot
```
POST /api/rhino/projects/{projectId}/upload
Authorization: Bearer vsk_live_abc123...
Content-Type: multipart/form-data

Form Data:
- image: [PNG file bytes]
- name: "Hull Profile View"
- metadata: {
    "width": 1920,
    "height": 1080,
    "viewportName": "Perspective",
    "displayMode": "Shaded"
  }

Response:
{
  "success": true,
  "imageId": "img123",
  "imageUrl": "https://storage.googleapis.com/...",
  "message": "Uploaded to Luxury Yacht 45ft gallery"
}
```

---

## 💻 C# Implementation (Simplified)

### API Client Method
```csharp
public async Task<(bool success, string message, string imageUrl)> UploadScreenshot(
    string projectId,
    byte[] imageBytes,
    string imageName,
    Dictionary<string, object> metadata)
{
    var content = new MultipartFormDataContent();
    content.Add(new ByteArrayContent(imageBytes), "image", "capture.png");
    content.Add(new StringContent(imageName), "name");
    content.Add(new StringContent(JsonConvert.SerializeObject(metadata)), "metadata");

    var response = await httpClient.PostAsync(
        $"{BASE_URL}/rhino/projects/{projectId}/upload",
        content
    );

    if (response.IsSuccessStatusCode)
    {
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<dynamic>(json);
        return (true, result.message.ToString(), result.imageUrl.ToString());
    }

    return (false, "Upload failed", null);
}
```

### Dialog (Simplified)
```csharp
public class CaptureDialog : Dialog<DialogResult>
{
    private DropDown projectDropdown;  // Choose project
    private TextBox nameTextBox;       // Name image
    
    // That's all we need!
}
```

---

## 🎨 Benefits of Simplified Design

### Developer Benefits
- ✅ Less code complexity
- ✅ Fewer parameters to pass
- ✅ Single storage path pattern
- ✅ Easier to test

### User Benefits
- ✅ Faster workflow (fewer clicks)
- ✅ Clearer intent (gallery is obvious destination)
- ✅ No decision fatigue
- ✅ Consistent behavior

### Backend Benefits
- ✅ Single storage pattern
- ✅ Simpler access control (at project level)
- ✅ Easier to query/display
- ✅ Less database complexity

---

## 🔐 Visibility Control

**Gallery visibility is controlled at the PROJECT level**, not per-image:

```typescript
// Project settings determine who can see gallery
interface Project {
  id: string;
  name: string;
  visibility: 'public' | 'private' | 'team';
  // All gallery images inherit this visibility
}
```

**Examples:**
- **Public project** → Gallery visible to everyone
- **Private project** → Gallery only visible to owner
- **Team project** → Gallery visible to team members

This makes more sense because:
- Users rarely want mixed visibility within a project
- Simpler mental model
- One place to control access
- Consistent with how most project tools work

---

## 📊 Comparison: Before vs After

### Before (Complex)
```
1. Pick project
2. Name image
3. Choose gallery or workspace  ← Removed!
4. Upload
```

### After (Simple)
```
1. Pick project
2. Name image
3. Upload
```

**Result:** One less decision = faster workflow

---

## 🚀 Implementation Checklist

### Backend (Vessel One)
- [ ] POST /api/rhino/validate
- [ ] GET /api/rhino/projects
- [ ] POST /api/rhino/projects/[id]/upload
- [ ] Firebase Storage: projects/{id}/gallery/
- [ ] Firestore: projects/{id}/gallery/{imageId}

### Frontend (Vessel One)
- [ ] API key management page
- [ ] Real-time gallery listener
- [ ] Toast notifications for new Rhino captures

### Rhino Plugin
- [ ] VesselStudioApiClient with 3 methods
- [ ] VesselCapture command with dialog
- [ ] VesselQuickCapture command
- [ ] Settings storage (API key, last project)

**Estimated Time:** 5-6 days total

---

## ✅ What Changed from Original Design

**Removed:**
- ❌ Gallery vs Workspace toggle
- ❌ Per-image visibility setting
- ❌ Multiple storage paths
- ❌ Workspace Firestore collection
- ❌ Visibility parameter in API

**Kept:**
- ✅ Project selection dropdown
- ✅ Image naming
- ✅ Quick capture mode
- ✅ Real-time updates
- ✅ API key authentication
- ✅ Metadata tracking

**Result:** Simpler, faster, easier to maintain

---

**Questions?** All captures go to project gallery. Project settings control visibility.
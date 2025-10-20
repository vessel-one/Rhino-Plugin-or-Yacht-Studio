# Vessel One API Integration - Visual Flow Diagram

**Date:** October 20, 2025

---

## 🎨 Complete User Journey

```
┌─────────────────────────────────────────────────────────────────┐
│                    ONE-TIME SETUP                               │
└─────────────────────────────────────────────────────────────────┘

┌──────────────────┐
│ Vessel One       │
│ Web App          │
│                  │
│ Profile →        │
│ Rhino Plugin →   │
│ Create API Key   │
└────────┬─────────┘
         │
         │ vsk_live_abc123xyz...
         │ (Copy to clipboard)
         ↓
┌──────────────────┐
│ Rhino 8          │
│                  │
│ > VesselSetApiKey│
│                  │
│ [Paste API Key]  │
│ [Validate]       │
│                  │
│ ✅ Connected!    │
└──────────────────┘


┌─────────────────────────────────────────────────────────────────┐
│                 STANDARD CAPTURE WORKFLOW                       │
└─────────────────────────────────────────────────────────────────┘

User is modeling in Rhino...
         │
         │ Ready to save viewport
         ↓
┌──────────────────────────────┐
│ > VesselCapture              │
└──────────┬───────────────────┘
           │
           │ Plugin loads user's projects
           ↓
┌─────────────────────────────────────────────────────────────┐
│  📸 Capture to Vessel One                                   │
│                                                             │
│  Send to project:                                           │
│  ┌────────────────────────────────────────┐                │
│  │ ▼ Luxury Yacht 45ft                    │ (dropdown)     │
│  └────────────────────────────────────────┘                │
│                                                             │
│  Other projects:                                            │
│  - Sport Fishing 60ft                                       │
│  - Catamaran Explorer 50ft                                  │
│  - Racing Sloop 38ft                                        │
│                                                             │
│  Image name (optional):                                     │
│  ┌────────────────────────────────────────┐                │
│  │ Hull Profile View                      │                │
│  └────────────────────────────────────────┘                │
│                                                             │
│  Add to:                                                    │
│  ⦿ Project Gallery (publicly visible)                      │
│  ○ Private Workspace (only visible to you)                 │
│                                                             │
│  [Cancel]          [Capture & Upload]                      │
└─────────────────────────────────────────────────────────────┘
           │
           │ User clicks "Capture & Upload"
           ↓
┌──────────────────────────────┐
│ Plugin captures viewport     │
│ (1920x1080 PNG)              │
└──────────┬───────────────────┘
           │
           │ HTTPS POST with:
           │ - Image bytes
           │ - Project ID
           │ - Image name
           │ - Visibility
           │ - Metadata (viewport, display mode, etc.)
           ↓
┌─────────────────────────────────────────────────────────────┐
│ Vessel One API                                              │
│                                                             │
│ POST /api/rhino/projects/{projectId}/upload                │
│                                                             │
│ 1. Validate API key ✓                                      │
│ 2. Check user has project access ✓                         │
│ 3. Upload to Firebase Storage                              │
│ 4. Save to Firestore (projects/{id}/gallery/{imageId})     │
│ 5. Update project timestamp                                │
└──────────┬──────────────────────────────────────────────────┘
           │
           │ Response: { success: true, imageUrl: "..." }
           ↓
┌──────────────────────────────┐
│ Rhino                        │
│                              │
│ ✅ Uploaded to Luxury Yacht  │
│    45ft gallery              │
│ 📷 View at: vessel.one/...   │
└──────────────────────────────┘
           │
           │ Meanwhile, in browser...
           ↓
┌─────────────────────────────────────────────────────────────┐
│ Vessel One Browser (user already has project open)         │
│                                                             │
│ Firestore real-time listener detects new image             │
│                 ↓                                           │
│  🔔 Toast: "New Rhino capture: Hull Profile View"          │
│                 ↓                                           │
│  Gallery auto-updates (no refresh needed):                 │
│                                                             │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐          │
│  │  [NEW!]    │  │            │  │            │          │
│  │  Hull      │  │  Previous  │  │  Previous  │          │
│  │  Profile   │  │  Image 1   │  │  Image 2   │          │
│  │  View      │  │            │  │            │          │
│  │  📐 Rhino  │  │            │  │            │          │
│  │  Just now  │  │  2 hrs ago │  │  1 day ago │          │
│  └────────────┘  └────────────┘  └────────────┘          │
└─────────────────────────────────────────────────────────────┘


┌─────────────────────────────────────────────────────────────────┐
│               QUICK CAPTURE WORKFLOW (POWER USERS)              │
└─────────────────────────────────────────────────────────────────┘

User is iterating on design...
         │
         │ (No interruption desired)
         ↓
┌──────────────────────────────┐
│ > VesselQuickCapture         │ (Command #1)
└──────────┬───────────────────┘
           │ ⚡ Instant capture
           │ ⚡ Uses last-selected project
           │ ⚡ Auto-generates name
           ↓
┌──────────────────────────────┐
│ ✅ Uploaded to Luxury Yacht  │
│    45ft gallery              │
│ (Quick Capture 14:32:15)     │
└──────────────────────────────┘
           │
           │ User keeps modeling...
           ↓
┌──────────────────────────────┐
│ > VesselQuickCapture         │ (Command #2)
└──────────┬───────────────────┘
           │ ⚡ Instant capture again
           ↓
┌──────────────────────────────┐
│ ✅ Uploaded                  │
│ (Quick Capture 14:35:42)     │
└──────────────────────────────┘
           │
           │ User continues...
           ↓
┌──────────────────────────────┐
│ > VesselQuickCapture         │ (Command #3)
└──────────┬───────────────────┘
           │ ⚡ Instant capture
           ↓
┌──────────────────────────────┐
│ ✅ Uploaded                  │
│ (Quick Capture 14:38:01)     │
└──────────────────────────────┘

Result: 3 progression shots in project gallery showing design evolution


┌─────────────────────────────────────────────────────────────────┐
│                    DATA FLOW ARCHITECTURE                       │
└─────────────────────────────────────────────────────────────────┘

┌───────────────────────────────────────────────────────────────┐
│                         Rhino Plugin                          │
│                                                               │
│  Commands:                                                    │
│  - VesselSetApiKey      → Store encrypted API key            │
│  - VesselCapture        → Show dialog + upload               │
│  - VesselQuickCapture   → Instant upload                     │
│  - VesselStatus         → Show connection status             │
│                                                               │
│  VesselStudioApiClient:                                       │
│  - ValidateApiKey()                                          │
│  - GetProjects()                                             │
│  - UploadScreenshot()                                        │
└──────────────┬────────────────────────────────────────────────┘
               │
               │ HTTPS (Authorization: Bearer vsk_live_...)
               ↓
┌───────────────────────────────────────────────────────────────┐
│                    Vessel One Backend API                     │
│                      (Next.js API Routes)                     │
│                                                               │
│  POST /api/rhino/validate                                    │
│    → Validate API key hash                                   │
│    → Return user info                                        │
│                                                               │
│  GET /api/rhino/projects                                     │
│    → Query Firestore for user's projects                     │
│    → Return project list                                     │
│                                                               │
│  POST /api/rhino/projects/[id]/upload                        │
│    → Validate access                                         │
│    → Upload to Firebase Storage                              │
│    → Create Firestore document                               │
│    → Return image URL                                        │
└──────────────┬────────────────────────────────────────────────┘
               │
               ↓
┌─────────────────────────────┬─────────────────────────────────┐
│     Firebase Storage        │       Firestore Database        │
│                             │                                 │
│  /projects/                 │  users/                         │
│    {projectId}/             │    {userId}/                    │
│      rhino-captures/        │      apiKeys/                   │
│        {imageId}.png        │        {keyId}                  │
│                             │                                 │
│  Public URLs:               │  projects/                      │
│  storage.googleapis.com/... │    {projectId}/                 │
│                             │      gallery/                   │
│                             │        {imageId}                │
│                             │      workspace/                 │
│                             │        {imageId}                │
└─────────────────────────────┴─────────────────────────────────┘
               │
               │ Real-time listeners (onSnapshot)
               ↓
┌───────────────────────────────────────────────────────────────┐
│                  Vessel One Frontend (Browser)                │
│                                                               │
│  useEffect(() => {                                           │
│    const unsubscribe = onSnapshot(                           │
│      collection(db, 'projects', projectId, 'gallery'),       │
│      (snapshot) => {                                         │
│        // Auto-update gallery when new images arrive         │
│        setGalleryImages(snapshot.docs.map(...));             │
│      }                                                        │
│    );                                                         │
│  }, [projectId]);                                            │
│                                                               │
│  🔔 Real-time toast notifications for new Rhino uploads      │
└───────────────────────────────────────────────────────────────┘


┌─────────────────────────────────────────────────────────────────┐
│                    API KEY LIFECYCLE                            │
└─────────────────────────────────────────────────────────────────┘

Creation:
┌────────────────────┐
│ User clicks        │
│ "Create API Key"   │
└─────────┬──────────┘
          │
          ↓
┌────────────────────────────────────────────────────────┐
│ Backend generates:                                     │
│                                                        │
│ randomBytes(24) → "abc123xyz..."                      │
│ fullKey = "vsk_live_abc123xyz..."                     │
│ keyHash = sha256(fullKey) → "9f86d081..."             │
│                                                        │
│ Store in Firestore:                                    │
│ - keyHash (for validation)                            │
│ - keyPrefix (for display: "vsk_live_abc1...")         │
│ - userId, permissions, createdAt, etc.                │
│                                                        │
│ Return fullKey ONE TIME ONLY                           │
└────────────────────────────────────────────────────────┘
          │
          │ User copies: vsk_live_abc123xyz...
          ↓
┌────────────────────┐
│ Never stored       │
│ in database!       │
│                    │
│ User must save     │
│ securely           │
└────────────────────┘

Usage:
┌────────────────────┐
│ Rhino sends:       │
│ Authorization:     │
│ Bearer vsk_live_.. │
└─────────┬──────────┘
          │
          ↓
┌────────────────────────────────────────────────────────┐
│ Backend validates:                                     │
│                                                        │
│ receivedKey = "vsk_live_abc123xyz..."                 │
│ receivedHash = sha256(receivedKey)                    │
│                                                        │
│ Query Firestore:                                       │
│ WHERE keyHash == receivedHash                          │
│ WHERE isActive == true                                 │
│                                                        │
│ If match found → ✅ Valid                              │
│ If no match → ❌ Invalid                               │
└────────────────────────────────────────────────────────┘

Revocation:
┌────────────────────┐
│ User clicks        │
│ "Revoke" button    │
└─────────┬──────────┘
          │
          ↓
┌────────────────────────────────────────────────────────┐
│ Update Firestore:                                      │
│ isActive = false                                       │
│                                                        │
│ All future API calls with this key will fail          │
└────────────────────────────────────────────────────────┘


┌─────────────────────────────────────────────────────────────────┐
│              IMAGE STORAGE: GALLERY VS WORKSPACE                │
└─────────────────────────────────────────────────────────────────┘

User Decision in Dialog:
┌────────────────────────────────────────────────────────┐
│ Add to:                                                │
│ ⦿ Project Gallery (publicly visible)                  │
│ ○ Private Workspace (only visible to you)             │
└─────────┬──────────────────────────────────────────────┘
          │
          ├─────────────────────┬─────────────────────────┐
          │                     │                         │
          ↓                     ↓                         │
    visibility="gallery"   visibility="private"          │
          │                     │                         │
          ↓                     ↓                         │
┌─────────────────────┐  ┌─────────────────────┐        │
│ Firebase Storage:   │  │ Firebase Storage:   │        │
│                     │  │                     │        │
│ /projects/          │  │ /projects/          │        │
│   {id}/             │  │   {id}/             │        │
│   rhino-captures/   │  │   rhino-captures/   │        │
│   img123.png        │  │   img456.png        │        │
│                     │  │                     │        │
│ ✅ file.makePublic()│  │ ❌ Private (no URL) │        │
│                     │  │                     │        │
│ Public URL:         │  │ Requires Firebase   │        │
│ storage.google...   │  │ auth to download    │        │
└─────────┬───────────┘  └─────────┬───────────┘        │
          │                        │                     │
          ↓                        ↓                     │
┌─────────────────────┐  ┌─────────────────────┐        │
│ Firestore:          │  │ Firestore:          │        │
│                     │  │                     │        │
│ projects/{id}/      │  │ projects/{id}/      │        │
│   gallery/          │  │   workspace/        │        │
│     img123          │  │     img456          │        │
│                     │  │                     │        │
│ Visible to:         │  │ Visible to:         │        │
│ - All users         │  │ - Owner only        │        │
│ - Public viewers    │  │ - Team members      │        │
│                     │  │   (if shared)       │        │
└─────────────────────┘  └─────────────────────┘        │
          │                        │                     │
          │                        │                     │
          ↓                        ↓                     │
┌──────────────────────────────────────────────┐        │
│           Browser Display                    │        │
│                                              │        │
│  Gallery Tab        Workspace Tab            │        │
│  ┌──────────┐      ┌──────────┐             │        │
│  │ img123   │      │ img456   │             │        │
│  │ Public   │      │ Private  │             │        │
│  └──────────┘      └──────────┘             │        │
└──────────────────────────────────────────────┘        │
                                                        │
Use Cases:                                              │
                                                        │
Gallery (Public):                                       │
- Final design presentations                            │
- Portfolio pieces                                      │
- Client reviews                                        │
- Marketing materials                                   │
                                                        │
Workspace (Private):                                    │
- Work-in-progress shots                                │
- Design iterations                                     │
- Internal team reviews                                 │
- Experimental concepts                                 │
└─────────────────────────────────────────────────────────┘


┌─────────────────────────────────────────────────────────────────┐
│                    METADATA CAPTURED                            │
└─────────────────────────────────────────────────────────────────┘

Every Rhino capture includes:

{
  "id": "xyz789",
  "projectId": "proj_abc123",
  "name": "Hull Profile View",
  "url": "https://storage.googleapis.com/...",
  "visibility": "gallery",
  "source": "rhino-plugin",
  "uploadedBy": "user_456",
  "uploadedAt": "2025-10-20T14:32:15Z",
  "metadata": {
    "width": 1920,
    "height": 1080,
    "viewportName": "Perspective",
    "displayMode": "Shaded",
    "rhinoVersion": "8.1.23325.13001",
    "captureTime": "2025-10-20T14:32:15Z"
  }
}

Benefits:
- Track which images came from Rhino vs uploaded manually
- Display Rhino badge in gallery
- Filter by viewport or display mode
- Version tracking with timestamps
- Debugging (which Rhino version, when captured)
```

---

## 🎯 Summary

### Two Capture Modes

**VesselCapture (Standard)**
- Shows dialog
- User selects project
- Names image
- Chooses gallery vs private
- Best for: Intentional captures

**VesselQuickCapture (Power User)**
- No dialog
- Uses last project
- Auto-names with timestamp
- Best for: Rapid iteration

### Data Flow

1. **Setup:** API key created in web app → entered in Rhino
2. **Capture:** Rhino screenshot → API upload → Firebase Storage
3. **Storage:** Public (gallery) or private (workspace)
4. **Display:** Real-time updates in browser via Firestore

### Key Features

✅ Real-time browser updates  
✅ Named captures  
✅ Gallery vs private workspace  
✅ Rapid-fire quick capture mode  
✅ API key security (hashed, revocable)  
✅ Project selection  
✅ Metadata tracking

---

**Next:** Implement API endpoints in Vessel One backend!
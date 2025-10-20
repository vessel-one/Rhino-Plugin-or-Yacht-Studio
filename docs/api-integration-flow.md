# Vessel One API Integration Flow

**Date:** October 20, 2025  
**Status:** Design Phase

---

## 🎯 User Journey

### Step 1: Authentication Setup
```
User opens Vessel One web app
  ↓
Profile → Rhino Plugin Settings
  ↓
Click "Create API Key"
  ↓
System generates unique API key
  ↓
User copies API key to clipboard
  ↓
Opens Rhino 8
  ↓
Types: VesselSetApiKey
  ↓
Pastes API key in dialog
  ↓
Plugin validates key with Vessel One API
  ↓
✅ "Connected to Vessel One as [username]"
```

---

## 🔐 API Key Management

### Backend: API Key Generation

**Database Schema (Firestore):**
```typescript
// Collection: users/{userId}/apiKeys/{keyId}
interface VesselOneApiKey {
  id: string;                    // Auto-generated
  userId: string;                // Owner
  keyPrefix: string;             // "vsk_live_" or "vsk_test_"
  keyHash: string;               // SHA-256 hash of full key
  name: string;                  // User-friendly name "Rhino Plugin"
  permissions: string[];         // ["rhino:upload", "projects:read"]
  createdAt: Timestamp;
  lastUsedAt: Timestamp | null;
  expiresAt: Timestamp | null;   // Optional expiration
  isActive: boolean;
  usageCount: number;            // Track API calls
}
```

**API Endpoint:**
```typescript
// app/api/user/api-keys/route.ts
import { auth } from '@/lib/auth';
import { db } from '@/lib/firebase-admin';
import { randomBytes } from 'crypto';
import { createHash } from 'crypto';

export async function POST(request: Request) {
  const session = await auth();
  if (!session?.user?.id) {
    return Response.json({ error: 'Unauthorized' }, { status: 401 });
  }

  const { name } = await request.json();

  // Generate API key: vsk_live_<random 32 chars>
  const randomPart = randomBytes(24).toString('base64url');
  const fullKey = `vsk_live_${randomPart}`;
  const keyHash = createHash('sha256').update(fullKey).digest('hex');

  const apiKeyDoc = {
    userId: session.user.id,
    keyPrefix: fullKey.substring(0, 12), // Store prefix for identification
    keyHash,
    name: name || 'Rhino Plugin',
    permissions: ['rhino:upload', 'projects:read', 'projects:list'],
    createdAt: new Date(),
    lastUsedAt: null,
    expiresAt: null,
    isActive: true,
    usageCount: 0,
  };

  const keyRef = await db.collection('users')
    .doc(session.user.id)
    .collection('apiKeys')
    .add(apiKeyDoc);

  // Only return full key once (never stored in DB)
  return Response.json({
    apiKey: fullKey,
    keyId: keyRef.id,
    message: 'Save this key securely - you won\'t see it again!'
  });
}
```

**Frontend UI (Profile Page):**
```tsx
// app/profile/api-keys/page.tsx
'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { CopyIcon, EyeIcon, TrashIcon } from 'lucide-react';

export default function ApiKeysPage() {
  const [apiKeys, setApiKeys] = useState([]);
  const [newKey, setNewKey] = useState<string | null>(null);

  async function createApiKey() {
    const res = await fetch('/api/user/api-keys', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name: 'Rhino Plugin' })
    });
    const data = await res.json();
    setNewKey(data.apiKey);
    loadApiKeys();
  }

  async function copyToClipboard(key: string) {
    await navigator.clipboard.writeText(key);
    toast.success('API key copied to clipboard!');
  }

  return (
    <div className="p-6">
      <h1>Rhino Plugin API Keys</h1>
      
      {newKey && (
        <div className="bg-yellow-50 border border-yellow-200 p-4 rounded mt-4">
          <p className="font-semibold">⚠️ Save this key - you won't see it again!</p>
          <div className="flex gap-2 mt-2">
            <Input value={newKey} readOnly className="font-mono" />
            <Button onClick={() => copyToClipboard(newKey)}>
              <CopyIcon /> Copy
            </Button>
          </div>
        </div>
      )}

      <Button onClick={createApiKey} className="mt-4">
        Create New API Key
      </Button>

      <div className="mt-6">
        <h2>Active Keys</h2>
        {apiKeys.map(key => (
          <div key={key.id} className="flex items-center justify-between p-3 border rounded">
            <div>
              <p className="font-mono">{key.keyPrefix}••••••••</p>
              <p className="text-sm text-gray-500">
                Created {new Date(key.createdAt).toLocaleDateString()}
              </p>
            </div>
            <Button variant="destructive" size="sm">
              <TrashIcon /> Revoke
            </Button>
          </div>
        ))}
      </div>
    </div>
  );
}
```

---

## 📸 Screenshot Capture & Upload Flow

### Option 1: Direct to Project Gallery (Recommended)

**User Experience:**
```
User has Vessel One project open in browser
  ↓
User switches to Rhino 8
  ↓
Types: VesselCapture
  ↓
Dialog appears:
  "Send to which project?"
  [Dropdown: My Yacht Projects]
  
  "Image name (optional):"
  [Input: e.g., "Hull Profile View"]
  
  [Cancel] [Capture & Upload]
  ↓
Plugin captures viewport
  ↓
Uploads to Vessel One API
  ↓
✅ "Uploaded to [Project Name] gallery"
  ↓
User switches back to browser
  ↓
New image appears in project gallery (real-time via Firestore)
```

**Benefits:**
- User explicitly chooses destination
- Can name images meaningfully
- Supports multiple projects
- Real-time updates in browser

---

### Option 2: Quick Capture Mode (Power User Feature)

**User Experience:**
```
User types: VesselQuickCapture
  ↓
Plugin uses last-selected project (cached)
  ↓
Auto-generates name: "Rhino Capture [timestamp]"
  ↓
Uploads immediately
  ↓
Toast notification: "✅ Uploaded to [Project]"
  ↓
User can capture 10 shots rapid-fire
```

**Benefits:**
- Fast workflow for iterating
- No interruption to modeling
- Good for design progression captures

---

### Recommended Approach: Both Modes

**Default Command:** `VesselCapture` (with dialog)  
**Quick Mode:** `VesselQuickCapture` (skips dialog)

**Configuration:**
```csharp
// Store last-used project for quick mode
public class VesselStudioSettings
{
    public string ApiKey { get; set; }
    public string LastProjectId { get; set; }
    public string LastProjectName { get; set; }
}
```

---

## 🏗️ Backend API Implementation

### Endpoint 1: Validate API Key

```typescript
// app/api/rhino/validate/route.ts
import { db } from '@/lib/firebase-admin';
import { createHash } from 'crypto';

export async function POST(request: Request) {
  const apiKey = request.headers.get('Authorization')?.replace('Bearer ', '');
  
  if (!apiKey || !apiKey.startsWith('vsk_')) {
    return Response.json({ error: 'Invalid API key format' }, { status: 401 });
  }

  const keyHash = createHash('sha256').update(apiKey).digest('hex');

  // Search for matching key
  const keysSnapshot = await db.collectionGroup('apiKeys')
    .where('keyHash', '==', keyHash)
    .where('isActive', '==', true)
    .limit(1)
    .get();

  if (keysSnapshot.empty) {
    return Response.json({ error: 'Invalid or revoked API key' }, { status: 401 });
  }

  const keyDoc = keysSnapshot.docs[0];
  const keyData = keyDoc.data();

  // Update last used timestamp
  await keyDoc.ref.update({
    lastUsedAt: new Date(),
    usageCount: (keyData.usageCount || 0) + 1,
  });

  // Get user info
  const userDoc = await db.collection('users').doc(keyData.userId).get();
  const userData = userDoc.data();

  return Response.json({
    valid: true,
    userId: keyData.userId,
    userName: userData?.displayName || userData?.email || 'User',
    permissions: keyData.permissions,
  });
}
```

---

### Endpoint 2: List User's Projects

```typescript
// app/api/rhino/projects/route.ts
import { db } from '@/lib/firebase-admin';
import { validateApiKey } from '@/lib/api-auth';

export async function GET(request: Request) {
  const authResult = await validateApiKey(request);
  if (!authResult.valid) {
    return Response.json({ error: 'Unauthorized' }, { status: 401 });
  }

  const userId = authResult.userId;

  // Get projects where user is owner or collaborator
  const projectsSnapshot = await db.collection('projects')
    .where('members', 'array-contains', userId)
    .orderBy('updatedAt', 'desc')
    .limit(50)
    .get();

  const projects = projectsSnapshot.docs.map(doc => ({
    id: doc.id,
    name: doc.data().name,
    type: doc.data().type, // 'yacht', 'powerboat', etc.
    thumbnailUrl: doc.data().thumbnailUrl,
    updatedAt: doc.data().updatedAt,
  }));

  return Response.json({ projects });
}
```

---

### Endpoint 3: Upload Screenshot to Project

```typescript
// app/api/rhino/projects/[projectId]/upload/route.ts
import { db, storage } from '@/lib/firebase-admin';
import { validateApiKey } from '@/lib/api-auth';
import { nanoid } from 'nanoid';

export async function POST(
  request: Request,
  { params }: { params: { projectId: string } }
) {
  const authResult = await validateApiKey(request);
  if (!authResult.valid) {
    return Response.json({ error: 'Unauthorized' }, { status: 401 });
  }

  const formData = await request.formData();
  const imageFile = formData.get('image') as File;
  const imageName = formData.get('name') as string || `Rhino Capture ${Date.now()}`;
  const metadata = JSON.parse(formData.get('metadata') as string || '{}');

  // Verify user has access to project
  const projectDoc = await db.collection('projects').doc(params.projectId).get();
  if (!projectDoc.exists) {
    return Response.json({ error: 'Project not found' }, { status: 404 });
  }

  const projectData = projectDoc.data()!;
  if (!projectData.members?.includes(authResult.userId)) {
    return Response.json({ error: 'Access denied' }, { status: 403 });
  }

  // Upload to Firebase Storage
  const imageId = nanoid();
  const imageBuffer = await imageFile.arrayBuffer();
  const bucket = storage.bucket();
  const filePath = `projects/${params.projectId}/gallery/${imageId}.png`;
  const file = bucket.file(filePath);

  await file.save(Buffer.from(imageBuffer), {
    metadata: {
      contentType: 'image/png',
      metadata: {
        uploadedBy: authResult.userId,
        uploadSource: 'rhino-plugin',
        originalName: imageName,
        ...metadata,
      },
    },
  });

  // Make publicly accessible (or handle via signed URLs based on project privacy)
  await file.makePublic();

  const publicUrl = `https://storage.googleapis.com/${bucket.name}/${filePath}`;

  // Save to Firestore gallery collection
  const imageDoc = {
    id: imageId,
    projectId: params.projectId,
    name: imageName,
    url: publicUrl,
    source: 'rhino-plugin',
    uploadedBy: authResult.userId,
    uploadedAt: new Date(),
    metadata: {
      width: metadata.width,
      height: metadata.height,
      viewportName: metadata.viewportName,
      displayMode: metadata.displayMode,
      rhinoVersion: metadata.rhinoVersion,
    },
  };

  await db.collection('projects')
    .doc(params.projectId)
    .collection('gallery')
    .doc(imageId)
    .set(imageDoc);

  // Update project's updatedAt timestamp
  await projectDoc.ref.update({ updatedAt: new Date() });

  return Response.json({
    success: true,
    imageId,
    imageUrl: publicUrl,
    message: `Uploaded to ${projectData.name} gallery`,
  });
}
```

---

## 🔌 Rhino Plugin Implementation

### Updated API Client

```csharp
// VesselStudioSimplePlugin/VesselStudioApiClient.cs
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace VesselStudioSimplePlugin
{
    public class VesselProject
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string ThumbnailUrl { get; set; }
    }

    public class VesselStudioApiClient
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private string apiKey;
        private const string BASE_URL = "https://vessel.one/api"; // Update with actual URL

        public VesselStudioApiClient(string apiKey)
        {
            this.apiKey = apiKey;
            httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", apiKey);
        }

        public async Task<(bool success, string userName)> ValidateApiKey()
        {
            try
            {
                var response = await httpClient.PostAsync($"{BASE_URL}/rhino/validate", null);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<dynamic>(json);
                    return (true, result.userName.ToString());
                }
                return (false, null);
            }
            catch
            {
                return (false, null);
            }
        }

        public async Task<List<VesselProject>> GetProjects()
        {
            try
            {
                var response = await httpClient.GetAsync($"{BASE_URL}/rhino/projects");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<dynamic>(json);
                    var projects = new List<VesselProject>();
                    
                    foreach (var proj in result.projects)
                    {
                        projects.Add(new VesselProject
                        {
                            Id = proj.id,
                            Name = proj.name,
                            Type = proj.type,
                            ThumbnailUrl = proj.thumbnailUrl
                        });
                    }
                    
                    return projects;
                }
                return new List<VesselProject>();
            }
            catch
            {
                return new List<VesselProject>();
            }
        }

        public async Task<(bool success, string message, string imageUrl)> UploadScreenshot(
            string projectId,
            byte[] imageBytes,
            string imageName,
            Dictionary<string, object> metadata)
        {
            try
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

                var error = await response.Content.ReadAsStringAsync();
                return (false, $"Upload failed: {error}", null);
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}", null);
            }
        }
    }
}
```

---

### Enhanced Capture Command with Dialog

```csharp
// VesselStudioSimplePlugin/VesselStudioCaptureCommand.cs
using Rhino;
using Rhino.Commands;
using Rhino.UI;
using Eto.Forms;
using Eto.Drawing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace VesselStudioSimplePlugin
{
    public class VesselStudioCaptureCommand : Command
    {
        public override string EnglishName => "VesselCapture";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var settings = VesselStudioSettings.Load();
            if (string.IsNullOrEmpty(settings.ApiKey))
            {
                RhinoApp.WriteLine("❌ API key not set. Run VesselSetApiKey first.");
                return Result.Failure;
            }

            var apiClient = new VesselStudioApiClient(settings.ApiKey);

            // Show capture dialog
            var dialog = new CaptureDialog(apiClient, settings);
            var result = dialog.ShowModal();

            if (result == DialogResult.Ok)
            {
                var captureResult = PerformCapture(
                    doc,
                    apiClient,
                    dialog.SelectedProjectId,
                    dialog.ImageName
                );

                if (captureResult.success)
                {
                    RhinoApp.WriteLine($"✅ {captureResult.message}");
                    RhinoApp.WriteLine($"📷 View at: {captureResult.imageUrl}");
                    
                    // Save last used project for quick capture
                    settings.LastProjectId = dialog.SelectedProjectId;
                    settings.LastProjectName = dialog.SelectedProjectName;
                    settings.Save();
                    
                    return Result.Success;
                }
                else
                {
                    RhinoApp.WriteLine($"❌ {captureResult.message}");
                    return Result.Failure;
                }
            }

            return Result.Cancel;
        }

        private (bool success, string message, string imageUrl) PerformCapture(
            RhinoDoc doc,
            VesselStudioApiClient apiClient,
            string projectId,
            string imageName)
        {
            try
            {
                var view = doc.Views.ActiveView;
                if (view == null)
                {
                    return (false, "No active viewport", null);
                }

                // Capture viewport as bitmap
                var bitmap = view.CaptureToBitmap(new Size(1920, 1080));
                
                // Convert to byte array
                byte[] imageBytes;
                using (var ms = new System.IO.MemoryStream())
                {
                    bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    imageBytes = ms.ToArray();
                }

                // Gather metadata
                var metadata = new Dictionary<string, object>
                {
                    { "width", 1920 },
                    { "height", 1080 },
                    { "viewportName", view.MainViewport.Name },
                    { "displayMode", view.MainViewport.DisplayMode.EnglishName },
                    { "rhinoVersion", RhinoApp.Version.ToString() },
                    { "captureTime", DateTime.UtcNow.ToString("o") }
                };

                // Upload
                var uploadTask = apiClient.UploadScreenshot(
                    projectId,
                    imageBytes,
                    imageName,
                    metadata
                );

                uploadTask.Wait();
                return uploadTask.Result;
            }
            catch (Exception ex)
            {
                return (false, $"Capture error: {ex.Message}", null);
            }
        }
    }

    // Capture dialog
    public class CaptureDialog : Dialog<DialogResult>
    {
        private DropDown projectDropdown;
        private TextBox nameTextBox;
        private List<VesselProject> projects;
        
        public string SelectedProjectId { get; private set; }
        public string SelectedProjectName { get; private set; }
        public string ImageName { get; private set; }

        public CaptureDialog(VesselStudioApiClient apiClient, VesselStudioSettings settings)
        {
            Title = "Capture to Vessel One";
            Padding = 10;
            Resizable = false;

            // Load projects
            var projectTask = apiClient.GetProjects();
            projectTask.Wait();
            projects = projectTask.Result;

            // Project dropdown
            projectDropdown = new DropDown();
            projectDropdown.Items.AddRange(projects.Select(p => new ListItem { Text = p.Name, Key = p.Id }));
            
            // Pre-select last used project
            if (!string.IsNullOrEmpty(settings.LastProjectId))
            {
                var lastIndex = projects.FindIndex(p => p.Id == settings.LastProjectId);
                if (lastIndex >= 0)
                    projectDropdown.SelectedIndex = lastIndex;
            }
            else if (projects.Count > 0)
            {
                projectDropdown.SelectedIndex = 0;
            }

            // Image name input
            nameTextBox = new TextBox 
            { 
                PlaceholderText = "e.g., Hull Profile View",
                Text = $"Rhino Capture {DateTime.Now:HH:mm:ss}"
            };

            // Layout
            Content = new TableLayout
            {
                Padding = 10,
                Spacing = new Size(5, 10),
                Rows =
                {
                    new TableRow(new Label { Text = "Send to project:" }),
                    new TableRow(projectDropdown),
                    new TableRow(new Label { Text = "Image name (optional):" }),
                    new TableRow(nameTextBox),
                }
            };

            // Buttons
            DefaultButton = new Button { Text = "Capture & Upload" };
            DefaultButton.Click += (s, e) =>
            {
                if (projectDropdown.SelectedIndex < 0)
                {
                    MessageBox.Show("Please select a project", MessageBoxType.Warning);
                    return;
                }

                var project = projects[projectDropdown.SelectedIndex];
                SelectedProjectId = project.Id;
                SelectedProjectName = project.Name;
                ImageName = string.IsNullOrWhiteSpace(nameTextBox.Text) 
                    ? $"Rhino Capture {DateTime.Now:HH:mm:ss}"
                    : nameTextBox.Text;
                
                Close(DialogResult.Ok);
            };

            AbortButton = new Button { Text = "Cancel" };
            AbortButton.Click += (s, e) => Close(DialogResult.Cancel);

            PositiveButtons.Add(DefaultButton);
            NegativeButtons.Add(AbortButton);
        }
    }
}
```

---

### Quick Capture Command (Power Users)

```csharp
// VesselStudioSimplePlugin/VesselStudioQuickCaptureCommand.cs
using Rhino;
using Rhino.Commands;
using System;

namespace VesselStudioSimplePlugin
{
    public class VesselStudioQuickCaptureCommand : Command
    {
        public override string EnglishName => "VesselQuickCapture";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var settings = VesselStudioSettings.Load();
            
            if (string.IsNullOrEmpty(settings.ApiKey))
            {
                RhinoApp.WriteLine("❌ API key not set. Run VesselSetApiKey first.");
                return Result.Failure;
            }

            if (string.IsNullOrEmpty(settings.LastProjectId))
            {
                RhinoApp.WriteLine("❌ No project selected. Run VesselCapture first to choose a project.");
                return Result.Failure;
            }

            var apiClient = new VesselStudioApiClient(settings.ApiKey);
            
            RhinoApp.WriteLine($"📸 Quick capturing to {settings.LastProjectName}...");

            var view = doc.Views.ActiveView;
            if (view == null)
            {
                RhinoApp.WriteLine("❌ No active viewport");
                return Result.Failure;
            }

            try
            {
                // Capture viewport
                var bitmap = view.CaptureToBitmap(new System.Drawing.Size(1920, 1080));
                
                byte[] imageBytes;
                using (var ms = new System.IO.MemoryStream())
                {
                    bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    imageBytes = ms.ToArray();
                }

                // Auto-generate name with timestamp
                var imageName = $"Quick Capture {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

                // Gather metadata
                var metadata = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "width", 1920 },
                    { "height", 1080 },
                    { "viewportName", view.MainViewport.Name },
                    { "displayMode", view.MainViewport.DisplayMode.EnglishName },
                    { "rhinoVersion", RhinoApp.Version.ToString() },
                    { "captureTime", DateTime.UtcNow.ToString("o") }
                };

                // Upload
                var uploadTask = apiClient.UploadScreenshot(
                    settings.LastProjectId,
                    imageBytes,
                    imageName,
                    metadata
                );

                uploadTask.Wait();
                var result = uploadTask.Result;

                if (result.success)
                {
                    RhinoApp.WriteLine($"✅ {result.message}");
                    return Result.Success;
                }
                else
                {
                    RhinoApp.WriteLine($"❌ {result.message}");
                    return Result.Failure;
                }
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"❌ Error: {ex.Message}");
                return Result.Failure;
            }
        }
    }
}
```

---

### Settings Storage

```csharp
// VesselStudioSimplePlugin/VesselStudioSettings.cs
using System;
using System.IO;
using Newtonsoft.Json;
using Rhino;

namespace VesselStudioSimplePlugin
{
    public class VesselStudioSettings
    {
        public string ApiKey { get; set; }
        public string LastProjectId { get; set; }
        public string LastProjectName { get; set; }
        public bool UploadToGallery { get; set; } = true;

        private static string SettingsPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VesselStudio",
            "settings.json"
        );

        public static VesselStudioSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    return JsonConvert.DeserializeObject<VesselStudioSettings>(json);
                }
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Warning: Could not load settings: {ex.Message}");
            }
            
            return new VesselStudioSettings();
        }

        public void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Warning: Could not save settings: {ex.Message}");
            }
        }
    }
}
```

---

## 🔄 Real-time Updates (Frontend)

### WebSocket Integration for Live Gallery Updates

```typescript
// app/projects/[id]/page.tsx
'use client';

import { useEffect, useState } from 'react';
import { db } from '@/lib/firebase';
import { collection, onSnapshot, query, orderBy } from 'firebase/firestore';

export default function ProjectPage({ params }: { params: { id: string } }) {
  const [galleryImages, setGalleryImages] = useState([]);

  useEffect(() => {
    // Real-time listener for new gallery images
    const q = query(
      collection(db, 'projects', params.id, 'gallery'),
      orderBy('uploadedAt', 'desc')
    );

    const unsubscribe = onSnapshot(q, (snapshot) => {
      const images = snapshot.docs.map(doc => ({
        id: doc.id,
        ...doc.data()
      }));
      setGalleryImages(images);
      
      // Show toast for new Rhino uploads
      snapshot.docChanges().forEach(change => {
        if (change.type === 'added' && change.doc.data().source === 'rhino-plugin') {
          toast.success(`New Rhino capture: ${change.doc.data().name}`);
        }
      });
    });

    return () => unsubscribe();
  }, [params.id]);

  return (
    <div className="gallery-grid">
      {galleryImages.map(image => (
        <div key={image.id} className="gallery-item">
          <img src={image.url} alt={image.name} />
          <div className="caption">
            <p>{image.name}</p>
            {image.source === 'rhino-plugin' && (
              <span className="badge">📐 Rhino</span>
            )}
          </div>
        </div>
      ))}
    </div>
  );
}
```

---

## 📊 User Flow Summary

### Initial Setup (One Time)
1. User creates API key in Vessel One web app
2. Copies key to Rhino via `VesselSetApiKey`
3. Plugin validates and stores key

### Standard Workflow
1. User types `VesselCapture` in Rhino
2. Selects destination project from dropdown
3. Optionally names the image
4. Chooses gallery vs private
5. Clicks "Capture & Upload"
6. Image appears in browser real-time

### Power User Workflow
1. User types `VesselQuickCapture`
2. Instantly captures to last-used project
3. Auto-generates timestamp name
4. No dialog interruption

---

## 🎨 UX Recommendations

### ✅ Best Practices
- **Cache last-used project** for quick capture mode
- **Show upload progress** for large images
- **Real-time browser updates** via Firestore listeners
- **Meaningful default names** with timestamps
- **Gallery vs private** clear distinction
- **API key validation** on first command run
- **Error messages** are actionable

### 🚀 Future Enhancements
- **Batch upload** multiple viewports at once
- **Annotation tools** draw on captures before upload
- **Upload queue** for offline capture → upload later
- **Project templates** save preferred upload settings
- **Keyboard shortcuts** for quick capture
- **Auto-capture on save** option
- **Canvas replacement mode** for iterative design
- **Version history** track design progression

---

## 🔐 Security Considerations

### API Key Security
- ✅ Keys hashed in database (SHA-256)
- ✅ Full key shown only once at creation
- ✅ Keys can be revoked anytime
- ✅ Usage tracking per key
- ✅ Permissions-based access

### Upload Security
- ✅ Verify user has project access
- ✅ Rate limiting on API endpoints
- ✅ File size limits (10MB max)
- ✅ Image validation (PNG/JPG only)
- ✅ Sanitize filenames

---

## 📝 Next Steps

1. **Implement API endpoints** in Vessel One backend (2-3 days)
2. **Update Rhino plugin** with dialog and API client (2-3 days)
3. **Add real-time listeners** to frontend (1 day)
4. **Testing** with real projects (1 day)
5. **Documentation** for users (0.5 day)

**Total Estimated Time:** 6-7 days

---

**Questions?**
- Where do images go? → Project Gallery (public) or Private Workspace
- How are projects selected? → Dropdown in capture dialog
- Quick capture? → `VesselQuickCapture` uses last-selected project
- Real-time updates? → Yes, via Firestore listeners
- Multiple shots? → Yes, quick capture mode supports rapid-fire


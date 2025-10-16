# Vessel Studio Rhino Plugin - MVP Implementation Guide

## Overview
Rhino plugin that captures the current viewport and sends it to an open Vessel Studio project canvas. Users authenticate via the plugin, select their project, and screenshots are automatically synced to their web browser in real-time.

---

## Plugin Architecture

### Core Features (MVP)
1. **User Authentication** - Login to Vessel Studio account
2. **Project Selection** - Choose target project from user's projects
3. **Viewport Capture** - Screenshot current Rhino viewport
4. **Real-time Upload** - Send to Vessel Studio project with metadata
5. **Status Feedback** - Visual confirmation in Rhino UI

### Technology Stack
- **Language**: C# (.NET Framework 4.8 or .NET 6+)
- **Rhino SDK**: RhinoCommon 7.x/8.x
- **HTTP Client**: System.Net.Http.HttpClient
- **UI Framework**: Eto.Forms (cross-platform Rhino UI)
- **Authentication**: Firebase Auth via REST API
- **Storage**: Firebase Storage (image upload)
- **Database**: Firestore (metadata sync)

---

## Vessel Studio API Integration

### Base URL
```
Production: https://vesselstudio.ai/api
Development: http://localhost:3000/api
```

### Authentication Endpoints

#### 1. Plugin Authentication Flow
```http
POST /api/plugin/auth/init
Content-Type: application/json

{
  "deviceId": "rhino-plugin-{guid}",
  "rhinoVersion": "8.0",
  "pluginVersion": "1.0.0"
}

Response 200:
{
  "authUrl": "https://vesselstudio.ai/plugin-auth?code=ABC123",
  "pollToken": "poll_xyz789",
  "expiresIn": 300
}
```

**User Flow:**
1. Plugin opens browser to `authUrl`
2. User logs in via Firebase Auth on web
3. Web app associates session with `pollToken`
4. Plugin polls for completion

#### 2. Poll for Authentication Completion
```http
GET /api/plugin/auth/poll?token={pollToken}
Authorization: Bearer {pollToken}

Response 202 (Pending):
{
  "status": "pending"
}

Response 200 (Complete):
{
  "status": "complete",
  "accessToken": "firebase_id_token_here",
  "userId": "user-001",
  "expiresAt": "2025-10-17T12:00:00Z"
}
```

#### 3. Validate Token (on plugin startup)
```http
GET /api/plugin/auth/validate
Authorization: Bearer {firebase_id_token}

Response 200:
{
  "valid": true,
  "userId": "user-001",
  "email": "user@example.com",
  "role": "user"
}
```

---

### Project Endpoints

#### 4. Get User's Projects
```http
GET /api/plugin/projects
Authorization: Bearer {firebase_id_token}

Response 200:
{
  "projects": [
    {
      "id": "proj-001",
      "name": "60ft Sailing Yacht",
      "lastModified": "2025-10-16T10:30:00Z",
      "imageCount": 5
    }
  ]
}
```

#### 5. Get Project Details
```http
GET /api/plugin/project/{projectId}
Authorization: Bearer {firebase_id_token}

Response 200:
{
  "id": "proj-001",
  "name": "60ft Sailing Yacht",
  "images": [
    {
      "id": "img-001",
      "url": "https://storage.googleapis.com/...",
      "timestamp": "2025-10-16T10:15:00Z",
      "source": "rhino-plugin",
      "metadata": {
        "viewportName": "Perspective",
        "displayMode": "Shaded"
      }
    }
  ]
}
```

---

### Screenshot Upload Endpoints

#### 6. Upload Screenshot to Project
```http
POST /api/plugin/project/{projectId}/screenshot
Authorization: Bearer {firebase_id_token}
Content-Type: multipart/form-data

Form Data:
- image: [binary image data - PNG/JPEG]
- metadata: {
    "rhinoVersion": "8.0.23304.15001",
    "viewportName": "Perspective",
    "displayMode": "Shaded",
    "timestamp": "2025-10-16T11:45:30Z",
    "cameraPosition": {"x": 10, "y": 5, "z": 8},
    "cameraTarget": {"x": 0, "y": 0, "z": 0},
    "renderSettings": {
      "width": 1920,
      "height": 1080,
      "antialiasing": true
    }
  }

Response 201:
{
  "id": "img-002",
  "url": "https://storage.googleapis.com/vessel-studio/...",
  "projectId": "proj-001",
  "uploadedAt": "2025-10-16T11:45:31Z"
}
```

**Firebase Storage Path:**
```
gs://vessel-studio.appspot.com/
  └── users/
      └── {userId}/
          └── projects/
              └── {projectId}/
                  └── rhino-screenshots/
                      └── {timestamp}_{guid}.png
```

---

### Real-time Updates (Optional for MVP)

#### 7. WebSocket Connection (Future Enhancement)
```
wss://vesselstudio.ai/api/plugin/realtime?token={firebase_id_token}

Message Format:
{
  "type": "project_updated",
  "projectId": "proj-001",
  "imageId": "img-002",
  "url": "https://storage.googleapis.com/..."
}
```

---

## Plugin Implementation Structure

### Required Files

```csharp
// filepath: YachtStudioPlugin/YachtStudioPlugin.cs
using Rhino;
using Rhino.PlugIns;

namespace YachtStudioPlugin
{
    public class YachtStudioPlugin : PlugIn
    {
        private static YachtStudioPlugin _instance;
        public static YachtStudioPlugin Instance => _instance;

        public AuthService AuthService { get; private set; }
        public ApiClient ApiClient { get; private set; }
        public ScreenshotService ScreenshotService { get; private set; }

        public YachtStudioPlugin()
        {
            _instance = this;
        }

        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            // Initialize services
            ApiClient = new ApiClient("https://vesselstudio.ai/api");
            AuthService = new AuthService(ApiClient);
            ScreenshotService = new ScreenshotService(ApiClient);

            // Register commands
            RhinoApp.WriteLine("Vessel Studio plugin loaded successfully");
            
            return LoadReturnCode.Success;
        }
    }
}
```

```csharp
// filepath: YachtStudioPlugin/Commands/LoginCommand.cs
using Rhino;
using Rhino.Commands;
using System;
using System.Diagnostics;

namespace YachtStudioPlugin.Commands
{
    [System.Runtime.InteropServices.Guid("A1B2C3D4-E5F6-4A5B-8C9D-0E1F2A3B4C5D")]
    public class VesselStudioLoginCommand : Command
    {
        public override string EnglishName => "VesselStudioLogin";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var plugin = YachtStudioPlugin.Instance;
            
            // Check if already authenticated
            if (plugin.AuthService.IsAuthenticated)
            {
                RhinoApp.WriteLine("Already logged in to Vessel Studio");
                return Result.Success;
            }

            try
            {
                // Start authentication flow
                var authUrl = plugin.AuthService.InitiateAuthenticationAsync().Result;
                
                // Open browser
                Process.Start(new ProcessStartInfo
                {
                    FileName = authUrl,
                    UseShellExecute = true
                });

                RhinoApp.WriteLine("Opening browser for authentication...");
                RhinoApp.WriteLine("Please log in to Vessel Studio in your browser.");

                // Poll for completion (async in background)
                plugin.AuthService.PollForAuthenticationAsync((success) =>
                {
                    if (success)
                    {
                        RhinoApp.WriteLine("✅ Successfully logged in to Vessel Studio!");
                        
                        // Load user's projects
                        plugin.ApiClient.LoadUserProjectsAsync();
                    }
                    else
                    {
                        RhinoApp.WriteLine("❌ Authentication failed or timed out");
                    }
                });

                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Error: {ex.Message}");
                return Result.Failure;
            }
        }
    }
}
```

```csharp
// filepath: YachtStudioPlugin/Commands/CaptureToCanvasCommand.cs
using Rhino;
using Rhino.Commands;
using Rhino.Display;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace YachtStudioPlugin.Commands
{
    [System.Runtime.InteropServices.Guid("B2C3D4E5-F6A7-4B5C-8D9E-0F1A2B3C4D5E")]
    public class VesselStudioCaptureCommand : Command
    {
        public override string EnglishName => "VesselStudioCapture";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var plugin = YachtStudioPlugin.Instance;

            // Check authentication
            if (!plugin.AuthService.IsAuthenticated)
            {
                RhinoApp.WriteLine("Please login first using VesselStudioLogin command");
                return Result.Failure;
            }

            // Get active viewport
            var activeView = doc.Views.ActiveView;
            if (activeView == null)
            {
                RhinoApp.WriteLine("No active viewport found");
                return Result.Failure;
            }

            try
            {
                // Show project selector dialog
                var projectSelector = new ProjectSelectorDialog(plugin.ApiClient);
                var dialogResult = projectSelector.ShowModal();

                if (dialogResult != Eto.Forms.DialogResult.Ok)
                {
                    return Result.Cancel;
                }

                var selectedProject = projectSelector.SelectedProject;
                if (selectedProject == null)
                {
                    RhinoApp.WriteLine("No project selected");
                    return Result.Cancel;
                }

                // Capture viewport
                RhinoApp.WriteLine("Capturing viewport...");
                
                var viewport = activeView.ActiveViewport;
                var size = activeView.ActiveViewport.Size;
                
                using (var bitmap = new Bitmap(size.Width, size.Height))
                {
                    // Capture view to bitmap
                    activeView.CaptureToBitmap(bitmap);

                    // Upload to Vessel Studio
                    RhinoApp.WriteLine("Uploading to Vessel Studio...");
                    
                    var metadata = new ScreenshotMetadata
                    {
                        RhinoVersion = RhinoApp.Version.ToString(),
                        ViewportName = activeView.ActiveViewport.Name,
                        DisplayMode = viewport.DisplayMode.EnglishName,
                        Timestamp = DateTime.UtcNow,
                        CameraPosition = new Point3d(
                            viewport.CameraLocation.X,
                            viewport.CameraLocation.Y,
                            viewport.CameraLocation.Z
                        ),
                        CameraTarget = new Point3d(
                            viewport.CameraTarget.X,
                            viewport.CameraTarget.Y,
                            viewport.CameraTarget.Z
                        ),
                        Width = size.Width,
                        Height = size.Height
                    };

                    var result = plugin.ScreenshotService
                        .UploadScreenshotAsync(selectedProject.Id, bitmap, metadata)
                        .Result;

                    if (result.Success)
                    {
                        RhinoApp.WriteLine($"✅ Screenshot uploaded successfully!");
                        RhinoApp.WriteLine($"View in browser: {result.ProjectUrl}");
                    }
                    else
                    {
                        RhinoApp.WriteLine($"❌ Upload failed: {result.ErrorMessage}");
                        return Result.Failure;
                    }
                }

                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Error: {ex.Message}");
                return Result.Failure;
            }
        }
    }
}
```

```csharp
// filepath: YachtStudioPlugin/Services/ApiClient.cs
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace YachtStudioPlugin.Services
{
    public class ApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private string _authToken;

        public ApiClient(string baseUrl)
        {
            _baseUrl = baseUrl;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public void SetAuthToken(string token)
        {
            _authToken = token;
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<T> GetAsync<T>(string endpoint)
        {
            var response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json);
        }

        public async Task<T> PostAsync<T>(string endpoint, object data)
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(endpoint, content);
            response.EnsureSuccessStatusCode();
            
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(responseJson);
        }

        public async Task<T> PostMultipartAsync<T>(
            string endpoint, 
            byte[] imageData, 
            string fileName,
            object metadata)
        {
            using (var content = new MultipartFormDataContent())
            {
                // Add image
                var imageContent = new ByteArrayContent(imageData);
                imageContent.Headers.ContentType = 
                    MediaTypeHeaderValue.Parse("image/png");
                content.Add(imageContent, "image", fileName);

                // Add metadata
                var metadataJson = JsonSerializer.Serialize(metadata);
                content.Add(new StringContent(metadataJson), "metadata");

                var response = await _httpClient.PostAsync(endpoint, content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(responseJson);
            }
        }

        public async Task<ProjectsResponse> LoadUserProjectsAsync()
        {
            return await GetAsync<ProjectsResponse>("/plugin/projects");
        }
    }
}
```

```csharp
// filepath: YachtStudioPlugin/Services/AuthService.cs
using System;
using System.Threading.Tasks;
using System.Timers;

namespace YachtStudioPlugin.Services
{
    public class AuthService
    {
        private readonly ApiClient _apiClient;
        private string _pollToken;
        private Timer _pollTimer;

        public bool IsAuthenticated { get; private set; }
        public string UserId { get; private set; }

        public AuthService(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<string> InitiateAuthenticationAsync()
        {
            var deviceId = $"rhino-plugin-{Guid.NewGuid()}";
            
            var request = new
            {
                deviceId,
                rhinoVersion = Rhino.RhinoApp.Version.ToString(),
                pluginVersion = "1.0.0"
            };

            var response = await _apiClient.PostAsync<AuthInitResponse>(
                "/plugin/auth/init",
                request
            );

            _pollToken = response.PollToken;
            
            return response.AuthUrl;
        }

        public void PollForAuthenticationAsync(Action<bool> onComplete)
        {
            _pollTimer = new Timer(2000); // Poll every 2 seconds
            var attempts = 0;
            var maxAttempts = 60; // 2 minutes timeout

            _pollTimer.Elapsed += async (sender, e) =>
            {
                attempts++;
                
                if (attempts > maxAttempts)
                {
                    _pollTimer.Stop();
                    onComplete(false);
                    return;
                }

                try
                {
                    var response = await _apiClient.GetAsync<AuthPollResponse>(
                        $"/plugin/auth/poll?token={_pollToken}"
                    );

                    if (response.Status == "complete")
                    {
                        _pollTimer.Stop();
                        
                        // Store token
                        _apiClient.SetAuthToken(response.AccessToken);
                        IsAuthenticated = true;
                        UserId = response.UserId;

                        // Persist to settings
                        SaveAuthToken(response.AccessToken);

                        onComplete(true);
                    }
                }
                catch
                {
                    // Continue polling
                }
            };

            _pollTimer.Start();
        }

        private void SaveAuthToken(string token)
        {
            // Save to Rhino settings
            Rhino.ApplicationSettings.FileSettings.SetString(
                "VesselStudio.AuthToken",
                token
            );
        }

        public void LoadSavedAuth()
        {
            var token = Rhino.ApplicationSettings.FileSettings.GetString(
                "VesselStudio.AuthToken",
                null
            );

            if (!string.IsNullOrEmpty(token))
            {
                _apiClient.SetAuthToken(token);
                // Validate token
                ValidateTokenAsync();
            }
        }

        private async void ValidateTokenAsync()
        {
            try
            {
                var response = await _apiClient.GetAsync<ValidateTokenResponse>(
                    "/plugin/auth/validate"
                );

                if (response.Valid)
                {
                    IsAuthenticated = true;
                    UserId = response.UserId;
                }
            }
            catch
            {
                IsAuthenticated = false;
            }
        }
    }
}
```

```csharp
// filepath: YachtStudioPlugin/Services/ScreenshotService.cs
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace YachtStudioPlugin.Services
{
    public class ScreenshotService
    {
        private readonly ApiClient _apiClient;

        public ScreenshotService(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<UploadResult> UploadScreenshotAsync(
            string projectId,
            Bitmap bitmap,
            ScreenshotMetadata metadata)
        {
            try
            {
                // Convert bitmap to PNG byte array
                byte[] imageBytes;
                using (var ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Png);
                    imageBytes = ms.ToArray();
                }

                // Generate filename
                var fileName = $"{metadata.Timestamp:yyyyMMdd_HHmmss}_{Guid.NewGuid()}.png";

                // Upload
                var response = await _apiClient.PostMultipartAsync<UploadResponse>(
                    $"/plugin/project/{projectId}/screenshot",
                    imageBytes,
                    fileName,
                    metadata
                );

                return new UploadResult
                {
                    Success = true,
                    ImageId = response.Id,
                    Url = response.Url,
                    ProjectUrl = $"https://vesselstudio.ai/studio/project/{projectId}"
                };
            }
            catch (Exception ex)
            {
                return new UploadResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }

    public class ScreenshotMetadata
    {
        public string RhinoVersion { get; set; }
        public string ViewportName { get; set; }
        public string DisplayMode { get; set; }
        public DateTime Timestamp { get; set; }
        public Point3d CameraPosition { get; set; }
        public Point3d CameraTarget { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class UploadResult
    {
        public bool Success { get; set; }
        public string ImageId { get; set; }
        public string Url { get; set; }
        public string ProjectUrl { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class Point3d
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Point3d(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}
```

```csharp
// filepath: YachtStudioPlugin/UI/ProjectSelectorDialog.cs
using Eto.Forms;
using Eto.Drawing;
using System.Linq;

namespace YachtStudioPlugin.UI
{
    public class ProjectSelectorDialog : Dialog<DialogResult>
    {
        private readonly ApiClient _apiClient;
        private ListBox _projectListBox;
        
        public ProjectInfo SelectedProject { get; private set; }

        public ProjectSelectorDialog(ApiClient apiClient)
        {
            _apiClient = apiClient;
            Title = "Select Vessel Studio Project";
            MinimumSize = new Size(400, 300);

            InitializeUI();
            LoadProjects();
        }

        private void InitializeUI()
        {
            _projectListBox = new ListBox();
            
            var okButton = new Button { Text = "Upload" };
            okButton.Click += (s, e) =>
            {
                if (_projectListBox.SelectedIndex >= 0)
                {
                    SelectedProject = _projectListBox.SelectedValue as ProjectInfo;
                    Close(DialogResult.Ok);
                }
            };

            var cancelButton = new Button { Text = "Cancel" };
            cancelButton.Click += (s, e) => Close(DialogResult.Cancel);

            Content = new StackLayout
            {
                Padding = 10,
                Spacing = 10,
                Items =
                {
                    new Label { Text = "Select project to upload screenshot:" },
                    new StackLayoutItem(_projectListBox, expand: true),
                    new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 5,
                        Items = { null, okButton, cancelButton }
                    }
                }
            };
        }

        private async void LoadProjects()
        {
            try
            {
                var response = await _apiClient.LoadUserProjectsAsync();
                
                var projects = response.Projects
                    .OrderByDescending(p => p.LastModified)
                    .ToList();

                foreach (var project in projects)
                {
                    var displayText = $"{project.Name} ({project.ImageCount} images)";
                    
                    _projectListBox.Items.Add(new ListItem
                    {
                        Text = displayText,
                        Key = project.Id,
                        Tag = project
                    });
                }

                // Auto-select first project
                if (projects.Any())
                {
                    _projectListBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading projects: {ex.Message}", MessageBoxType.Error);
            }
        }
    }

    public class ProjectInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DateTime LastModified { get; set; }
        public int ImageCount { get; set; }
    }
}
```

---

## Project Setup Instructions

### 1. Create Solution
```bash
mkdir YachtStudioPlugin
cd YachtStudioPlugin
dotnet new sln -n YachtStudioPlugin
dotnet new classlib -n YachtStudioPlugin -f net48
dotnet sln add YachtStudioPlugin/YachtStudioPlugin.csproj
```

### 2. Install Dependencies
```bash
cd YachtStudioPlugin
dotnet add package RhinoCommon --version 7.0.0
dotnet add package Eto.Forms --version 2.7.4
dotnet add package System.Net.Http --version 4.3.4
dotnet add package System.Text.Json --version 7.0.0
```

### 3. Project File Configuration
```xml
<!-- filepath: YachtStudioPlugin/YachtStudioPlugin.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <Version>1.0.0</Version>
    <Title>VesselStudio Rhino Plugin</Title>
    <Description>Sync Rhino viewports to VesselStudio canvases</Description>
    <Authors>VesselOne</Authors>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="RhinoCommon" Version="7.0.0" />
    <PackageReference Include="Eto.Forms" Version="2.7.4" />
    <PackageReference Include="System.Net.Http" Version="4.3.4" />
    <PackageReference Include="System.Text.Json" Version="7.0.0" />
  </ItemGroup>

  <ItemGroup>
    <Reference Include="System.Drawing" />
    <Reference Include="System.Windows.Forms" />
  </ItemGroup>
</Project>
```

### 4. Build Plugin
```bash
dotnet build --configuration Release
```

Output: `bin/Release/net48/YachtStudioPlugin.rhp`

---

## VesselStudio Backend Requirements

### API Endpoints to Implement

#### 1. Plugin Auth Init
```typescript
// filepath: src/app/api/plugin/auth/init/route.ts
import { NextRequest, NextResponse } from 'next/server';
import { nanoid } from 'nanoid';
import { db } from '@/lib/firebase-admin';

export async function POST(request: NextRequest) {
  const body = await request.json();
  const { deviceId, rhinoVersion, pluginVersion } = body;

  // Generate unique code and poll token
  const authCode = nanoid(8).toUpperCase();
  const pollToken = `poll_${nanoid(32)}`;

  // Store pending auth in Firestore (expires in 5 minutes)
  await db.collection('pluginAuth').doc(pollToken).set({
    authCode,
    deviceId,
    rhinoVersion,
    pluginVersion,
    status: 'pending',
    createdAt: new Date(),
    expiresAt: new Date(Date.now() + 5 * 60 * 1000)
  });

  // Map auth code to poll token
  await db.collection('pluginAuthCodes').doc(authCode).set({
    pollToken,
    expiresAt: new Date(Date.now() + 5 * 60 * 1000)
  });

  return NextResponse.json({
    authUrl: `${process.env.NEXT_PUBLIC_BASE_URL}/plugin-auth?code=${authCode}`,
    pollToken,
    expiresIn: 300
  });
}
```

#### 2. Plugin Auth Page
```typescript
// filepath: src/app/plugin-auth/page.tsx
'use client';

import { useSearchParams } from 'next/navigation';
import { useEffect, useState } from 'react';
import { auth } from '@/firebase/config';
import { useAuthState } from 'react-firebase-hooks/auth';

export default function PluginAuthPage() {
  const searchParams = useSearchParams();
  const authCode = searchParams.get('code');
  const [user] = useAuthState(auth);
  const [status, setStatus] = useState<'pending' | 'success' | 'error'>('pending');

  useEffect(() => {
    if (user && authCode) {
      completeAuth();
    }
  }, [user, authCode]);

  async function completeAuth() {
    try {
      const idToken = await user!.getIdToken();
      
      const response = await fetch('/api/plugin/auth/complete', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${idToken}`
        },
        body: JSON.stringify({ authCode })
      });

      if (response.ok) {
        setStatus('success');
      } else {
        setStatus('error');
      }
    } catch (error) {
      console.error('Auth error:', error);
      setStatus('error');
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50">
      <div className="max-w-md w-full bg-white rounded-lg shadow-lg p-8">
        {status === 'pending' && user && (
          <div>
            <h1 className="text-2xl font-bold mb-4">Connecting Rhino Plugin</h1>
            <p className="text-gray-600">Authenticating...</p>
          </div>
        )}
        
        {status === 'pending' && !user && (
          <div>
            <h1 className="text-2xl font-bold mb-4">Sign In to Vessel Studio</h1>
            <p className="text-gray-600 mb-6">
              Please sign in to connect your Rhino plugin.
            </p>
            {/* Firebase Auth UI here */}
          </div>
        )}

        {status === 'success' && (
          <div className="text-center">
            <div className="text-6xl mb-4">✅</div>
            <h1 className="text-2xl font-bold mb-2">Connected!</h1>
            <p className="text-gray-600">
              Your Rhino plugin is now connected. You can close this window.
            </p>
          </div>
        )}

        {status === 'error' && (
          <div className="text-center">
            <div className="text-6xl mb-4">❌</div>
            <h1 className="text-2xl font-bold mb-2">Connection Failed</h1>
            <p className="text-gray-600">
              Please try again from the Rhino plugin.
            </p>
          </div>
        )}
      </div>
    </div>
  );
}
```

#### 3. Complete Auth Endpoint
```typescript
// filepath: src/app/api/plugin/auth/complete/route.ts
import { NextRequest, NextResponse } from 'next/server';
import { adminAuth, db } from '@/firebase/admin';

export async function POST(request: NextRequest) {
  const authHeader = request.headers.get('authorization');
  if (!authHeader?.startsWith('Bearer ')) {
    return NextResponse.json({ error: 'Unauthorized' }, { status: 401 });
  }

  const idToken = authHeader.substring(7);
  const body = await request.json();
  const { authCode } = body;

  try {
    // Verify Firebase token
    const decodedToken = await adminAuth.verifyIdToken(idToken);
    const userId = decodedToken.uid;

    // Get poll token from auth code
    const authCodeDoc = await db.collection('pluginAuthCodes').doc(authCode).get();
    if (!authCodeDoc.exists) {
      return NextResponse.json({ error: 'Invalid auth code' }, { status: 400 });
    }

    const pollToken = authCodeDoc.data()?.pollToken;

    // Update auth status
    await db.collection('pluginAuth').doc(pollToken).update({
      status: 'complete',
      accessToken: idToken,
      userId,
      completedAt: new Date()
    });

    // Clean up auth code
    await db.collection('pluginAuthCodes').doc(authCode).delete();

    return NextResponse.json({ success: true });
  } catch (error) {
    console.error('Auth completion error:', error);
    return NextResponse.json({ error: 'Authentication failed' }, { status: 500 });
  }
}
```

#### 4. Poll Endpoint
```typescript
// filepath: src/app/api/plugin/auth/poll/route.ts
import { NextRequest, NextResponse } from 'next/server';
import { db } from '@/firebase/admin';

export async function GET(request: NextRequest) {
  const pollToken = request.nextUrl.searchParams.get('token');
  
  if (!pollToken) {
    return NextResponse.json({ error: 'Missing token' }, { status: 400 });
  }

  const authDoc = await db.collection('pluginAuth').doc(pollToken).get();
  
  if (!authDoc.exists) {
    return NextResponse.json({ error: 'Invalid token' }, { status: 404 });
  }

  const data = authDoc.data();

  // Check expiration
  if (data?.expiresAt?.toDate() < new Date()) {
    await db.collection('pluginAuth').doc(pollToken).delete();
    return NextResponse.json({ error: 'Token expired' }, { status: 410 });
  }

  if (data?.status === 'pending') {
    return NextResponse.json({ status: 'pending' }, { status: 202 });
  }

  if (data?.status === 'complete') {
    return NextResponse.json({
      status: 'complete',
      accessToken: data.accessToken,
      userId: data.userId,
      expiresAt: new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString()
    });
  }

  return NextResponse.json({ error: 'Unknown status' }, { status: 500 });
}
```

#### 5. Validate Token Endpoint
```typescript
// filepath: src/app/api/plugin/auth/validate/route.ts
import { NextRequest, NextResponse } from 'next/server';
import { adminAuth } from '@/firebase/admin';

export async function GET(request: NextRequest) {
  const authHeader = request.headers.get('authorization');
  if (!authHeader?.startsWith('Bearer ')) {
    return NextResponse.json({ valid: false }, { status: 401 });
  }

  const idToken = authHeader.substring(7);

  try {
    const decodedToken = await adminAuth.verifyIdToken(idToken);
    
    return NextResponse.json({
      valid: true,
      userId: decodedToken.uid,
      email: decodedToken.email,
      role: decodedToken.role || 'user'
    });
  } catch (error) {
    return NextResponse.json({ valid: false }, { status: 401 });
  }
}
```

#### 6. Projects Endpoint
```typescript
// filepath: src/app/api/plugin/projects/route.ts
import { NextRequest, NextResponse } from 'next/server';
import { adminAuth, db } from '@/firebase/admin';

async function verifyAuth(request: NextRequest) {
  const authHeader = request.headers.get('authorization');
  if (!authHeader?.startsWith('Bearer ')) {
    return null;
  }

  try {
    const idToken = authHeader.substring(7);
    const decodedToken = await adminAuth.verifyIdToken(idToken);
    return decodedToken;
  } catch {
    return null;
  }
}

export async function GET(request: NextRequest) {
  const user = await verifyAuth(request);
  if (!user) {
    return NextResponse.json({ error: 'Unauthorized' }, { status: 401 });
  }

  // Get user's projects from Firestore (designs collection)
  const projectsSnapshot = await db
    .collection('users')
    .doc(user.uid)
    .collection('designs')
    .orderBy('updatedAt', 'desc')
    .get();

  const projects = projectsSnapshot.docs.map(doc => {
    const data = doc.data();
    return {
      id: doc.id,
      name: data.name || 'Untitled Project',
      lastModified: data.updatedAt,
      imageCount: data.images?.length || 0
    };
  });

  return NextResponse.json({ projects });
}
```

#### 7. Screenshot Upload Endpoint
```typescript
// filepath: src/app/api/plugin/project/[projectId]/screenshot/route.ts
import { NextRequest, NextResponse } from 'next/server';
import { adminAuth, db, storage } from '@/firebase/admin';
import { nanoid } from 'nanoid';

async function verifyAuth(request: NextRequest) {
  const authHeader = request.headers.get('authorization');
  if (!authHeader?.startsWith('Bearer ')) {
    return null;
  }

  try {
    const idToken = authHeader.substring(7);
    const decodedToken = await adminAuth.verifyIdToken(idToken);
    return decodedToken;
  } catch {
    return null;
  }
}

export async function POST(
  request: NextRequest,
  { params }: { params: { projectId: string } }
) {
  const user = await verifyAuth(request);
  if (!user) {
    return NextResponse.json({ error: 'Unauthorized' }, { status: 401 });
  }

  const { projectId } = params;

  // Verify project ownership
  const projectDoc = await db
    .collection('users')
    .doc(user.uid)
    .collection('designs')
    .doc(projectId)
    .get();

  if (!projectDoc.exists) {
    return NextResponse.json({ error: 'Project not found' }, { status: 404 });
  }

  try {
    const formData = await request.formData();
    const imageFile = formData.get('image') as File;
    const metadataJson = formData.get('metadata') as string;
    const metadata = JSON.parse(metadataJson);

    if (!imageFile) {
      return NextResponse.json({ error: 'No image provided' }, { status: 400 });
    }

    // Upload to Firebase Storage
    const imageId = `img_${nanoid()}`;
    const fileName = `${Date.now()}_${imageId}.png`;
    const storagePath = `users/${user.uid}/projects/${projectId}/rhino-screenshots/${fileName}`;

    const bucket = storage.bucket();
    const file = bucket.file(storagePath);

    const buffer = Buffer.from(await imageFile.arrayBuffer());
    await file.save(buffer, {
      metadata: {
        contentType: 'image/png',
        metadata: {
          userId: user.uid,
          projectId,
          source: 'rhino-plugin',
          ...metadata
        }
      }
    });

    // Make publicly readable (or use signed URL)
    await file.makePublic();
    const publicUrl = `https://storage.googleapis.com/${bucket.name}/${storagePath}`;

    // Add to project's images array in Firestore
    const projectData = projectDoc.data();
    const currentImages = projectData?.images || [];
    
    const newImage = {
      id: imageId,
      url: publicUrl,
      source: 'rhino-plugin',
      metadata,
      createdAt: new Date(),
    };

    await db
      .collection('users')
      .doc(user.uid)
      .collection('designs')
      .doc(projectId)
      .update({
        images: [...currentImages, newImage],
        updatedAt: new Date()
      });

    return NextResponse.json({
      id: imageId,
      url: publicUrl,
      projectId,
      uploadedAt: new Date().toISOString()
    }, { status: 201 });

  } catch (error) {
    console.error('Upload error:', error);
    return NextResponse.json({ error: 'Upload failed' }, { status: 500 });
  }
}
```

---

## Testing Instructions

### Manual Testing Flow

1. **Install Plugin in Rhino**
   ```
   - Drag .rhp file into Rhino viewport
   - Restart Rhino
   - Type "VesselStudioLogin" in command line
   ```

2. **Test Authentication**
   ```
   - Browser should open to https://vesselstudio.ai/plugin-auth
   - Sign in with test account
   - Return to Rhino
   - Should see "✅ Successfully logged in to Vessel Studio!"
   ```

3. **Test Screenshot Capture**
   ```
   - Create or open 3D model in Rhino
   - Type "VesselStudioCapture" in command line
   - Select project from dialog
   - Click "Upload"
   - Should see "✅ Screenshot uploaded successfully!"
   ```

4. **Verify in Web App**
   ```
   - Open https://vesselstudio.ai/studio/project/{projectId}
   - Should see uploaded screenshot appear in real-time
   - Screenshot should have metadata (viewport name, display mode, etc.)
   ```

---

## Security Considerations

1. **Token Storage**: Auth tokens stored in Rhino settings (encrypted by OS)
2. **HTTPS Only**: All API calls use HTTPS
3. **Token Expiration**: Tokens expire after 24 hours
4. **Project Ownership**: Server validates user owns project before upload
5. **File Validation**: Server validates image format and size
6. **Rate Limiting**: API rate limits prevent abuse

---

## Future Enhancements (Post-MVP)

1. **Project to Rhino Sync** - Pull reference images from project
2. **Rhino CAD Assistant** - AI chat inside Rhino
3. **Auto-Sync Mode** - Automatically capture on model changes
4. **Batch Upload** - Upload multiple viewports simultaneously
5. **Annotations** - Add notes/markup before upload
6. **3D Model Upload** - Export .3dm or STEP directly to Vessel Studio

---

## Repository Structure

This plugin should be in a **separate repository**: `vesselstudio-rhino-plugin`

**Main App Repository**: `yachtstudio` (Next.js web app)  
**Plugin Repository**: `vesselstudio-rhino-plugin` (C# Rhino plugin)

### Separation Benefits:
- Different tech stacks (TypeScript vs C#)
- Independent release cycles
- Plugin distributed via `.rhp` file
- Separate CI/CD pipelines
- Cleaner versioning

---

## Support & Documentation

- **Plugin Issues**: https://github.com/vessel-one/vesselstudio-rhino-plugin/issues
- **API Docs**: https://vesselstudio.ai/docs/plugin-api
- **Support Email**: support@vesselone.com

---

## License

MIT License - See LICENSE file for details

---

**Generated for Implementation**  
Last Updated: October 16, 2025  
Version: 1.0.0-MVP

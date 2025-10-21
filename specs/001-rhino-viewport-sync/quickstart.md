# Quickstart: Rhino Viewport Sync Plugin

**Feature**: 001-rhino-viewport-sync  
**Date**: October 16, 2025  
**Purpose**: Development setup and implementation guide

## Development Environment Setup

### Prerequisites
- **Visual Studio 2022** or **Visual Studio Code** with C# extension
- **Rhino 7** or **Rhino 8** installed for testing
- **.NET Framework 4.8 SDK** (for Rhino 7 compatibility) or **.NET 6 SDK** (for Rhino 8)
- **Git** for version control

### Project Structure Creation
```bash
# Clone repository and navigate to project
cd "C:\Users\rikki.mcguire\Documents\vesselone\Yacht Studio Rhino Plugin"

# Create plugin project structure
mkdir VesselStudioPlugin
cd VesselStudioPlugin

# Create Visual Studio solution
dotnet new sln -n VesselStudioPlugin

# Create class library project
dotnet new classlib -n VesselStudioPlugin -f net48
dotnet sln add VesselStudioPlugin/VesselStudioPlugin.csproj

# Create test project
dotnet new nunit -n VesselStudioPlugin.Tests -f net48
dotnet sln add VesselStudioPlugin.Tests/VesselStudioPlugin.Tests.csproj
dotnet add VesselStudioPlugin.Tests reference VesselStudioPlugin
```

### Dependencies Installation
```bash
# Navigate to plugin project
cd VesselStudioPlugin

# Add Rhino dependencies
dotnet add package RhinoCommon --version 7.0.0
dotnet add package Eto.Forms --version 2.7.4

# Add HTTP and JSON dependencies
dotnet add package System.Net.Http --version 4.3.4
dotnet add package System.Text.Json --version 7.0.0

# Add credential management (Windows)
dotnet add package Microsoft.Win32.Registry --version 5.0.0

# Add testing dependencies to test project
cd ../VesselStudioPlugin.Tests
dotnet add package NUnit --version 3.13.3
dotnet add package NUnit3TestAdapter --version 4.5.0
dotnet add package Microsoft.NET.Test.Sdk --version 17.7.2
dotnet add package Moq --version 4.20.69
```

### Project Configuration

#### VesselStudioPlugin.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <Version>1.0.0</Version>
    <Title>Vessel Studio Rhino Plugin</Title>
    <Description>Sync Rhino viewports to Vessel Studio canvases</Description>
    <Authors>VesselOne</Authors>
    <Company>VesselOne</Company>
    <Product>Vessel Studio</Product>
    <Copyright>Copyright © 2025 VesselOne</Copyright>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="RhinoCommon" Version="7.0.0" />
    <PackageReference Include="Eto.Forms" Version="2.7.4" />
    <PackageReference Include="System.Net.Http" Version="4.3.4" />
    <PackageReference Include="System.Text.Json" Version="7.0.0" />
    <PackageReference Include="Microsoft.Win32.Registry" Version="5.0.0" />
  </ItemGroup>

  <ItemGroup>
    <Reference Include="System.Drawing" />
    <Reference Include="System.Windows.Forms" />
  </ItemGroup>

  <!-- Post-build: Copy to Rhino plugins directory for testing -->
  <Target Name="CopyToRhino" AfterTargets="Build">
    <ItemGroup>
      <PluginFiles Include="$(OutputPath)**\*" />
    </ItemGroup>
    <Copy 
      SourceFiles="@(PluginFiles)" 
      DestinationFolder="$(APPDATA)\McNeel\Rhinoceros\7.0\Plug-ins\VesselStudio\$(Configuration)" 
      SkipUnchangedFiles="true" />
  </Target>
</Project>
```

## Core Implementation Steps

### Step 1: Plugin Entry Point
Create the main plugin class that Rhino will load:

**File**: `VesselStudioPlugin/VesselStudioPlugin.cs`
```csharp
using Rhino;
using Rhino.PlugIns;
using System;

namespace VesselStudioPlugin
{
    public class VesselStudioPlugin : PlugIn
    {
        private static VesselStudioPlugin _instance;
        public static VesselStudioPlugin Instance => _instance;

        // Core services
        public IApiClient ApiClient { get; private set; }
        public IAuthService AuthService { get; private set; }
        public IScreenshotService ScreenshotService { get; private set; }

        public VesselStudioPlugin()
        {
            _instance = this;
        }

        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            try
            {
                // Initialize services with dependency injection pattern
                ApiClient = new ApiClient("https://vesselstudio.ai/api");
                AuthService = new AuthService(ApiClient);
                ScreenshotService = new ScreenshotService(ApiClient);

                RhinoApp.WriteLine("Vessel Studio plugin loaded successfully");
                return LoadReturnCode.Success;
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to load Vessel Studio plugin: {ex.Message}";
                return LoadReturnCode.ErrorShowDialog;
            }
        }

        protected override void OnShutDown()
        {
            // Cleanup resources
            AuthService?.Dispose();
            ApiClient?.Dispose();
            base.OnShutDown();
        }
    }
}
```

### Step 2: Service Interfaces
Define clean interfaces for testability:

**File**: `VesselStudioPlugin/Services/IApiClient.cs`
```csharp
using System;
using System.Threading.Tasks;

namespace VesselStudioPlugin.Services
{
    public interface IApiClient : IDisposable
    {
        void SetAuthToken(string token);
        Task<T> GetAsync<T>(string endpoint);
        Task<T> PostAsync<T>(string endpoint, object data);
        Task<T> PostMultipartAsync<T>(string endpoint, byte[] imageData, string fileName, object metadata);
    }
}
```

### Step 3: Authentication Service
Implement the device flow authentication:

**File**: `VesselStudioPlugin/Services/AuthService.cs`
```csharp
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Timers;

namespace VesselStudioPlugin.Services
{
    public class AuthService : IAuthService, IDisposable
    {
        private readonly IApiClient _apiClient;
        private Timer _pollTimer;
        private string _pollToken;

        public bool IsAuthenticated { get; private set; }
        public string UserId { get; private set; }

        public AuthService(IApiClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        public async Task<string> InitiateAuthenticationAsync()
        {
            var request = new
            {
                deviceId = $"rhino-plugin-{Environment.MachineName}-{Guid.NewGuid()}",
                rhinoVersion = RhinoApp.Version.ToString(),
                pluginVersion = "1.0.0"
            };

            var response = await _apiClient.PostAsync<AuthInitResponse>("/plugin/auth/init", request);
            _pollToken = response.PollToken;
            
            return response.AuthUrl;
        }

        public void StartPolling(Action<bool> onComplete)
        {
            _pollTimer = new Timer(2000); // Poll every 2 seconds
            var attempts = 0;
            var maxAttempts = 150; // 5 minutes timeout

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
                    var response = await _apiClient.GetAsync<AuthPollResponse>($"/plugin/auth/poll?token={_pollToken}");
                    if (response.Status == "complete")
                    {
                        _pollTimer.Stop();
                        await CompleteAuthentication(response.AccessToken, response.UserId);
                        onComplete(true);
                    }
                }
                catch
                {
                    // Continue polling on errors
                }
            };

            _pollTimer.Start();
        }

        private async Task CompleteAuthentication(string accessToken, string userId)
        {
            _apiClient.SetAuthToken(accessToken);
            IsAuthenticated = true;
            UserId = userId;

            // Store credentials securely
            await SecureStorage.StoreCredentialsAsync(accessToken, userId);
        }

        public void Dispose()
        {
            _pollTimer?.Dispose();
        }
    }
}
```

### Step 4: Command Registration
Register Rhino commands for user interaction:

**File**: `VesselStudioPlugin/Commands/VesselStudioLoginCommand.cs`
```csharp
using Rhino;
using Rhino.Commands;
using System;
using System.Diagnostics;

namespace VesselStudioPlugin.Commands
{
    [System.Runtime.InteropServices.Guid("12345678-1234-1234-1234-123456789012")]
    public class VesselStudioLoginCommand : Command
    {
        public override string EnglishName => "VesselStudioLogin";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var plugin = VesselStudioPlugin.Instance;

            if (plugin.AuthService.IsAuthenticated)
            {
                RhinoApp.WriteLine("Already logged in to Vessel Studio");
                return Result.Success;
            }

            try
            {
                // Start async authentication
                _ = Task.Run(async () =>
                {
                    var authUrl = await plugin.AuthService.InitiateAuthenticationAsync();
                    
                    // Open browser
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = authUrl,
                        UseShellExecute = true
                    });

                    RhinoApp.WriteLine("Opening browser for authentication...");

                    plugin.AuthService.StartPolling((success) =>
                    {
                        if (success)
                        {
                            RhinoApp.WriteLine("✅ Successfully logged in to Vessel Studio!");
                        }
                        else
                        {
                            RhinoApp.WriteLine("❌ Authentication failed or timed out");
                        }
                    });
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

## Testing Strategy

### Unit Testing Setup
Create comprehensive unit tests for core functionality:

**File**: `VesselStudioPlugin.Tests/Services/AuthServiceTests.cs`
```csharp
using NUnit.Framework;
using Moq;
using VesselStudioPlugin.Services;
using System.Threading.Tasks;

namespace VesselStudioPlugin.Tests.Services
{
    [TestFixture]
    public class AuthServiceTests
    {
        private Mock<IApiClient> _mockApiClient;
        private AuthService _authService;

        [SetUp]
        public void Setup()
        {
            _mockApiClient = new Mock<IApiClient>();
            _authService = new AuthService(_mockApiClient.Object);
        }

        [Test]
        public async Task InitiateAuthenticationAsync_ReturnsAuthUrl()
        {
            // Arrange
            var expectedResponse = new AuthInitResponse
            {
                AuthUrl = "https://vesselstudio.ai/plugin-auth?code=ABC123",
                PollToken = "poll_xyz789"
            };
            _mockApiClient.Setup(x => x.PostAsync<AuthInitResponse>("/plugin/auth/init", It.IsAny<object>()))
                         .ReturnsAsync(expectedResponse);

            // Act
            var result = await _authService.InitiateAuthenticationAsync();

            // Assert
            Assert.AreEqual(expectedResponse.AuthUrl, result);
            _mockApiClient.Verify(x => x.PostAsync<AuthInitResponse>("/plugin/auth/init", It.IsAny<object>()), Times.Once);
        }

        [TearDown]
        public void TearDown()
        {
            _authService?.Dispose();
        }
    }
}
```

### Integration Testing
Set up integration tests that verify API communication:

**File**: `VesselStudioPlugin.Tests/Integration/ApiIntegrationTests.cs`
```csharp
using NUnit.Framework;
using VesselStudioPlugin.Services;
using System.Threading.Tasks;

namespace VesselStudioPlugin.Tests.Integration
{
    [TestFixture]
    [Category("Integration")]
    public class ApiIntegrationTests
    {
        private ApiClient _apiClient;

        [SetUp]
        public void Setup()
        {
            // Use test API endpoint
            _apiClient = new ApiClient("http://localhost:3000/api");
        }

        [Test]
        public async Task AuthInit_WithValidRequest_ReturnsAuthUrl()
        {
            // This test requires the Vessel Studio API to be running
            var request = new
            {
                deviceId = "test-device-123",
                rhinoVersion = "8.0.0",
                pluginVersion = "1.0.0"
            };

            var response = await _apiClient.PostAsync<AuthInitResponse>("/plugin/auth/init", request);

            Assert.IsNotNull(response);
            Assert.IsNotEmpty(response.AuthUrl);
            Assert.IsNotEmpty(response.PollToken);
        }

        [TearDown]
        public void TearDown()
        {
            _apiClient?.Dispose();
        }
    }
}
```

## Build and Distribution

### Build Configuration
```bash
# Debug build for development
dotnet build --configuration Debug

# Release build for distribution
dotnet build --configuration Release

# Create .rhp plugin file
# The build process should output VesselStudioPlugin.rhp in bin/Release/
```

### Plugin Testing in Rhino
1. **Load Plugin**: Drag `VesselStudioPlugin.rhp` into Rhino viewport
2. **Restart Rhino**: Plugin loads on next startup
3. **Test Commands**: 
   - Type `VesselStudioLogin` to test authentication
   - Type `VesselStudioCapture` to test viewport capture
4. **Check Output**: Monitor Rhino command line for feedback

### Debugging Setup
1. **Attach Debugger**: In Visual Studio, attach to `Rhino.exe` process
2. **Set Breakpoints**: Place breakpoints in plugin code
3. **Trigger Commands**: Run plugin commands in Rhino to hit breakpoints
4. **Debug Output**: Use `RhinoApp.WriteLine()` for runtime debugging

## Development Workflow

### Daily Development Cycle
1. **Code Changes**: Modify plugin source code
2. **Unit Tests**: Run `dotnet test` to verify changes
3. **Build**: `dotnet build` to create updated plugin
4. **Restart Rhino**: Load updated plugin (required for code changes)
5. **Manual Test**: Verify functionality in Rhino
6. **Integration Test**: Test against live Vessel Studio API (if available)

### Git Workflow
```bash
# Create feature branch for plugin work
git checkout -b feature/rhino-plugin-implementation

# Commit frequently with clear messages
git add .
git commit -m "feat: implement authentication service"

# Push to remote for collaboration
git push origin feature/rhino-plugin-implementation

# Create pull request when feature is complete
```

### Next Steps
1. **Implement Core Services**: Start with ApiClient and AuthService
2. **Add Command Handlers**: Create VesselStudioLoginCommand and VesselStudioCaptureCommand
3. **Build UI Components**: Implement ProjectSelectorDialog using Eto.Forms
4. **Add Screenshot Capture**: Integrate with RhinoCommon viewport APIs
5. **Implement Offline Queue**: Add local storage and retry logic
6. **Cross-Platform Testing**: Verify functionality on both Windows and macOS
7. **API Integration**: Test against actual Vessel Studio backend
8. **User Testing**: Get feedback from yacht designers using Rhino

This quickstart provides the foundation for implementing the full Rhino Viewport Sync Plugin according to the specification and design documents.
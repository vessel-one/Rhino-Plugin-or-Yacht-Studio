# RhinoMCP Architecture Analysis

**Repository:** https://github.com/jingcheng-chen/rhinomcp.git  
**Analyzed:** October 20, 2025  
**Purpose:** Learn from existing Rhino MCP implementation before building Vessel Studio plugin

---

## Overview

RhinoMCP is a **Model Context Protocol (MCP)** implementation that connects Rhino to AI agents (Claude Desktop, Cursor). It enables **two-way communication** between AI and Rhino 3D for prompt-assisted modeling.

### Key Insight
This is NOT a simple viewport capture plugin - it's a **full AI-to-CAD integration** using:
- TCP socket server inside Rhino
- Python MCP server as middleware
- JSON-based command protocol
- Real-time bidirectional communication

---

## Architecture Components

### 1. **Rhino Plugin** (`rhino_mcp_plugin/`)
C# plugin that runs inside Rhino and creates a TCP socket server.

#### Core Files
```
rhino_mcp_plugin/
├── RhinoMCPPlugin.cs              # Main plugin class (simple)
├── RhinoMCPServer.cs              # TCP socket server (12KB, complex)
├── RhinoMCPServerController.cs    # Singleton controller
├── Commands/
│   ├── MCPStartCommand.cs         # Start server command
│   ├── MCPStopCommand.cs          # Stop server command
│   └── MCPVersionCommand.cs       # Version info
├── Functions/                     # Command handlers
│   ├── CreateObject.cs            # Create 3D objects
│   ├── ModifyObject.cs            # Modify objects
│   ├── DeleteObject.cs            # Delete objects
│   ├── GetDocumentInfo.cs         # Inspect document
│   ├── ExecuteRhinoscript.cs      # Run Python scripts
│   ├── SelectObjects.cs           # Select by filters
│   ├── CreateLayer.cs             # Layer management
│   └── _utils.cs                  # Helper functions
└── Serializers/                   # JSON serialization
```

#### Key Features
- **TCP Socket Server** on `127.0.0.1:1999`
- **JSON Protocol** for commands/responses
- **Main Thread Execution** via `RhinoApp.InvokeOnUiThread()`
- **Undo Support** with `BeginUndoRecord/EndUndoRecord`
- **Error Handling** with try-catch and JSON error responses

---

### 2. **MCP Server** (`rhino_mcp_server/`)
Python server that implements Model Context Protocol and connects to Rhino plugin.

#### Structure
```
rhino_mcp_server/
├── main.py                        # Entry point
├── src/rhinomcp/
│   ├── server.py                  # MCP server implementation
│   ├── tools/                     # Tool definitions
│   ├── prompts/                   # AI prompts
│   └── static/                    # Static assets
├── pyproject.toml                 # UV package config
└── uv.lock                        # Dependency lock
```

#### Key Technologies
- **UV Package Manager** for Python dependencies
- **MCP Protocol** for AI agent communication
- **Socket Client** to connect to Rhino plugin
- **Tool Definitions** exposed to AI agents

---

## Communication Protocol

### Socket Communication Flow

```
AI Agent (Claude/Cursor)
    ↕ MCP Protocol
Python MCP Server (127.0.0.1:XXXX)
    ↕ TCP Socket JSON
Rhino Plugin Server (127.0.0.1:1999)
    ↕ Main Thread Invocation
Rhino 3D Application
```

### JSON Command Format

**Request:**
```json
{
  "type": "create_object",
  "params": {
    "object_type": "sphere",
    "center": [0, 0, 0],
    "radius": 5.0
  }
}
```

**Response (Success):**
```json
{
  "status": "success",
  "result": {
    "object_id": "abc-123-def",
    "object_type": "sphere"
  }
}
```

**Response (Error):**
```json
{
  "status": "error",
  "message": "Invalid object type"
}
```

---

## Supported Operations

### Document Operations
- `get_document_info` - Get current document details (max 30 objects to avoid overwhelm)
- `get_object_info` - Get info about specific object
- `get_selected_objects_info` - Get info about selected objects

### Object Creation
- `create_object` - Create single primitive (point, line, circle, box, sphere, cone, cylinder, etc.)
- `create_objects` - Batch create multiple objects

### Object Manipulation
- `modify_object` - Modify single object
- `modify_objects` - Batch modify objects
- `delete_object` - Delete object

### Selection
- `select_objects` - Select objects by filters (name, color, category) with AND/OR logic

### Layer Management
- `create_layer` - Create new layer
- `get_or_set_current_layer` - Get/set active layer
- `delete_layer` - Delete layer

### Script Execution (Experimental)
- `execute_rhinoscript_python_code` - Execute Python scripts in Rhino

---

## Key Implementation Patterns

### 1. **Plugin Structure (Clean & Simple)**
```csharp
public class RhinoMCPPlugin : Rhino.PlugIns.PlugIn
{
    public RhinoMCPPlugin()
    {
        Instance = this;
    }
    
    public static RhinoMCPPlugin Instance { get; private set; }
}
```

### 2. **Command Pattern**
```csharp
public class MCPStartCommand : Command
{
    public static MCPStartCommand Instance { get; private set; }
    
    public override string EnglishName => "mcpstart";
    
    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        RhinoMCPServerController.StartServer();
        return Result.Success;
    }
}
```

### 3. **Singleton Server Controller**
```csharp
class RhinoMCPServerController
{
    private static RhinoMCPServer server;
    
    public static void StartServer()
    {
        if (server == null)
            server = new RhinoMCPServer();
        server.Start();
    }
    
    public static void StopServer()
    {
        if (server != null)
        {
            server.Stop();
            server = null;
        }
    }
}
```

### 4. **Thread-Safe Server**
```csharp
private readonly object lockObject = new object();
private bool running;

public void Start()
{
    lock (lockObject)
    {
        if (running) return;
        running = true;
    }
    
    listener = new TcpListener(IPAddress.Parse(host), port);
    listener.Start();
    
    serverThread = new Thread(ServerLoop);
    serverThread.IsBackground = true;
    serverThread.Start();
}
```

### 5. **Main Thread Execution**
```csharp
RhinoApp.InvokeOnUiThread(new Action(() =>
{
    try
    {
        JObject response = ExecuteCommand(command);
        SendResponse(response);
    }
    catch (Exception e)
    {
        SendError(e.Message);
    }
}));
```

### 6. **Command Handler Pattern**
```csharp
Dictionary<string, Func<JObject, JObject>> handlers = new Dictionary<string, Func<JObject, JObject>>
{
    ["get_document_info"] = this.handler.GetDocumentInfo,
    ["create_object"] = this.handler.CreateObject,
    ["delete_object"] = this.handler.DeleteObject
};

if (handlers.TryGetValue(cmdType, out var handler))
{
    var doc = RhinoDoc.ActiveDoc;
    var record = doc.BeginUndoRecord("Run MCP command");
    try
    {
        JObject result = handler(parameters);
        return new JObject { ["status"] = "success", ["result"] = result };
    }
    finally
    {
        doc.EndUndoRecord(record);
    }
}
```

---

## Lessons for Vessel Studio Plugin

### ✅ What to Adopt

1. **Clean Plugin Structure**
   - Simple main plugin class
   - Singleton pattern for server
   - Separate command classes

2. **Socket Server Pattern**
   - TCP socket for external communication
   - Background thread for server loop
   - Thread-safe operations with locks

3. **Main Thread Invocation**
   - Always use `RhinoApp.InvokeOnUiThread()` for Rhino API calls
   - Critical for UI operations and document modifications

4. **JSON Protocol**
   - Simple, human-readable
   - Easy to debug
   - Type-safe with proper parsing

5. **Command Handler Dictionary**
   - Extensible architecture
   - Easy to add new commands
   - Clean separation of concerns

6. **Undo Support**
   - Wrap operations in `BeginUndoRecord/EndUndoRecord`
   - Better user experience

### ⚠️ What to Avoid

1. **Complex MCP Server** (for our use case)
   - We don't need AI agent integration
   - Direct HTTP API is simpler

2. **Socket Server Overhead** (for simple viewport capture)
   - Unnecessary for one-way upload
   - HTTP POST is sufficient

3. **Experimental Script Execution**
   - Security concerns
   - Not needed for viewport capture

---

## Differences from Our Requirements

| Feature | RhinoMCP | Vessel Studio Plugin |
|---------|----------|---------------------|
| **Purpose** | AI agent control | Viewport capture & upload |
| **Communication** | Bidirectional TCP | One-way HTTP POST |
| **Complexity** | High (MCP + Socket) | Low (Simple HTTP) |
| **Operations** | Many (create, modify, delete) | Single (capture & upload) |
| **External Server** | Python MCP server | Direct to Vessel Studio API |
| **AI Integration** | Claude Desktop, Cursor | None needed |
| **Installation** | Plugin + Python server | Plugin only |

---

## Recommended Architecture for Vessel Studio

Based on RhinoMCP analysis, here's what we should build:

```
VesselStudioPlugin/
├── VesselStudioPlugin.cs          # Main plugin (simple)
├── VesselStudioApiClient.cs       # HTTP client (not socket)
├── Commands/
│   ├── VesselStudioCaptureCommand.cs    # Capture viewport
│   ├── VesselStudioSetApiKeyCommand.cs  # Set API key
│   └── VesselStudioStatusCommand.cs     # Check status
└── Models/
    └── ScreenshotMetadata.cs      # Metadata structure
```

### Why Simpler?
1. **One-way communication** - Upload only, no AI control
2. **HTTPS API** - No need for local socket server
3. **Single operation** - Just capture and upload
4. **No Python dependency** - Pure C# plugin
5. **Easier installation** - Drag & drop `.rhp` file

---

## Key Code Patterns to Reuse

### 1. Main Thread Viewport Capture
```csharp
RhinoApp.InvokeOnUiThread(new Action(() =>
{
    var activeView = RhinoDoc.ActiveDoc.Views.ActiveView;
    var bitmap = activeView.CaptureToBitmap(size);
    // Upload bitmap
}));
```

### 2. Async HTTP Upload with Blocking Wait
```csharp
var uploadTask = apiClient.UploadScreenshotAsync(bitmap);
var result = uploadTask.GetAwaiter().GetResult();
```

### 3. Error Response Pattern
```csharp
return new UploadResult 
{ 
    Success = false, 
    Message = $"Upload failed: {ex.Message}" 
};
```

---

## Installation Comparison

### RhinoMCP Installation
1. Install UV package manager
2. Install Rhino plugin via Package Manager
3. Configure MCP server in Claude/Cursor
4. Run `mcpstart` in Rhino
5. Connect AI agent

### Vessel Studio Installation (Simpler!)
1. Drag `.rhp` file to Rhino
2. Restart Rhino
3. Run `VesselStudioSetApiKey`
4. Run `VesselStudioCapture`

---

## Conclusion

**RhinoMCP teaches us:**
- ✅ Clean plugin architecture patterns
- ✅ Proper main thread invocation
- ✅ Command handler organization
- ✅ Error handling best practices

**But we should keep it simpler:**
- ❌ No socket server needed
- ❌ No Python MCP server
- ❌ No AI agent integration
- ✅ Direct HTTPS API upload
- ✅ Single-purpose plugin

**Our simple plugin approach is correct!** We learned the right patterns from RhinoMCP but don't need the complexity for viewport capture.

---

**Next Steps:**
1. ✅ Keep our simple plugin architecture
2. ✅ Use proper main thread invocation patterns
3. ✅ Implement clean command structure
4. ✅ Add proper error handling
5. ✅ Test viewport capture and upload
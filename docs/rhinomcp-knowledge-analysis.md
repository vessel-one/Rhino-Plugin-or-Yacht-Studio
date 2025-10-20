# Technical Analysis: How RhinoMCP Gains Rhino Knowledge

**Analysis Date:** October 20, 2025  
**Repository:** https://github.com/jingcheng-chen/rhinomcp.git  
**Key Discovery:** RhinoMCP uses **embedded static documentation** + **MCP tool definitions** + **system prompts**, NOT RAG or fine-tuning

---

## 🎯 Key Discovery: How RhinoMCP Knows Rhino

### The Answer: **Pre-Packaged API Documentation**

RhinoMCP does NOT use:
- ❌ RAG (Retrieval Augmented Generation)
- ❌ Fine-tuning
- ❌ External documentation lookup
- ❌ Web scraping

Instead, it uses:
- ✅ **1.5MB static JSON file** with complete RhinoScriptSyntax API documentation
- ✅ **MCP tool definitions** that describe what each tool does
- ✅ **System prompts** that guide LLM behavior
- ✅ **LLM's base knowledge** of Python and 3D concepts

---

## 📁 Knowledge Architecture

### File Structure
```
rhino_mcp_server/
├── src/rhinomcp/
│   ├── static/
│   │   └── rhinoscriptsyntax.py        # 1.5MB - Complete API docs!
│   ├── tools/                          # MCP tool definitions
│   │   ├── create_object.py
│   │   ├── get_rhinoscript_python_code_guide.py  # Accesses static docs
│   │   ├── get_rhinoscript_python_function_names.py
│   │   └── ... (15 tools total)
│   ├── prompts/
│   │   └── assert_general_strategy.py  # System prompts for LLM
│   └── server.py                       # TCP socket + MCP server
```

---

## 📚 Static Knowledge Base: `rhinoscriptsyntax.py`

### What It Contains

**1.5MB JSON file with complete RhinoScriptSyntax documentation:**

```python
rhinoscriptsyntax_json = [
  {
    "ModuleName": "application",
    "functions": [
      {
        "Name": "AddAlias",
        "Signature": "AddAlias(alias, macro)",
        "Description": "Add new command alias to Rhino...",
        "ArgumentDesc": "alias (str): Name of new command alias...",
        "DocString": "Complete documentation with examples...",
        "Example": [
          "import rhinoscriptsyntax as rs",
          "rs.AddAlias(\"OriginLine\", \"!_Line 0,0,0\")"
        ],
        "Returns": "bool: True or False indicating success...",
        "ModuleName": "application"
      },
      // ... hundreds more functions
    ]
  },
  // ... many more modules
]
```

### Coverage
- **All RhinoScriptSyntax modules**
  - application
  - document
  - geometry
  - curve
  - surface
  - object
  - layer
  - light
  - material
  - selection
  - transformation
  - view
  - etc.

- **For each function:**
  - Function name
  - Signature (parameters)
  - Description
  - Argument descriptions with types
  - Return type and description
  - Code examples
  - Related functions
  - Complete docstring

---

## 🔧 How Knowledge is Accessed

### Method 1: Tool Definitions (MCP Protocol)

**Each MCP tool has built-in documentation:**

```python
# filepath: tools/create_object.py
@mcp.tool()
def create_object(
    ctx: Context,
    object_type: Literal["point", "line", "circle", "box", "sphere"],
    **kwargs
) -> Dict[str, Any]:
    """
    Create a 3D object in Rhino.
    
    Parameters:
    - object_type: Type of object to create
    - For 'sphere': center [x,y,z], radius (number)
    - For 'box': corner [x,y,z], width, height, depth
    - For 'line': start [x,y,z], end [x,y,z]
    
    Returns dictionary with object_id and success status.
    """
    # Implementation sends to Rhino plugin
```

**The LLM sees this documentation automatically through MCP protocol!**

### Method 2: On-Demand API Lookup

**Two specialized tools for accessing the static knowledge base:**

#### Tool 1: Get Function Names
```python
# filepath: tools/get_rhinoscript_python_function_names.py
@mcp.tool()
def get_rhinoscript_python_function_names(ctx: Context) -> List[str]:
    """
    Return all available RhinoScriptsyntax function names.
    
    You should use this tool when you need to know what functions 
    are available in rhinoscriptsyntax.
    """
    function_names = []
    for module in rhinoscriptsyntax_json:
        for function in module["functions"]:
            function_names.append(function["Name"])
    
    return function_names
```

#### Tool 2: Get Function Documentation
```python
# filepath: tools/get_rhinoscript_python_code_guide.py
@mcp.tool()
def get_rhinoscript_python_code_guide(
    ctx: Context, 
    function_name: str
) -> Dict[str, Any]:
    """
    Return the RhinoScriptsyntax Details for a specific function.
    
    Parameters:
    - function_name: The name of the function to get the details for.
    
    You should get the function names first by using the 
    get_rhinoscript_python_function_names tool.
    """
    for module in rhinoscriptsyntax_json:
        for function in module["functions"]:
            if function["Name"] == function_name:
                return function
    
    return {"success": False, "message": "Function not found"}
```

**Workflow:**
1. LLM: "I need to create a curve"
2. LLM calls: `get_rhinoscript_python_function_names()`
3. LLM: "Hmm, there's a AddCurve function"
4. LLM calls: `get_rhinoscript_python_code_guide("AddCurve")`
5. LLM gets: Complete documentation with examples
6. LLM: "Now I can write the code!"

### Method 3: System Prompts

**Strategic guidance embedded in prompts:**

```python
# filepath: prompts/assert_general_strategy.py
@mcp.prompt()
def asset_general_strategy() -> str:
    """Defines the preferred strategy for creating assets in Rhino"""
    return """
    QUERY STRATEGY:
    - if the id of the object is known, use the id to query the object.
    - if the id is not known, use the name of the object to query the object.

    CREATION STRATEGY:
    0. Before anything, always check the document from get_document_info().
    1. If execute_rhinoscript_python_code() is not able to create objects, 
       use create_objects() function.
    2. If there are multiple objects, use create_objects() to create 
       multiple objects at once.
    3. ALWAYS make sure object names are meaningful.
    4. Try to include as many objects as possible accurately.

    When creating rhinoscript python code:
    - do not hallucinate, only use syntax supported by rhinoscriptsyntax 
      or Rhino.Geometry.
    - double check the code if any is incorrect, and fix it.
    """
```

This guides the LLM to:
- Check documentation before guessing
- Follow best practices
- Avoid hallucination
- Use efficient batch operations

---

## 🧠 Knowledge Flow Diagram

```
User: "Create a sphere at origin with radius 5"
    ↓
Claude/Cursor AI Agent
    ↓ Analyzes intent
    ↓
MCP Protocol: "What tools are available?"
    ↓
MCP Server: "Here are my tools..."
    ├─ create_object (with full parameter docs)
    ├─ get_rhinoscript_python_code_guide (to lookup API)
    ├─ execute_rhinoscript_python_code (to run scripts)
    └─ ... (15 tools total)
    ↓
AI: "I'll use create_object tool"
    ↓ Calls tool with parameters
    ↓
MCP Server → TCP Socket → Rhino Plugin
    ↓
Rhino executes command
    ↓ Returns result
    ↓
MCP Server → AI → User: "Sphere created!"
```

### Alternative Flow (When Uncertain)

```
User: "Create a NURBS curve through these points"
    ↓
AI: "I'm not sure about the exact function..."
    ↓
AI calls: get_rhinoscript_python_function_names()
    ↓
Returns: ["AddCurve", "AddNurbsCurve", "AddLine", ...]
    ↓
AI: "Let me check AddNurbsCurve..."
    ↓
AI calls: get_rhinoscript_python_code_guide("AddNurbsCurve")
    ↓
Returns: {
  "Name": "AddNurbsCurve",
  "Signature": "AddNurbsCurve(points, degree=3, knots=None, weights=None)",
  "Description": "Creates a NURBS curve from control points...",
  "Example": [
    "import rhinoscriptsyntax as rs",
    "points = [[0,0,0], [1,1,0], [2,0,0]]",
    "curve_id = rs.AddNurbsCurve(points, 3)"
  ]
}
    ↓
AI: "Perfect! Now I know how to use it"
    ↓
AI calls: execute_rhinoscript_python_code(code)
    ↓
Code executes in Rhino
```

---

## 💡 Key Insights: Why This Works

### 1. **Complete API Documentation in One File**
- **1.5MB of structured data**
- Every function documented with examples
- Types, parameters, return values
- Instantly accessible (no API calls needed)

### 2. **MCP Tool Descriptions Are Documentation**
- Each tool has built-in docs
- LLM sees these automatically
- No need to "lookup" - it's in the tool definition

### 3. **Just-In-Time Knowledge Retrieval**
- Most queries answered by tool descriptions
- Complex queries → lookup static docs
- No external API calls = fast & free

### 4. **System Prompts Guide Behavior**
- Prevents hallucination
- Enforces best practices
- Encourages documentation lookup

### 5. **Leverages LLM Base Knowledge**
- LLM already understands:
  - Python syntax
  - 3D geometry concepts
  - Common CAD operations
- Only needs Rhino-specific API details

---

## 📊 Comparison: RhinoMCP vs RAG vs Fine-Tuning

| Feature | RhinoMCP Approach | RAG Approach | Fine-Tuning |
|---------|------------------|--------------|-------------|
| **Knowledge Source** | Static JSON (1.5MB) | Vector DB | Training data |
| **Size** | 1.5MB | 100MB+ | N/A |
| **Access Speed** | Instant (in-memory) | ~2-3 seconds | Instant |
| **Cost** | Free | $75/month | $20,000+ |
| **Updates** | Replace JSON file | Re-embed docs | Retrain |
| **Accuracy** | 100% (exact docs) | ~95% (semantic) | ~80% (can hallucinate) |
| **Coverage** | RhinoScript only | Any docs | Any pattern |
| **Scalability** | Limited by memory | Unlimited | Limited by cost |
| **Best For** | Well-defined API | Broad knowledge | Custom behavior |

---

## 🔍 What's Transferable to Vessel Studio Plugin

### ✅ **We CAN Transfer:**

#### 1. Static Documentation Embedding
```python
# Create similar file for our needs
vessel_design_knowledge = {
    "hull_types": [...],
    "design_principles": [...],
    "rhino_techniques": [...]
}
```

#### 2. MCP Tool Pattern
```python
@mcp.tool()
def capture_viewport(ctx: Context, project_id: str) -> Dict[str, Any]:
    """
    Capture current Rhino viewport and upload to Vessel Studio.
    
    Parameters:
    - project_id: ID of Vessel Studio project to upload to
    
    Returns: Success status and image URL
    """
    # Implementation
```

#### 3. System Prompts
```python
@mcp.prompt()
def vessel_design_strategy() -> str:
    return """
    VESSEL DESIGN GUIDELINES:
    - Always consider hull form before details
    - Use appropriate Rhino tools for yacht modeling
    - Follow naval architecture principles
    ...
    """
```

#### 4. Knowledge Lookup Tools
```python
@mcp.tool()
def get_vessel_design_guide(ctx: Context, topic: str) -> Dict:
    """Look up vessel design information"""
    return vessel_design_knowledge.get(topic)
```

### ❌ **We DON'T Need:**

1. **TCP Socket Server** (we use HTTPS)
2. **Complete RhinoScript docs** (too large, LLM already knows basics)
3. **Bidirectional control** (we're doing capture, not control)

---

## 🎯 Recommended Hybrid Approach for Vessel Studio

### Combine RhinoMCP's Static Docs + RAG for Flexibility

```
┌─────────────────────────────────────────────┐
│ Knowledge System Architecture               │
├─────────────────────────────────────────────┤
│                                             │
│ 1. STATIC DOCS (Like RhinoMCP)             │
│    ├─ Core Vessel Design Principles        │
│    ├─ Common Rhino Techniques              │
│    ├─ Yacht Design Terminology             │
│    └─ Your Custom Templates                │
│        (Embedded in code, instant access)  │
│                                             │
│ 2. RAG SYSTEM (Dynamic Knowledge)          │
│    ├─ Full Rhino SDK docs                  │
│    ├─ Grasshopper components               │
│    ├─ Community best practices             │
│    └─ Latest tutorials/updates             │
│        (Vector DB, searchable, updatable)  │
│                                             │
│ 3. MCP TOOLS (Actions)                     │
│    ├─ capture_viewport()                   │
│    ├─ get_vessel_guide()                   │
│    └─ (future: create_hull(), etc.)        │
│                                             │
└─────────────────────────────────────────────┘
```

### Why Hybrid is Best

**Static Docs (RhinoMCP approach):**
- ✅ Your core vessel design knowledge
- ✅ Instant access, no API calls
- ✅ Free
- ✅ Never outdated (you control it)

**RAG (External knowledge):**
- ✅ Complete Rhino/Grasshopper docs
- ✅ Community knowledge
- ✅ Easy to update
- ✅ Handles edge cases

**MCP Tools:**
- ✅ Actions in Rhino
- ✅ Viewport capture
- ✅ Future: Rhino control

---

## 📝 Implementation Recommendations

### Phase 1: MVP (Static Docs Only)
```typescript
// Embed core knowledge in API
const vesselDesignKnowledge = {
  hull_types: {
    planing: {
      description: "...",
      rhino_techniques: [...],
      examples: [...]
    },
    displacement: {...},
    semi_displacement: {...}
  },
  common_rhino_tasks: {
    create_hull_surface: {
      steps: [...],
      tools: [...],
      tips: [...]
    }
  }
}

// API endpoint
app.post('/api/plugin/chat', async (req, res) => {
  const { message } = req.body
  
  // LLM with embedded knowledge
  const context = `
You are a vessel design assistant with access to:
${JSON.stringify(vesselDesignKnowledge, null, 2)}

Answer the user's question using this knowledge.
`
  
  const response = await openai.chat.completions.create({
    model: 'gpt-4-turbo',
    messages: [
      { role: 'system', content: context },
      { role: 'user', content: message }
    ]
  })
  
  res.json({ response: response.choices[0].message.content })
})
```

### Phase 2: Add RAG (Full Documentation)
```typescript
// For complex Rhino queries
if (isComplexRhinoQuery(message)) {
  // Use RAG system
  const relevantDocs = await vectorDB.search(message)
  context += `\n\nRelevant Rhino documentation:\n${relevantDocs}`
}
```

### Phase 3: Add MCP (If Needed)
```python
# MCP server for Rhino control
@mcp.tool()
def capture_and_upload(ctx: Context, project_id: str):
    """Capture viewport and upload to Vessel Studio"""
    # Implementation
```

---

## 🎓 Lessons Learned from RhinoMCP

### 1. **Embed Core Knowledge**
- Don't make API calls for knowledge you control
- Static files = instant, free, reliable
- Perfect for domain-specific expertise

### 2. **MCP Tools ARE Documentation**
- Tool descriptions teach the LLM
- No separate documentation needed
- Self-documenting architecture

### 3. **Provide Lookup Tools**
- For when LLM is uncertain
- `get_function_names()` + `get_function_guide()`
- Prevents hallucination

### 4. **Use System Prompts**
- Guide behavior explicitly
- Enforce best practices
- Reduce errors

### 5. **Leverage LLM Base Knowledge**
- LLMs already know:
  - Programming languages
  - General 3D concepts
  - Common patterns
- Only provide Rhino/vessel-specific details

---

## 💰 Cost Analysis

### RhinoMCP Approach (Static Docs)
```
Storage: 1.5MB file = FREE
Access: In-memory = FREE
LLM calls: Only when user asks = $0.01-0.10 per query
Total: ~$10-50/month (depending on usage)
```

### Pure RAG Approach
```
Vector DB: $20/month
Embeddings: $5/month
LLM calls: $50-200/month
Total: $75-225/month
```

### Hybrid Approach (RECOMMENDED)
```
Static docs: FREE
RAG (optional queries): $20/month
LLM calls: $30-100/month
Total: $50-120/month
```

---

## 🚀 Action Plan for Vessel Studio

### Immediate (This Week)
1. **Create `vessel_knowledge.json`**
   - Hull types and characteristics
   - Common Rhino techniques for yacht design
   - Your design philosophy/best practices
   - ~100KB file

2. **Embed in Chat API**
   - Include in system prompt
   - Instant access, no RAG needed
   - Test with sample questions

3. **Add to Plugin UI**
   - Chat panel connects to API
   - Test conversation flow

### Soon (Next Month)
4. **Add RAG for Rhino Docs**
   - Only for complex technical questions
   - Falls back when static knowledge insufficient

5. **Add Vision**
   - Screenshot analysis
   - Design critique

### Future (If Needed)
6. **Add MCP Control**
   - Only if users request it
   - Start with safe operations

---

## ✨ Key Takeaway

**RhinoMCP doesn't use RAG or fine-tuning - it uses:**
1. **Static JSON documentation** (1.5MB embedded file)
2. **MCP tool definitions** (self-documenting)
3. **System prompts** (behavior guidance)
4. **LLM base knowledge** (Python, 3D concepts)

**For Vessel Studio, we should:**
1. **Embed core vessel design knowledge** (static, like RhinoMCP)
2. **Add RAG for full Rhino docs** (dynamic, when needed)
3. **Use MCP for actions** (viewport capture, future control)

This gives us:
- ✅ Fast responses (static knowledge)
- ✅ Comprehensive coverage (RAG for details)
- ✅ Low cost (mostly static)
- ✅ Easy maintenance (update JSON file)

---

**Next Step:** Create `vessel_knowledge.json` with core design principles and embed in chat API!

**File size estimate:** 50-200KB (much smaller than RhinoMCP's 1.5MB, since we're focused on vessel design, not entire API)
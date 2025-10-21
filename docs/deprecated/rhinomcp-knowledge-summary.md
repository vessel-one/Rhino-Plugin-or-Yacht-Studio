# RhinoMCP Knowledge System - Quick Summary

**Date:** October 20, 2025  
**Discovery:** RhinoMCP uses static embedded documentation, NOT RAG or fine-tuning!

---

## 🎯 The Big Discovery

### How RhinoMCP "Knows" Rhino API

**NOT using:**
- ❌ RAG (no vector database)
- ❌ Fine-tuning (no custom model)
- ❌ Web scraping (no external calls)

**Actually using:**
```
1. 1.5MB static JSON file → rhinoscriptsyntax.py
   └─ Complete RhinoScript API documentation
   └─ All functions, parameters, examples
   └─ Embedded directly in Python code

2. MCP Tool Definitions → Self-documenting
   └─ Each tool describes itself
   └─ LLM reads tool descriptions automatically

3. System Prompts → Behavior guidance
   └─ Best practices
   └─ Anti-hallucination rules

4. LLM Base Knowledge → Python + 3D concepts
   └─ GPT-4 already understands basics
   └─ Just needs Rhino-specific API details
```

---

## 📊 Knowledge Architecture

```
┌─────────────────────────────────────────────┐
│ RhinoMCP Knowledge System                   │
├─────────────────────────────────────────────┤
│                                             │
│ 📄 rhinoscriptsyntax.py (1.5MB)            │
│    ├─ All RhinoScript functions            │
│    ├─ Parameters & types                   │
│    ├─ Code examples                        │
│    └─ Return values                        │
│                                             │
│ 🔧 MCP Tools (15 tools)                    │
│    ├─ create_object() → docs in tool      │
│    ├─ get_rhinoscript_python_code_guide() │
│    │    → Searches static JSON             │
│    └─ execute_rhinoscript_python_code()   │
│                                             │
│ 📝 System Prompts                          │
│    └─ "Don't hallucinate, check docs"     │
│                                             │
│ 🧠 LLM (Claude/GPT-4)                      │
│    └─ Already knows Python + 3D basics    │
│                                             │
└─────────────────────────────────────────────┘
```

---

## 🔄 How It Works (Example)

### User: "Create a sphere at origin"

```
Step 1: LLM sees MCP tools
├─ create_object(object_type, center, radius)
│   Description: "Create 3D object..."
│   Parameters: Listed with types
└─ LLM: "I know exactly what to do!"

Step 2: LLM calls tool
├─ create_object(
│     object_type="sphere",
│     center=[0,0,0],
│     radius=5
│   )
└─ No documentation lookup needed!

Step 3: MCP sends to Rhino
├─ TCP socket → Rhino plugin
└─ Sphere created
```

### User: "Create a NURBS curve" (Uncertain)

```
Step 1: LLM uncertain
└─ "I need to check the exact function..."

Step 2: LLM calls lookup tool
├─ get_rhinoscript_python_function_names()
├─ Returns: ["AddCurve", "AddNurbsCurve", ...]
└─ LLM: "AddNurbsCurve looks right"

Step 3: LLM gets documentation
├─ get_rhinoscript_python_code_guide("AddNurbsCurve")
├─ Returns: Complete docs from static JSON
│   {
│     "Signature": "AddNurbsCurve(points, degree=3)",
│     "Description": "...",
│     "Example": ["import rhinoscriptsyntax as rs", ...]
│   }
└─ LLM: "Perfect! Now I know how to use it"

Step 4: LLM executes code
└─ execute_rhinoscript_python_code(...)
```

---

## 💡 Key Insights

### 1. Static > RAG for Well-Defined APIs
- **Faster:** Instant (no vector search)
- **Cheaper:** Free (no embeddings/queries)
- **More Accurate:** Exact docs, no semantic search errors

### 2. MCP Tools = Self-Documentation
- Tool definitions teach the LLM
- No separate documentation system needed
- LLM automatically reads tool descriptions

### 3. Lookup Tools Prevent Hallucination
- When uncertain → lookup function names
- Then get specific function docs
- Never guess API signatures

### 4. System Prompts Guide Behavior
- "Check docs before guessing"
- "Use batch operations when possible"
- "Don't hallucinate API calls"

---

## 🎯 Recommendations for Vessel Studio

### Use Hybrid Approach

**Static Knowledge (Like RhinoMCP) - FREE**
```json
// vessel_knowledge.json (~100KB)
{
  "hull_types": {
    "planing": {
      "description": "...",
      "rhino_techniques": ["..."],
      "deadrise_angles": "12-24 degrees"
    }
  },
  "design_principles": {
    "lwl_ratio": "...",
    "displacement_speed": "..."
  },
  "common_tasks": {
    "create_hull_surface": {
      "steps": ["..."],
      "rhino_commands": ["..."]
    }
  }
}
```

**RAG (For Comprehensive Rhino Docs) - $75/month**
- Full Rhino SDK documentation
- Grasshopper components
- Community tutorials
- Used only when static knowledge insufficient

**MCP Tools (For Actions) - $40/month**
- `capture_viewport(project_id)`
- Future: `create_hull()`, etc.
- Self-documenting through tool definitions

---

## 💰 Cost Comparison

| Approach | Setup | Monthly | Knowledge Coverage |
|----------|-------|---------|-------------------|
| **RhinoMCP Style** (Static) | 1 day | FREE | Domain-specific (perfect!) |
| **Pure RAG** | 1 week | $75-225 | Broad (overkill) |
| **Hybrid** ⭐ | 1 week | $50-120 | Domain + broad (best!) |
| **Fine-tuning** | 3 months | $20,000+ | Custom (unnecessary) |

---

## 📋 Implementation Checklist

### Phase 1: Static Knowledge (This Week)
- [ ] Create `vessel_knowledge.json`
  - [ ] Hull types and characteristics
  - [ ] Design principles
  - [ ] Common Rhino techniques
  - [ ] Your design templates
- [ ] Embed in chat API system prompt
- [ ] Test with sample questions
- [ ] Add to plugin chat UI

### Phase 2: RAG Enhancement (Next Month)
- [ ] Set up vector DB (Pinecone/Supabase)
- [ ] Scrape Rhino SDK docs
- [ ] Generate embeddings
- [ ] Add fallback logic: static → RAG

### Phase 3: MCP Tools (Future)
- [ ] Define MCP tools for Rhino actions
- [ ] Add TCP server if needed
- [ ] Start with safe operations only

---

## 🎓 What We Learned

### From RhinoMCP Analysis:

1. **Static docs can be sufficient** for well-defined domains
2. **MCP tools are self-documenting** - no separate docs needed
3. **Lookup tools prevent hallucination** - provide escape hatch
4. **System prompts guide LLM behavior** - explicit > implicit
5. **Hybrid is best** - static for core, RAG for breadth

### For Vessel Studio:

1. **Embed vessel design knowledge** in code (static)
2. **Use RAG for Rhino docs** (dynamic)
3. **MCP for actions** (viewport capture, etc.)
4. **Don't fine-tune** - waste of money

---

## 🚀 Next Action

**Create `vessel_knowledge.json` with:**
- Hull types (planing, displacement, semi-displacement)
- Design principles (LWL ratios, displacement, speed)
- Common Rhino tasks for yacht modeling
- Your design methodology

**Estimated size:** 50-200KB (vs RhinoMCP's 1.5MB)  
**Estimated time:** 2-3 days to compile  
**Cost:** FREE forever  
**Benefit:** Instant, accurate vessel design knowledge

---

**Key Takeaway:**  
RhinoMCP proves that **embedded static documentation** can rival RAG for well-defined domains, at zero cost and instant speed. We should do the same for vessel design knowledge, then add RAG only for comprehensive Rhino docs.

**Files Created:**
- `docs/rhinomcp-knowledge-analysis.md` - Full technical analysis
- This summary - Quick reference

**Ready to build:** Static knowledge → Chat API → Plugin UI → Test → Ship! 🚀
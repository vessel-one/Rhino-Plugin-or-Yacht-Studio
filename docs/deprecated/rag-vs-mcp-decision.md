# AI Integration: Quick Decision Guide

## The Big Picture

```
Your Goal: AI assistant in Rhino that understands:
├─ Rhino 3D modeling
├─ Grasshopper
├─ Vessel/yacht design
└─ Can see what user is doing (vision)

Your Options:
├─ RAG (Retrieval Augmented Generation) ← RECOMMENDED
├─ Fine-Tuning ← TOO EXPENSIVE
└─ MCP (Model Context Protocol) ← FOR CONTROL, NOT KNOWLEDGE
```

---

## Simple Decision Tree

```
┌─────────────────────────────────────┐
│ What do you want the AI to do?     │
└──────────────┬──────────────────────┘
               │
       ┌───────┴────────┐
       │                │
       ▼                ▼
┌──────────────┐  ┌──────────────┐
│ Answer       │  │ Take Actions │
│ Questions    │  │ (Create box) │
└──────┬───────┘  └──────┬───────┘
       │                 │
       ▼                 ▼
   Use RAG          Use MCP
   ($100/mo)        (Complex)
       │                 │
       └────────┬────────┘
                ▼
         Use Both Eventually!
```

---

## RAG vs Fine-Tuning vs MCP

| | RAG | Fine-Tuning | MCP |
|---|-----|-------------|-----|
| **Good For** | Knowledge Q&A | Custom behavior | Tool execution |
| **Cost** | $100/month | $20,000+ | $50/month |
| **Setup Time** | 1 week | 2-3 months | 2-3 weeks |
| **Updateable** | ✅ Instant | ❌ Retrain needed | ✅ Add tools |
| **Accuracy** | ✅ High (cites sources) | ⚠️ Can hallucinate | ✅ Deterministic |
| **Use Case** | "How do I use NURBS?" | "Write code like me" | "Create a sphere" |

---

## Your MVP Stack (Recommended)

### Phase 1: RAG Chat (Start Here!)

```
┌─────────────────────────────────────────────┐
│ Rhino Plugin (C# + Eto.Forms)               │
│ ┌─────────────────────────────────────┐     │
│ │ Chat Panel                          │     │
│ │ ┌──────────────────────────────┐    │     │
│ │ │ User: "How to create NURBS?"│    │     │
│ │ └──────────────┬───────────────┘    │     │
│ └────────────────┼────────────────────┘     │
└──────────────────┼──────────────────────────┘
                   │ HTTPS POST
                   ▼
┌─────────────────────────────────────────────┐
│ Vessel Studio API (Next.js)                 │
│ ┌─────────────────────────────────────┐     │
│ │ /api/plugin/chat                    │     │
│ │ 1. Search vector DB                 │     │
│ │ 2. Retrieve docs                    │     │
│ │ 3. Build context                    │     │
│ │ 4. Call LLM                         │     │
│ └──────────────┬──────────────────────┘     │
└────────────────┼──────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────┐
│ Knowledge Base                              │
│ ┌──────────────┐ ┌──────────────┐          │
│ │ Rhino Docs   │ │ Grasshopper  │          │
│ │ (scraped)    │ │ (scraped)    │          │
│ └──────────────┘ └──────────────┘          │
│ ┌──────────────┐ ┌──────────────┐          │
│ │ Vessel       │ │ Your Custom  │          │
│ │ Design Docs  │ │ Content      │          │
│ └──────────────┘ └──────────────┘          │
└─────────────────────────────────────────────┘
                 │
                 ▼
         Vector Database
         (Pinecone/Supabase)
```

---

## What Each Technology Does

### RAG (Retrieval Augmented Generation)
```python
# Pseudo-code
def answer_question(user_question):
    # 1. Find relevant docs
    relevant_docs = vector_db.search(user_question, top_k=5)
    
    # 2. Build prompt with context
    prompt = f"""
    Based on these docs:
    {relevant_docs}
    
    Answer: {user_question}
    """
    
    # 3. Ask LLM
    answer = llm.complete(prompt)
    
    return answer + sources
```

**Example:**
- User: "How do I create a NURBS surface?"
- System finds: RhinoCommon.Geometry.NurbsSurface docs
- LLM answers: "Use `NurbsSurface.Create()` method..."
- Includes: Source citation

### Fine-Tuning (Don't Use)
```python
# Pseudo-code
def fine_tune_model():
    # 1. Prepare thousands of examples
    training_data = [
        {"input": "create box", "output": "Use Rhino.Geometry.Box..."},
        # ... 10,000 more examples
    ]
    
    # 2. Train for weeks
    custom_model = openai.fine_tune(
        model="gpt-4",
        training_data=training_data,
        epochs=10  # $$$$$$
    )
    
    # 3. Deploy expensive custom model
    return custom_model
```

**Why avoid:**
- ❌ Costs $10,000+
- ❌ Takes months
- ❌ Can't easily update
- ❌ Black box

### MCP (Model Context Protocol)
```python
# Pseudo-code
def handle_mcp_command(user_input):
    # 1. LLM decides what to do
    intent = llm.classify(user_input)
    
    # 2. Call appropriate tool
    if intent == "create_box":
        result = rhino_tools.create_box(
            center=[0,0,0],
            size=[1,1,1]
        )
    
    # 3. Return result
    return f"Created box: {result.id}"
```

**Example:**
- User: "Create a box at the origin"
- LLM: Calls `create_box(center=[0,0,0])`
- Rhino: Executes command
- User: Sees box appear

---

## Implementation Phases

### ✅ Phase 0: DONE
- Screenshot capture to Vessel Studio ✅

### 🎯 Phase 1: RAG Chat (2-3 weeks)
```
Week 1: Backend
- Set up Pinecone/Supabase vector DB
- Scrape Rhino documentation
- Create embeddings
- Build /api/plugin/chat endpoint

Week 2: Plugin UI
- Add Eto.Forms chat panel
- Connect to API
- Handle async responses
- Add conversation history

Week 3: Testing & Refinement
- Test with real questions
- Tune retrieval
- Add vessel design docs
- Improve prompts
```

### 🔮 Phase 2: Vision (1-2 weeks)
```
- Add screenshot context to chat
- Use GPT-4 Vision API
- Send viewport + question
- Get visual analysis
```

### 🔮 Phase 3: MCP Control (3-4 weeks)
```
- Evaluate if needed
- Implement TCP server
- Add tool definitions
- Safe command execution
```

---

## Cost Breakdown

### Monthly Operating Costs

#### RAG System
```
Pinecone Starter:        $20/mo  (vector database)
OpenAI Embeddings:       $5/mo   (document indexing)
OpenAI GPT-4 Turbo:      $50/mo  (100 chats/day)
Total:                   $75/mo
                         ↓
With heavy usage:        $150/mo (500 chats/day)
```

#### Fine-Tuning (DON'T DO)
```
Training data prep:      $5,000  (one-time)
Fine-tuning:            $10,000+ (one-time)
Custom model hosting:    $500/mo (ongoing)
Total Year 1:           $21,000+
```

#### MCP Only
```
OpenAI API:              $30/mo  (just tool calls)
Infrastructure:          $10/mo  (server/hosting)
Total:                   $40/mo
```

---

## Why RAG Beats Fine-Tuning

### Knowledge Updates

**Fine-Tuning:**
```
Rhino 8.5 released with new API
↓
Collect 1000+ new examples
↓
Re-train entire model ($10,000)
↓
Wait 2 weeks
↓
Deploy new model
↓
Users get updated knowledge
```

**RAG:**
```
Rhino 8.5 released with new API
↓
Scrape new docs (10 minutes)
↓
Generate embeddings ($0.50)
↓
Upload to vector DB (instant)
↓
Users get updated knowledge immediately
```

### Example Comparison

**User Question:** "How do I use the new Rhino 8.5 SubD feature?"

**Fine-Tuned Model:**
- ❌ Doesn't know about Rhino 8.5 (trained on 8.0)
- ❌ Makes up fake API (hallucinates)
- ❌ No source citation
- Cost to update: $10,000

**RAG System:**
- ✅ Retrieves Rhino 8.5 SubD docs
- ✅ Gives accurate answer
- ✅ Cites official documentation
- Cost to update: $0.50

---

## Your Specific Use Cases

### 1. "How do I model a box?" (RAG)
```
User: "How do I create a box in Rhino?"
↓
RAG retrieves: RhinoCommon.Geometry.Box documentation
↓
AI: "You can create a box using the Box class:
     var box = new Box(
       new BoundingBox(
         new Point3d(0,0,0),
         new Point3d(1,1,1)
       )
     );
     doc.Objects.AddBox(box);
     
     Source: RhinoCommon API - Box Class"
```

### 2. "Create a box" (MCP)
```
User: "Create a box at the origin"
↓
LLM: Classifies intent → create_box action
↓
MCP tool call: create_box(center=[0,0,0], size=[1,1,1])
↓
Rhino executes command
↓
AI: "Created a 1x1x1 box at the origin"
User: Sees box in viewport
```

### 3. Visual Analysis (Vision + RAG)
```
User: Takes screenshot + asks "What's wrong with my hull shape?"
↓
Vision API: Analyzes image
↓
RAG: Retrieves vessel design principles
↓
AI: "I see the hull has excessive deadrise near the stern. 
     For optimal hydrodynamics, consider reducing the angle
     to 12-15 degrees. Reference: Naval Architecture Basics,
     Section 4.2: Hull Form Design"
```

---

## Quick Start Checklist

### To Add RAG Chat to Plugin:

- [ ] **Backend (1 week)**
  - [ ] Set up Pinecone account (free tier)
  - [ ] Scrape Rhino docs (script provided)
  - [ ] Generate embeddings
  - [ ] Create `/api/plugin/chat` endpoint
  
- [ ] **Plugin UI (1 week)**
  - [ ] Add `ChatPanel.cs` (Eto.Forms)
  - [ ] Add `VesselStudioChatCommand.cs`
  - [ ] Connect to chat API
  - [ ] Test conversation flow
  
- [ ] **Knowledge Base (ongoing)**
  - [ ] Rhino SDK documentation
  - [ ] Grasshopper components
  - [ ] Your vessel design guides
  - [ ] Update as needed

---

## Final Recommendation

### START WITH: RAG Chat
**Why:**
- ✅ Solves immediate need (knowledge Q&A)
- ✅ Cheap ($75/month)
- ✅ Fast to implement (2-3 weeks)
- ✅ Easy to update
- ✅ Valuable immediately

### ADD LATER: Vision
**Why:**
- ✅ Enhances RAG answers
- ✅ Enables visual suggestions
- ✅ Only $10/month more

### MAYBE LATER: MCP Control
**Why:**
- ⚠️ Complex to implement safely
- ⚠️ Risk of breaking user models
- ⚠️ May not be needed if RAG works well

### NEVER: Fine-Tuning
**Why:**
- ❌ 100x more expensive
- ❌ Harder to maintain
- ❌ RAG does same job better

---

## Next Action

**I recommend we:**
1. Build RAG backend first (Pinecone + scraping)
2. Add chat UI to plugin
3. Test with real users
4. Add vision if needed
5. Consider MCP only if users request control features

**Would you like me to start building the RAG system?**

I can provide:
- Documentation scraping script
- Vector DB setup code
- Chat API endpoint
- Plugin chat UI component

Just let me know which part to start with! 🚀
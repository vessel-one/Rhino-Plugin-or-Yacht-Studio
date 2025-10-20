# AI Integration Strategy: RAG vs MCP for Vessel Studio Rhino Plugin

**Date:** October 20, 2025  
**Context:** Plugin loads successfully, planning AI assistant integration

---

## Your Requirements Analysis

### MVP Priorities (In Order)
1. ✅ **DONE:** Screenshot capture → Vessel Studio canvas
2. 🎯 **NEXT:** Basic chat UI within Rhino
3. 🎯 **NEXT:** AI with Rhino modeling knowledge
4. 🎯 **NEXT:** AI with vessel design knowledge
5. 🔮 **FUTURE:** Vision capabilities for suggestions
6. 🔮 **FUTURE:** Project awareness (Vessel Studio sync)
7. 🔮 **FUTURE:** Direct Rhino control (create box, etc.)

---

## RAG vs MCP: Which to Use?

### Quick Answer
**Use BOTH - They solve different problems:**
- **RAG (Retrieval Augmented Generation):** For knowledge/documentation
- **MCP (Model Context Protocol):** For Rhino control/actions

### Detailed Comparison

| Feature | RAG | MCP | Best For |
|---------|-----|-----|----------|
| **Purpose** | Knowledge retrieval | Tool execution | Different goals |
| **Use Case** | "How do I create a NURBS surface?" | "Create a box at 0,0,0" | Complementary |
| **Training** | No training needed | No training needed | Both scalable |
| **Data** | Docs, PDFs, websites | Function definitions | Different inputs |
| **Response** | Text answers | Actions in Rhino | Different outputs |
| **Complexity** | Medium | High | RAG easier to start |
| **Cost** | Embedding + LLM calls | LLM calls only | RAG slightly higher |
| **Latency** | ~2-3 seconds | ~1-2 seconds | MCP faster |

---

## Recommended Architecture

### Phase 1: MVP Chat (RAG Only) 🎯 START HERE

```
User in Rhino Plugin
    ↓ Question: "How to create NURBS surface?"
Chat UI (Eto.Forms in plugin)
    ↓ HTTP POST
Vessel Studio API (/api/plugin/chat)
    ↓ Vector similarity search
RAG System (Vector DB)
    ↓ Retrieved context
LLM (OpenAI/Anthropic)
    ↓ Answer with context
Response to Rhino Plugin
```

**Why RAG First:**
- ✅ Easier to implement
- ✅ Valuable immediately (answers questions)
- ✅ No risk (can't break user's model)
- ✅ Builds foundation for MCP later

### Phase 2: Add Vision 🔮 LATER

```
User in Rhino Plugin
    ↓ Screenshot + Question
Chat UI with viewport context
    ↓ HTTP POST with image
Vessel Studio API
    ↓ Image + text
Vision-capable LLM (GPT-4 Vision, Claude 3)
    ↓ Visual analysis + RAG context
Response with suggestions
```

### Phase 3: Add MCP Control 🔮 MUCH LATER

```
User: "Create a box at origin"
    ↓ Intent classification
LLM + MCP Tools
    ↓ Function call: create_box()
Plugin TCP Server (like RhinoMCP)
    ↓ Execute on main thread
Rhino Document updated
```

---

## Technical Implementation Plan

### MVP: RAG-Powered Chat in Rhino

#### 1. Knowledge Base Sources

**Priority 1: Essential Docs**
- Rhino SDK Documentation (RhinoCommon API)
- Grasshopper Component Documentation
- Vessel Design Basics (your custom content)

**Priority 2: Nice to Have**
- Rhino user manual
- Naval architecture principles
- Yacht design standards

**Priority 3: Advanced**
- Community forum Q&A
- Tutorial transcripts
- Expert vessel designs

#### 2. RAG System Architecture

```typescript
// Vessel Studio Backend (Next.js API)

// Vector Database Setup
import { PineconeClient } from '@pinecone-database/pinecone'
import { OpenAIEmbeddings } from 'langchain/embeddings/openai'

// Document Processing Pipeline
1. Scrape/collect documentation
   - Rhino docs: https://developer.rhino3d.com
   - Grasshopper docs: https://grasshopperdocs.com
   - Your vessel design guides

2. Chunk documents (500-1000 tokens each)
   - Semantic chunking (by section/topic)
   - Overlap chunks for context

3. Generate embeddings
   - OpenAI text-embedding-ada-002
   - Store in Pinecone/Supabase/Weaviate

4. Store with metadata
   - Source (Rhino/Grasshopper/Vessel)
   - Category (API/Tutorial/Concept)
   - Version info
```

#### 3. Chat API Endpoint

```typescript
// filepath: src/app/api/plugin/chat/route.ts
import { NextRequest, NextResponse } from 'next/server'
import { OpenAI } from 'openai'
import { retrieveRelevantDocs } from '@/lib/rag'

export async function POST(request: NextRequest) {
  const { message, userId, projectId, conversationHistory } = await request.json()
  
  // 1. Retrieve relevant documentation
  const relevantDocs = await retrieveRelevantDocs(message, {
    topK: 5,
    filters: {
      sources: ['rhino-sdk', 'vessel-design'],
      minScore: 0.7
    }
  })
  
  // 2. Build context-aware prompt
  const systemPrompt = `You are a helpful assistant for Rhino 3D and vessel design.
You have access to the following documentation:

${relevantDocs.map(doc => `
Source: ${doc.source}
Content: ${doc.content}
`).join('\n---\n')}

Answer the user's question using this documentation. If you reference specific 
functions or concepts, cite the source.`
  
  // 3. Call LLM with context
  const openai = new OpenAI()
  const completion = await openai.chat.completions.create({
    model: 'gpt-4-turbo-preview',
    messages: [
      { role: 'system', content: systemPrompt },
      ...conversationHistory,
      { role: 'user', content: message }
    ],
    temperature: 0.7,
    max_tokens: 1000
  })
  
  return NextResponse.json({
    response: completion.choices[0].message.content,
    sources: relevantDocs.map(d => ({
      title: d.title,
      url: d.url
    }))
  })
}
```

#### 4. Rhino Plugin Chat UI

```csharp
// filepath: VesselStudioPlugin/UI/ChatPanel.cs
using Eto.Forms;
using Eto.Drawing;
using System.Net.Http;
using Newtonsoft.Json;

namespace VesselStudioPlugin.UI
{
    public class ChatPanel : Panel
    {
        private TextArea chatHistory;
        private TextBox messageInput;
        private Button sendButton;
        private List<ChatMessage> conversation;
        
        public ChatPanel()
        {
            conversation = new List<ChatMessage>();
            InitializeUI();
        }
        
        private void InitializeUI()
        {
            // Chat history display
            chatHistory = new TextArea
            {
                ReadOnly = true,
                Wrap = true,
                Text = "Welcome! Ask me about Rhino modeling or vessel design.\n\n"
            };
            
            // Input area
            messageInput = new TextBox
            {
                PlaceholderText = "Ask a question..."
            };
            
            messageInput.KeyDown += (s, e) =>
            {
                if (e.Key == Keys.Enter && !e.Shift)
                {
                    SendMessage();
                    e.Handled = true;
                }
            };
            
            sendButton = new Button { Text = "Send" };
            sendButton.Click += (s, e) => SendMessage();
            
            // Layout
            Content = new StackLayout
            {
                Padding = 10,
                Spacing = 10,
                Items =
                {
                    new StackLayoutItem(chatHistory, expand: true),
                    new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 5,
                        Items =
                        {
                            new StackLayoutItem(messageInput, expand: true),
                            sendButton
                        }
                    }
                }
            };
        }
        
        private async void SendMessage()
        {
            var message = messageInput.Text.Trim();
            if (string.IsNullOrEmpty(message)) return;
            
            // Add user message to UI
            chatHistory.Append($"You: {message}\n");
            messageInput.Text = "";
            conversation.Add(new ChatMessage { Role = "user", Content = message });
            
            // Show loading
            chatHistory.Append("Assistant: Thinking...\n");
            
            try
            {
                // Call API
                var response = await SendChatRequest(message);
                
                // Update UI with response
                chatHistory.Text = chatHistory.Text.Replace("Thinking...\n", "");
                chatHistory.Append($"{response}\n\n");
                
                conversation.Add(new ChatMessage { Role = "assistant", Content = response });
            }
            catch (Exception ex)
            {
                chatHistory.Text = chatHistory.Text.Replace("Thinking...\n", "");
                chatHistory.Append($"Error: {ex.Message}\n\n");
            }
        }
        
        private async Task<string> SendChatRequest(string message)
        {
            var plugin = VesselStudioPlugin.Instance;
            using var client = new HttpClient();
            
            var requestBody = new
            {
                message,
                userId = plugin.ApiClient.UserId,
                conversationHistory = conversation.TakeLast(10).ToList()
            };
            
            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await client.PostAsync(
                "https://vesselstudio.ai/api/plugin/chat",
                content
            );
            
            response.EnsureSuccessStatusCode();
            var responseText = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ChatResponse>(responseText);
            
            return result.Response;
        }
    }
    
    public class ChatMessage
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }
    
    public class ChatResponse
    {
        public string Response { get; set; }
        public List<Source> Sources { get; set; }
    }
    
    public class Source
    {
        public string Title { get; set; }
        public string Url { get; set; }
    }
}
```

#### 5. Add Chat Panel Command

```csharp
// filepath: VesselStudioPlugin/Commands/VesselStudioChatCommand.cs
using Rhino;
using Rhino.Commands;
using Eto.Forms;

namespace VesselStudioPlugin.Commands
{
    public class VesselStudioChatCommand : Command
    {
        private static Form chatWindow;
        
        public override string EnglishName => "VesselStudioChat";
        
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Create or show chat window
            if (chatWindow == null || !chatWindow.Visible)
            {
                chatWindow = new Form
                {
                    Title = "Vessel Studio AI Assistant",
                    Size = new Size(400, 600),
                    Content = new ChatPanel()
                };
                
                chatWindow.Show();
            }
            else
            {
                chatWindow.Focus();
            }
            
            return Result.Success;
        }
    }
}
```

---

## RAG vs Fine-Tuning: Why RAG is Better

### Fine-Tuning ❌ (Not Recommended)

```
Training Process:
1. Collect 1000+ Q&A pairs about Rhino
2. Fine-tune GPT-4 ($$$$$)
3. Wait days for training
4. Deploy custom model

Problems:
❌ Expensive ($10,000+ for quality results)
❌ Static knowledge (outdated quickly)
❌ Hard to update (retrain entire model)
❌ Black box (can't see what it learned)
❌ Hallucination risk (makes up APIs)
```

### RAG ✅ (Recommended)

```
Setup Process:
1. Scrape documentation (1 day)
2. Generate embeddings ($50)
3. Deploy immediately
4. Update anytime

Benefits:
✅ Cheap ($50 setup + $10/month)
✅ Dynamic knowledge (update docs anytime)
✅ Easy to update (add new docs)
✅ Transparent (see retrieved sources)
✅ Accurate (quotes real documentation)
✅ Citable (shows source references)
```

---

## Implementation Roadmap

### Week 1: RAG Foundation
- [ ] Set up vector database (Pinecone/Supabase)
- [ ] Scrape Rhino SDK docs
- [ ] Generate embeddings
- [ ] Build chat API endpoint
- [ ] Test with simple queries

### Week 2: Plugin Chat UI
- [ ] Create Eto.Forms chat panel
- [ ] Add chat command to plugin
- [ ] Connect to API
- [ ] Test conversation flow
- [ ] Handle errors gracefully

### Week 3: Knowledge Enhancement
- [ ] Add Grasshopper docs
- [ ] Add vessel design content
- [ ] Improve chunking strategy
- [ ] Tune retrieval parameters
- [ ] Add conversation memory

### Week 4: Vision MVP
- [ ] Add screenshot context to chat
- [ ] Test vision model (GPT-4V)
- [ ] Build visual analysis prompts
- [ ] Test suggestion quality

### Month 2: MCP Integration (Optional)
- [ ] Evaluate RhinoMCP approach
- [ ] Decide on control scope
- [ ] Implement TCP server if needed
- [ ] Add safe command execution
- [ ] Test with simple commands

---

## Cost Estimation

### RAG System (Monthly)
- **Vector Database:** $20/month (Pinecone Starter)
- **OpenAI Embeddings:** $5/month (1M tokens)
- **OpenAI GPT-4 API:** $50-200/month (depends on usage)
- **Total:** ~$75-225/month

### Fine-Tuning (One-Time + Monthly)
- **Training Data Prep:** $5,000 (manual work)
- **Fine-Tuning Cost:** $10,000+ (GPT-4)
- **Inference:** $500+/month (custom model)
- **Total First Year:** $20,000+

**RAG is 100x cheaper and more maintainable!**

---

## Technical Stack Recommendation

### Backend (Vessel Studio)
```typescript
// Vector Database
- Pinecone (easiest) OR
- Supabase pgvector (cheaper) OR
- Weaviate (self-hosted)

// Embeddings
- OpenAI text-embedding-ada-002 ($0.0001/1K tokens)
- OpenAI text-embedding-3-small (cheaper, faster)

// LLM
- OpenAI GPT-4 Turbo (best quality)
- Anthropic Claude 3 Opus (good alternative)
- OpenAI GPT-3.5 Turbo (faster, cheaper for simple questions)

// Framework
- LangChain (comprehensive but heavy)
- LlamaIndex (focused on RAG)
- Custom implementation (lightweight)
```

### Plugin (Rhino C#)
```csharp
// UI Framework
- Eto.Forms (cross-platform, already using)

// HTTP Client
- System.Net.Http.HttpClient (built-in)

// JSON
- Newtonsoft.Json (already using)

// Async/Await
- Task-based async (built-in C#)
```

---

## Security & Safety

### API Security
```csharp
// Always authenticate
private async Task<string> SendChatRequest(string message)
{
    var request = new HttpRequestMessage(HttpMethod.Post, chatEndpoint);
    request.Headers.Authorization = 
        new AuthenticationHeaderValue("Bearer", apiKey);
    // ...
}
```

### Content Filtering
```typescript
// Backend: Filter inappropriate queries
if (containsInappropriateContent(message)) {
  return { error: 'Please ask questions related to Rhino or vessel design' }
}
```

### Rate Limiting
```typescript
// Prevent abuse
const rateLimit = rateLimit({
  windowMs: 60 * 1000, // 1 minute
  max: 10 // 10 requests per minute
})
```

---

## MVP Success Criteria

### Phase 1: Basic Chat ✅
- [ ] User can open chat panel in Rhino
- [ ] User can ask questions about Rhino SDK
- [ ] AI responds with relevant documentation
- [ ] Responses cite sources
- [ ] Conversation maintains context

### Phase 2: Vessel Knowledge ✅
- [ ] AI knows vessel design terminology
- [ ] AI can answer yacht design questions
- [ ] AI references your custom content
- [ ] Responses are domain-specific

### Phase 3: Vision ✅
- [ ] AI can see current viewport
- [ ] AI makes suggestions based on visual
- [ ] AI identifies modeling mistakes
- [ ] Visual context improves answers

---

## Recommendation Summary

### START WITH (MVP):
1. ✅ **Screenshot capture to canvas** (DONE!)
2. 🎯 **RAG-powered chat in Rhino** (NEXT)
3. 🎯 **Rhino + vessel design knowledge** (NEXT)

### ADD LATER (Enhanced):
4. 🔮 **Vision capabilities** (Month 2)
5. 🔮 **Project awareness** (Month 2-3)
6. 🔮 **MCP control** (Month 3+, if needed)

### DON'T DO:
- ❌ Fine-tune custom model (too expensive)
- ❌ Build complex MCP first (overkill)
- ❌ Try to do everything at once

---

## Next Steps

Would you like me to:

1. **Build the RAG backend** - Set up vector DB and chat API?
2. **Create chat UI in plugin** - Add Eto.Forms chat panel?
3. **Scrape documentation** - Start building knowledge base?
4. **All of the above** - Full MVP implementation?

**My recommendation:** Let's start with #3 (scrape docs) and #1 (RAG backend) first, then #2 (plugin UI) once backend is working.

---

**The key insight:** RAG gives you a knowledgeable AI assistant without the cost and complexity of fine-tuning or full MCP integration. Start simple, prove value, then enhance!
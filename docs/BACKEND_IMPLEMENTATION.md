# Backend Implementation Guide - Vessel One Rhino Integration

**Date:** October 20, 2025  
**Purpose:** Complete implementation guide for Vessel One backend to support Rhino plugin integration  
**Target Audience:** Vessel One backend developers

---

## 🎯 Overview

This guide provides complete implementation details for adding Rhino plugin support to the Vessel One backend. The Rhino plugin will upload viewport captures directly to project galleries with real-time browser updates.

---

## 📋 Implementation Checklist

### Phase 1: API Key Management (2-3 hours)
- [ ] Database schema for API keys
- [ ] API key generation endpoint
- [ ] API key validation endpoint
- [ ] Frontend: API key management UI

### Phase 2: Rhino API Endpoints (3-4 hours)
- [ ] List projects endpoint
- [ ] Upload screenshot endpoint
- [ ] Firebase Storage integration
- [ ] Firestore gallery collection

### Phase 3: Frontend Integration (2-3 hours)
- [ ] Real-time gallery listeners
- [ ] Toast notifications for Rhino uploads
- [ ] Rhino badge/icon for captures

### Phase 4: Testing (1-2 hours)
- [ ] API endpoint testing
- [ ] Rhino plugin integration test
- [ ] Real-time update verification

**Total Estimated Time:** 8-12 hours

---

## 🗄️ Database Schema

### Firestore Collections

#### 1. API Keys Collection
```typescript
// Collection: users/{userId}/apiKeys/{keyId}
interface VesselOneApiKey {
  id: string;                    // Auto-generated document ID
  userId: string;                // Owner user ID
  keyPrefix: string;             // "vsk_live_abc1..." (for display)
  keyHash: string;               // SHA-256 hash of full key
  name: string;                  // User-friendly name
  permissions: string[];         // ["rhino:upload", "projects:read"]
  createdAt: Timestamp;
  lastUsedAt: Timestamp | null;
  expiresAt: Timestamp | null;   // Optional expiration
  isActive: boolean;
  usageCount: number;            // Track API calls
}
```

#### 2. Gallery Images Collection
```typescript
// Collection: projects/{projectId}/gallery/{imageId}
interface GalleryImage {
  id: string;                    // Auto-generated document ID
  projectId: string;
  name: string;                  // User-provided image name
  url: string;                   // Firebase Storage public URL
  source: string;                // "rhino-plugin" | "web-upload" | "mobile"
  uploadedBy: string;            // User ID
  uploadedAt: Timestamp;
  metadata: {
    width: number;
    height: number;
    viewportName?: string;       // Rhino-specific
    displayMode?: string;        // Rhino-specific
    rhinoVersion?: string;       // Rhino-specific
  };
}
```

---

## 🔌 API Endpoints

### 1. Create API Key

**Endpoint:** `POST /api/user/api-keys`  
**Auth:** Required (user session)  
**Purpose:** Generate a new API key for Rhino plugin

**Request Body:**
```typescript
{
  name: string;  // Optional: "Rhino Plugin", "Dev Machine", etc.
}
```

**Response:**
```typescript
{
  apiKey: string;        // "vsk_live_abc123xyz..." (ONLY SHOWN ONCE!)
  keyId: string;         // Document ID
  message: string;       // "Save this key securely - you won't see it again!"
}
```

**Implementation:**

```typescript
// app/api/user/api-keys/route.ts
import { auth } from '@/lib/auth';
import { db } from '@/lib/firebase-admin';
import { randomBytes, createHash } from 'crypto';

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
  const keyPrefix = fullKey.substring(0, 16); // "vsk_live_abc1..."

  const apiKeyDoc = {
    userId: session.user.id,
    keyPrefix,
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

  return Response.json({
    apiKey: fullKey,  // ONLY TIME WE RETURN THIS
    keyId: keyRef.id,
    message: 'Save this key securely - you won\'t see it again!'
  });
}

// List user's API keys
export async function GET(request: Request) {
  const session = await auth();
  if (!session?.user?.id) {
    return Response.json({ error: 'Unauthorized' }, { status: 401 });
  }

  const keysSnapshot = await db.collection('users')
    .doc(session.user.id)
    .collection('apiKeys')
    .orderBy('createdAt', 'desc')
    .get();

  const keys = keysSnapshot.docs.map(doc => ({
    id: doc.id,
    ...doc.data(),
    keyHash: undefined, // Don't expose hash
  }));

  return Response.json({ keys });
}
```

---

### 2. Validate API Key

**Endpoint:** `POST /api/rhino/validate`  
**Auth:** Bearer token (API key)  
**Purpose:** Validate Rhino plugin API key

**Headers:**
```
Authorization: Bearer vsk_live_abc123xyz...
```

**Response:**
```typescript
{
  valid: boolean;
  userId: string;
  userName: string;
  permissions: string[];
}
```

**Implementation:**

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

  // Search for matching key across all users
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
    permissions: keyData.permissions || [],
  });
}
```

**Helper Function (create this):**

```typescript
// lib/api-auth.ts
import { db } from './firebase-admin';
import { createHash } from 'crypto';

export async function validateApiKey(request: Request) {
  const apiKey = request.headers.get('Authorization')?.replace('Bearer ', '');
  
  if (!apiKey || !apiKey.startsWith('vsk_')) {
    return { valid: false, userId: null, permissions: [] };
  }

  const keyHash = createHash('sha256').update(apiKey).digest('hex');

  const keysSnapshot = await db.collectionGroup('apiKeys')
    .where('keyHash', '==', keyHash)
    .where('isActive', '==', true)
    .limit(1)
    .get();

  if (keysSnapshot.empty) {
    return { valid: false, userId: null, permissions: [] };
  }

  const keyDoc = keysSnapshot.docs[0];
  const keyData = keyDoc.data();

  // Update usage stats (non-blocking)
  keyDoc.ref.update({
    lastUsedAt: new Date(),
    usageCount: (keyData.usageCount || 0) + 1,
  }).catch(() => {}); // Ignore errors

  return {
    valid: true,
    userId: keyData.userId,
    permissions: keyData.permissions || [],
  };
}
```

---

### 3. List Projects

**Endpoint:** `GET /api/rhino/projects`  
**Auth:** Bearer token (API key)  
**Purpose:** Get list of user's projects for dropdown

**Response:**
```typescript
{
  projects: Array<{
    id: string;
    name: string;
    type: string;
    thumbnailUrl?: string;
    updatedAt: string;
  }>;
}
```

**Implementation:**

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

  // Get projects where user is member (owner or collaborator)
  const projectsSnapshot = await db.collection('projects')
    .where('members', 'array-contains', userId)
    .orderBy('updatedAt', 'desc')
    .limit(50)
    .get();

  const projects = projectsSnapshot.docs.map(doc => {
    const data = doc.data();
    return {
      id: doc.id,
      name: data.name,
      type: data.type || 'yacht',
      thumbnailUrl: data.thumbnailUrl,
      updatedAt: data.updatedAt?.toDate()?.toISOString(),
    };
  });

  return Response.json({ projects });
}
```

---

### 4. Upload Screenshot

**Endpoint:** `POST /api/rhino/projects/[projectId]/upload`  
**Auth:** Bearer token (API key)  
**Purpose:** Upload viewport screenshot to project gallery

**Request:** `multipart/form-data`
- `image`: PNG file (binary)
- `name`: Image name (string)
- `metadata`: JSON string with viewport info

**Response:**
```typescript
{
  success: boolean;
  imageId: string;
  imageUrl: string;
  message: string;
}
```

**Implementation:**

```typescript
// app/api/rhino/projects/[projectId]/upload/route.ts
import { db, storage } from '@/lib/firebase-admin';
import { validateApiKey } from '@/lib/api-auth';
import { nanoid } from 'nanoid';

export async function POST(
  request: Request,
  { params }: { params: { projectId: string } }
) {
  // Validate API key
  const authResult = await validateApiKey(request);
  if (!authResult.valid) {
    return Response.json({ error: 'Unauthorized' }, { status: 401 });
  }

  // Verify project access
  const projectDoc = await db.collection('projects').doc(params.projectId).get();
  if (!projectDoc.exists) {
    return Response.json({ error: 'Project not found' }, { status: 404 });
  }

  const projectData = projectDoc.data()!;
  if (!projectData.members?.includes(authResult.userId)) {
    return Response.json({ error: 'Access denied to project' }, { status: 403 });
  }

  // Parse form data
  const formData = await request.formData();
  const imageFile = formData.get('image') as File;
  const imageName = formData.get('name') as string || `Rhino Capture ${Date.now()}`;
  const metadataStr = formData.get('metadata') as string || '{}';
  const metadata = JSON.parse(metadataStr);

  if (!imageFile) {
    return Response.json({ error: 'No image provided' }, { status: 400 });
  }

  // Validate image size (max 10MB)
  if (imageFile.size > 10 * 1024 * 1024) {
    return Response.json({ error: 'Image too large (max 10MB)' }, { status: 400 });
  }

  try {
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

    // Make publicly accessible
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
        width: metadata.width || 1920,
        height: metadata.height || 1080,
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
  } catch (error) {
    console.error('Upload error:', error);
    return Response.json(
      { error: 'Upload failed', details: error.message },
      { status: 500 }
    );
  }
}
```

---

## 🎨 Frontend Implementation

### 1. API Key Management Page

**File:** `app/profile/api-keys/page.tsx`

```typescript
'use client';

import { useState, useEffect } from 'react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { toast } from 'sonner';
import { Copy, Trash2, Plus } from 'lucide-react';

export default function ApiKeysPage() {
  const [apiKeys, setApiKeys] = useState([]);
  const [newKey, setNewKey] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    loadApiKeys();
  }, []);

  async function loadApiKeys() {
    const res = await fetch('/api/user/api-keys');
    const data = await res.json();
    setApiKeys(data.keys || []);
  }

  async function createApiKey() {
    setLoading(true);
    try {
      const res = await fetch('/api/user/api-keys', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name: 'Rhino Plugin' })
      });
      const data = await res.json();
      setNewKey(data.apiKey);
      await loadApiKeys();
    } catch (error) {
      toast.error('Failed to create API key');
    } finally {
      setLoading(false);
    }
  }

  async function revokeKey(keyId: string) {
    if (!confirm('Revoke this API key? Rhino plugin will stop working.')) {
      return;
    }
    
    try {
      await fetch(`/api/user/api-keys/${keyId}`, { method: 'DELETE' });
      toast.success('API key revoked');
      await loadApiKeys();
    } catch (error) {
      toast.error('Failed to revoke key');
    }
  }

  function copyToClipboard(key: string) {
    navigator.clipboard.writeText(key);
    toast.success('API key copied to clipboard!');
  }

  return (
    <div className="container mx-auto p-6 max-w-4xl">
      <h1 className="text-3xl font-bold mb-2">Rhino Plugin API Keys</h1>
      <p className="text-gray-600 mb-6">
        Generate API keys to connect Rhino 8 to Vessel One
      </p>

      {newKey && (
        <div className="bg-yellow-50 border-2 border-yellow-300 rounded-lg p-4 mb-6">
          <p className="font-semibold text-yellow-900 mb-2">
            ⚠️ Save this key - you won't see it again!
          </p>
          <div className="flex gap-2">
            <Input 
              value={newKey} 
              readOnly 
              className="font-mono text-sm"
            />
            <Button onClick={() => copyToClipboard(newKey)}>
              <Copy className="w-4 h-4 mr-2" />
              Copy
            </Button>
          </div>
          <p className="text-sm text-gray-600 mt-2">
            In Rhino 8, type: <code className="bg-gray-200 px-2 py-1 rounded">VesselSetApiKey</code>
          </p>
        </div>
      )}

      <Button onClick={createApiKey} disabled={loading} className="mb-6">
        <Plus className="w-4 h-4 mr-2" />
        Create New API Key
      </Button>

      <div className="space-y-3">
        <h2 className="text-xl font-semibold">Active Keys</h2>
        {apiKeys.length === 0 ? (
          <p className="text-gray-500">No API keys yet. Create one to get started.</p>
        ) : (
          apiKeys.map((key: any) => (
            <div 
              key={key.id} 
              className="flex items-center justify-between p-4 border rounded-lg hover:bg-gray-50"
            >
              <div>
                <p className="font-mono text-sm">{key.keyPrefix}••••••••••••••</p>
                <p className="text-xs text-gray-500">
                  Created {new Date(key.createdAt.toDate()).toLocaleDateString()}
                  {key.lastUsedAt && (
                    <> • Last used {new Date(key.lastUsedAt.toDate()).toLocaleDateString()}</>
                  )}
                </p>
                <p className="text-xs text-gray-500">
                  {key.usageCount || 0} API calls
                </p>
              </div>
              <Button 
                variant="destructive" 
                size="sm"
                onClick={() => revokeKey(key.id)}
              >
                <Trash2 className="w-4 h-4 mr-1" />
                Revoke
              </Button>
            </div>
          ))
        )}
      </div>

      <div className="mt-8 p-4 bg-blue-50 rounded-lg">
        <h3 className="font-semibold mb-2">How to use:</h3>
        <ol className="list-decimal list-inside space-y-1 text-sm">
          <li>Create an API key above</li>
          <li>Copy the key to clipboard</li>
          <li>Open Rhino 8</li>
          <li>Type: <code className="bg-white px-2 py-1 rounded">VesselSetApiKey</code></li>
          <li>Paste your API key when prompted</li>
          <li>Start capturing with: <code className="bg-white px-2 py-1 rounded">VesselCapture</code></li>
        </ol>
      </div>
    </div>
  );
}
```

---

### 2. Real-time Gallery Updates

**File:** `app/projects/[id]/page.tsx` (add to existing)

```typescript
'use client';

import { useEffect, useState } from 'react';
import { db } from '@/lib/firebase';
import { collection, onSnapshot, query, orderBy } from 'firebase/firestore';
import { toast } from 'sonner';

export default function ProjectPage({ params }: { params: { id: string } }) {
  const [galleryImages, setGalleryImages] = useState([]);

  useEffect(() => {
    // Real-time listener for gallery images
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
        if (change.type === 'added') {
          const data = change.doc.data();
          if (data.source === 'rhino-plugin') {
            toast.success(
              `📐 New Rhino capture: ${data.name}`,
              {
                duration: 4000,
                action: {
                  label: 'View',
                  onClick: () => {
                    // Scroll to image or open lightbox
                    document.getElementById(`img-${change.doc.id}`)?.scrollIntoView();
                  }
                }
              }
            );
          }
        }
      });
    });

    return () => unsubscribe();
  }, [params.id]);

  return (
    <div>
      {/* Your existing project UI */}
      
      <div className="gallery-grid grid grid-cols-3 gap-4">
        {galleryImages.map(image => (
          <div 
            key={image.id} 
            id={`img-${image.id}`}
            className="gallery-item relative group"
          >
            <img 
              src={image.url} 
              alt={image.name}
              className="w-full h-auto rounded-lg"
            />
            <div className="absolute bottom-0 left-0 right-0 bg-black/70 text-white p-2 rounded-b-lg opacity-0 group-hover:opacity-100 transition-opacity">
              <p className="text-sm font-medium">{image.name}</p>
              <p className="text-xs text-gray-300">
                {image.source === 'rhino-plugin' && '📐 Rhino'} 
                {' • '}
                {new Date(image.uploadedAt.toDate()).toLocaleString()}
              </p>
              {image.metadata?.viewportName && (
                <p className="text-xs text-gray-400">
                  {image.metadata.viewportName} • {image.metadata.displayMode}
                </p>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
```

---

## 🔐 Security Considerations

### API Key Security
1. **Never store plaintext keys** in database
   - Store SHA-256 hash only
   - Compare hashes on validation

2. **Rate limiting** on API endpoints
   ```typescript
   // Implement rate limiting per API key
   // Suggested: 100 requests per hour
   ```

3. **Key revocation** support
   - User can revoke keys instantly
   - All future requests fail immediately

4. **Permissions model**
   - Each key has specific permissions array
   - Check permissions before allowing operations

### Upload Security
1. **File validation**
   - Max size: 10MB
   - Only PNG/JPG allowed
   - Validate content-type header

2. **Project access control**
   - Verify user is project member
   - Check before upload

3. **Storage security rules**
   ```javascript
   // Firebase Storage rules
   rules_version = '2';
   service firebase.storage {
     match /b/{bucket}/o {
       match /projects/{projectId}/gallery/{imageId} {
         allow read: if true; // Public read
         allow write: if false; // Only via API
       }
     }
   }
   ```

---

## 🧪 Testing

### 1. API Endpoint Testing

```bash
# Create API key (requires auth session)
curl -X POST https://vessel.one/api/user/api-keys \
  -H "Cookie: session=..." \
  -H "Content-Type: application/json" \
  -d '{"name":"Test Key"}'

# Validate API key
curl -X POST https://vessel.one/api/rhino/validate \
  -H "Authorization: Bearer vsk_live_abc123..."

# List projects
curl https://vessel.one/api/rhino/projects \
  -H "Authorization: Bearer vsk_live_abc123..."

# Upload screenshot
curl -X POST https://vessel.one/api/rhino/projects/PROJECT_ID/upload \
  -H "Authorization: Bearer vsk_live_abc123..." \
  -F "image=@test.png" \
  -F "name=Test Upload" \
  -F 'metadata={"width":1920,"height":1080}'
```

### 2. Integration Testing

**Test Flow:**
1. Create API key in web app
2. Copy key to clipboard
3. Open Rhino 8
4. Run: `VesselSetApiKey`
5. Paste key, validate
6. Run: `VesselCapture`
7. Select project, name image
8. Verify upload success
9. Check browser for real-time update
10. Verify toast notification appears

---

## 📊 Monitoring & Analytics

### Metrics to Track
- API keys created per day
- Rhino uploads per project
- Upload success/failure rates
- Average upload time
- API key usage distribution

### Logging
```typescript
// Log all Rhino API requests
console.log({
  type: 'rhino-api-request',
  endpoint: request.url,
  userId: authResult.userId,
  projectId: params.projectId,
  timestamp: new Date().toISOString(),
});
```

---

## 🚀 Deployment Checklist

### Before Production
- [ ] All API endpoints tested
- [ ] Rate limiting enabled
- [ ] Error logging configured
- [ ] Firebase Storage rules deployed
- [ ] Firestore indexes created (gallery collection)
- [ ] API key UI tested
- [ ] Real-time listeners working
- [ ] Toast notifications functional

### Environment Variables
```bash
# Ensure these are set
FIREBASE_PROJECT_ID=...
FIREBASE_STORAGE_BUCKET=...
NEXT_PUBLIC_FIREBASE_API_KEY=...
```

---

## 📝 Next Steps After Implementation

1. **Test with Rhino plugin** - End-to-end integration test
2. **Documentation** - Update user docs with API key instructions
3. **Monitoring** - Set up alerts for API errors
4. **Optimization** - Add image compression if needed
5. **Features** - Consider batch upload, video support

---

## 🆘 Support & Troubleshooting

### Common Issues

**Issue:** API key validation fails
- Check SHA-256 hashing implementation
- Verify collectionGroup query works
- Check API key format (vsk_live_...)

**Issue:** Upload fails
- Verify Firebase Storage bucket configured
- Check file size limits
- Validate project access control

**Issue:** Real-time updates not working
- Check Firestore rules allow reads
- Verify onSnapshot listener setup
- Test with Firestore emulator first

---

## 📚 Additional Resources

- **API Auth Pattern:** See `lib/api-auth.ts` for helper function
- **Firebase Setup:** Ensure Storage and Firestore enabled
- **Testing:** Use Postman collection for API testing
- **Rhino Plugin:** See `docs/api-integration-flow.md` for plugin details

---

**Questions?** Contact backend team or refer to `docs/api-integration-quick-reference.md`

---

**Implementation Time Estimate:** 8-12 hours total  
**Priority:** High (enables core Rhino integration)  
**Dependencies:** Firebase Admin SDK, existing auth system


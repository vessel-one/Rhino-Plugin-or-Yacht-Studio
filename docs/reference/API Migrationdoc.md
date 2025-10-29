# Rhino Plugin API Migration Guide - Trial Tier Support

**Date**: October 29, 2025  
**API Version**: 1.1  
**Breaking Changes**: ⚠️ Minor (Response structure expanded, backward compatible)

---

## 🎯 What Changed

The `/api/rhino/validate` endpoint now returns **additional fields** for trial tier support. Users on trial plans (Pro Trial, Standard Trial) now have access to Rhino plugin during their trial period.

### Previous Behavior (v1.0)
- Only `standard`, `pro`, and `educational` base subscription tiers had access
- Users on trial tiers were **blocked** from using Rhino plugin

### New Behavior (v1.1)
- Trial tiers (`trialTier` field) now grant access
- `effectiveTier` field determines current active access level
- Trial tier **takes precedence** over base subscription tier

---

## 📝 API Response Changes

### Validation Endpoint: `POST /api/rhino/validate`

#### OLD Response (v1.0) - Still Valid
```json
{
  "success": true,
  "user": {
    "id": "user123",
    "email": "designer@example.com",
    "displayName": "John Designer",
    "subscriptionTier": "free"
  }
}
```

#### NEW Response (v1.1) - Enhanced
```json
{
  "success": true,
  "user": {
    "id": "user123",
    "email": "designer@example.com",
    "displayName": "John Designer",
    "subscriptionTier": "free",           // NEW: Base tier (after trial)
    "effectiveTier": "pro",               // NEW: Current active tier
    "trialTier": "pro",                   // NEW: Trial plan (null if none)
    "trialExpiresAt": "2025-11-28T00:00:00Z", // NEW: Trial expiration
    "hasTrialActive": true                // NEW: Boolean flag
  }
}
```

### New Fields Explained

| Field | Type | Description | Example |
|-------|------|-------------|---------|
| `subscriptionTier` | string | Base subscription tier (unchanged) | `"free"`, `"standard"`, `"pro"` |
| `effectiveTier` | string | **Current active tier** (trial OR base) | `"pro"`, `"standard"`, `"free"` |
| `trialTier` | string\|null | Trial plan if active, null otherwise | `"pro"`, `null` |
| `trialExpiresAt` | string\|null | ISO timestamp when trial ends | `"2025-11-28T00:00:00Z"` |
| `hasTrialActive` | boolean | Quick check if user is on trial | `true`, `false` |

---

## 🔧 Required C# Code Changes

### Option 1: Minimal Change (Backward Compatible) ✅ **RECOMMENDED**

**No changes required!** Your existing code will continue to work:

```csharp
// Your current code (v1.0)
var response = await client.PostAsync("/api/rhino/validate", null);
var result = await response.Content.ReadAsAsync<ValidateResponse>();

if (result.success) {
    var tier = result.user.subscriptionTier; // Still works!
    // Plugin functions normally
}
```

**Why it works:**
- Old field `subscriptionTier` still exists
- `success` flag logic unchanged
- New fields are **additive only** (no removals)

---

### Option 2: Recommended Upgrade (Use Effective Tier) ⭐

Update your model to include new fields and use `effectiveTier` for access checks:

```csharp
// Updated C# model (v1.1)
public class ValidateResponse {
    public bool Success { get; set; }
    public UserInfo User { get; set; }
}

public class UserInfo {
    public string Id { get; set; }
    public string Email { get; set; }
    public string DisplayName { get; set; }
    
    // Existing field
    public string SubscriptionTier { get; set; }
    
    // NEW FIELDS (add these)
    public string EffectiveTier { get; set; }
    public string TrialTier { get; set; }
    public string TrialExpiresAt { get; set; }
    public bool HasTrialActive { get; set; }
}

// Use effectiveTier for UI display
public async Task<bool> ValidateApiKey(string apiKey) {
    var response = await _httpClient.PostAsync("/api/rhino/validate", null);
    var result = await response.Content.ReadAsAsync<ValidateResponse>();
    
    if (!result.Success) return false;
    
    // Use effectiveTier instead of subscriptionTier
    var activeTier = result.User.EffectiveTier ?? result.User.SubscriptionTier;
    
    // Display in UI
    if (result.User.HasTrialActive) {
        StatusLabel.Text = $"Trial Access: {activeTier.ToUpper()} (expires {FormatDate(result.User.TrialExpiresAt)})";
    } else {
        StatusLabel.Text = $"Plan: {activeTier.ToUpper()}";
    }
    
    return true;
}
```

---

### Option 3: Full Implementation (Trial Expiration Warnings) 🔔

Show proactive warnings to users before trial expires:

```csharp
public void CheckTrialStatus(UserInfo user) {
    if (!user.HasTrialActive) return;
    
    var expiresAt = DateTime.Parse(user.TrialExpiresAt);
    var daysRemaining = (expiresAt - DateTime.UtcNow).Days;
    
    if (daysRemaining <= 0) {
        // Trial expired (server will block on next API call)
        ShowWarning("Your trial has expired. Upgrade to continue using Rhino plugin.");
    } else if (daysRemaining <= 3) {
        // Show warning 3 days before expiration
        ShowWarning($"Your trial expires in {daysRemaining} days. Upgrade now to avoid interruption.");
    } else if (daysRemaining <= 7) {
        // Show info banner 7 days before
        ShowInfoBanner($"Your trial expires in {daysRemaining} days.");
    }
}
```

---

## 🚨 Error Response Changes

### New Error Type: `INSUFFICIENT_TIER`

Previously, access denial only returned `API_ACCESS_SUSPENDED`. Now there are **two error types**:

#### Error Response (v1.1)
```json
{
  "success": false,
  "error": "INSUFFICIENT_TIER",
  "message": "Rhino Plugin Access Denied",
  "details": "Your current plan (FREE) does not include Rhino plugin access.",
  "userMessage": "Your Rhino plugin access requires a Standard, Pro, or Educational plan.\n\nUpgrade your plan at: https://vesselstudio.io/settings?tab=billing",
  "requiresUpgrade": true,
  "currentPlan": "free",
  "basePlan": "free",          // NEW
  "trialPlan": null,           // NEW
  "trialExpiresAt": null,      // NEW
  "requiredPlans": ["standard", "pro", "educational"],
  "upgradeUrl": "https://vesselstudio.io/settings?tab=billing"
}
```

### Updated C# Error Handling

```csharp
public async Task<ValidationResult> ValidateAccess(string apiKey) {
    try {
        var response = await _httpClient.PostAsync("/api/rhino/validate", null);
        
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden) {
            var error = await response.Content.ReadAsAsync<ErrorResponse>();
            
            // Check error type
            if (error.Error == "API_ACCESS_SUSPENDED") {
                return ValidationResult.Suspended(error.UserMessage, error.UpgradeUrl);
            } else if (error.Error == "INSUFFICIENT_TIER") {
                return ValidationResult.InsufficientTier(error.UserMessage, error.UpgradeUrl);
            }
        }
        
        // ... handle other cases
    } catch (Exception ex) {
        return ValidationResult.Error(ex.Message);
    }
}
```

---

## 📊 Decision Matrix for Plugin Developers

### Should You Update Your Code?

| Scenario | Action Required | Priority |
|----------|-----------------|----------|
| Plugin currently works | ✅ No changes needed | Low |
| Want to show trial status in UI | ⚠️ Add `effectiveTier`, `hasTrialActive` fields | Medium |
| Want trial expiration warnings | ⚠️ Add `trialExpiresAt` logic | Medium |
| Want detailed tier display | ⚠️ Show both base & trial tiers | Low |
| Building new plugin version | ✅ Use v1.1 response model | High |

---

## 🧪 Testing Guide

### Test Case 1: User on Pro Trial (Access Granted)
```powershell
# Setup: User has trialTier = "pro", subscriptionTier = "free"
POST https://vesselstudio.io/api/rhino/validate
Authorization: Bearer vsk_live_USER_WITH_TRIAL

# Expected Response:
{
  "success": true,
  "user": {
    "subscriptionTier": "free",
    "effectiveTier": "pro",
    "trialTier": "pro",
    "hasTrialActive": true
  }
}

# Plugin Behavior: ✅ Allow full access
```

### Test Case 2: User on Free Tier (Access Denied)
```powershell
# Setup: User has subscriptionTier = "free", trialTier = null
POST https://vesselstudio.io/api/rhino/validate
Authorization: Bearer vsk_live_FREE_USER

# Expected Response (403):
{
  "success": false,
  "error": "INSUFFICIENT_TIER",
  "currentPlan": "free",
  "trialPlan": null
}

# Plugin Behavior: ❌ Show upgrade dialog
```

### Test Case 3: Trial Expired Yesterday (Access Denied)
```powershell
# Setup: trialTier was "pro", trialExpiresAt was yesterday
POST https://vesselstudio.io/api/rhino/validate
Authorization: Bearer vsk_live_EXPIRED_TRIAL

# Expected Response (403):
{
  "success": false,
  "error": "INSUFFICIENT_TIER",
  "currentPlan": "free",
  "trialPlan": null,  # Server auto-cleared expired trial
  "trialExpiresAt": null
}

# Plugin Behavior: ❌ Show "trial expired" message + upgrade link
```

---

## 📝 Migration Checklist

### Phase 1: Minimal (No Code Changes) ✅
- [ ] Test current plugin version with trial users
- [ ] Verify existing code works with v1.1 responses
- [ ] Confirm error handling works for both error types

### Phase 2: Enhanced UI (Optional)
- [ ] Update C# models to include new fields
- [ ] Show trial status badge in plugin UI
- [ ] Display trial expiration date
- [ ] Add "Upgrade" button for free/expired users

### Phase 3: Proactive Warnings (Optional)
- [ ] Implement trial expiration countdown
- [ ] Show warnings 7 days before expiration
- [ ] Show critical alerts 3 days before expiration
- [ ] Cache validation response locally (avoid excessive API calls)

---

## 🔗 Related Documentation

- **API Quick Reference**: `docs/RHINO_PLUGIN_QUICK_REFERENCE.md`
- **Complete API Guide**: `docs/RHINO_PLUGIN_API_GUIDE.md`
- **Plugin Implementation**: `docs/RHINO_PLUGIN_IMPLEMENTATION.md`
- **Trial System Docs**: `docs/features/PRO_TRIAL_SYSTEM.md`

---

## 💡 Recommendations

### For Existing Plugins (Already in Production)
1. **Do nothing immediately** - Your plugin will continue working
2. **Plan UI update** - Add trial status display in next release
3. **Test with trial users** - Verify access is granted correctly

### For New Plugin Development
1. **Use v1.1 response model** - Include all new fields from day 1
2. **Show effective tier** - Use `effectiveTier` instead of `subscriptionTier` in UI
3. **Implement expiration warnings** - Proactive UX improves retention

### Best Practice: Cache Validation Response
```csharp
// Cache validation result for 1 hour
private static DateTime _lastValidation = DateTime.MinValue;
private static UserInfo _cachedUser = null;

public async Task<UserInfo> GetUserInfo() {
    if ((DateTime.UtcNow - _lastValidation).TotalHours < 1 && _cachedUser != null) {
        return _cachedUser; // Return cached result
    }
    
    // Call API and cache result
    var result = await ValidateApiKey();
    if (result.Success) {
        _cachedUser = result.User;
        _lastValidation = DateTime.UtcNow;
    }
    
    return _cachedUser;
}
```

**Why?** Reduces unnecessary API calls, improves performance, respects rate limits.

---

## 🆘 Support & Questions

**Issues with migration?**
- Check Cloud Functions logs for server errors
- Verify API key format: `Authorization: Bearer vsk_live_...`
- Test with `/api/rhino/validate` endpoint directly (Postman/curl)

**Questions about trial tiers?**
- See `docs/features/PRO_TRIAL_SYSTEM.md` for complete trial system docs
- Trial tiers granted by admin via Users Manager (Admin Panel)
- Trials auto-expire via Cloud Function (runs daily 3 AM UTC)

**Contact:**
- GitHub Issues: https://github.com/vessel-one/yachtstudio/issues
- Email: support@vesselstudio.io

---

**Last Updated**: October 29, 2025  
**API Version**: 1.1  
**Status**: Production Ready ✅

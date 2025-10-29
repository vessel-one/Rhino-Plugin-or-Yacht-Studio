# Trial Account Implementation Plan

**Date**: October 29, 2025  
**Target API Version**: 1.1  
**Implementation Type**: Phase 2 (Enhanced UI - Recommended)

---

## 📋 Implementation Overview

### Current State (v1.0)
- ✅ Validates API key via `POST /api/rhino/validate`
- ✅ Checks `subscriptionTier` against allowed tiers: `standard`, `pro`, `educational`
- ❌ **Does NOT support trial tiers** - users on trial are blocked
- ❌ Does not display trial status or expiration

### Target State (v1.1)
- ✅ Support trial tier access (`trialTier`, `effectiveTier`)
- ✅ Display trial status badge in UI
- ✅ Show trial expiration warnings (3 days, 7 days before)
- ✅ Differentiate between base subscription and active trial
- ✅ Handle new error type: `INSUFFICIENT_TIER`

---

## 🎯 Implementation Phases

### Phase 1: Update Data Models (Required)
**Files to modify:**
- `VesselStudioApiClient.cs` - Update `ValidationResult` class

**Changes:**
```csharp
public class ValidationResult
{
    // Existing fields
    public bool Success { get; set; }
    public string UserName { get; set; }
    public string UserEmail { get; set; }
    public string ErrorMessage { get; set; }
    public string ErrorDetails { get; set; }
    public bool HasValidSubscription { get; set; }
    public ApiErrorResponse SubscriptionError { get; set; }
    
    // NEW FIELDS - API v1.1
    public string SubscriptionTier { get; set; }      // Base tier (free, standard, pro)
    public string EffectiveTier { get; set; }         // Current active tier (trial OR base)
    public string TrialTier { get; set; }             // Trial plan if active
    public string TrialExpiresAt { get; set; }        // ISO timestamp
    public bool HasTrialActive { get; set; }          // Quick boolean check
}
```

---

### Phase 2: Update Validation Logic (Required)
**File:** `VesselStudioApiClient.cs` - `ValidateApiKeyAsync()` method

**Current Logic:**
```csharp
// Lines 110-114
var subscriptionTier = result.user?.subscriptionTier?.ToString()?.ToLower();
var allowedTiers = new[] { "standard", "pro", "educational" };
var hasValidSubscription = !string.IsNullOrEmpty(subscriptionTier) && 
                           Array.Exists(allowedTiers, tier => tier == subscriptionTier);
```

**New Logic:**
```csharp
// Extract all tier fields
var subscriptionTier = result.user?.subscriptionTier?.ToString()?.ToLower();
var effectiveTier = result.user?.effectiveTier?.ToString()?.ToLower();
var trialTier = result.user?.trialTier?.ToString()?.ToLower();
var hasTrialActive = result.user?.hasTrialActive ?? false;
var trialExpiresAt = result.user?.trialExpiresAt?.ToString();

// Use effectiveTier for access check (falls back to subscriptionTier if not present)
var activeTier = effectiveTier ?? subscriptionTier;
var allowedTiers = new[] { "standard", "pro", "educational" };
var hasValidSubscription = !string.IsNullOrEmpty(activeTier) && 
                           Array.Exists(allowedTiers, tier => tier == activeTier);
```

**Why this works:**
- `effectiveTier` = trial tier if active, otherwise base tier
- Backward compatible: if server doesn't send `effectiveTier`, falls back to `subscriptionTier`
- Trial users automatically granted access via `effectiveTier = "pro"` or `"standard"`

---

### Phase 3: Update Success Response Mapping (Required)
**File:** `VesselStudioApiClient.cs` - Lines 140-150

**Current Code:**
```csharp
// Valid subscription
return new ValidationResult
{
    Success = true,
    HasValidSubscription = true,
    UserName = result.user?.displayName?.ToString(),
    UserEmail = result.user?.email?.ToString(),
    ErrorMessage = null,
    ErrorDetails = null
};
```

**Updated Code:**
```csharp
// Valid subscription
return new ValidationResult
{
    Success = true,
    HasValidSubscription = true,
    UserName = result.user?.displayName?.ToString(),
    UserEmail = result.user?.email?.ToString(),
    ErrorMessage = null,
    ErrorDetails = null,
    
    // NEW: Trial tier fields
    SubscriptionTier = subscriptionTier,
    EffectiveTier = effectiveTier ?? subscriptionTier,
    TrialTier = trialTier,
    TrialExpiresAt = trialExpiresAt,
    HasTrialActive = hasTrialActive
};
```

---

### Phase 4: Update Error Response Mapping (Required)
**File:** `VesselStudioApiClient.cs` - Lines 120-138

**Current Code:**
```csharp
if (!hasValidSubscription)
{
    return new ValidationResult
    {
        Success = true,
        HasValidSubscription = false,
        UserName = result.user?.displayName?.ToString(),
        UserEmail = result.user?.email?.ToString(),
        ErrorMessage = "Subscription upgrade required",
        ErrorDetails = $"Your current plan ({subscriptionTier}) does not include...",
        SubscriptionError = new ApiErrorResponse { ... }
    };
}
```

**Updated Code:**
```csharp
if (!hasValidSubscription)
{
    // Determine user-friendly plan name
    var displayPlan = hasTrialActive 
        ? $"{trialTier?.ToUpper()} Trial (Expired)" 
        : subscriptionTier?.ToUpper() ?? "FREE";
    
    return new ValidationResult
    {
        Success = true,
        HasValidSubscription = false,
        UserName = result.user?.displayName?.ToString(),
        UserEmail = result.user?.email?.ToString(),
        ErrorMessage = "Subscription upgrade required",
        ErrorDetails = $"Your current plan ({displayPlan}) does not include Rhino plugin access.",
        
        // NEW: Include trial fields even in error case
        SubscriptionTier = subscriptionTier,
        EffectiveTier = effectiveTier ?? subscriptionTier,
        TrialTier = trialTier,
        TrialExpiresAt = trialExpiresAt,
        HasTrialActive = hasTrialActive,
        
        SubscriptionError = new ApiErrorResponse
        {
            Success = false,
            Error = "SUBSCRIPTION_INSUFFICIENT",
            UserMessage = $"Your {displayPlan} plan does not include Rhino plugin access.\n\nUpgrade to Standard, Pro, or Educational to use this plugin.",
            CurrentPlan = subscriptionTier,
            RequiredPlans = new[] { "Standard", "Pro", "Educational" },
            UpgradeUrl = "https://vesselstudio.io/settings?tab=billing"
        }
    };
}
```

---

### Phase 5: Update Toolbar UI (Recommended)
**File:** `VesselStudioToolbarPanel.cs` - `LoadProjectsAsync()` method

**Current Status Display (Lines 720-730):**
```csharp
else if (!string.IsNullOrEmpty(settings.LastProjectName))
{
    _statusLabel.Text = $"✓ Ready - {settings.LastProjectName}";
    _statusLabel.ForeColor = Color.FromArgb(76, 175, 80);
}
```

**Enhanced Status Display:**
```csharp
else if (!string.IsNullOrEmpty(settings.LastProjectName))
{
    // Check if user is on trial
    var validation = await apiClient.ValidateApiKeyAsync();
    
    if (validation.HasTrialActive)
    {
        var daysRemaining = GetDaysUntilExpiration(validation.TrialExpiresAt);
        _statusLabel.Text = $"🎁 {validation.EffectiveTier?.ToUpper()} Trial - {daysRemaining}d left";
        _statusLabel.ForeColor = daysRemaining <= 3 
            ? Color.FromArgb(255, 140, 0)  // Orange warning
            : Color.FromArgb(76, 175, 80);  // Green
    }
    else
    {
        _statusLabel.Text = $"✓ Ready - {settings.LastProjectName}";
        _statusLabel.ForeColor = Color.FromArgb(76, 175, 80);
    }
}
```

---

### Phase 6: Add Trial Expiration Helper (Recommended)
**File:** `VesselStudioToolbarPanel.cs` - Add new method

```csharp
/// <summary>
/// Calculate days until trial expires
/// </summary>
private int GetDaysUntilExpiration(string trialExpiresAt)
{
    if (string.IsNullOrEmpty(trialExpiresAt)) return 0;
    
    try
    {
        var expiresAt = DateTime.Parse(trialExpiresAt);
        var daysRemaining = (expiresAt - DateTime.UtcNow).Days;
        return Math.Max(0, daysRemaining);
    }
    catch
    {
        return 0;
    }
}
```

---

### Phase 7: Add Trial Warning Dialog (Optional)
**File:** New file `VesselStudioTrialWarningDialog.cs`

**Purpose:** Show proactive warning when trial is about to expire

```csharp
using System;
using System.Drawing;
using System.Windows.Forms;

namespace VesselStudioSimplePlugin
{
    public class VesselStudioTrialWarningDialog : Form
    {
        public VesselStudioTrialWarningDialog(int daysRemaining, string upgradeUrl)
        {
            Text = "Trial Expiring Soon";
            Size = new Size(450, 280);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            
            var warningIcon = new Label
            {
                Text = "⚠️",
                Font = new Font("Segoe UI", 36),
                Location = new Point(20, 20),
                Size = new Size(80, 80),
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(warningIcon);
            
            var titleLabel = new Label
            {
                Text = "Your Trial is Expiring Soon",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Location = new Point(110, 30),
                Size = new Size(310, 30)
            };
            Controls.Add(titleLabel);
            
            var messageLabel = new Label
            {
                Text = $"You have {daysRemaining} day{(daysRemaining != 1 ? "s" : "")} remaining on your Rhino plugin trial.\n\n" +
                       "Upgrade now to avoid interruption to your workflow.",
                Font = new Font("Segoe UI", 10),
                Location = new Point(110, 65),
                Size = new Size(310, 70)
            };
            Controls.Add(messageLabel);
            
            var upgradeButton = new Button
            {
                Text = "Upgrade Now",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(250, 180),
                Size = new Size(150, 40),
                BackColor = Color.FromArgb(64, 123, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            upgradeButton.Click += (s, e) => {
                System.Diagnostics.Process.Start(upgradeUrl);
                DialogResult = DialogResult.OK;
            };
            Controls.Add(upgradeButton);
            
            var laterButton = new Button
            {
                Text = "Remind Me Later",
                Location = new Point(110, 180),
                Size = new Size(130, 40),
                FlatStyle = FlatStyle.Standard
            };
            laterButton.Click += (s, e) => DialogResult = DialogResult.Cancel;
            Controls.Add(laterButton);
        }
    }
}
```

---

## 🧪 Testing Checklist

### Test Case 1: User on Active Pro Trial
**Setup:**
- User has `trialTier = "pro"`, `subscriptionTier = "free"`
- `trialExpiresAt` is 10 days from now

**Expected Behavior:**
- ✅ Validation succeeds (`Success = true`, `HasValidSubscription = true`)
- ✅ `effectiveTier = "pro"` used for access check
- ✅ Projects load successfully
- ✅ Status shows: "🎁 PRO Trial - 10d left" (green)

**Test Command:**
```csharp
var result = await apiClient.ValidateApiKeyAsync();
Assert.IsTrue(result.Success);
Assert.IsTrue(result.HasValidSubscription);
Assert.AreEqual("pro", result.EffectiveTier);
Assert.IsTrue(result.HasTrialActive);
```

---

### Test Case 2: Trial Expiring in 2 Days
**Setup:**
- User has active trial, `trialExpiresAt` is 2 days from now

**Expected Behavior:**
- ✅ Validation succeeds (trial still active)
- ⚠️ Status shows: "🎁 PRO Trial - 2d left" (orange warning color)
- ⚠️ Show trial warning dialog on plugin load

**Test Command:**
```csharp
var result = await apiClient.ValidateApiKeyAsync();
var daysRemaining = GetDaysUntilExpiration(result.TrialExpiresAt);
Assert.AreEqual(2, daysRemaining);
// Should trigger warning dialog
```

---

### Test Case 3: Trial Expired Yesterday
**Setup:**
- User had trial, `trialExpiresAt` was yesterday
- Server auto-cleared trial (`trialTier = null`, `effectiveTier = "free"`)

**Expected Behavior:**
- ❌ Validation fails (`Success = true`, `HasValidSubscription = false`)
- ❌ Error message: "Your FREE plan does not include Rhino plugin access"
- ❌ Projects dropdown disabled
- ❌ Show upgrade dialog

**Test Command:**
```csharp
var result = await apiClient.ValidateApiKeyAsync();
Assert.IsTrue(result.Success); // API key valid
Assert.IsFalse(result.HasValidSubscription); // But tier insufficient
Assert.IsFalse(result.HasTrialActive);
```

---

### Test Case 4: Standard Subscriber (No Trial)
**Setup:**
- User has `subscriptionTier = "standard"`, no trial

**Expected Behavior:**
- ✅ Validation succeeds
- ✅ `effectiveTier = "standard"` (same as base)
- ✅ Status shows: "✓ Ready - Project Name" (green)
- ✅ No trial badge or warnings

---

## 📂 Files to Modify

### Required Changes (Phase 1-4)
1. **`VesselStudioApiClient.cs`**
   - Update `ValidationResult` class (add 5 new fields)
   - Update `ValidateApiKeyAsync()` method (extract new fields, use `effectiveTier`)
   - Update success/error response mapping

### Recommended Changes (Phase 5-6)
2. **`VesselStudioToolbarPanel.cs`**
   - Update `UpdateStatus()` to show trial status
   - Add `GetDaysUntilExpiration()` helper method
   - Show trial expiration warnings

### Optional Changes (Phase 7)
3. **`VesselStudioTrialWarningDialog.cs`** (new file)
   - Create warning dialog for expiring trials
   - Add to `.csproj` file

---

## ⏱️ Estimated Implementation Time

| Phase | Task | Time | Priority |
|-------|------|------|----------|
| 1 | Update ValidationResult model | 5 min | Required |
| 2 | Update validation logic | 10 min | Required |
| 3 | Update success response | 5 min | Required |
| 4 | Update error response | 10 min | Required |
| 5 | Update toolbar UI | 15 min | Recommended |
| 6 | Add expiration helper | 5 min | Recommended |
| 7 | Create warning dialog | 30 min | Optional |
| **Total** | | **1-1.5 hours** | |

---

## 🚀 Deployment Plan

### Step 1: Implement & Test Locally
```powershell
# Make code changes (Phases 1-6)
.\dev-build.ps1 -install

# Test in Rhino with trial user
# Verify access granted, trial status shown
```

### Step 2: Update Version & Changelog
```powershell
# Update CHANGELOG.md
## [1.5.0] - 2025-10-29
### Added
- Trial tier support (Pro Trial, Standard Trial)
- Trial status badge in toolbar
- Trial expiration warnings (3/7 days before)

# Bump version
.\update-version.ps1 -NewVersion 1.5.0
```

### Step 3: Release
```powershell
# Build, package, and publish
.\release.ps1
```

---

## 🔍 Backward Compatibility

**Is this breaking?** ❌ **NO - Fully backward compatible**

**Why?**
1. All new fields are **additive** (no fields removed)
2. Existing `subscriptionTier` field still present
3. Fallback logic: `effectiveTier ?? subscriptionTier`
4. Old plugins (v1.0) will continue working (won't see trial fields, but base tier still works)

**Migration path:**
- Users on old plugin version: ✅ Continue working (except trial users blocked)
- Users on new plugin version: ✅ Trial support enabled

---

## 📝 Next Steps

1. ✅ Review this plan
2. 🔨 Implement Phases 1-4 (required changes)
3. 🎨 Implement Phases 5-6 (recommended UI improvements)
4. 🧪 Test with trial users (or mock trial responses)
5. 📦 Release as v1.5.0
6. 📢 Notify users of trial tier support

---

**Ready to start implementation?** 
Begin with Phase 1 (update ValidationResult model).

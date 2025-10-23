# Subscription Lock Implementation Plan

**Feature**: Lock Rhino plugin features when user doesn't have required subscription plan

**Date**: 2025-10-23  
**Version**: 1.4.0 (planned)

---

## Overview

Implement subscription-based access control in the Rhino plugin. Users on Free/Starter plans should see locked UI with upgrade prompts. Only Standard/Pro/Educational users can use capture features.

---

## API Contract (Already Implemented)

The backend API returns `403 Forbidden` when subscription is insufficient:

```json
{
  "success": false,
  "error": "API_ACCESS_SUSPENDED",
  "message": "Rhino Plugin Access Suspended",
  "details": "Your API key has been suspended due to a plan change. You are currently on the FREE plan.",
  "userMessage": "Your Rhino plugin access requires a Standard, Pro, or Educational subscription.\n\nUpgrade your plan at: https://vesselstudio.io/settings?tab=billing",
  "requiresUpgrade": true,
  "currentPlan": "free",
  "requiredPlans": ["standard", "pro", "educational"],
  "upgradeUrl": "https://vesselstudio.io/settings?tab=billing"
}
```

---

## Implementation Plan

### Phase 1: Data Models & API Response Handling

#### 1.1 Create Subscription Response Model

**File**: `VesselStudioSimplePlugin/VesselStudioApiClient.cs`

```csharp
/// <summary>
/// API error response for subscription issues
/// </summary>
public class ApiErrorResponse
{
    public bool Success { get; set; }
    public string Error { get; set; }
    public string Message { get; set; }
    public string Details { get; set; }
    public string UserMessage { get; set; }
    public bool RequiresUpgrade { get; set; }
    public string CurrentPlan { get; set; }
    public string[] RequiredPlans { get; set; }
    public string UpgradeUrl { get; set; }
}

/// <summary>
/// Validation result with subscription status
/// </summary>
public class ValidationResult
{
    public bool Success { get; set; }
    public bool HasValidSubscription { get; set; }
    public string UserEmail { get; set; }
    public string UserName { get; set; }
    public ApiErrorResponse SubscriptionError { get; set; }
}
```

#### 1.2 Update ValidateApiKey Method

**File**: `VesselStudioSimplePlugin/VesselStudioApiClient.cs`

```csharp
/// <summary>
/// Validates API key and checks subscription status
/// Returns ValidationResult with subscription details
/// </summary>
public async Task<ValidationResult> ValidateApiKeyAsync()
{
    if (!IsAuthenticated)
    {
        return new ValidationResult 
        { 
            Success = false,
            HasValidSubscription = false
        };
    }

    try
    {
        var response = await _httpClient.PostAsync("/api/rhino/validate", null);
        var responseText = await response.Content.ReadAsStringAsync();
        
        // 403 = Subscription issue
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            var errorData = JsonConvert.DeserializeObject<ApiErrorResponse>(responseText);
            
            if (errorData?.Error == "API_ACCESS_SUSPENDED")
            {
                return new ValidationResult
                {
                    Success = false,
                    HasValidSubscription = false,
                    SubscriptionError = errorData
                };
            }
        }
        
        // 200 = Success
        if (response.IsSuccessStatusCode)
        {
            var result = JsonConvert.DeserializeObject<dynamic>(responseText);
            return new ValidationResult
            {
                Success = true,
                HasValidSubscription = true,
                UserEmail = result.user?.email?.ToString(),
                UserName = result.user?.displayName?.ToString()
            };
        }
        
        // Other errors (401, etc)
        return new ValidationResult 
        { 
            Success = false,
            HasValidSubscription = false
        };
    }
    catch (Exception ex)
    {
        RhinoApp.WriteLine($"Validation error: {ex.Message}");
        return new ValidationResult 
        { 
            Success = false,
            HasValidSubscription = false
        };
    }
}
```

---

### Phase 2: Settings Storage

#### 2.1 Add Subscription State to Settings

**File**: `VesselStudioSimplePlugin/VesselStudioSettings.cs`

```csharp
public class VesselStudioSettings
{
    // Existing properties...
    public string ApiKey { get; set; }
    public string LastProjectId { get; set; }
    public string LastProjectName { get; set; }
    
    // NEW: Subscription status (cached)
    public bool HasValidSubscription { get; set; } = true; // Default to true for backwards compatibility
    public DateTime LastSubscriptionCheck { get; set; }
    public string SubscriptionErrorMessage { get; set; }
    public string UpgradeUrl { get; set; }
    
    // Cache subscription check for 1 hour
    public bool ShouldRecheckSubscription()
    {
        return (DateTime.Now - LastSubscriptionCheck).TotalHours > 1;
    }
}
```

---

### Phase 3: UI Locking

#### 3.1 Update Toolbar Panel with Locked State

**File**: `VesselStudioSimplePlugin/VesselStudioToolbarPanel.cs`

Add locked state UI:

```csharp
private Label _subscriptionWarningLabel;
private Button _upgradeButton;

private void UpdateStatus()
{
    var settings = VesselStudioSettings.Load();
    
    // Check subscription status
    if (!string.IsNullOrEmpty(settings.ApiKey) && !settings.HasValidSubscription)
    {
        ShowLockedState(settings);
        return;
    }
    
    // Existing status logic...
    if (string.IsNullOrEmpty(settings.ApiKey))
    {
        _statusPanel.BackColor = Color.FromArgb(255, 245, 245);
        _statusLabel.Text = "⚠ API key not configured";
        _statusLabel.ForeColor = Color.FromArgb(220, 38, 38);
        _captureButton.Enabled = false;
        _projectComboBox.Enabled = false;
    }
    else
    {
        _statusPanel.BackColor = Color.FromArgb(240, 253, 244);
        _statusLabel.Text = "✓ API key configured";
        _statusLabel.ForeColor = Color.FromArgb(22, 163, 74);
        _captureButton.Enabled = true;
        _projectComboBox.Enabled = true;
    }
}

private void ShowLockedState(VesselStudioSettings settings)
{
    // Hide normal controls
    _captureButton.Visible = false;
    _projectComboBox.Visible = false;
    _projectLabel.Visible = false;
    _refreshProjectsButton.Visible = false;
    
    // Show subscription warning
    if (_subscriptionWarningLabel == null)
    {
        _subscriptionWarningLabel = new Label
        {
            Location = new Point(10, 100),
            Size = new Size(260, 100),
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(220, 38, 38),
            Text = settings.SubscriptionErrorMessage ?? 
                   "Your Rhino plugin access requires a Standard, Pro, or Educational subscription.",
            TextAlign = ContentAlignment.TopLeft
        };
        this.Controls.Add(_subscriptionWarningLabel);
    }
    
    if (_upgradeButton == null)
    {
        _upgradeButton = CreateButton("🔓 Upgrade Plan", 10, 210, OnUpgradeClick);
        _upgradeButton.BackColor = Color.FromArgb(34, 197, 94);
        _upgradeButton.ForeColor = Color.White;
        this.Controls.Add(_upgradeButton);
    }
    
    _subscriptionWarningLabel.Visible = true;
    _upgradeButton.Visible = true;
    
    // Update status panel
    _statusPanel.BackColor = Color.FromArgb(254, 252, 232);
    _statusLabel.Text = "🔒 Subscription Required";
    _statusLabel.ForeColor = Color.FromArgb(234, 179, 8);
}

private void OnUpgradeClick(object sender, EventArgs e)
{
    var settings = VesselStudioSettings.Load();
    var upgradeUrl = settings.UpgradeUrl ?? "https://vesselstudio.io/settings?tab=billing";
    
    try
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = upgradeUrl,
            UseShellExecute = true
        });
        
        RhinoApp.WriteLine($"Opening upgrade page: {upgradeUrl}");
    }
    catch (Exception ex)
    {
        RhinoApp.WriteLine($"Failed to open browser: {ex.Message}");
    }
}
```

#### 3.2 Lock Capture Commands

**File**: `VesselStudioSimplePlugin/VesselCaptureCommand.cs`

```csharp
protected override Result RunCommand(RhinoDoc doc, RunMode mode)
{
    // Load settings
    var settings = VesselStudioSettings.Load();
    
    if (string.IsNullOrEmpty(settings.ApiKey))
    {
        RhinoApp.WriteLine("❌ API key not set. Run VesselSetApiKey first.");
        return Result.Failure;
    }
    
    // NEW: Check subscription status
    if (!settings.HasValidSubscription)
    {
        RhinoApp.WriteLine("🔒 Subscription Required");
        RhinoApp.WriteLine(settings.SubscriptionErrorMessage ?? 
            "Your Rhino plugin access requires a Standard, Pro, or Educational subscription.");
        RhinoApp.WriteLine($"Upgrade at: {settings.UpgradeUrl ?? "https://vesselstudio.io/settings?tab=billing"}");
        return Result.Failure;
    }
    
    // Existing capture logic...
}
```

---

### Phase 4: Subscription Validation Flow

#### 4.1 Update API Key Validation Command

**File**: `VesselStudioSimplePlugin/VesselSetApiKeyCommand.cs`

```csharp
protected override Result RunCommand(RhinoDoc doc, RunMode mode)
{
    // ... existing API key input ...
    
    RhinoApp.WriteLine("Validating API key and subscription...");
    
    var apiClient = new VesselStudioApiClient();
    apiClient.SetApiKey(apiKey);
    
    var validationTask = apiClient.ValidateApiKeyAsync();
    validationTask.Wait();
    var validation = validationTask.Result;
    
    if (!validation.Success)
    {
        if (validation.SubscriptionError != null)
        {
            // Subscription issue - save but mark as locked
            settings.ApiKey = apiKey;
            settings.HasValidSubscription = false;
            settings.LastSubscriptionCheck = DateTime.Now;
            settings.SubscriptionErrorMessage = validation.SubscriptionError.UserMessage;
            settings.UpgradeUrl = validation.SubscriptionError.UpgradeUrl;
            settings.Save();
            
            RhinoApp.WriteLine("🔒 API key saved but subscription is required");
            RhinoApp.WriteLine(validation.SubscriptionError.UserMessage);
            RhinoApp.WriteLine($"Current plan: {validation.SubscriptionError.CurrentPlan}");
            RhinoApp.WriteLine($"Required plans: {string.Join(", ", validation.SubscriptionError.RequiredPlans)}");
            return Result.Success; // Key is valid, just locked
        }
        else
        {
            RhinoApp.WriteLine("❌ Invalid API key");
            return Result.Failure;
        }
    }
    
    // Success - save with valid subscription
    settings.ApiKey = apiKey;
    settings.HasValidSubscription = true;
    settings.LastSubscriptionCheck = DateTime.Now;
    settings.SubscriptionErrorMessage = null;
    settings.UpgradeUrl = null;
    settings.Save();
    
    RhinoApp.WriteLine($"✅ API key validated successfully");
    RhinoApp.WriteLine($"User: {validation.UserName} ({validation.UserEmail})");
    return Result.Success;
}
```

#### 4.2 Background Subscription Recheck

**File**: `VesselStudioSimplePlugin/VesselStudioToolbarPanel.cs`

Add periodic recheck when toolbar is shown:

```csharp
private async void LoadProjectsAsync()
{
    var settings = VesselStudioSettings.Load();
    
    if (string.IsNullOrEmpty(settings?.ApiKey))
    {
        // ... existing no-API-key logic ...
        return;
    }
    
    // NEW: Recheck subscription every hour
    if (settings.ShouldRecheckSubscription())
    {
        await RecheckSubscriptionAsync(settings);
    }
    
    // If locked, don't load projects
    if (!settings.HasValidSubscription)
    {
        UpdateStatus();
        return;
    }
    
    // ... existing project loading logic ...
}

private async Task RecheckSubscriptionAsync(VesselStudioSettings settings)
{
    try
    {
        var apiClient = new VesselStudioApiClient();
        apiClient.SetApiKey(settings.ApiKey);
        
        var validation = await apiClient.ValidateApiKeyAsync();
        
        // Update cached subscription status
        settings.HasValidSubscription = validation.HasValidSubscription;
        settings.LastSubscriptionCheck = DateTime.Now;
        
        if (!validation.HasValidSubscription && validation.SubscriptionError != null)
        {
            settings.SubscriptionErrorMessage = validation.SubscriptionError.UserMessage;
            settings.UpgradeUrl = validation.SubscriptionError.UpgradeUrl;
        }
        else
        {
            settings.SubscriptionErrorMessage = null;
            settings.UpgradeUrl = null;
        }
        
        settings.Save();
        
        if (InvokeRequired)
        {
            Invoke(new Action(() => UpdateStatus()));
        }
        else
        {
            UpdateStatus();
        }
    }
    catch (Exception ex)
    {
        RhinoApp.WriteLine($"Subscription recheck failed: {ex.Message}");
    }
}
```

---

### Phase 5: User Experience Enhancements

#### 5.1 Upgrade Success Detection

When user upgrades and returns to Rhino:

```csharp
private Button _checkAgainButton;

// Add to locked state UI
_checkAgainButton = CreateButton("↻ Check Again", 10, 255, OnCheckAgainClick);
this.Controls.Add(_checkAgainButton);

private async void OnCheckAgainClick(object sender, EventArgs e)
{
    _checkAgainButton.Enabled = false;
    _checkAgainButton.Text = "⏳ Checking...";
    
    var settings = VesselStudioSettings.Load();
    await RecheckSubscriptionAsync(settings);
    
    _checkAgainButton.Text = "↻ Check Again";
    _checkAgainButton.Enabled = true;
    
    // If now unlocked, reload projects
    if (settings.HasValidSubscription)
    {
        RhinoApp.WriteLine("✅ Subscription activated! Plugin unlocked.");
        LoadProjectsAsync();
    }
    else
    {
        RhinoApp.WriteLine("Still requires subscription upgrade.");
    }
}
```

#### 5.2 About Dialog Subscription Status

**File**: `VesselStudioSimplePlugin/VesselStudioAboutDialog.cs`

Add subscription status to About dialog:

```csharp
var settings = VesselStudioSettings.Load();

var statusLabel = new Label
{
    Text = settings.HasValidSubscription ? 
           "✓ Subscription Active" : 
           "🔒 Subscription Required",
    ForeColor = settings.HasValidSubscription ? 
                Color.FromArgb(22, 163, 74) : 
                Color.FromArgb(220, 38, 38),
    Font = new Font("Segoe UI", 9, FontStyle.Bold),
    Location = new Point(20, yPos),
    AutoSize = true
};
this.Controls.Add(statusLabel);
```

---

## Testing Plan

### Test Case 1: User with Valid Subscription
1. Set API key for Standard/Pro user
2. Verify toolbar shows unlocked state
3. Verify capture button enabled
4. Verify project dropdown populates
5. Verify capture uploads successfully

### Test Case 2: User with No Subscription (Free Plan)
1. Set API key for Free plan user
2. Verify API returns 403 with subscription error
3. Verify toolbar shows locked state
4. Verify capture button hidden/disabled
5. Verify upgrade button shown
6. Click upgrade button → Opens browser to billing page

### Test Case 3: Downgrade During Session
1. Start with Standard subscription (unlocked)
2. User downgrades to Free in web app
3. Wait 1 hour OR click refresh in toolbar
4. Verify plugin detects downgrade and locks
5. Verify warning message displayed

### Test Case 4: Upgrade During Session
1. Start with Free subscription (locked)
2. User upgrades to Standard in web app
3. Click "Check Again" button in toolbar
4. Verify plugin detects upgrade and unlocks
5. Verify projects load automatically

### Test Case 5: Offline/Network Error
1. Disconnect network
2. Try to validate API key
3. Verify graceful error handling
4. Verify cached subscription status used (doesn't lock if was previously valid)

---

## Migration Plan

### Version 1.3.0 → 1.4.0

**Breaking Changes**: None (backwards compatible)

**Default Behavior**: 
- If `HasValidSubscription` not present in settings → Default to `true`
- Avoids locking existing users during update

**First-Time Check**:
- On next API operation, validate subscription
- Cache result in settings

**User Communication**:
- Update CHANGELOG.md with subscription requirement note
- Add notice to plugin description in Package Manager

---

## Rollout Strategy

### Phase 1: Silent Deployment (Week 1)
- Deploy v1.4.0 with subscription checking
- Only log subscription status (don't lock yet)
- Monitor backend for 403 responses

### Phase 2: Warning Mode (Week 2)
- Show warning banner for users without subscription
- Don't block functionality yet
- Track how many users affected

### Phase 3: Enforcement (Week 3)
- Enable full locking for Free/Starter users
- Monitor support tickets
- Provide clear upgrade path

---

## Error Messages

### Console Output (Command Line)

```
🔒 Subscription Required
Your Rhino plugin access requires a Standard, Pro, or Educational subscription.

Current Plan: FREE
Required Plans: Standard, Pro, Educational

Upgrade your plan at: https://vesselstudio.io/settings?tab=billing
```

### Toolbar Panel (UI)

```
┌─────────────────────────────────────┐
│ 🔒 Subscription Required            │
│                                     │
│ Your Rhino plugin access requires   │
│ a Standard, Pro, or Educational     │
│ subscription.                       │
│                                     │
│ Your API key is preserved and will  │
│ automatically reactivate when you   │
│ upgrade.                            │
│                                     │
│ ┌──────────────────┐               │
│ │  🔓 Upgrade Plan  │               │
│ └──────────────────┘               │
│ ┌──────────────────┐               │
│ │  ↻ Check Again   │               │
│ └──────────────────┘               │
└─────────────────────────────────────┘
```

---

## Backend Requirements (Already Implemented ✓)

The backend API already supports all required functionality:

- ✅ Returns 403 for insufficient subscription
- ✅ Includes `error`, `currentPlan`, `requiredPlans`, `upgradeUrl`
- ✅ Preserves API keys on downgrade (suspended, not deleted)
- ✅ Auto-reactivates keys on upgrade

**No backend changes needed!**

---

## Files to Modify

1. **VesselStudioApiClient.cs**
   - Add `ApiErrorResponse` class
   - Add `ValidationResult` class
   - Update `ValidateApiKeyAsync()` to return `ValidationResult`

2. **VesselStudioSettings.cs**
   - Add `HasValidSubscription` property
   - Add `LastSubscriptionCheck` property
   - Add `SubscriptionErrorMessage` property
   - Add `UpgradeUrl` property
   - Add `ShouldRecheckSubscription()` method

3. **VesselStudioToolbarPanel.cs**
   - Add `_subscriptionWarningLabel`, `_upgradeButton`, `_checkAgainButton`
   - Add `ShowLockedState()` method
   - Add `OnUpgradeClick()` method
   - Add `OnCheckAgainClick()` method
   - Add `RecheckSubscriptionAsync()` method
   - Update `UpdateStatus()` to check subscription
   - Update `LoadProjectsAsync()` to recheck hourly

4. **VesselCaptureCommand.cs**
   - Add subscription check at start of `RunCommand()`
   - Show locked message if not subscribed

5. **VesselSetApiKeyCommand.cs**
   - Update validation to use `ValidateApiKeyAsync()`
   - Handle 403 response gracefully
   - Save subscription status to settings

6. **VesselStudioAboutDialog.cs** (optional)
   - Add subscription status indicator

---

## Timeline

- **Day 1**: Implement Phase 1 (Data Models)
- **Day 2**: Implement Phase 2 (Settings) + Phase 3 (UI Locking)
- **Day 3**: Implement Phase 4 (Validation Flow) + Phase 5 (UX)
- **Day 4**: Testing (all test cases)
- **Day 5**: Code review + bug fixes
- **Week 2**: Phased rollout (Silent → Warning → Enforcement)

---

## Success Criteria

1. ✅ Free users see locked state with clear upgrade path
2. ✅ Standard/Pro/Educational users experience no disruption
3. ✅ Downgrades detected within 1 hour
4. ✅ Upgrades detected when user clicks "Check Again"
5. ✅ No false positives (valid users locked incorrectly)
6. ✅ Support ticket volume < 5 per week
7. ✅ Conversion rate: 15%+ of locked users upgrade

---

## Future Enhancements

### v1.5.0+
- Show trial period countdown (if applicable)
- "Remind Me Later" button (delay lock for 7 days)
- In-app pricing comparison table
- Analytics: Track how many users see locked state vs upgrade

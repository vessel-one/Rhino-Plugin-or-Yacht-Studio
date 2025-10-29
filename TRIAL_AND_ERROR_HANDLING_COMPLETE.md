# Trial Support & Error Handling - COMPLETE ✅

**Date:** October 29, 2025  
**Build Status:** ✅ SUCCESS (0 errors, 0 warnings)  
**Version:** 1.5.0 (ready for release)

---

## Phase Summary

### Phase 1-4: API Integration ✅ COMPLETED
- Integrated API v1.1 trial tier support
- Extract: `effectiveTier`, `trialTier`, `trialExpiresAt`, `hasTrialActive`
- ValidationResult updated with 5 new trial fields
- Access check uses `effectiveTier ?? subscriptionTier`

### Phase 5-6: UI & Storage ✅ COMPLETED
- VesselStudioSettings: 4 new fields for trial tracking
- Helper methods: `GetDaysUntilExpiration()`, `ShowTrialExpiringWarning()`
- Trial data stored in JSON settings (avoid repeated API calls)
- Last warning timestamp tracked for rate limiting

### Phase 7: Warning Dialog ✅ COMPLETED
- Trial expiring warning modal shows when ≤3 days remaining
- 24-hour cooldown: User sees max once per day
- Beautiful layout with proper WinForms padding and styling
- "Upgrade Now" button opens billing page
- "Dismiss" button closes dialog

### Error Handling ✅ COMPLETED
- **401 Unauthorized**: Delete API key (true auth failure)
- **403 Forbidden**: Keep API key (subscription tier insufficient)
- **Invalid or expired API key**: Delete from memory
- Batch upload detects API key errors and stops immediately
- Batch upload detects subscription errors and preserves queue
- Capture command handles upload errors with proper messaging

---

## Key Improvements

### 1. API Key Preservation
**Before:** API key deleted on any 4xx error  
**After:** Only deleted on 401 or truly invalid key errors

**Benefit:** Users can upgrade plan and continue using same API key

### 2. Trial Warning Cooldown
**Before:** Modal could show multiple times in quick succession  
**After:** Max once per 24 hours using `LastTrialWarningShown` timestamp

**Benefit:** Reduces modal fatigue while keeping user informed

### 3. Error Type Detection
**Code Distinction:**
```csharp
// 401 = Auth failed - delete key
ex.Message.Contains("401 Unauthorized")

// 403 = Subscription insufficient - keep key
ex.Message.Contains("403 Forbidden")

// Both paths properly update settings
```

### 4. Dialog Layout & Styling
**Improvements:**
- Header panel with light orange background (255, 245, 230)
- Content panel with proper padding (25px)
- Button panel at bottom with light gray background (248, 249, 250)
- Warning icon: 48pt (larger, more visible)
- Text with proper line wrapping and alignment
- Consistent 40px button height for touch-friendly UI

### 5. Subscription vs Authentication Errors
**Subscription Error Flow:**
1. API returns 403 Forbidden + subscription error details
2. ValidationResult.Success = true (key is valid)
3. ValidationResult.HasValidSubscription = false
4. Settings NOT cleared, error message stored
5. User can upgrade without reconfiguring key

**Authentication Error Flow:**
1. API returns 401 Unauthorized or other auth error
2. ValidationResult.Success = false
3. API key deleted from settings
4. User must reconfigure with new key

---

## File Changes

### VesselStudioApiClient.cs
- ValidationResult: Added 5 trial fields
- ValidateApiKeyAsync: Extracts trial data from API v1.1 response
- Access check: Uses `effectiveTier ?? subscriptionTier`
- Returns Success = true even for subscription errors (key is valid)

### VesselStudioSettings.cs
- Added: `HasTrialActive`, `TrialTier`, `TrialExpiresAt`
- Added: `LastTrialWarningShown` (DateTime for rate limiting)

### VesselStudioToolbarPanel.cs
- LoadProjectsAsync: Checks for trial data, stores it, shows warning
- GetDaysUntilExpiration(): Calculates days until trial ends
- ShowTrialExpiringWarning(): Beautiful modal dialog
- Updated catch block: Distinguish 401 from 403 errors
- Trial cooldown: Check `LastTrialWarningShown == DateTime.MinValue` OR `>= 24h`

### VesselCaptureCommand.cs
- Updated upload error handling
- Detects "Invalid or expired API key" errors
- Clears API key if key becomes invalid during upload
- Logs guidance to reconfigure

### VesselSendBatchCommand.cs
- Updated error reporting
- Handles `ApiKeyInvalid` flag from batch upload
- Handles `SubscriptionInvalid` flag separately
- Proper messaging for auth vs subscription errors

### BatchUploadResult.cs
- Added: `ApiKeyInvalid` flag (tracks if key became invalid during upload)
- Added: `SubscriptionInvalid` flag (tracks tier error during batch)

### BatchUploadService.cs
- Detects "Invalid or expired API key" in upload responses
- Detects subscription/tier errors (Insufficient tier, subscription, upgrade)
- Sets appropriate flags when errors detected
- Stops batch upload immediately on auth/subscription errors

---

## Test Scenarios

### Scenario 1: Trial Account (Starter tier)
```
✅ API returns: trialTier="starter", hasTrialActive=true, trialExpiresAt="2025-11-28"
✅ Error: "Insufficient tier" (starter not in allowed list)
✅ API key: PRESERVED in settings
✅ Status: Shows red error with upgrade link
✅ Dialog: Not shown (no valid subscription, access denied)
```

### Scenario 2: Trial Expiring in 2 Days
```
✅ API returns: trialTier="standard", hasTrialActive=true, daysRemaining=2
✅ Dialog: SHOWN (first time)
✅ Cooldown: Max once per 24h until expires
✅ Button: "Upgrade Now" opens https://vesselstudio.io/settings?tab=billing
```

### Scenario 3: Upgrade to Standard Plan
```
✅ User upgrades in browser
✅ Next validation check: API returns effectiveTier="standard"
✅ Access: GRANTED (standard in allowed tiers)
✅ API key: STILL VALID (no reconfiguration needed)
✅ Projects: Load successfully
```

### Scenario 4: API Key Expires
```
✅ API returns: 401 Unauthorized + "Invalid or expired API key"
✅ API key: DELETED from settings
✅ Status: "API key not configured"
✅ User: Must run VesselSetApiKey to reconfigure
```

### Scenario 5: Batch Upload with Invalid Key
```
✅ Upload starts successfully
✅ Middle of batch: API returns "Invalid or expired API key"
✅ Batch: Stops immediately
✅ API key: CLEARED from settings
✅ Settings: SubscriptionErrorMessage updated
✅ User: Guided to reconfigure
```

---

## Commits in This Session

1. **Fix: Reorganize trial warning methods** - Resolved C# scope issue
2. **Fix: Preserve API key when subscription tier insufficient** - Don't delete on tier errors
3. **Improve error handling for API key and subscription errors** - Better error detection
4. **Improve trial warning dialog layout and padding** - Modern WinForms design
5. **Fix: Respect 24-hour cooldown for trial warning dialog** - Rate limiting
6. **Fix: Don't delete API key for 403 Forbidden errors** - Better auth vs subscription detection

---

## Ready for Release

✅ All trial features implemented  
✅ Error handling comprehensive  
✅ UI properly styled and responsive  
✅ Settings persistence working  
✅ Batch upload error handling improved  
✅ Build: 0 errors, 0 warnings  

**Next Step:** Create release v1.5.0 with these improvements

---

## Technical Details

### Why DateTime.MinValue Check?
```csharp
// LastTrialWarningShown defaults to DateTime.MinValue (0001-01-01)
// When never shown: DateTime.MinValue == DateTime.MinValue → true
// When shown recently: DateTime.Now - recent_time < 24h → false
// Avoids: "First time check always > 24h since MinValue"
```

### Why Separate 401 from 403?
```csharp
// 401 Unauthorized = "Who are you?" → Delete key, ask to login again
// 403 Forbidden = "I know you, but you don't have permission" → Keep key, ask to upgrade
// Both are 4xx errors, but very different user experiences
```

### Why Store Trial Data in Settings?
```
Pros:
- Avoid repeated API calls for trial expiration check
- Show local countdown without network
- Track warning rate limit without API
- Persist user preferences (dismiss count)

Cons:
- Data can be stale (cached for 1 hour)
- Solved by: Recheck if stale during LoadProjectsAsync
```

---

## Known Limitations

1. **Trial countdown is cached** - Updates every hour via LastSubscriptionCheck
2. **Warning shows max once per day** - By design, not configurable
3. **"Upgrade Now" opens browser** - No in-app upgrade option available
4. **Starter tier always insufficient** - API restriction, not plugin limitation

---

## Future Enhancements (Post v1.5.0)

- [ ] Store trial warning dismissal per-plan (allow re-showing if plan changes)
- [ ] Add "Learn More" link to trial FAQ
- [ ] Show upgrade options in dialog (Standard vs Pro pricing)
- [ ] Remember user choice ("Don't show again for this trial")
- [ ] Improved retry logic for transient network errors

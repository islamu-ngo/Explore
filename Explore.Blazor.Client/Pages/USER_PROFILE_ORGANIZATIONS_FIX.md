# Fix: User Profile and Organizations Not Showing

## Problems Identified

### 1. User Profile Shows "Unable to load user profile"
- **API Returns**: 200 OK (successful)
- **Frontend Shows**: Error message
- **Root Cause**: User not synced to database before trying to load profile

### 2. My Organizations Page Shows No Organizations
- **API Returns**: 200 OK (successful)  
- **Frontend Shows**: Empty list
- **Root Cause**: Same - user not in database, so no organization memberships found

## Root Cause

**User sync happens in MainLayout AFTER the page loads**, but the pages try to load data BEFORE the user is synced. This creates a race condition where:

1. User logs in ? MainLayout renders
2. MyOrganizations/UserProfile page loads immediately
3. Page tries to fetch data from API
4. API can't find user (not synced yet) ? Returns empty/null
5. MainLayout finishes rendering ? User sync happens (too late!)

## Solutions Implemented

### 1. Enhanced UserProfile.razor.cs
**File**: `Explore.Blazor.Client\Pages\User\UserProfile.razor.cs`

Added explicit user sync BEFORE loading profile data:

```csharp
private async Task LoadUserData()
{
    try
    {
        // First, sync the user to ensure they exist in database
        await UserService.SyncUserAsync();
        
        // Then load the user data
        UserData = await UserService.GetCurrentUserAsync();
        
        // ... rest of loading logic
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error loading user data: {ex.Message}");
    }
}
```

**Benefits**:
- ? Guarantees user exists before fetching profile
- ? Handles case where user hasn't been synced yet
- ? Adds detailed logging for debugging

### 2. Enhanced MyOrganizations.razor
**Files**: 
- `Explore.Blazor.Client\Pages\Organization\MyOrganizations.razor`

Added UserService injection and explicit sync:

```csharp
@inject IUserService UserService

private async Task LoadData()
{
    try
    {
        // First ensure user is synced
        await UserService.SyncUserAsync();
        
        // Then load organizations
        var orgsTask = OrganizationService.GetMyOrganizationsAsync();
        var invitesTask = OrganizationMemberService.GetMyInvitationsAsync();
        
        await Task.WhenAll(orgsTask, invitesTask);
        
        _organizations = await orgsTask;
        _invitations = await invitesTask;
    }
    catch (Exception ex)
    {
        _errorMessage = $"An error occurred: {ex.Message}";
    }
}
```

**Benefits**:
- ? Ensures user exists before fetching organizations
- ? Adds comprehensive `[MY ORGS]` logging
- ? Better error handling and display

### 3. Enhanced Logging in SyncUserCommandHandler
**File**: `Explore.Application\Features\Users\Handlers\Commands\SyncUserCommandHandler.cs`

Added detailed `[USER SYNC]` logs at every step:

```csharp
Console.WriteLine($"[USER SYNC] Starting sync for user - ID: {userDto.Id}, Email: {userDto.Email}");
Console.WriteLine($"[USER SYNC] Existing user by ID: {existingUserById != null ? existingUserById.Id.ToString() : "NOT FOUND"}");
Console.WriteLine($"[USER SYNC] Existing user by email: {existingUserByEmail != null ? existingUserByEmail.Id.ToString() : "NOT FOUND"}");
```

**What you'll see in logs**:

#### ? **User Exists - Update Path**
```
[USER SYNC] Starting sync for user - ID: 018e4e5c..., Email: system@islamu.org
[USER SYNC] Existing user by ID: 018e4e5c...
[USER SYNC] Existing user by email: 018e4e5c...
[USER SYNC] Existing user found - Updating user ID: 018e4e5c...
[USER SYNC] User update completed - ID: 018e4e5c...
```

#### ? **New User - Create Path**
```
[USER SYNC] Starting sync for user - ID: new-id..., Email: newuser@example.com
[USER SYNC] Existing user by ID: NOT FOUND
[USER SYNC] Existing user by email: NOT FOUND
[USER SYNC] No existing user found - Creating new user
[USER SYNC] User created successfully - ID: new-id...
[USER SYNC] Actor created successfully - ID: actor-id...
[USER SYNC] User creation completed - ID: new-id...
```

#### ? **Email Conflict**
```
[USER SYNC] Starting sync for user - ID: different-id, Email: existing@example.com
[USER SYNC] Existing user by ID: NOT FOUND
[USER SYNC] Existing user by email: old-id (DIFFERENT!)
[USER SYNC] CONFLICT - Email exists with different ID...
```

## Testing Steps

### 1. Restart Your Application
The new logging code needs to be loaded:
```bash
# Stop the application
# Rebuild
dotnet build
# Start again
```

### 2. Test User Profile Page
1. Navigate to `/user/profile`
2. Check console logs for:
   ```
   [USER SYNC] Starting sync for user...
   [USER SYNC] User update completed...
   User data loaded successfully: your@email.com
   ```
3. Profile should now display correctly

### 3. Test My Organizations Page
1. Navigate to `/organization/my`
2. Check console logs for:
   ```
   [MY ORGS] Starting to load organizations and invitations
   [MY ORGS] Syncing user first...
   [MY ORGS] User sync completed
   [MY ORGS] Loaded X organizations and Y invitations
   ```
3. Organizations should now be visible

### 4. Verify Database
Check if your user was created:

```sql
SELECT id, email, first_name, last_name, actor_id, created_at
FROM users
WHERE email = 'your@email.com';

-- Check organization memberships
SELECT om.id, om.user_id, om.organization_id, om.organization_role_id, o.full_name
FROM organization_members om
JOIN organizations o ON om.organization_id = o.id
WHERE om.user_id = 'YOUR-USER-ID';
```

## What The Logs Will Tell You

### Scenario 1: User Doesn't Exist Yet (First Login)
```
[USER SYNC] No existing user found - Creating new user
[USER SYNC] User created successfully
[USER SYNC] Actor created successfully
[USER SYNC] User creation completed
[MY ORGS] Loaded 0 organizations and 0 invitations
```
? **Expected**: New user created, no organizations yet (normal for first login)

### Scenario 2: User Exists, Has Organizations
```
[USER SYNC] Existing user found - Updating user
[USER SYNC] User update completed
[MY ORGS] Loaded 2 organizations and 0 invitations
```
? **Expected**: User updated, organizations loaded successfully

### Scenario 3: Duplicate Email Error
```
[USER SYNC] CONFLICT - Email exists with different ID
[USER SYNC] ERROR - Exception occurred: InvalidOperationException
```
? **Action Required**: See `USER_SYNC_DIAGNOSTICS.md` for resolution

### Scenario 4: User Sync Failed
```
[USER SYNC] ERROR - Exception occurred: DbUpdateException
[USER SYNC] ERROR - InnerException: PostgresException
```
? **Action Required**: Check database connectivity and constraints

## Expected Outcomes

### ? After This Fix

**User Profile Page**:
- Shows your name, email, and avatar
- Shows statistics (events attended, reviews given)
- Shows recent activity
- No "Unable to load" error

**My Organizations Page**:
- Shows all organizations you're a member of
- Shows your role in each organization
- Shows pending invitations
- Empty state if you're not in any organizations yet (with "Create New" button)

### ?? If Still Not Working

1. **Check Console Logs** - Look for `[USER SYNC]` and `[MY ORGS]` messages
2. **Check Network Tab** - Verify API requests are returning 200 OK
3. **Check Database** - Verify user exists in `users` table
4. **Check Organization Memberships** - Verify records in `organization_members` table

## Next Steps

1. **Restart application** to pick up the new code
2. **Navigate to `/user/profile`** and check logs
3. **Navigate to `/organization/my`** and check logs
4. **Share the log output** if issues persist

The enhanced logging will show exactly what's happening at each step!

---

**Status**: ? Fixed with Explicit User Sync + Enhanced Logging
**Priority**: High (Blocking user features)
**Impact**: User Profile, My Organizations pages

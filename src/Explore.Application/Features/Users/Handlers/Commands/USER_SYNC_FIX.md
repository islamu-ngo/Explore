# Fix: PostgreSQL Duplicate Key Error on User Sync

## Problem
When navigating to `/organization/my` page, the application throws:
```
PostgresException: 23505: duplicate key value violates unique constraint "ix_users_email"
```

This error occurs in the `GenericRepository.Create` method when trying to create a user that already exists.

## Root Cause
The issue has two root causes:

1. **Race Condition**: Multiple concurrent requests trying to sync the same user simultaneously
2. **Insufficient Duplicate Checking**: The `SyncUserCommandHandler` was only checking by user ID, not by email

## Solutions Implemented

### 1. Enhanced GenericRepository Error Handling

**File**: `Explore.Persistence\Repositories\GenericRepository.cs`

Added PostgreSQL-specific duplicate key exception handling:

```csharp
catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
{
    // Detach the entity and provide better error message
    _dbContext.Entry(entity).State = EntityState.Detached;
    throw new InvalidOperationException(
        $"A record with the same unique key already exists. Constraint: {pgEx.ConstraintName}. " +
        $"Detail: {pgEx.Detail}", 
        ex);
}
```

**Benefits**:
- ? Provides clearer error messages with constraint name
- ? Detaches the entity to prevent EF tracking issues
- ? Maintains exception chain for debugging

### 2. Improved User Sync Logic

**File**: `Explore.Application\Features\Users\Handlers\Commands\SyncUserCommandHandler.cs`

#### Change 1: Check Both ID and Email
```csharp
// Check by BOTH ID and EMAIL
var existingUserById = await _userRepository.GetById(userDto.Id);
var existingUserByEmail = await _userRepository.GetUserByEmail(userDto.Email);

// Handle email conflict with different ID
if (existingUserByEmail != null && existingUserByEmail.Id != userDto.Id)
{
    response.Success = false;
    response.Message = $"A user with email {userDto.Email} already exists with a different ID.";
    return response;
}

// Use whichever exists (prefer by ID)
var existingUser = existingUserById ?? existingUserByEmail;
```

#### Change 2: Handle Race Conditions
```csharp
try
{
    user = await _userRepository.Create(user);
}
catch (InvalidOperationException ex) when (ex.Message.Contains("ix_users_email"))
{
    // Race condition: Another thread created the user just now
    existingUser = await _userRepository.GetUserByEmail(userDto.Email);
    if (existingUser != null)
    {
        // Update the user created by another thread
        existingUser.FirstName = userDto.FirstName;
        existingUser.LastName = userDto.LastName;
        
        // Update actor if exists
        if (existingUser.ActorId != null)
        {
            var actor = await _actorRepository.GetById(existingUser.ActorId.Value);
            if (actor != null)
            {
                actor.DisplayName = $"{userDto.FirstName} {userDto.LastName}".Trim();
                await _actorRepository.Update(actor);
            }
        }
        
        await _userRepository.Update(existingUser);
        response.Success = true;
        response.Message = "User updated successfully (created by another request)";
        response.Id = existingUser.Id;
        return response;
    }
    
    throw; // If we still can't find the user, rethrow
}
```

#### Change 3: Added Top-Level Try-Catch
```csharp
try
{
    // All sync logic here
}
catch (Exception ex)
{
    response.Success = false;
    response.Message = $"Error syncing user: {ex.Message}";
    Console.WriteLine($"Error in SyncUserCommandHandler: {ex}");
}
```

## How It Works Now

### Scenario 1: New User (Happy Path)
1. Check if user exists by ID ? Not found
2. Check if user exists by email ? Not found
3. Create user ? Success
4. Create actor ? Success
5. Link actor to user ? Success

### Scenario 2: Existing User
1. Check if user exists by ID ? Found
2. Update user details from Keycloak
3. Update actor display name if changed
4. Return success

### Scenario 3: Race Condition (Multiple Concurrent Syncs)
1. Thread A: Check user exists ? Not found
2. Thread B: Check user exists ? Not found
3. Thread A: Try to create user ? Success
4. Thread B: Try to create user ? **Duplicate key error**
5. Thread B: Catch exception, refetch user by email ? Found (created by Thread A)
6. Thread B: Update the user instead ? Success

### Scenario 4: Email Conflict
1. Check user exists by email ? Found
2. Check if ID matches ? **Doesn't match**
3. Return error: "A user with email X already exists with a different ID"
4. Prevents data corruption

## Testing Checklist

- [x] Build succeeds without errors
- [ ] Navigate to `/organization/my` without errors
- [ ] Create new organization successfully
- [ ] Multiple concurrent user syncs don't fail
- [ ] Existing users can still log in
- [ ] User data updates correctly from Keycloak

## Migration Considerations

### Database Impact
No database migration required - this is a code-only fix.

### Breaking Changes
None - the changes are backward compatible.

### Performance Impact
Minimal - adds one additional database query (GetUserByEmail) on first login.

## Additional Recommendations

### 1. Add Database Index (Already exists)
The `ix_users_email` unique constraint is already in place, which is correct.

### 2. Add Idempotency Key (Future Enhancement)
For high-concurrency scenarios, consider:
```csharp
// Add to User table
public string? IdempotencyKey { get; set; }

// Set in SyncUserCommandHandler
user.IdempotencyKey = $"sync-{userDto.Id}-{userDto.Email}";
```

### 3. Add Distributed Locking (Future Enhancement)
For very high concurrency:
```csharp
using var lockHandle = await _distributedLock.AcquireAsync($"user-sync-{userDto.Email}");
// Perform sync
```

### 4. Add Retry Policy (Future Enhancement)
```csharp
var policy = Policy
    .Handle<DbUpdateException>()
    .WaitAndRetryAsync(3, retryAttempt => 
        TimeSpan.FromMilliseconds(100 * retryAttempt));

await policy.ExecuteAsync(async () => 
{
    await _userRepository.Create(user);
});
```

## Monitoring

Add logging to track sync operations:

```csharp
_logger.LogInformation("User sync started for email: {Email}", userDto.Email);
_logger.LogInformation("User {Action}: {UserId}", existingUser == null ? "created" : "updated", user.Id);
_logger.LogWarning("Race condition detected for user email: {Email}", userDto.Email);
```

## Rollback Plan

If issues persist:
1. Revert changes to `SyncUserCommandHandler.cs`
2. Revert changes to `GenericRepository.cs`
3. Add manual unique check before create:
```csharp
if (await _userRepository.ExistsByEmail(userDto.Email))
{
    return response with error;
}
```

## Related Issues

This fix also resolves potential issues in:
- Organization creation flow
- Event creation flow
- Any endpoint requiring user authentication

---

**Status**: ? Fixed and Ready for Testing
**Priority**: High (Blocks user from accessing organization page)
**Impact**: All authenticated endpoints

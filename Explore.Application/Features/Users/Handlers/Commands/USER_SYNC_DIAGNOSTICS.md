# User Sync Duplicate Email Error - Diagnostic Guide

## Problem Description

**Error**: `PostgresException: 23505: duplicate key value violates unique constraint "ix_users_email"`

**When**: User sync endpoint `POST /api/v1/User/sync` fails when there's already a user in the database with the same email.

## Root Cause Analysis

The error occurs because of **insufficient checking** before attempting to create a user. The system needs to handle two distinct scenarios:

### Scenario 1: Same User (? Should Update)
- **Keycloak User ID**: `abc-123-def`
- **Keycloak Email**: `user@example.com`
- **Database User ID**: `abc-123-def` (same)
- **Database Email**: `user@example.com` (same)
- **Expected Behavior**: UPDATE existing user
- **Current Behavior**: ? Works correctly (with recent fixes)

### Scenario 2: Different Users (? Should Error)
- **Keycloak User ID**: `xyz-789-ghi`
- **Keycloak Email**: `user@example.com`
- **Database User ID**: `abc-123-def` (different!)
- **Database Email**: `user@example.com` (same)
- **Expected Behavior**: Return error - email conflict
- **Current Behavior**: ? Correctly returns error message

### Scenario 3: Race Condition (? Should Retry)
- Two concurrent sync requests for the same new user
- Thread A creates user successfully
- Thread B tries to create ? Gets duplicate key error
- Thread B catches exception, fetches user, updates instead
- **Current Behavior**: ? Handles correctly

## How to Diagnose Your Specific Issue

With the enhanced logging added, when you run user sync, you'll see output like:

```
[USER SYNC] Starting sync for user - ID: abc-123-def, Email: user@example.com
[USER SYNC] Existing user by ID: abc-123-def
[USER SYNC] Existing user by email: abc-123-def
[USER SYNC] Existing user found - Updating user ID: abc-123-def
[USER SYNC] User update completed - ID: abc-123-def
```

### If You See Scenario 1 (Same User):
```
[USER SYNC] Existing user by ID: abc-123-def
[USER SYNC] Existing user by email: abc-123-def
```
? **This is CORRECT** - the user should be updated, not created

### If You See Scenario 2 (Email Conflict):
```
[USER SYNC] Existing user by ID: NOT FOUND
[USER SYNC] Existing user by email: xyz-789-ghi
[USER SYNC] CONFLICT - Email exists with different ID...
```
? **This is a DATA INTEGRITY ISSUE** - you have two different Keycloak accounts with the same email

### If You See Scenario 3 (Race Condition):
```
[USER SYNC] No existing user found - Creating new user
[USER SYNC] Race condition detected - User was created by another thread
[USER SYNC] Found user created by another thread - ID: abc-123-def
```
? **This is NORMAL** - handled gracefully

## Checking Your Database

Run this SQL to see what's in your database:

```sql
-- Check all users
SELECT id, email, first_name, last_name, auth_provider, auth_provider_id, actor_id
FROM users
ORDER BY email;

-- Find duplicate emails
SELECT email, COUNT(*) as count
FROM users
GROUP BY email
HAVING COUNT(*) > 1;

-- Check specific user
SELECT id, email, first_name, last_name, auth_provider, auth_provider_id
FROM users
WHERE email = 'YOUR_EMAIL_HERE';
```

## Current Logic Flow

```
START User Sync
  ?
Get Keycloak User Info (ID, Email, Name from JWT token)
  ?
Check DB for existing user by ID
  ?
Check DB for existing user by Email
  ?
???????????????????????????????????????????
? Does email exist with DIFFERENT ID?     ?
? (existingUserByEmail != null &&         ?
?  existingUserByEmail.Id != userDto.Id)  ?
???????????????????????????????????????????
  ? YES                              ? NO
  ?                                  ?
Return Error                    Continue
"Email conflict"                    ?
                              ???????????????????
                              ? User exists?    ?
                              ???????????????????
                              ? YES      ? NO
                              ?          ?
                         UPDATE     CREATE
                         User        ?
                         ?         Try Create
                                    ?
                              ??????????????????
                              ? Duplicate Key? ?
                              ??????????????????
                              ? YES      ? NO
                              ?          ?
                         Fetch &     Success
                         Update      ?
                         (Race 
                         Condition)
                         ?
```

## What To Do Based on Logs

### Log Shows: "Existing user found - Updating"
? **WORKING CORRECTLY**
- Your user exists in DB with same ID and email
- System is updating, not creating
- No duplicate key error should occur
- If error still occurs, check the UPDATE query

### Log Shows: "CONFLICT - Email exists with different ID"
? **DATA INTEGRITY ISSUE**
- You have TWO Keycloak accounts with the same email
- This is a configuration problem in Keycloak
- **Solution**: 
  1. Decide which Keycloak account is the correct one
  2. Merge or delete the other account in Keycloak
  3. OR: Manually update the database user record

### Log Shows: "No existing user found - Creating new user" followed by duplicate error
?? **POSSIBLE ISSUES**:

1. **Case Sensitivity**:
   - Email in DB: `User@Example.com`
   - Email from Keycloak: `user@example.com`
   - PostgreSQL is case-sensitive by default
   - **Fix**: Normalize emails to lowercase

2. **Whitespace**:
   - Email in DB: `user@example.com `
   - Email from Keycloak: `user@example.com`
   - **Fix**: Trim emails

3. **Race Condition** (should be handled, but check logs):
   - Should see: "Race condition detected"
   - If not, the exception isn't being caught properly

## Fixes Applied

### 1. Enhanced Duplicate Detection
? Check by BOTH ID and email before creating
? Detect email conflicts with different IDs
? Prefer existing user by ID over email

### 2. Race Condition Handling
? Catch `InvalidOperationException` when creating user
? Retry by fetching user created by concurrent thread
? Update instead of failing

### 3. Comprehensive Logging
? Log every step of the sync process
? Log user IDs and emails for debugging
? Log conflicts and race conditions
? Log all exceptions with full details

## Testing Your Fix

1. **Test Same User Sync** (should work):
   ```bash
   # Login with existing user
   # Call POST /api/v1/User/sync
   # Check logs for: "Existing user found - Updating"
   ```

2. **Test New User Sync** (should work):
   ```bash
   # Login with brand new Keycloak user
   # Call POST /api/v1/User/sync
   # Check logs for: "User created successfully"
   ```

3. **Test Concurrent Syncs** (should work):
   ```bash
   # Open two browser tabs
   # Login with same NEW user in both
   # Call sync simultaneously
   # One should create, other should detect race condition
   ```

4. **Test Email Conflict** (should error gracefully):
   ```bash
   # Manually create user in DB with email X and ID A
   # Login to Keycloak with different account (email X, ID B)
   # Call sync
   # Should see: "Email exists with different ID"
   ```

## Recommended Next Steps

1. **Run the application** with the enhanced logging
2. **Try to sync your user** that's causing the error
3. **Check the console logs** for the `[USER SYNC]` messages
4. **Based on what you see**:
   - If "Existing user found" ? Should work! (update path)
   - If "CONFLICT" ? Fix Keycloak duplicate emails
   - If "Creating new user" then error ? Check for case/whitespace issues

---

**With the enhanced logging, we can now precisely diagnose what's happening in your specific case.**

<!-- ABOUTME: Catalog of concrete Invariant-Breaker adversarial test recipes for Tiers 0, 1, and 2. -->
<!-- ABOUTME: Provides plug-and-play C# / TUnit test structures for tenant-jumping, concurrency races, replays, and PII leakage. -->

# Adversarial Test Archetypes Catalog

> **Audience**: AI Agents & Engineers working on Tier 0 (Sovereign), Tier 1 (Security), and Tier 2 (Privacy) tasks.
> **Principle**: Author failing Invariant-Breaker tests **before** writing implementation code (Red Phase).

---

## 1. Archetype: Cross-Tenant Isolation & Spoofing (Tier 1)

### Threat Vector
An authenticated actor attempts to read or mutate another tenant's entity by forging the `X-Tenant-Slug` header, guessing aggregate GUIDs, or bypassing query filters.

### Recipe (Integration Test / TUnit)
```csharp
[Test]
[DisplayName("Cross-tenant entity request must fail closed with 404 ProblemDetails")]
public async Task CrossTenant_Request_FailsClosed_WithNotFound()
{
    // 1. Arrange: Create resources belonging to Tenant A and Tenant B
    var tenantAClient = Factory.CreateAuthenticatedClient(tenantSlug: "tenant-a");
    var entityInTenantB = await SeedEntityInTenantAsync(tenantSlug: "tenant-b");

    // 2. Act: Tenant A attempts to access Tenant B's entity
    var response = await tenantAClient.GetAsync($"/api/v1/resources/{entityInTenantB.Id}");

    // 3. Assert: Fail-closed 404 (never 403 or 200) to prevent entity existence enumeration
    await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
    await Assert.That(problem?.Status).IsEqualTo(404);
}
```

---

## 2. Archetype: Concurrency Race & Inventory Hold Contention (Tier 0)

### Threat Vector
Two concurrent requests attempt to reserve the last available ticket/seat or simultaneously capture payment against an expiring reservation hold.

### Recipe (Concurrency Race Simulation)
```csharp
[Test]
[DisplayName("Simultaneous checkout on last available capacity allows exactly one capture")]
public async Task Concurrent_Reservations_AllowExactlyOneSuccess()
{
    // 1. Arrange: Capacity of exactly 1 seat remaining
    var eventId = await SeedEventWithCapacityAsync(remainingCapacity: 1);
    var clientA = Factory.CreateAuthenticatedClient(userId: Guid.NewGuid());
    var clientB = Factory.CreateAuthenticatedClient(userId: Guid.NewGuid());

    // 2. Act: Fire concurrent checkout requests simultaneously
    var taskA = clientA.PostAsJsonAsync($"/api/v1/events/{eventId}/registrations", new CreateRegistrationRequest());
    var taskB = clientB.PostAsJsonAsync($"/api/v1/events/{eventId}/registrations", new CreateRegistrationRequest());
    var responses = await Task.WhenAll(taskA, taskB);

    // 3. Assert: Exactly one 201 Created and one 409 Conflict / 422 Unprocessable
    var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
    var conflictCount = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict || r.StatusCode == HttpStatusCode.UnprocessableEntity);

    await Assert.That(successCount).IsEqualTo(1);
    await Assert.That(conflictCount).IsEqualTo(1);
}
```

---

## 3. Archetype: Replay & Idempotency Key Tampering (Tier 0 / Tier 1)

### Threat Vector
A webhook provider or client retries a request with the same `Idempotency-Key` but changes the payload (e.g. altering payment amount), or replays a successfully processed transaction.

### Recipe (Idempotency Replay & Tamper Test)
```csharp
[Test]
[DisplayName("Identical idempotency key with mutated payload must reject with 409 Conflict")]
public async Task IdempotencyKey_WithMutatedPayload_ReturnsConflict()
{
    var idempotencyKey = Guid.NewGuid().ToString();
    var originalRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders/checkout")
    {
        Headers = { { "Idempotency-Key", idempotencyKey } },
        Content = JsonContent.Create(new CheckoutCommand { AmountMinorUnits = 5000 })
    };

    // 1. First execution succeeds
    var firstResponse = await Client.SendAsync(originalRequest);
    await Assert.That(firstResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

    // 2. Replay with mutated payload
    var tamperedRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders/checkout")
    {
        Headers = { { "Idempotency-Key", idempotencyKey } },
        Content = JsonContent.Create(new CheckoutCommand { AmountMinorUnits = 10000 }) // Tampered amount
    };
    var tamperedResponse = await Client.SendAsync(tamperedRequest);

    // 3. Assert: Tampering rejected
    await Assert.That(tamperedResponse.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
}
```

---

## 4. Archetype: Invalid Aggregate State Transition (Tier 0 / Tier 3)

### Threat Vector
A client attempts an out-of-order state machine transition (e.g., transitioning an event from `Draft` $\rightarrow$ `Completed` or attempting a refund on an uncaptured order).

### Recipe (Domain Invariant Test)
```csharp
[Test]
[DisplayName("Transitioning Draft event directly to Completed throws DomainException")]
public async Task Event_DraftToCompleted_ThrowsDomainException()
{
    var draftEvent = Event.CreateDraft("Tech Summit", TenantId);

    // Act & Assert
    var exception = Assert.Throws<DomainException>(() => draftEvent.MarkCompleted());
    await Assert.That(exception.Message).Contains("Invalid state transition");
    await Assert.That(draftEvent.Status).IsEqualTo(EventStatus.Draft);
}
```

---

## 5. Archetype: Log Sink PII Scan & Redaction (Tier 2)

### Threat Vector
Sensitive user data (email, phone, address, tax identifiers) is passed directly to `ILogger` without passing through the privacy redaction gateway.

### Recipe (Telemetry / Log Capture Test)
```csharp
[Test]
[DisplayName("Handler execution logs must contain zero plaintext PII")]
public async Task HandlerExecution_EmitsZeroPlaintextPii_ToLogSink()
{
    var fakeLogSink = new TestMemoryLogSink();
    var handler = CreateHandlerWithLogSink(fakeLogSink);

    var sensitiveCommand = new UpdateUserProfileCommand
    {
        UserId = Guid.NewGuid(),
        Email = "user@example.com",
        PhoneNumber = "+32470123456"
    };

    await handler.Handle(sensitiveCommand, CancellationToken.None);

    var allLogs = fakeLogSink.GetAllLoggedMessages();
    await Assert.That(allLogs).DoesNotContain("user@example.com");
    await Assert.That(allLogs).DoesNotContain("+32470123456");
}
```

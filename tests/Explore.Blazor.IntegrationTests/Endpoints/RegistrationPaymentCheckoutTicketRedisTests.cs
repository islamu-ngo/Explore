// ABOUTME: Proves checkout-cookie protection and one-time nonce state survive across Split BFF instances.
// ABOUTME: Uses the production Redis Data Protection and atomic nonce paths against an isolated container.

using Explore.Blazor.Extensions;
using Explore.Blazor.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Redis;

namespace Explore.Blazor.IntegrationTests.Endpoints;

public sealed class RegistrationPaymentCheckoutTicketRedisTests
{
    [Test]
    [Category(BffTestCategories.Runtime)]
    [Explicit]
    public async Task RedisTicket_IssuesOnOneInstanceAndConsumesOnceAfterRestart()
    {
        await using var redis = new RedisBuilder("redis:7-alpine").Build();
        await redis.StartAsync();
        string protectedCookie;
        HttpRequest request = CreateRequest();

        await using (ServiceProvider first = CreateProvider(redis.GetConnectionString()))
        {
            RegistrationPaymentCheckoutTicketStore store = first.GetRequiredService<RegistrationPaymentCheckoutTicketStore>();
            RegistrationPaymentCheckoutTicketIssue issue = store.PrepareIssue(
                new Uri("https://checkout.stripe.com/c/pay/cs_redis_restart"),
                Guid.Parse("018e4e5c-7f00-7000-8000-000000000101"),
                Guid.Parse("018e4e5c-7f00-7000-8000-000000000201"),
                request,
                "acme",
                "checkout-session")!;
            await store.CommitIssueAsync(issue, CancellationToken.None);
            protectedCookie = issue.ProtectedCookie;
        }

        await using (ServiceProvider second = CreateProvider(redis.GetConnectionString()))
        {
            RegistrationPaymentCheckoutTicketStore store = second.GetRequiredService<RegistrationPaymentCheckoutTicketStore>();
            RegistrationPaymentCheckoutTicketValidation? ticket = store.Validate(protectedCookie, request, "acme", "checkout-session");

            await Assert.That(ticket).IsNotNull();
            await Assert.That((await store.PeekTargetAsync(ticket!, CancellationToken.None))?.AbsoluteUri)
                .IsEqualTo("https://checkout.stripe.com/c/pay/cs_redis_restart");
            await Assert.That((await store.ConsumeTargetAsync(ticket!, CancellationToken.None))?.AbsoluteUri)
                .IsEqualTo("https://checkout.stripe.com/c/pay/cs_redis_restart");
            await Assert.That(await store.ConsumeTargetAsync(ticket!, CancellationToken.None)).IsNull();
        }
    }

    private static ServiceProvider CreateProvider(string redisConnectionString)
    {
        var services = new ServiceCollection();
        services.AddBffDataProtection(redisConnectionString);
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(new RegistrationPaymentCheckoutTicketStoreOptions(RequiresRedis: true));
        services.AddSingleton<RegistrationPaymentCheckoutTicketStore>();
        return services.BuildServiceProvider();
    }

    private static HttpRequest CreateRequest()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("event.example");
        context.Request.PathBase = "/t/acme";
        context.Request.Headers.Cookie = ".AspNetCore.Antiforgery.test=session-cookie";
        return context.Request;
    }
}

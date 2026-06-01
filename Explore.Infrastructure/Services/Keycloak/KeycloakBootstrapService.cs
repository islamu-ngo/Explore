// ABOUTME: Implements setup-time Keycloak realm and client bootstrap through the Keycloak Admin API.
// ABOUTME: Keeps admin credentials and provider response bodies out of persisted state, logs, and result DTOs.

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Onboarding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Explore.Infrastructure.Services.Keycloak;

public sealed class KeycloakBootstrapService : IKeycloakBootstrapService
{
    public const string HttpClientName = "KeycloakBootstrapClient";

    private const string AdminCliClientId = "admin-cli";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<KeycloakBootstrapService> _logger;
    private readonly KeycloakBootstrapOptions _options;

    public KeycloakBootstrapService(
        IHttpClientFactory httpClientFactory,
        ILogger<KeycloakBootstrapService> logger)
        : this(httpClientFactory, logger, Options.Create(new KeycloakBootstrapOptions()))
    {
    }

    public KeycloakBootstrapService(
        IHttpClientFactory httpClientFactory,
        ILogger<KeycloakBootstrapService> logger,
        IOptions<KeycloakBootstrapOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<KeycloakBootstrapResultDto> BootstrapAsync(
        KeycloakBootstrapRequestDto request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryNormalizeBaseUri(request.KeycloakBaseUrl, _options.AllowLocalUrls, out var baseUri, out var failureCode))
        {
            return Failure(request, failureCode, "Keycloak base URL is not safe for bootstrap.");
        }

        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            var accessToken = await RequestAdminTokenAsync(client, baseUri!, request, cancellationToken);
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return Failure(request, "keycloak_auth_failed", "Keycloak admin authentication failed.");
            }

            var realmStatus = await EnsureRealmAsync(client, baseUri!, request, accessToken, cancellationToken);
            if (!realmStatus.Success)
                return realmStatus;

            var blazorUpdated = await EnsureClientSecretAsync(
                client,
                baseUri!,
                request.Realm,
                request.BlazorClientId,
                request.BlazorClientSecret,
                accessToken,
                bearerOnly: false,
                cancellationToken);

            if (!blazorUpdated.Success)
                return Failure(request, blazorUpdated.FailureCode, blazorUpdated.Message, realmStatus.RealmCreated);

            var apiUpdated = false;
            if (!string.IsNullOrWhiteSpace(request.ApiClientId) && !string.IsNullOrWhiteSpace(request.ApiClientSecret))
            {
                var apiUpdate = await EnsureClientSecretAsync(
                    client,
                    baseUri!,
                    request.Realm,
                    request.ApiClientId,
                    request.ApiClientSecret,
                    accessToken,
                    bearerOnly: true,
                    cancellationToken);

                if (!apiUpdate.Success)
                    return Failure(request, apiUpdate.FailureCode, apiUpdate.Message, realmStatus.RealmCreated, blazorClientUpdated: true);

                apiUpdated = true;
            }

            _logger.LogInformation(
                "Keycloak bootstrap Admin API calls completed. Realm: {Realm}, BlazorClientId: {BlazorClientId}, ApiClientId: {ApiClientId}, Mode: {Mode}, RealmCreated: {RealmCreated}, ApiClientUpdated: {ApiClientUpdated}",
                request.Realm,
                request.BlazorClientId,
                request.ApiClientId,
                request.Mode,
                realmStatus.RealmCreated,
                apiUpdated);

            return new KeycloakBootstrapResultDto
            {
                Success = true,
                Message = "Keycloak bootstrap completed successfully.",
                Mode = request.Mode,
                Realm = request.Realm,
                BlazorClientId = request.BlazorClientId,
                ApiClientId = request.ApiClientId,
                RealmCreated = realmStatus.RealmCreated,
                BlazorClientUpdated = true,
                ApiClientUpdated = apiUpdated
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Failure(request, "keycloak_timeout", "Keycloak bootstrap timed out before completion.");
        }
        catch (HttpRequestException)
        {
            return Failure(request, "keycloak_unreachable", "Keycloak Admin API was unreachable during bootstrap.");
        }
        catch (JsonException)
        {
            return Failure(request, "keycloak_invalid_response", "Keycloak Admin API returned an invalid response.");
        }
    }

    public async Task<KeycloakRealmDoctorResultDto> DiagnoseRealmAsync(
        AuthProviderConfigurationDto configuration,
        KeycloakRealmDoctorRequestDto request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var checks = new List<KeycloakRealmDoctorCheckDto>();
        var result = new KeycloakRealmDoctorResultDto
        {
            Authority = configuration.KeycloakAuthority,
            ClientId = configuration.KeycloakClientId,
            ApiClientId = request.ApiClientId
        };

        if (!configuration.KeycloakEnabled)
        {
            checks.Add(DoctorCheck(
                "keycloak_disabled",
                "Keycloak enabled",
                "blocked",
                "Keycloak sign-in is not enabled for this instance.",
                "Enable Keycloak before running realm diagnostics."));
            return CompleteDoctorResult(result, checks);
        }

        if (string.IsNullOrWhiteSpace(configuration.KeycloakAuthority)
            || string.IsNullOrWhiteSpace(configuration.KeycloakClientId))
        {
            checks.Add(DoctorCheck(
                "keycloak_configuration_missing",
                "Keycloak configuration",
                "blocked",
                "Keycloak authority and client ID must both be configured.",
                "Save the Keycloak authority URL and Blazor client ID, then rerun diagnostics."));
            return CompleteDoctorResult(result, checks);
        }

        if (!TryParseAuthority(configuration.KeycloakAuthority, _options.AllowLocalUrls, out var baseUri, out var realm, out var failureCode))
        {
            checks.Add(DoctorCheck(
                failureCode,
                "Authority URL",
                "blocked",
                "Keycloak authority URL is not valid or is not safe for diagnostics.",
                "Use a Keycloak realm authority such as https://keycloak.example.com/realms/islamu."));
            return CompleteDoctorResult(result, checks);
        }

        result.Realm = realm;
        checks.Add(DoctorCheck(
            "keycloak_configuration_present",
            "Keycloak configuration",
            "healthy",
            "Keycloak provider configuration is present."));

        var client = _httpClientFactory.CreateClient(HttpClientName);
        try
        {
            await AddDiscoveryCheckAsync(client, baseUri!, realm, checks, cancellationToken);
            if (HasBlockingCheck(checks))
                return CompleteDoctorResult(result, checks);

            if (!request.UseTemporaryAdminCredentials)
            {
                checks.Add(DoctorCheck(
                    "keycloak_admin_credentials_required",
                    "Deep realm inspection",
                    "warning",
                    "OIDC discovery is reachable. Temporary admin credentials are required to inspect clients, scopes, and offline_access mappings.",
                    "Run the doctor with temporary Keycloak admin credentials when you need drift details. The credentials are not stored."));
                return CompleteDoctorResult(result, checks);
            }

            if (string.IsNullOrWhiteSpace(request.BootstrapAdminUsername)
                || string.IsNullOrWhiteSpace(request.BootstrapAdminPassword))
            {
                checks.Add(DoctorCheck(
                    "keycloak_admin_credentials_missing",
                    "Temporary admin credentials",
                    "blocked",
                    "Temporary admin username and password are required for deep realm inspection.",
                    "Enter temporary credentials for a Keycloak realm administrator and rerun diagnostics."));
                return CompleteDoctorResult(result, checks);
            }

            var tokenRequest = new KeycloakBootstrapRequestDto
            {
                KeycloakBaseUrl = baseUri!.ToString(),
                Realm = realm,
                BlazorClientId = configuration.KeycloakClientId,
                BootstrapAdminUsername = request.BootstrapAdminUsername,
                BootstrapAdminPassword = request.BootstrapAdminPassword
            };

            var accessToken = await RequestAdminTokenAsync(client, baseUri!, tokenRequest, cancellationToken);
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                checks.Add(DoctorCheck(
                    "keycloak_admin_auth_failed",
                    "Temporary admin authentication",
                    "blocked",
                    "Keycloak admin authentication failed.",
                    "Verify the temporary admin username, password, and Keycloak master realm configuration."));
                return CompleteDoctorResult(result, checks);
            }

            await AddAdminRealmChecksAsync(client, baseUri!, realm, configuration.KeycloakClientId, request.ApiClientId, accessToken, checks, cancellationToken);
            return CompleteDoctorResult(result, checks);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            checks.Add(DoctorCheck(
                "keycloak_timeout",
                "Keycloak reachability",
                "blocked",
                "Keycloak diagnostics timed out before completion.",
                "Check network connectivity and retry."));
            return CompleteDoctorResult(result, checks);
        }
        catch (HttpRequestException)
        {
            checks.Add(DoctorCheck(
                "keycloak_unreachable",
                "Keycloak reachability",
                "blocked",
                "Keycloak was unreachable during diagnostics.",
                "Check the Keycloak authority URL, DNS, TLS, and firewall configuration."));
            return CompleteDoctorResult(result, checks);
        }
        catch (JsonException)
        {
            checks.Add(DoctorCheck(
                "keycloak_invalid_response",
                "Keycloak response format",
                "blocked",
                "Keycloak returned a response that could not be parsed safely.",
                "Verify the authority points to a Keycloak realm and retry."));
            return CompleteDoctorResult(result, checks);
        }
    }

    public async Task<KeycloakRealmSyncPlanDto> PreviewRealmSyncAsync(
        AuthProviderConfigurationDto configuration,
        KeycloakRealmSyncPreviewRequestDto request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var diagnostics = new List<KeycloakRealmDoctorCheckDto>();
        var operations = new List<KeycloakRealmSyncOperationDto>();
        var plan = new KeycloakRealmSyncPlanDto
        {
            Authority = configuration.KeycloakAuthority,
            ClientId = configuration.KeycloakClientId,
            ApiClientId = request.ApiClientId,
            DestructiveOperationsSupported = false
        };

        if (!configuration.KeycloakEnabled)
        {
            diagnostics.Add(DoctorCheck(
                "keycloak_disabled",
                "Keycloak enabled",
                "blocked",
                "Keycloak sign-in is not enabled for this instance.",
                "Enable Keycloak before generating a realm sync preview."));
            operations.Add(SyncOperation(
                "keycloak-disabled",
                "configuration",
                "auth-provider",
                "Keycloak",
                "none",
                "blocked",
                "Keycloak is disabled.",
                "A sync preview requires an enabled Keycloak provider."));
            return CompleteSyncPlan(plan, operations, diagnostics);
        }

        if (string.IsNullOrWhiteSpace(configuration.KeycloakAuthority)
            || string.IsNullOrWhiteSpace(configuration.KeycloakClientId))
        {
            diagnostics.Add(DoctorCheck(
                "keycloak_configuration_missing",
                "Keycloak configuration",
                "blocked",
                "Keycloak authority and client ID must both be configured.",
                "Save the Keycloak authority URL and Blazor client ID, then rerun the preview."));
            operations.Add(SyncOperation(
                "keycloak-configuration-missing",
                "configuration",
                "auth-provider",
                "Keycloak",
                "none",
                "blocked",
                "Keycloak provider configuration is incomplete.",
                "Authority and client ID are required before drift can be computed."));
            return CompleteSyncPlan(plan, operations, diagnostics);
        }

        if (!TryParseAuthority(configuration.KeycloakAuthority, _options.AllowLocalUrls, out var baseUri, out var realm, out var failureCode))
        {
            diagnostics.Add(DoctorCheck(
                failureCode,
                "Authority URL",
                "blocked",
                "Keycloak authority URL is not valid or is not safe for sync preview.",
                "Use a Keycloak realm authority such as https://keycloak.example.com/realms/islamu."));
            operations.Add(SyncOperation(
                "keycloak-authority-invalid",
                "configuration",
                "authority",
                "Keycloak authority",
                "none",
                "blocked",
                "Keycloak authority cannot be inspected safely.",
                "The sync preview blocks unsafe or malformed authority URLs."));
            return CompleteSyncPlan(plan, operations, diagnostics);
        }

        plan.Realm = realm;
        plan.DesiredState = BuildDesiredState(realm, configuration.KeycloakClientId, request);
        diagnostics.Add(DoctorCheck(
            "keycloak_configuration_present",
            "Keycloak configuration",
            "healthy",
            "Keycloak provider configuration is present."));

        var client = _httpClientFactory.CreateClient(HttpClientName);
        try
        {
            await AddDiscoveryCheckAsync(client, baseUri!, realm, diagnostics, cancellationToken);
            if (HasBlockingCheck(diagnostics))
                return CompleteSyncPlan(plan, operations, diagnostics);

            if (!request.UseTemporaryAdminCredentials)
            {
                operations.Add(SyncOperation(
                    "keycloak-admin-credentials-required",
                    "inspection",
                    "realm",
                    realm,
                    "none",
                    "blocked",
                    "Temporary admin credentials are required to compute a drift-aware sync plan.",
                    "The basic preview can show desired state, but Keycloak Admin API reads are required to compare current realm state."));
                return CompleteSyncPlan(plan, operations, diagnostics);
            }

            if (string.IsNullOrWhiteSpace(request.BootstrapAdminUsername)
                || string.IsNullOrWhiteSpace(request.BootstrapAdminPassword))
            {
                diagnostics.Add(DoctorCheck(
                    "keycloak_admin_credentials_missing",
                    "Temporary admin credentials",
                    "blocked",
                    "Temporary admin username and password are required for sync preview.",
                    "Enter temporary credentials for a Keycloak realm administrator and rerun the preview."));
                return CompleteSyncPlan(plan, operations, diagnostics);
            }

            var tokenRequest = new KeycloakBootstrapRequestDto
            {
                KeycloakBaseUrl = baseUri!.ToString(),
                Realm = realm,
                BlazorClientId = configuration.KeycloakClientId,
                BootstrapAdminUsername = request.BootstrapAdminUsername,
                BootstrapAdminPassword = request.BootstrapAdminPassword
            };

            var accessToken = await RequestAdminTokenAsync(client, baseUri!, tokenRequest, cancellationToken);
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                diagnostics.Add(DoctorCheck(
                    "keycloak_admin_auth_failed",
                    "Temporary admin authentication",
                    "blocked",
                    "Keycloak admin authentication failed.",
                    "Verify the temporary admin username, password, and Keycloak master realm configuration."));
                return CompleteSyncPlan(plan, operations, diagnostics);
            }

            await AddSyncPreviewOperationsAsync(client, baseUri!, realm, configuration.KeycloakClientId, request, accessToken, operations, diagnostics, cancellationToken);
            return CompleteSyncPlan(plan, operations, diagnostics);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            diagnostics.Add(DoctorCheck(
                "keycloak_timeout",
                "Keycloak reachability",
                "blocked",
                "Keycloak sync preview timed out before completion.",
                "Check network connectivity and retry."));
            return CompleteSyncPlan(plan, operations, diagnostics);
        }
        catch (HttpRequestException)
        {
            diagnostics.Add(DoctorCheck(
                "keycloak_unreachable",
                "Keycloak reachability",
                "blocked",
                "Keycloak was unreachable during sync preview.",
                "Check the Keycloak authority URL, DNS, TLS, and firewall configuration."));
            return CompleteSyncPlan(plan, operations, diagnostics);
        }
        catch (JsonException)
        {
            diagnostics.Add(DoctorCheck(
                "keycloak_invalid_response",
                "Keycloak response format",
                "blocked",
                "Keycloak returned a response that could not be parsed safely.",
                "Verify the authority points to a Keycloak realm and retry."));
            return CompleteSyncPlan(plan, operations, diagnostics);
        }
    }

    public async Task<KeycloakRealmSyncPlanDto> ApplyRealmSyncAsync(
        AuthProviderConfigurationDto configuration,
        KeycloakRealmSyncApplyRequestDto request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var previewRequest = new KeycloakRealmSyncPreviewRequestDto
        {
            UseTemporaryAdminCredentials = true,
            BootstrapAdminUsername = request.BootstrapAdminUsername,
            BootstrapAdminPassword = request.BootstrapAdminPassword,
            ApiClientId = request.ApiClientId,
            BlazorRedirectUris = request.BlazorRedirectUris,
            BlazorWebOrigins = request.BlazorWebOrigins
        };
        var diagnostics = new List<KeycloakRealmDoctorCheckDto>();
        var operations = new List<KeycloakRealmSyncOperationDto>();
        var plan = new KeycloakRealmSyncPlanDto
        {
            Authority = configuration.KeycloakAuthority,
            ClientId = configuration.KeycloakClientId,
            ApiClientId = request.ApiClientId,
            DestructiveOperationsSupported = false
        };

        if (!request.BackupConfirmed)
        {
            operations.Add(SyncOperation(
                "keycloak-backup-confirmation-required",
                "safety",
                "realm",
                "Keycloak backup",
                "none",
                "blocked",
                "Keycloak backup confirmation is required before applying realm repairs.",
                "Create or verify a Keycloak database backup, then confirm before applying additive changes."));
            return CompleteSyncPlan(plan, operations, diagnostics);
        }

        if (!configuration.KeycloakEnabled
            || string.IsNullOrWhiteSpace(configuration.KeycloakAuthority)
            || string.IsNullOrWhiteSpace(configuration.KeycloakClientId))
        {
            operations.Add(SyncOperation(
                "keycloak-configuration-missing",
                "configuration",
                "auth-provider",
                "Keycloak",
                "none",
                "blocked",
                "Keycloak provider configuration is incomplete.",
                "Enable Keycloak and save authority/client settings before applying realm repairs."));
            return CompleteSyncPlan(plan, operations, diagnostics);
        }

        if (!TryParseAuthority(configuration.KeycloakAuthority, _options.AllowLocalUrls, out var baseUri, out var realm, out var failureCode))
        {
            diagnostics.Add(DoctorCheck(
                failureCode,
                "Authority URL",
                "blocked",
                "Keycloak authority URL is not valid or is not safe for sync apply.",
                "Use a Keycloak realm authority such as https://keycloak.example.com/realms/islamu."));
            return CompleteSyncPlan(plan, operations, diagnostics);
        }

        plan.Realm = realm;
        plan.DesiredState = BuildDesiredState(realm, configuration.KeycloakClientId, previewRequest);

        if (string.IsNullOrWhiteSpace(request.BootstrapAdminUsername)
            || string.IsNullOrWhiteSpace(request.BootstrapAdminPassword))
        {
            diagnostics.Add(DoctorCheck(
                "keycloak_admin_credentials_missing",
                "Temporary admin credentials",
                "blocked",
                "Temporary admin username and password are required to apply Keycloak repairs.",
                "Enter temporary credentials for a Keycloak realm administrator and retry."));
            return CompleteSyncPlan(plan, operations, diagnostics);
        }

        var client = _httpClientFactory.CreateClient(HttpClientName);
        try
        {
            await AddDiscoveryCheckAsync(client, baseUri!, realm, diagnostics, cancellationToken);
            if (HasBlockingCheck(diagnostics))
                return CompleteSyncPlan(plan, operations, diagnostics);

            var tokenRequest = new KeycloakBootstrapRequestDto
            {
                KeycloakBaseUrl = baseUri!.ToString(),
                Realm = realm,
                BlazorClientId = configuration.KeycloakClientId,
                BootstrapAdminUsername = request.BootstrapAdminUsername,
                BootstrapAdminPassword = request.BootstrapAdminPassword
            };

            var accessToken = await RequestAdminTokenAsync(client, baseUri!, tokenRequest, cancellationToken);
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                diagnostics.Add(DoctorCheck(
                    "keycloak_admin_auth_failed",
                    "Temporary admin authentication",
                    "blocked",
                    "Keycloak admin authentication failed.",
                    "Verify the temporary admin username, password, and Keycloak master realm configuration."));
                return CompleteSyncPlan(plan, operations, diagnostics);
            }

            await AddSyncApplyOperationsAsync(
                client,
                baseUri!,
                realm,
                configuration,
                previewRequest,
                accessToken,
                operations,
                diagnostics,
                cancellationToken);
            return CompleteSyncPlan(plan, operations, diagnostics);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            diagnostics.Add(DoctorCheck(
                "keycloak_timeout",
                "Keycloak reachability",
                "blocked",
                "Keycloak sync apply timed out before completion.",
                "Check network connectivity and retry."));
            return CompleteSyncPlan(plan, operations, diagnostics);
        }
        catch (HttpRequestException)
        {
            diagnostics.Add(DoctorCheck(
                "keycloak_unreachable",
                "Keycloak reachability",
                "blocked",
                "Keycloak was unreachable during sync apply.",
                "Check the Keycloak authority URL, DNS, TLS, and firewall configuration."));
            return CompleteSyncPlan(plan, operations, diagnostics);
        }
        catch (JsonException)
        {
            diagnostics.Add(DoctorCheck(
                "keycloak_invalid_response",
                "Keycloak response format",
                "blocked",
                "Keycloak returned a response that could not be parsed safely.",
                "Verify the authority points to a Keycloak realm and retry."));
            return CompleteSyncPlan(plan, operations, diagnostics);
        }
    }

    private static bool TryNormalizeBaseUri(string keycloakBaseUrl, bool allowLocalUrls, out Uri? baseUri, out string failureCode)
    {
        baseUri = null;
        failureCode = "keycloak_invalid_url";

        if (!Uri.TryCreate(keycloakBaseUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment)
            || string.IsNullOrWhiteSpace(uri.Host))
        {
            return false;
        }

        if (!allowLocalUrls && IsBlockedHost(uri.Host))
        {
            failureCode = "keycloak_unsafe_host";
            return false;
        }

        baseUri = new Uri(uri.GetLeftPart(UriPartial.Path).TrimEnd('/') + "/");
        return true;
    }

    private static bool TryParseAuthority(
        string authority,
        bool allowLocalUrls,
        out Uri? baseUri,
        out string realm,
        out string failureCode)
    {
        baseUri = null;
        realm = string.Empty;
        failureCode = "keycloak_invalid_authority";

        if (!Uri.TryCreate(authority, UriKind.Absolute, out var authorityUri))
            return false;

        var segments = authorityUri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var realmsIndex = Array.FindIndex(segments, segment => string.Equals(segment, "realms", StringComparison.OrdinalIgnoreCase));
        if (realmsIndex < 0 || realmsIndex + 1 >= segments.Length)
            return false;

        realm = Uri.UnescapeDataString(segments[realmsIndex + 1]);
        var basePath = string.Join('/', segments.Take(realmsIndex));
        var builder = new UriBuilder(authorityUri)
        {
            Path = basePath,
            Query = string.Empty,
            Fragment = string.Empty
        };

        return TryNormalizeBaseUri(builder.Uri.ToString(), allowLocalUrls, out baseUri, out failureCode);
    }

    private static bool IsBlockedHost(string host)
    {
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!IPAddress.TryParse(host, out var address))
            return false;

        if (IPAddress.IsLoopback(address))
            return true;

        if (address.Equals(IPAddress.Any)
            || address.Equals(IPAddress.IPv6Any)
            || address.Equals(IPAddress.None)
            || address.Equals(IPAddress.IPv6None))
        {
            return true;
        }

        var bytes = address.GetAddressBytes();
        return address.AddressFamily switch
        {
            System.Net.Sockets.AddressFamily.InterNetwork =>
                bytes[0] == 169 && bytes[1] == 254 || bytes[0] >= 224,
            System.Net.Sockets.AddressFamily.InterNetworkV6 =>
                address.IsIPv6LinkLocal || address.IsIPv6Multicast || address.IsIPv6SiteLocal,
            _ => true
        };
    }

    private static Uri BuildUri(Uri baseUri, params string[] segments)
    {
        var existingPath = baseUri.AbsolutePath.Trim('/');
        var pathSegments = segments.Select(segment => Uri.EscapeDataString(segment.Trim('/')));
        var path = string.Join('/', string.IsNullOrEmpty(existingPath)
            ? pathSegments
            : [existingPath, .. pathSegments]);

        return new UriBuilder(baseUri)
        {
            Path = path
        }.Uri;
    }

    private static Uri BuildClientLookupUri(Uri baseUri, string realm, string clientId)
    {
        var builder = new UriBuilder(BuildUri(baseUri, "admin", "realms", realm, "clients"))
        {
            Query = $"clientId={Uri.EscapeDataString(clientId)}"
        };

        return builder.Uri;
    }

    private static async Task<string?> RequestAdminTokenAsync(
        HttpClient client,
        Uri baseUri,
        KeycloakBootstrapRequestDto request,
        CancellationToken cancellationToken)
    {
        using var tokenRequest = new HttpRequestMessage(
            HttpMethod.Post,
            BuildUri(baseUri, "realms", "master", "protocol", "openid-connect", "token"))
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = AdminCliClientId,
                ["username"] = request.BootstrapAdminUsername,
                ["password"] = request.BootstrapAdminPassword
            })
        };

        using var response = await client.SendAsync(tokenRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var token = await JsonSerializer.DeserializeAsync<KeycloakTokenResponse>(stream, JsonOptions, cancellationToken);
        return token?.AccessToken;
    }

    private async Task<KeycloakBootstrapResultDto> EnsureRealmAsync(
        HttpClient client,
        Uri baseUri,
        KeycloakBootstrapRequestDto request,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var checkRequest = CreateAdminRequest(
            HttpMethod.Get,
            BuildUri(baseUri, "admin", "realms", request.Realm),
            accessToken);
        using var checkResponse = await client.SendAsync(checkRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (checkResponse.IsSuccessStatusCode)
            return Success(request, realmCreated: false);

        if (checkResponse.StatusCode != HttpStatusCode.NotFound)
        {
            _logger.LogWarning(
                "Keycloak realm check failed. Realm: {Realm}, Mode: {Mode}, StatusCode: {StatusCode}",
                request.Realm,
                request.Mode,
                (int)checkResponse.StatusCode);

            return Failure(request, "keycloak_realm_check_failed", "Keycloak realm status could not be verified.");
        }

        if (request.Mode != KeycloakBootstrapMode.CreateRealm)
            return Failure(request, "keycloak_realm_not_found", "Keycloak realm was not found and create mode was not requested.");

        using var createRequest = CreateAdminRequest(
            HttpMethod.Post,
            BuildUri(baseUri, "admin", "realms"),
            accessToken,
            new KeycloakRealmRepresentation(request.Realm, Enabled: true));
        using var createResponse = await client.SendAsync(createRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (createResponse.IsSuccessStatusCode || createResponse.StatusCode == HttpStatusCode.Conflict)
            return Success(request, realmCreated: createResponse.StatusCode != HttpStatusCode.Conflict);

        _logger.LogWarning(
            "Keycloak realm create failed. Realm: {Realm}, StatusCode: {StatusCode}",
            request.Realm,
            (int)createResponse.StatusCode);

        return Failure(request, "keycloak_realm_create_failed", "Keycloak realm could not be created.");
    }

    private async Task<ClientSecretUpdateResult> EnsureClientSecretAsync(
        HttpClient client,
        Uri baseUri,
        string realm,
        string clientId,
        string secret,
        string accessToken,
        bool bearerOnly,
        CancellationToken cancellationToken)
    {
        var lookup = await FindClientAsync(client, baseUri, realm, clientId, accessToken, cancellationToken);
        if (!lookup.Success)
            return ClientSecretUpdateResult.Failure("keycloak_client_lookup_failed", "Keycloak client lookup failed.");

        var clientUuid = lookup.ClientUuid;
        if (clientUuid is null)
        {
            var createResult = await CreateClientAsync(client, baseUri, realm, clientId, accessToken, bearerOnly, cancellationToken);
            if (!createResult.Success)
                return createResult;

            lookup = await FindClientAsync(client, baseUri, realm, clientId, accessToken, cancellationToken);
            if (!lookup.Success)
                return ClientSecretUpdateResult.Failure("keycloak_client_lookup_failed", "Keycloak client lookup failed.");

            clientUuid = lookup.ClientUuid;
            if (clientUuid is null)
                return ClientSecretUpdateResult.Failure("keycloak_client_not_found", "Keycloak client could not be located after creation.");
        }

        var clientUri = BuildUri(baseUri, "admin", "realms", realm, "clients", clientUuid);
        var clientRepresentation = await GetClientRepresentationAsync(client, clientUri, accessToken, cancellationToken);
        if (clientRepresentation is null)
            return ClientSecretUpdateResult.Failure("keycloak_client_lookup_failed", "Keycloak client representation could not be loaded.");

        var representationSecretMatches = string.Equals(
            clientRepresentation["secret"]?.GetValue<string>(),
            secret,
            StringComparison.Ordinal);

        if (!bearerOnly)
        {
            var roleResult = await EnsureOfflineAccessRealmRoleAsync(client, baseUri, realm, accessToken, cancellationToken);
            if (!roleResult.Success)
                return roleResult;

            var scopeMappingResult = await EnsureOfflineAccessClientScopeMappingAsync(
                client,
                baseUri,
                realm,
                accessToken,
                cancellationToken);
            if (!scopeMappingResult.Success)
                return scopeMappingResult;
        }

        if (representationSecretMatches && (bearerOnly || HasRefreshTokenSettings(clientRepresentation)))
            return ClientSecretUpdateResult.Succeeded();

        var currentSecret = representationSecretMatches
            ? secret
            : await GetCurrentClientSecretAsync(client, baseUri, realm, clientUuid, accessToken, cancellationToken);
        var secretMatches = string.Equals(currentSecret, secret, StringComparison.Ordinal);

        if (secretMatches && (bearerOnly || HasRefreshTokenSettings(clientRepresentation)))
            return ClientSecretUpdateResult.Succeeded();

        if (!bearerOnly)
        {
            var scopeResult = await EnsureOfflineAccessScopeAsync(
                client,
                baseUri,
                realm,
                clientUuid,
                accessToken,
                cancellationToken);
            if (!scopeResult.Success)
                return scopeResult;
        }

        if (!secretMatches)
            clientRepresentation["secret"] = secret;

        if (!bearerOnly)
            PreserveRefreshTokenSettings(clientRepresentation);

        using var secretRequest = CreateAdminRequest(
            HttpMethod.Put,
            clientUri,
            accessToken,
            clientRepresentation);
        using var secretResponse = await client.SendAsync(secretRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (secretResponse.IsSuccessStatusCode)
            return ClientSecretUpdateResult.Succeeded();

        _logger.LogWarning(
            "Keycloak client secret update failed. Realm: {Realm}, ClientId: {ClientId}, StatusCode: {StatusCode}",
            realm,
            clientId,
            (int)secretResponse.StatusCode);

        return ClientSecretUpdateResult.Failure(
            "keycloak_client_secret_update_failed",
            "Keycloak client secret could not be updated.");
    }

    private static async Task<ClientLookupResult> FindClientAsync(
        HttpClient client,
        Uri baseUri,
        string realm,
        string clientId,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var request = CreateAdminRequest(
            HttpMethod.Get,
            BuildClientLookupUri(baseUri, realm, clientId),
            accessToken);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return ClientLookupResult.Failure();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var clients = await JsonSerializer.DeserializeAsync<List<KeycloakClientLookupResult>>(stream, JsonOptions, cancellationToken);
        var clientUuid = clients?.FirstOrDefault(x => string.Equals(x.ClientId, clientId, StringComparison.Ordinal))?.Id;
        return ClientLookupResult.Succeeded(clientUuid);
    }

    private static async Task<JsonObject?> GetClientRepresentationAsync(
        HttpClient client,
        Uri clientUri,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var request = CreateAdminRequest(HttpMethod.Get, clientUri, accessToken);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return null;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<JsonObject>(stream, JsonOptions, cancellationToken);
    }

    private static async Task<string?> GetCurrentClientSecretAsync(
        HttpClient client,
        Uri baseUri,
        string realm,
        string clientUuid,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var request = CreateAdminRequest(
            HttpMethod.Get,
            BuildUri(baseUri, "admin", "realms", realm, "clients", clientUuid, "client-secret"),
            accessToken);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return null;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var representation = await JsonSerializer.DeserializeAsync<JsonObject>(stream, JsonOptions, cancellationToken);
        return representation?["value"]?.GetValue<string>();
    }

    private static bool HasRefreshTokenSettings(JsonObject clientRepresentation)
    {
        var hasOfflineAccess = ContainsScope(clientRepresentation["optionalClientScopes"], "offline_access")
            || ContainsScope(clientRepresentation["defaultClientScopes"], "offline_access");

        var attributes = clientRepresentation["attributes"] as JsonObject;
        var usesRefreshTokens = string.Equals(
            attributes?["use.refresh.tokens"]?.GetValue<string>(),
            "true",
            StringComparison.OrdinalIgnoreCase);

        return hasOfflineAccess && usesRefreshTokens;
    }

    private static bool ContainsScope(JsonNode? scopesNode, string expectedScope)
    {
        return scopesNode is JsonArray scopes
            && scopes.Any(scope => string.Equals(scope?.GetValue<string>(), expectedScope, StringComparison.Ordinal));
    }

    private static async Task<ClientSecretUpdateResult> EnsureOfflineAccessRealmRoleAsync(
        HttpClient client,
        Uri baseUri,
        string realm,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var offlineRole = await GetRealmRoleAsync(client, baseUri, realm, "offline_access", accessToken, cancellationToken);
        if (offlineRole is null)
        {
            return ClientSecretUpdateResult.Failure(
                "keycloak_offline_access_role_not_found",
                "Keycloak offline access realm role could not be located.");
        }

        var defaultRoleName = $"default-roles-{realm.ToLowerInvariant()}";
        var defaultRole = await GetRealmRoleAsync(client, baseUri, realm, defaultRoleName, accessToken, cancellationToken);
        var defaultRoleId = defaultRole?["id"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(defaultRoleId))
        {
            return ClientSecretUpdateResult.Failure(
                "keycloak_default_role_not_found",
                "Keycloak default realm role could not be located.");
        }

        using var request = CreateAdminRequest(
            HttpMethod.Post,
            BuildUri(baseUri, "admin", "realms", realm, "roles-by-id", defaultRoleId, "composites"),
            accessToken,
            new JsonArray(offlineRole.DeepClone()));
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        return response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Conflict
            ? ClientSecretUpdateResult.Succeeded()
            : ClientSecretUpdateResult.Failure(
                "keycloak_offline_access_role_update_failed",
                "Keycloak offline access realm role could not be assigned to the default role.");
    }

    private static async Task<JsonObject?> GetRealmRoleAsync(
        HttpClient client,
        Uri baseUri,
        string realm,
        string roleName,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var request = CreateAdminRequest(
            HttpMethod.Get,
            BuildUri(baseUri, "admin", "realms", realm, "roles", roleName),
            accessToken);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return null;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<JsonObject>(stream, JsonOptions, cancellationToken);
    }

    private static async Task<ClientSecretUpdateResult> EnsureOfflineAccessScopeAsync(
        HttpClient client,
        Uri baseUri,
        string realm,
        string clientUuid,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var scopeId = await FindClientScopeIdAsync(client, baseUri, realm, "offline_access", accessToken, cancellationToken);
        if (string.IsNullOrWhiteSpace(scopeId))
        {
            return ClientSecretUpdateResult.Failure(
                "keycloak_client_scope_not_found",
                "Keycloak offline access client scope could not be located.");
        }

        using var request = CreateAdminRequest(
            HttpMethod.Put,
            BuildUri(baseUri, "admin", "realms", realm, "clients", clientUuid, "optional-client-scopes", scopeId),
            accessToken);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        return response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Conflict
            ? ClientSecretUpdateResult.Succeeded()
            : ClientSecretUpdateResult.Failure(
                "keycloak_client_scope_update_failed",
                "Keycloak offline access client scope could not be assigned.");
    }

    private static async Task<ClientSecretUpdateResult> EnsureOfflineAccessClientScopeMappingAsync(
        HttpClient client,
        Uri baseUri,
        string realm,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var scopeId = await FindClientScopeIdAsync(client, baseUri, realm, "offline_access", accessToken, cancellationToken);
        if (string.IsNullOrWhiteSpace(scopeId))
        {
            return ClientSecretUpdateResult.Failure(
                "keycloak_client_scope_not_found",
                "Keycloak offline access client scope could not be located.");
        }

        return await EnsureOfflineAccessScopeMappingAsync(
            client,
            baseUri,
            realm,
            scopeId,
            accessToken,
            cancellationToken);
    }

    private static async Task<ClientSecretUpdateResult> EnsureOfflineAccessScopeMappingAsync(
        HttpClient client,
        Uri baseUri,
        string realm,
        string scopeId,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var offlineRole = await GetRealmRoleAsync(client, baseUri, realm, "offline_access", accessToken, cancellationToken);
        if (offlineRole is null)
        {
            return ClientSecretUpdateResult.Failure(
                "keycloak_offline_access_role_not_found",
                "Keycloak offline access realm role could not be located.");
        }

        using var request = CreateAdminRequest(
            HttpMethod.Post,
            BuildUri(baseUri, "admin", "realms", realm, "client-scopes", scopeId, "scope-mappings", "realm"),
            accessToken,
            new JsonArray(offlineRole.DeepClone()));
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        return response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Conflict
            ? ClientSecretUpdateResult.Succeeded()
            : ClientSecretUpdateResult.Failure(
                "keycloak_offline_access_scope_mapping_failed",
                "Keycloak offline access role could not be assigned to the offline access client scope.");
    }

    private static async Task<string?> FindClientScopeIdAsync(
        HttpClient client,
        Uri baseUri,
        string realm,
        string scopeName,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var builder = new UriBuilder(BuildUri(baseUri, "admin", "realms", realm, "client-scopes"))
        {
            Query = $"search={Uri.EscapeDataString(scopeName)}"
        };
        using var request = CreateAdminRequest(HttpMethod.Get, builder.Uri, accessToken);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return null;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var scopes = await JsonSerializer.DeserializeAsync<List<KeycloakClientScopeLookupResult>>(stream, JsonOptions, cancellationToken);
        return scopes?.FirstOrDefault(scope => string.Equals(scope.Name, scopeName, StringComparison.Ordinal))?.Id;
    }


    private static void PreserveRefreshTokenSettings(JsonObject clientRepresentation)
    {
        var optionalScopes = clientRepresentation["optionalClientScopes"] as JsonArray;
        if (optionalScopes is null)
        {
            optionalScopes = [];
            clientRepresentation["optionalClientScopes"] = optionalScopes;
        }

        if (!optionalScopes.Any(scope => string.Equals(scope?.GetValue<string>(), "offline_access", StringComparison.Ordinal)))
            optionalScopes.Add("offline_access");

        var attributes = clientRepresentation["attributes"] as JsonObject;
        if (attributes is null)
        {
            attributes = [];
            clientRepresentation["attributes"] = attributes;
        }

        attributes["use.refresh.tokens"] = "true";
    }

    private async Task<ClientSecretUpdateResult> CreateClientAsync(
        HttpClient client,
        Uri baseUri,
        string realm,
        string clientId,
        string accessToken,
        bool bearerOnly,
        CancellationToken cancellationToken)
    {
        var representation = bearerOnly
            ? KeycloakClientRepresentation.CreateBearerOnly(clientId)
            : KeycloakClientRepresentation.CreateConfidentialOidc(clientId);
        using var request = CreateAdminRequest(
            HttpMethod.Post,
            BuildUri(baseUri, "admin", "realms", realm, "clients"),
            accessToken,
            representation);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Conflict)
            return ClientSecretUpdateResult.Succeeded();

        _logger.LogWarning(
            "Keycloak client create failed. Realm: {Realm}, ClientId: {ClientId}, StatusCode: {StatusCode}",
            realm,
            clientId,
            (int)response.StatusCode);

        return ClientSecretUpdateResult.Failure("keycloak_client_create_failed", "Keycloak client could not be created.");
    }

    private static HttpRequestMessage CreateAdminRequest(
        HttpMethod method,
        Uri requestUri,
        string accessToken,
        object? body = null)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (body is not null)
        {
            request.Content = new StringContent(
                JsonSerializer.Serialize(body, JsonOptions),
                Encoding.UTF8,
                "application/json");
        }

        return request;
    }

    private static async Task AddDiscoveryCheckAsync(
        HttpClient client,
        Uri baseUri,
        string realm,
        List<KeycloakRealmDoctorCheckDto> checks,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            BuildUri(baseUri, "realms", realm, ".well-known", "openid-configuration"));
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        checks.Add(response.IsSuccessStatusCode
            ? DoctorCheck(
                "keycloak_discovery_reachable",
                "OIDC discovery",
                "healthy",
                "Keycloak OIDC discovery is reachable for the configured realm.")
            : DoctorCheck(
                "keycloak_discovery_unreachable",
                "OIDC discovery",
                "blocked",
                "Keycloak OIDC discovery is not reachable for the configured realm.",
                "Verify the authority URL, realm name, reverse proxy path, and TLS configuration."));
    }

    private static async Task AddAdminRealmChecksAsync(
        HttpClient client,
        Uri baseUri,
        string realm,
        string blazorClientId,
        string? apiClientId,
        string accessToken,
        List<KeycloakRealmDoctorCheckDto> checks,
        CancellationToken cancellationToken)
    {
        using var realmRequest = CreateAdminRequest(HttpMethod.Get, BuildUri(baseUri, "admin", "realms", realm), accessToken);
        using var realmResponse = await client.SendAsync(realmRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        checks.Add(realmResponse.IsSuccessStatusCode
            ? DoctorCheck("keycloak_realm_exists", "Realm", "healthy", "Keycloak realm exists and is readable.")
            : DoctorCheck("keycloak_realm_not_found", "Realm", "blocked", "Keycloak realm could not be read through the Admin API.", "Verify the realm exists and the temporary admin has read access."));

        if (!realmResponse.IsSuccessStatusCode)
            return;

        var blazorLookup = await FindClientAsync(client, baseUri, realm, blazorClientId, accessToken, cancellationToken);
        if (!blazorLookup.Success || string.IsNullOrWhiteSpace(blazorLookup.ClientUuid))
        {
            checks.Add(DoctorCheck(
                "keycloak_blazor_client_missing",
                "Blazor OIDC client",
                "needs-repair",
                "The configured Blazor Keycloak client was not found.",
                "Run setup bootstrap or create the confidential OIDC client additively in Keycloak."));
            return;
        }

        checks.Add(DoctorCheck("keycloak_blazor_client_exists", "Blazor OIDC client", "healthy", "The configured Blazor Keycloak client exists."));
        var clientRepresentation = await GetClientRepresentationAsync(
            client,
            BuildUri(baseUri, "admin", "realms", realm, "clients", blazorLookup.ClientUuid),
            accessToken,
            cancellationToken);

        if (clientRepresentation is null)
        {
            checks.Add(DoctorCheck(
                "keycloak_blazor_client_unreadable",
                "Blazor client representation",
                "blocked",
                "The Blazor client exists but its representation could not be read.",
                "Verify the temporary admin has permission to view clients."));
            return;
        }

        AddClientRepresentationChecks(clientRepresentation, checks);
        await AddOfflineAccessChecksAsync(client, baseUri, realm, blazorLookup.ClientUuid, accessToken, checks, cancellationToken);
        await AddApiClientCheckAsync(client, baseUri, realm, apiClientId, accessToken, checks, cancellationToken);
    }

    private static void AddClientRepresentationChecks(JsonObject clientRepresentation, List<KeycloakRealmDoctorCheckDto> checks)
    {
        var standardFlowEnabled = clientRepresentation["standardFlowEnabled"]?.GetValue<bool>() == true;
        checks.Add(standardFlowEnabled
            ? DoctorCheck("keycloak_standard_flow_enabled", "Authorization code flow", "healthy", "The Blazor client has standard authorization code flow enabled.")
            : DoctorCheck("keycloak_standard_flow_disabled", "Authorization code flow", "needs-repair", "The Blazor client does not have standard authorization code flow enabled.", "Enable Standard flow on the Blazor OIDC client."));

        var refreshTokensEnabled = HasRefreshTokenSettings(clientRepresentation);
        checks.Add(refreshTokensEnabled
            ? DoctorCheck("keycloak_refresh_tokens_enabled", "Refresh token settings", "healthy", "The Blazor client includes offline_access scope and refresh-token settings.")
            : DoctorCheck("keycloak_refresh_tokens_missing", "Refresh token settings", "needs-repair", "The Blazor client is missing offline_access scope or refresh-token settings.", "Assign offline_access as a client scope and set use.refresh.tokens=true."));
    }

    private static async Task AddOfflineAccessChecksAsync(
        HttpClient client,
        Uri baseUri,
        string realm,
        string clientUuid,
        string accessToken,
        List<KeycloakRealmDoctorCheckDto> checks,
        CancellationToken cancellationToken)
    {
        var offlineRole = await GetRealmRoleAsync(client, baseUri, realm, "offline_access", accessToken, cancellationToken);
        checks.Add(offlineRole is not null
            ? DoctorCheck("keycloak_offline_access_role_exists", "offline_access realm role", "healthy", "The offline_access realm role exists.")
            : DoctorCheck("keycloak_offline_access_role_missing", "offline_access realm role", "needs-repair", "The offline_access realm role was not found.", "Restore or create the offline_access realm role before enabling offline sessions."));

        var defaultRoleName = $"default-roles-{realm.ToLowerInvariant()}";
        var defaultRole = await GetRealmRoleAsync(client, baseUri, realm, defaultRoleName, accessToken, cancellationToken);
        checks.Add(defaultRole is not null
            ? DoctorCheck("keycloak_default_role_exists", "Default realm role", "healthy", "The default realm role exists.")
            : DoctorCheck("keycloak_default_role_missing", "Default realm role", "needs-repair", "The default realm role was not found.", "Recreate the default realm role or repair realm defaults."));

        if (offlineRole is not null && defaultRole?["id"]?.GetValue<string>() is { Length: > 0 } defaultRoleId)
        {
            var defaultComposites = await GetRealmRoleCompositesAsync(client, baseUri, realm, defaultRoleId, accessToken, cancellationToken);
            var hasOfflineComposite = defaultComposites.Any(role => string.Equals(role.Name, "offline_access", StringComparison.Ordinal));
            checks.Add(hasOfflineComposite
                ? DoctorCheck("keycloak_default_role_offline_access", "Default role offline_access composite", "healthy", "The default realm role includes offline_access as a composite.")
                : DoctorCheck("keycloak_default_role_offline_access_missing", "Default role offline_access composite", "needs-repair", "The default realm role does not include offline_access as a composite.", "Add offline_access to the default realm role composites."));
        }

        var scopeId = await FindClientScopeIdAsync(client, baseUri, realm, "offline_access", accessToken, cancellationToken);
        checks.Add(!string.IsNullOrWhiteSpace(scopeId)
            ? DoctorCheck("keycloak_offline_access_scope_exists", "offline_access client scope", "healthy", "The offline_access client scope exists.")
            : DoctorCheck("keycloak_offline_access_scope_missing", "offline_access client scope", "needs-repair", "The offline_access client scope was not found.", "Restore or create the offline_access client scope."));

        if (!string.IsNullOrWhiteSpace(scopeId))
        {
            var optionalScopes = await GetClientScopesAsync(client, baseUri, realm, clientUuid, "optional-client-scopes", accessToken, cancellationToken);
            var defaultScopes = await GetClientScopesAsync(client, baseUri, realm, clientUuid, "default-client-scopes", accessToken, cancellationToken);
            var clientHasOfflineScope = optionalScopes.Concat(defaultScopes).Any(scope => string.Equals(scope.Name, "offline_access", StringComparison.Ordinal));
            checks.Add(clientHasOfflineScope
                ? DoctorCheck("keycloak_client_offline_access_scope", "Blazor client offline_access scope", "healthy", "The Blazor client has offline_access assigned as a client scope.")
                : DoctorCheck("keycloak_client_offline_access_scope_missing", "Blazor client offline_access scope", "needs-repair", "The Blazor client does not have offline_access assigned as a client scope.", "Assign offline_access to the Blazor client as an optional or default client scope."));

            var scopeMappings = await GetClientScopeRealmMappingsAsync(client, baseUri, realm, scopeId, accessToken, cancellationToken);
            var scopeMapsOfflineRole = scopeMappings.Any(role => string.Equals(role.Name, "offline_access", StringComparison.Ordinal));
            checks.Add(scopeMapsOfflineRole
                ? DoctorCheck("keycloak_scope_maps_offline_access", "offline_access scope mapping", "healthy", "The offline_access client scope maps the offline_access realm role.")
                : DoctorCheck("keycloak_scope_maps_offline_access_missing", "offline_access scope mapping", "needs-repair", "The offline_access client scope does not map the offline_access realm role.", "Add offline_access to the realm role mappings for the offline_access client scope."));
        }
    }

    private static async Task AddApiClientCheckAsync(
        HttpClient client,
        Uri baseUri,
        string realm,
        string? apiClientId,
        string accessToken,
        List<KeycloakRealmDoctorCheckDto> checks,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(apiClientId))
            return;

        var lookup = await FindClientAsync(client, baseUri, realm, apiClientId, accessToken, cancellationToken);
        checks.Add(lookup.Success && !string.IsNullOrWhiteSpace(lookup.ClientUuid)
            ? DoctorCheck("keycloak_api_client_exists", "API audience client", "healthy", "The configured API client exists.")
            : DoctorCheck("keycloak_api_client_missing", "API audience client", "warning", "The configured API client was not found.", "Create the API audience client if this instance validates API audience through Keycloak."));
    }

    private static async Task<IReadOnlyList<KeycloakRoleLookupResult>> GetRealmRoleCompositesAsync(
        HttpClient client,
        Uri baseUri,
        string realm,
        string roleId,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var request = CreateAdminRequest(
            HttpMethod.Get,
            BuildUri(baseUri, "admin", "realms", realm, "roles-by-id", roleId, "composites", "realm"),
            accessToken);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return [];

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<List<KeycloakRoleLookupResult>>(stream, JsonOptions, cancellationToken) ?? [];
    }

    private static async Task<IReadOnlyList<KeycloakClientScopeLookupResult>> GetClientScopesAsync(
        HttpClient client,
        Uri baseUri,
        string realm,
        string clientUuid,
        string scopeEndpoint,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var request = CreateAdminRequest(
            HttpMethod.Get,
            BuildUri(baseUri, "admin", "realms", realm, "clients", clientUuid, scopeEndpoint),
            accessToken);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return [];

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<List<KeycloakClientScopeLookupResult>>(stream, JsonOptions, cancellationToken) ?? [];
    }

    private static async Task<IReadOnlyList<KeycloakRoleLookupResult>> GetClientScopeRealmMappingsAsync(
        HttpClient client,
        Uri baseUri,
        string realm,
        string scopeId,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var request = CreateAdminRequest(
            HttpMethod.Get,
            BuildUri(baseUri, "admin", "realms", realm, "client-scopes", scopeId, "scope-mappings", "realm", "composite"),
            accessToken);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return [];

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<List<KeycloakRoleLookupResult>>(stream, JsonOptions, cancellationToken) ?? [];
    }

    private static KeycloakRealmDoctorCheckDto DoctorCheck(
        string code,
        string name,
        string status,
        string message,
        string? remediation = null) => new()
        {
            Code = code,
            Name = name,
            Status = status,
            Message = message,
            Remediation = remediation
        };

    private static KeycloakRealmDoctorResultDto CompleteDoctorResult(
        KeycloakRealmDoctorResultDto result,
        IReadOnlyList<KeycloakRealmDoctorCheckDto> checks)
    {
        result.Checks = checks;
        result.OverallStatus = HasBlockingCheck(checks)
            ? "blocked"
            : checks.Any(check => string.Equals(check.Status, "needs-repair", StringComparison.OrdinalIgnoreCase))
                ? "needs-repair"
                : checks.Any(check => string.Equals(check.Status, "warning", StringComparison.OrdinalIgnoreCase))
                    ? "needs-repair"
                    : "healthy";
        result.Message = result.OverallStatus switch
        {
            "healthy" => "Keycloak realm diagnostics did not find drift.",
            "needs-repair" => "Keycloak realm diagnostics found repairable drift or incomplete inspection.",
            _ => "Keycloak realm diagnostics are blocked. Resolve the blocking checks and retry."
        };

        return result;
    }

    private static bool HasBlockingCheck(IEnumerable<KeycloakRealmDoctorCheckDto> checks) =>
        checks.Any(check => string.Equals(check.Status, "blocked", StringComparison.OrdinalIgnoreCase));

    private static KeycloakRealmDesiredStateDto BuildDesiredState(
        string realm,
        string blazorClientId,
        KeycloakRealmSyncPreviewRequestDto request)
    {
        var clients = new List<KeycloakClientDesiredStateDto>
        {
            new()
            {
                ClientId = blazorClientId,
                DisplayName = blazorClientId,
                ClientKind = "blazor-confidential",
                Enabled = true,
                PublicClient = false,
                BearerOnly = false,
                StandardFlowEnabled = true,
                DirectAccessGrantsEnabled = false,
                ServiceAccountsEnabled = false,
                RedirectUris = request.BlazorRedirectUris,
                WebOrigins = request.BlazorWebOrigins,
                OptionalClientScopes = ["offline_access"]
            }
        };

        if (!string.IsNullOrWhiteSpace(request.ApiClientId))
        {
            clients.Add(new KeycloakClientDesiredStateDto
            {
                ClientId = request.ApiClientId,
                DisplayName = request.ApiClientId,
                ClientKind = "api-bearer",
                Enabled = true,
                PublicClient = false,
                BearerOnly = true,
                StandardFlowEnabled = false,
                DirectAccessGrantsEnabled = false,
                ServiceAccountsEnabled = false,
                ProtocolMappers =
                [
                    new KeycloakProtocolMapperDesiredStateDto
                    {
                        Name = $"{request.ApiClientId}-audience",
                        MapperType = "oidc-audience-mapper",
                        IncludedClientAudience = request.ApiClientId,
                        AddToAccessToken = true,
                        AddToIdToken = false
                    }
                ]
            });
        }

        return new KeycloakRealmDesiredStateDto
        {
            Realm = realm,
            BlazorClientId = blazorClientId,
            ApiClientId = request.ApiClientId,
            DestructiveOperationsSupported = false,
            RequiredRealmRoles = ["offline_access"],
            RoleComposites =
            [
                new KeycloakRoleCompositeDesiredStateDto
                {
                    RoleName = $"default-roles-{realm.ToLowerInvariant()}",
                    CompositeRoleNames = ["offline_access"]
                }
            ],
            ClientScopes =
            [
                new KeycloakClientScopeDesiredStateDto
                {
                    Name = "offline_access",
                    RealmRoleMappings = ["offline_access"]
                }
            ],
            Clients = clients
        };
    }

    private static async Task AddSyncPreviewOperationsAsync(
        HttpClient client,
        Uri baseUri,
        string realm,
        string blazorClientId,
        KeycloakRealmSyncPreviewRequestDto request,
        string accessToken,
        List<KeycloakRealmSyncOperationDto> operations,
        List<KeycloakRealmDoctorCheckDto> diagnostics,
        CancellationToken cancellationToken)
    {
        using var realmRequest = CreateAdminRequest(HttpMethod.Get, BuildUri(baseUri, "admin", "realms", realm), accessToken);
        using var realmResponse = await client.SendAsync(realmRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        diagnostics.Add(realmResponse.IsSuccessStatusCode
            ? DoctorCheck("keycloak_realm_exists", "Realm", "healthy", "Keycloak realm exists and is readable.")
            : DoctorCheck("keycloak_realm_not_found", "Realm", "blocked", "Keycloak realm could not be read through the Admin API.", "Verify the realm exists and the temporary admin has read access."));

        if (!realmResponse.IsSuccessStatusCode)
        {
            operations.Add(SyncOperation(
                "keycloak-realm-unreadable",
                "realm",
                "realm",
                realm,
                "none",
                "blocked",
                "Realm cannot be inspected.",
                "The preview does not create or reimport realms in post-onboarding repair mode."));
            return;
        }

        await AddBlazorClientSyncOperationsAsync(client, baseUri, realm, blazorClientId, request, accessToken, operations, cancellationToken);
        await AddOfflineAccessSyncOperationsAsync(client, baseUri, realm, blazorClientId, accessToken, operations, cancellationToken);
        await AddApiClientSyncOperationsAsync(client, baseUri, realm, request.ApiClientId, accessToken, operations, cancellationToken);
    }

    private static async Task AddBlazorClientSyncOperationsAsync(
        HttpClient client,
        Uri baseUri,
        string realm,
        string blazorClientId,
        KeycloakRealmSyncPreviewRequestDto request,
        string accessToken,
        List<KeycloakRealmSyncOperationDto> operations,
        CancellationToken cancellationToken)
    {
        var blazorLookup = await FindClientAsync(client, baseUri, realm, blazorClientId, accessToken, cancellationToken);
        if (!blazorLookup.Success)
        {
            operations.Add(SyncOperation(
                "keycloak-blazor-client-lookup-failed",
                "client",
                "client",
                blazorClientId,
                "none",
                "blocked",
                "Blazor client lookup failed.",
                "Keycloak did not allow the temporary admin to list clients."));
            return;
        }

        if (string.IsNullOrWhiteSpace(blazorLookup.ClientUuid))
        {
            operations.Add(SyncOperation(
                "keycloak-blazor-client-add",
                "client",
                "client",
                blazorClientId,
                "add",
                "planned",
                "Create the confidential Blazor OIDC client.",
                "The configured Blazor client is missing from the realm.",
                ["Create confidential OIDC client", "Enable standard authorization-code flow", "Assign offline_access as an optional scope"],
                requiresBackupBeforeApply: true));
            return;
        }

        var representation = await GetClientRepresentationAsync(
            client,
            BuildUri(baseUri, "admin", "realms", realm, "clients", blazorLookup.ClientUuid),
            accessToken,
            cancellationToken);
        if (representation is null)
        {
            operations.Add(SyncOperation(
                "keycloak-blazor-client-unreadable",
                "client",
                "client",
                blazorClientId,
                "none",
                "blocked",
                "Blazor client representation cannot be read.",
                "The temporary admin needs permission to view client details."));
            return;
        }

        if (representation["standardFlowEnabled"]?.GetValue<bool>() != true)
        {
            operations.Add(SyncOperation(
                "keycloak-blazor-standard-flow-enable",
                "client",
                "client",
                blazorClientId,
                "update",
                "planned",
                "Enable standard authorization-code flow on the Blazor client.",
                "The Blazor client must support browser OIDC sign-in.",
                ["Set standardFlowEnabled=true"],
                requiresBackupBeforeApply: true));
        }

        if (!HasRefreshTokenSettings(representation))
        {
            operations.Add(SyncOperation(
                "keycloak-blazor-refresh-token-settings",
                "client",
                "client",
                blazorClientId,
                "update",
                "planned",
                "Add refresh-token/offline_access settings to the Blazor client.",
                "Offline sessions require offline_access and refresh token settings.",
                ["Assign offline_access client scope", "Set use.refresh.tokens=true"],
                requiresBackupBeforeApply: true));
        }

        AddMissingStringSetOperation(
            operations,
            "keycloak-blazor-redirect-uris",
            blazorClientId,
            "redirectUris",
            GetStringArray(representation["redirectUris"]),
            request.BlazorRedirectUris);
        AddMissingStringSetOperation(
            operations,
            "keycloak-blazor-web-origins",
            blazorClientId,
            "webOrigins",
            GetStringArray(representation["webOrigins"]),
            request.BlazorWebOrigins);
    }

    private static async Task AddOfflineAccessSyncOperationsAsync(
        HttpClient client,
        Uri baseUri,
        string realm,
        string blazorClientId,
        string accessToken,
        List<KeycloakRealmSyncOperationDto> operations,
        CancellationToken cancellationToken)
    {
        var offlineRole = await GetRealmRoleAsync(client, baseUri, realm, "offline_access", accessToken, cancellationToken);
        if (offlineRole is null)
        {
            operations.Add(SyncOperation(
                "keycloak-offline-access-role-add",
                "role",
                "realm-role",
                "offline_access",
                "add",
                "planned",
                "Create the offline_access realm role.",
                "The realm is missing the offline_access role required for offline sessions.",
                ["Add realm role offline_access"],
                requiresBackupBeforeApply: true));
        }

        var defaultRoleName = $"default-roles-{realm.ToLowerInvariant()}";
        var defaultRole = await GetRealmRoleAsync(client, baseUri, realm, defaultRoleName, accessToken, cancellationToken);
        if (defaultRole is null)
        {
            operations.Add(SyncOperation(
                "keycloak-default-role-missing",
                "role",
                "realm-role",
                defaultRoleName,
                "none",
                "blocked",
                "Default realm role is missing.",
                "The preview does not recreate Keycloak-managed default roles."));
        }
        else if (offlineRole is not null && defaultRole["id"]?.GetValue<string>() is { Length: > 0 } defaultRoleId)
        {
            var composites = await GetRealmRoleCompositesAsync(client, baseUri, realm, defaultRoleId, accessToken, cancellationToken);
            if (!composites.Any(role => string.Equals(role.Name, "offline_access", StringComparison.Ordinal)))
            {
                operations.Add(SyncOperation(
                    "keycloak-default-role-offline-access-add",
                    "role-composite",
                    "realm-role",
                    defaultRoleName,
                    "update",
                    "planned",
                    "Add offline_access to the default realm role composites.",
                    "New users need the offline_access composite for offline sessions.",
                    ["Add offline_access composite"],
                    requiresBackupBeforeApply: true));
            }
        }

        var scopeId = await FindClientScopeIdAsync(client, baseUri, realm, "offline_access", accessToken, cancellationToken);
        if (string.IsNullOrWhiteSpace(scopeId))
        {
            operations.Add(SyncOperation(
                "keycloak-offline-access-scope-add",
                "client-scope",
                "client-scope",
                "offline_access",
                "add",
                "planned",
                "Create the offline_access client scope.",
                "The realm is missing the offline_access client scope required by the Blazor client.",
                ["Add client scope offline_access", "Map offline_access realm role"],
                requiresBackupBeforeApply: true));
            return;
        }

        var blazorLookup = await FindClientAsync(client, baseUri, realm, blazorClientId, accessToken, cancellationToken);
        if (blazorLookup.Success && !string.IsNullOrWhiteSpace(blazorLookup.ClientUuid))
        {
            var optionalScopes = await GetClientScopesAsync(client, baseUri, realm, blazorLookup.ClientUuid, "optional-client-scopes", accessToken, cancellationToken);
            var defaultScopes = await GetClientScopesAsync(client, baseUri, realm, blazorLookup.ClientUuid, "default-client-scopes", accessToken, cancellationToken);
            var hasOfflineScope = optionalScopes.Concat(defaultScopes).Any(scope => string.Equals(scope.Name, "offline_access", StringComparison.Ordinal));
            if (!hasOfflineScope)
            {
                operations.Add(SyncOperation(
                    "keycloak-blazor-offline-access-scope-add",
                    "client-scope",
                    "client",
                    blazorClientId,
                    "update",
                    "planned",
                    "Assign offline_access to the Blazor client.",
                    "The Blazor client is missing the offline_access client scope.",
                    ["Add offline_access as an optional client scope"],
                    requiresBackupBeforeApply: true));
            }
        }

        var mappings = await GetClientScopeRealmMappingsAsync(client, baseUri, realm, scopeId, accessToken, cancellationToken);
        if (!mappings.Any(role => string.Equals(role.Name, "offline_access", StringComparison.Ordinal)))
        {
            operations.Add(SyncOperation(
                "keycloak-offline-access-scope-mapping-add",
                "scope-mapping",
                "client-scope",
                "offline_access",
                "update",
                "planned",
                "Map the offline_access realm role to the offline_access client scope.",
                "The client scope does not currently issue the offline_access role.",
                ["Add offline_access realm-role mapping"],
                requiresBackupBeforeApply: true));
        }
    }

    private static async Task AddApiClientSyncOperationsAsync(
        HttpClient client,
        Uri baseUri,
        string realm,
        string? apiClientId,
        string accessToken,
        List<KeycloakRealmSyncOperationDto> operations,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(apiClientId))
            return;

        var lookup = await FindClientAsync(client, baseUri, realm, apiClientId, accessToken, cancellationToken);
        if (!lookup.Success)
        {
            operations.Add(SyncOperation(
                "keycloak-api-client-lookup-failed",
                "client",
                "client",
                apiClientId,
                "none",
                "blocked",
                "API client lookup failed.",
                "Keycloak did not allow the temporary admin to list clients."));
            return;
        }

        if (string.IsNullOrWhiteSpace(lookup.ClientUuid))
        {
            operations.Add(SyncOperation(
                "keycloak-api-client-add",
                "client",
                "client",
                apiClientId,
                "add",
                "planned",
                "Create the API audience client.",
                "The configured API client is missing from the realm.",
                ["Create bearer-only API client", "Reserve audience mapper contract for access tokens"],
                requiresBackupBeforeApply: true));
        }
    }

    private static void AddMissingStringSetOperation(
        List<KeycloakRealmSyncOperationDto> operations,
        string operationId,
        string clientId,
        string fieldName,
        IReadOnlyList<string> currentValues,
        IReadOnlyList<string> desiredValues)
    {
        var missingValues = desiredValues
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Except(currentValues, StringComparer.Ordinal)
            .ToArray();
        if (missingValues.Length == 0)
            return;

        operations.Add(SyncOperation(
            operationId,
            "client",
            "client",
            clientId,
            "update",
            "planned",
            $"Add missing {fieldName} values to the Blazor client.",
            "The sync plan is additive and does not remove operator-managed values.",
            missingValues.Select(value => $"Add {fieldName} value {value}").ToArray(),
            requiresBackupBeforeApply: true));
    }

    private static IReadOnlyList<string> GetStringArray(JsonNode? node)
    {
        return node is JsonArray array
            ? array.Select(value => value?.GetValue<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).Cast<string>().ToArray()
            : [];
    }

    private static KeycloakRealmSyncOperationDto SyncOperation(
        string operationId,
        string category,
        string targetType,
        string target,
        string action,
        string status,
        string summary,
        string reason,
        IReadOnlyList<string>? changes = null,
        bool requiresBackupBeforeApply = false) => new()
        {
            OperationId = operationId,
            Category = category,
            TargetType = targetType,
            Target = target,
            Action = action,
            Status = status,
            Summary = summary,
            Reason = reason,
            Changes = changes ?? [],
            RequiresBackupBeforeApply = requiresBackupBeforeApply
        };

    private static KeycloakRealmSyncPlanDto CompleteSyncPlan(
        KeycloakRealmSyncPlanDto plan,
        IReadOnlyList<KeycloakRealmSyncOperationDto> operations,
        IReadOnlyList<KeycloakRealmDoctorCheckDto> diagnostics)
    {
        plan.Operations = operations;
        plan.Diagnostics = diagnostics;
        plan.RequiresBackupBeforeApply = operations.Any(operation => operation.RequiresBackupBeforeApply);
        plan.Status = HasBlockingCheck(diagnostics) || operations.Any(operation => string.Equals(operation.Status, "blocked", StringComparison.OrdinalIgnoreCase))
            ? "blocked"
            : operations.Any(operation => string.Equals(operation.Status, "planned", StringComparison.OrdinalIgnoreCase))
                ? "changes-planned"
                : "up-to-date";
        plan.Message = plan.Status switch
        {
            "up-to-date" => "Keycloak realm sync preview did not find additive drift.",
            "changes-planned" => "Keycloak realm sync preview found additive repair operations. Review and back up Keycloak before applying in a future step.",
            _ => "Keycloak realm sync preview is blocked. Resolve the blocking checks and retry."
        };

        return plan;
    }

    private static KeycloakBootstrapResultDto Success(KeycloakBootstrapRequestDto request, bool realmCreated)
    {
        return new KeycloakBootstrapResultDto
        {
            Success = true,
            Mode = request.Mode,
            Realm = request.Realm,
            BlazorClientId = request.BlazorClientId,
            ApiClientId = request.ApiClientId,
            RealmCreated = realmCreated
        };
    }

    private static KeycloakBootstrapResultDto Failure(
        KeycloakBootstrapRequestDto request,
        string failureCode,
        string message,
        bool realmCreated = false,
        bool blazorClientUpdated = false)
    {
        return new KeycloakBootstrapResultDto
        {
            Success = false,
            Message = message,
            FailureCode = failureCode,
            Mode = request.Mode,
            Realm = request.Realm,
            BlazorClientId = request.BlazorClientId,
            ApiClientId = request.ApiClientId,
            RealmCreated = realmCreated,
            BlazorClientUpdated = blazorClientUpdated
        };
    }

    private sealed class KeycloakTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
    }

    private sealed record KeycloakRealmRepresentation(string Realm, bool Enabled);

    private sealed class KeycloakClientLookupResult
    {
        public string Id { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
    }

    private sealed class KeycloakClientScopeLookupResult
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    private sealed class KeycloakRoleLookupResult
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    private sealed class KeycloakClientRepresentation
    {
        public string ClientId { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public bool Enabled { get; init; } = true;
        public string Protocol { get; init; } = "openid-connect";
        public bool PublicClient { get; init; }
        public bool StandardFlowEnabled { get; init; }
        public bool DirectAccessGrantsEnabled { get; init; }
        public bool ServiceAccountsEnabled { get; init; }
        public bool BearerOnly { get; init; }

        public static KeycloakClientRepresentation CreateConfidentialOidc(string clientId) => new()
        {
            ClientId = clientId,
            Name = clientId,
            PublicClient = false,
            StandardFlowEnabled = true,
            DirectAccessGrantsEnabled = false,
            ServiceAccountsEnabled = false,
            BearerOnly = false
        };

        public static KeycloakClientRepresentation CreateBearerOnly(string clientId) => new()
        {
            ClientId = clientId,
            Name = clientId,
            PublicClient = false,
            StandardFlowEnabled = false,
            DirectAccessGrantsEnabled = false,
            ServiceAccountsEnabled = false,
            BearerOnly = true
        };
    }

    private sealed record ClientSecretUpdateResult(bool Success, string FailureCode, string Message)
    {
        public static ClientSecretUpdateResult Succeeded() => new(true, string.Empty, string.Empty);

        public static ClientSecretUpdateResult Failure(string failureCode, string message) => new(false, failureCode, message);
    }

    private sealed record ClientLookupResult(bool Success, string? ClientUuid)
    {
        public static ClientLookupResult Succeeded(string? clientUuid) => new(true, clientUuid);

        public static ClientLookupResult Failure() => new(false, null);
    }
}

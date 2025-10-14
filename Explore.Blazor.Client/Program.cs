using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Explore.Blazor.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddMudServices();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<HttpClient>();

// wanted to have webassembly as client for keycloak but went with blazor server the correct choice so these are not needed so commented
//var keycloakConfig = builder.Configuration.GetSection("Keycloak");

//var authority = keycloakConfig["Authority"];
//var clientId = keycloakConfig["ClientId"];
//var realm = keycloakConfig["Realm"];

//if (string.IsNullOrEmpty(authority) || string.IsNullOrEmpty(clientId))
//{
//    throw new InvalidOperationException("Keycloak configuration is missing");
//}

//builder.Services.AddOidcAuthentication(options =>
//{
//    options.ProviderOptions.Authority = authority;
//    options.ProviderOptions.ClientId = clientId;
//    options.ProviderOptions.ResponseType = "code";
//    options.ProviderOptions.DefaultScopes.Add("openid");
//    options.ProviderOptions.DefaultScopes.Add("profile");
//    options.ProviderOptions.DefaultScopes.Add("email");
//});

await builder.Build().RunAsync();

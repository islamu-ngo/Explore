namespace Explore.API.Extensions;

using Explore.API.Hateoas;
using Explore.API.Middleware;
using Explore.Application.Contracts.Hateoas;

/// <summary>
/// Extension methods for registering HATEOAS services.
/// </summary>
public static class HateoasServiceExtensions
{
    /// <summary>
    /// Adds HATEOAS services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHateoas(this IServiceCollection services)
    {
        // Register core HATEOAS infrastructure
        services.AddScoped<IHateoasLinkGenerator, HateoasLinkGenerator>();
        services.AddScoped<IHateoasAuthorizationEvaluator, HateoasAuthorizationEvaluator>();

        // Resource assemblers are registered by AddHateoasAssemblers
        // or can be registered individually

        return services;
    }

    /// <summary>
    /// Adds HATEOAS middleware to the application pipeline.
    /// Should be called early in the pipeline, before UseRouting.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseHateoas(this IApplicationBuilder app)
    {
        // Add Prefer header middleware for RFC 7240 support
        app.UsePreferHeader();

        return app;
    }

    /// <summary>
    /// Registers a resource assembler with its link policies.
    /// </summary>
    /// <typeparam name="TDto">The detail DTO type.</typeparam>
    /// <typeparam name="TListDto">The list DTO type.</typeparam>
    /// <typeparam name="TAssembler">The assembler implementation type.</typeparam>
    /// <typeparam name="TDetailPolicy">The detail link policy type.</typeparam>
    /// <typeparam name="TCollectionPolicy">The collection link policy type.</typeparam>
    public static IServiceCollection AddResourceAssembler<TDto, TListDto, TAssembler, TDetailPolicy, TCollectionPolicy>(
        this IServiceCollection services)
        where TDto : class
        where TListDto : class
        where TAssembler : class, IResourceAssembler<TDto, TListDto>
        where TDetailPolicy : class, ILinkPolicy<TDto>
        where TCollectionPolicy : class, ICollectionLinkPolicy<TListDto>
    {
        services.AddScoped<ILinkPolicy<TDto>, TDetailPolicy>();
        services.AddScoped<ICollectionLinkPolicy<TListDto>, TCollectionPolicy>();
        services.AddScoped<IResourceAssembler<TDto, TListDto>, TAssembler>();

        return services;
    }

    /// <summary>
    /// Registers a resource assembler for entities with a single DTO type.
    /// </summary>
    /// <typeparam name="TDto">The DTO type.</typeparam>
    /// <typeparam name="TAssembler">The assembler implementation type.</typeparam>
    /// <typeparam name="TDetailPolicy">The detail link policy type.</typeparam>
    /// <typeparam name="TCollectionPolicy">The collection link policy type.</typeparam>
    public static IServiceCollection AddResourceAssembler<TDto, TAssembler, TDetailPolicy, TCollectionPolicy>(
        this IServiceCollection services)
        where TDto : class
        where TAssembler : class, IResourceAssembler<TDto>
        where TDetailPolicy : class, ILinkPolicy<TDto>
        where TCollectionPolicy : class, ICollectionLinkPolicy<TDto>
    {
        services.AddScoped<ILinkPolicy<TDto>, TDetailPolicy>();
        services.AddScoped<ICollectionLinkPolicy<TDto>, TCollectionPolicy>();
        services.AddScoped<IResourceAssembler<TDto>, TAssembler>();
        services.AddScoped<IResourceAssembler<TDto, TDto>, TAssembler>();

        return services;
    }
}

// ABOUTME: Manages tenant module capability records (Core, Islamic, Tech modules).
// ABOUTME: Extracted from InstanceGovernanceSettingService per Guardrail 1: capabilities ≠ settings.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Domain.Modules;

namespace Explore.Application.Services;

public class ModuleCapabilityService : IModuleCapabilityService
{
    private const string CoreModuleKey = "Mod_Core";
    private const string IslamicModuleKey = "Mod_Islamic";
    private const string TechModuleKey = "Mod_Tech";

    private readonly ITenantCapabilityRepository _tenantCapabilityRepository;
    private readonly IModuleDefinitionRepository _moduleDefinitionRepository;

    public ModuleCapabilityService(
        ITenantCapabilityRepository tenantCapabilityRepository,
        IModuleDefinitionRepository moduleDefinitionRepository)
    {
        _tenantCapabilityRepository = tenantCapabilityRepository;
        _moduleDefinitionRepository = moduleDefinitionRepository;
    }

    public async Task SyncTenantModuleCapabilitiesAsync(Guid tenantId, bool enableIslamic, bool enableTech, Guid? actorUserId)
    {
        await UpsertCapabilityAsync(tenantId, CoreModuleKey, true, actorUserId);
        await UpsertCapabilityAsync(tenantId, IslamicModuleKey, enableIslamic, actorUserId);
        await UpsertCapabilityAsync(tenantId, TechModuleKey, enableTech, actorUserId);
    }

    private async Task UpsertCapabilityAsync(Guid tenantId, string moduleKey, bool isEnabled, Guid? actorUserId)
    {
        var module = await _moduleDefinitionRepository.GetByKey(moduleKey);
        if (module == null) return;

        var existing = await _tenantCapabilityRepository.GetByTenantAndModuleKey(tenantId, moduleKey);
        if (existing == null)
        {
            await _tenantCapabilityRepository.Create(new TenantCapability
            {
                TenantId = tenantId,
                Tenant = null!,
                ModuleId = module.Id,
                Module = null!,
                IsEnabled = isEnabled,
                EnabledAt = DateTime.UtcNow,
                EnabledBy = actorUserId
            });
            return;
        }

        existing.IsEnabled = isEnabled;
        if (isEnabled && existing.EnabledAt == default)
        {
            existing.EnabledAt = DateTime.UtcNow;
            existing.EnabledBy = actorUserId;
        }
        await _tenantCapabilityRepository.Update(existing);
    }
}

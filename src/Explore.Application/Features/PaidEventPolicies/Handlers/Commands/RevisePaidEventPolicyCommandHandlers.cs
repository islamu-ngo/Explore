// ABOUTME: Handles immutable paid-event policy revisions for instance and tenant scopes.
// ABOUTME: Applies tenant narrowing rules inside the serializable Application unit-of-work boundary.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.PaidEventPolicies;
using Explore.Application.Features.PaidEventPolicies.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Services.Registration;
using Explore.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace Explore.Application.Features.PaidEventPolicies.Handlers.Commands;

public sealed class ReviseInstancePaidEventPolicyCommandHandler(IPaidEventPolicyRepository policies, IUnitOfWork unitOfWork)
    : IRequestHandler<ReviseInstancePaidEventPolicyCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(ReviseInstancePaidEventPolicyCommand request, CancellationToken cancellationToken) =>
        await ReviseAsync(null, request.Policy, policies, unitOfWork, cancellationToken);

    internal static async Task<BaseCommandResponse<Guid>> ReviseAsync(
        Guid? tenantId,
        RevisePaidEventPolicyDto request,
        IPaidEventPolicyRepository policies,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        try
        {
            var validation = await new RevisePaidEventPolicyCommandValidator().ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                return Failure("paid_event_policy_validation_failed", validation.Errors[0].ErrorMessage);
            }

            Guid revisionId = await unitOfWork.ExecuteSerializableAsync(async token =>
            {
                PaidEventPolicyVersion? instancePolicy = await policies.GetActiveInstanceAsync(token);
                if (instancePolicy is null || !instancePolicy.IsActive)
                {
                    throw new InvalidOperationException("Active instance paid-event policy is required.");
                }

                PaidEventPolicyVersion revision;
                if (tenantId is { } tenant)
                {
                    PaidEventPolicyVersion? currentTenantPolicy = await policies.GetActiveTenantAsync(tenant, token);
                    PaidEventPolicyVersion candidate = currentTenantPolicy is null
                        ? CreateTenant(tenant, request)
                        : CreateRevision(currentTenantPolicy, request);
                    PaidEventPolicyRules.ValidateTenantPolicy(instancePolicy, candidate);
                    revision = candidate;
                }
                else
                {
                    revision = CreateRevision(instancePolicy, request);
                }

                await policies.AddAsync(revision, token);
                await policies.SaveChangesAsync(token);
                return revision.Id;
            }, cancellationToken);

            return Success(revisionId, "Paid-event policy revised.");
        }
        catch (ArgumentException exception)
        {
            return Failure("paid_event_policy_validation_failed", exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Failure("paid_event_policy_validation_failed", exception.Message);
        }
    }

    private static PaidEventPolicyVersion CreateTenant(Guid tenantId, RevisePaidEventPolicyDto request) => PaidEventPolicyVersion.CreateTenant(
        tenantId,
        request.IsPaymentsEnabled,
        OrganizerKinds(request),
        request.RequiresLocalVerification,
        request.AllowedCurrencyCodes,
        request.DefaultCurrencyCode,
        RefundProtections(request),
        RiskLimits(request),
        request.RequiresFirstPaidEventReview,
        request.FarFutureReviewThresholdDays);

    private static PaidEventPolicyVersion CreateRevision(PaidEventPolicyVersion current, RevisePaidEventPolicyDto request) => current.CreateRevision(
        request.IsPaymentsEnabled,
        OrganizerKinds(request),
        request.RequiresLocalVerification,
        request.AllowedCurrencyCodes,
        request.DefaultCurrencyCode,
        RefundProtections(request),
        RiskLimits(request),
        request.RequiresFirstPaidEventReview,
        request.FarFutureReviewThresholdDays);

    private static IEnumerable<ActorTypeEnum> OrganizerKinds(RevisePaidEventPolicyDto request) =>
        request.AllowedOrganizerKindIds.Select(id => (ActorTypeEnum)id);

    private static IEnumerable<PaidEventRefundProtection> RefundProtections(RevisePaidEventPolicyDto request) =>
        request.RefundProtectionIds.Select(id => (PaidEventRefundProtection)id);

    private static IEnumerable<PaidEventPolicyCurrencyRiskLimit> RiskLimits(RevisePaidEventPolicyDto request) =>
        request.CurrencyRiskLimits.Select(limit => PaidEventPolicyCurrencyRiskLimit.Create(
            limit.CurrencyCode,
            limit.PerEventSalesCeilingMinor,
            limit.PerEventSalesCountCeiling,
            limit.RollingOrganizerSalesCeilingMinor,
            limit.RollingOrganizerSalesCountCeiling,
            limit.RollingOrganizerWindowDays,
            limit.HighValueReviewThresholdMinor));

    private static BaseCommandResponse<Guid> Success(Guid id, string message) => new() { Id = id, Success = true, Message = message };

    private static BaseCommandResponse<Guid> Failure(string code, string message) => new()
    {
        Success = false,
        FailureCode = code,
        Message = "Paid-event policy is invalid.",
        Errors = [message]
    };
}

public sealed class RevisePaidEventPolicyCommandValidator : AbstractValidator<RevisePaidEventPolicyDto>
{
    public RevisePaidEventPolicyCommandValidator()
    {
        RuleFor(policy => policy).NotNull();
        When(policy => policy is not null, () =>
        {
            RuleFor(policy => policy.AllowedOrganizerKindIds).NotEmpty();
            RuleForEach(policy => policy.AllowedOrganizerKindIds).Must(id => Enum.IsDefined(typeof(ActorTypeEnum), id));
            RuleFor(policy => policy.AllowedCurrencyCodes).NotEmpty();
            RuleForEach(policy => policy.AllowedCurrencyCodes).NotEmpty().MaximumLength(3);
            RuleFor(policy => policy.DefaultCurrencyCode).MaximumLength(3);
            RuleFor(policy => policy.RefundProtectionIds).NotEmpty();
            RuleForEach(policy => policy.RefundProtectionIds).Must(id => Enum.IsDefined(typeof(PaidEventRefundProtection), id));
            RuleForEach(policy => policy.CurrencyRiskLimits).SetValidator(new PaidEventPolicyCurrencyRiskLimitDtoValidator());
            RuleFor(policy => policy.FarFutureReviewThresholdDays).GreaterThan(0).When(policy => policy.FarFutureReviewThresholdDays.HasValue);
        });
    }
}

internal sealed class PaidEventPolicyCurrencyRiskLimitDtoValidator : AbstractValidator<PaidEventPolicyCurrencyRiskLimitDto>
{
    public PaidEventPolicyCurrencyRiskLimitDtoValidator()
    {
        RuleFor(limit => limit.CurrencyCode).NotEmpty().MaximumLength(3);
        RuleFor(limit => limit.PerEventSalesCeilingMinor).GreaterThan(0).When(limit => limit.PerEventSalesCeilingMinor.HasValue);
        RuleFor(limit => limit.PerEventSalesCountCeiling).GreaterThan(0).When(limit => limit.PerEventSalesCountCeiling.HasValue);
        RuleFor(limit => limit.RollingOrganizerSalesCeilingMinor).GreaterThan(0).When(limit => limit.RollingOrganizerSalesCeilingMinor.HasValue);
        RuleFor(limit => limit.RollingOrganizerSalesCountCeiling).GreaterThan(0).When(limit => limit.RollingOrganizerSalesCountCeiling.HasValue);
        RuleFor(limit => limit.RollingOrganizerWindowDays).GreaterThan(0).When(limit => limit.RollingOrganizerWindowDays.HasValue);
        RuleFor(limit => limit.HighValueReviewThresholdMinor).GreaterThan(0).When(limit => limit.HighValueReviewThresholdMinor.HasValue);
    }
}

public sealed class ReviseTenantPaidEventPolicyCommandHandler(IPaidEventPolicyRepository policies, IUnitOfWork unitOfWork)
    : IRequestHandler<ReviseTenantPaidEventPolicyCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(ReviseTenantPaidEventPolicyCommand request, CancellationToken cancellationToken) =>
        request.TenantId == Guid.Empty
            ? new BaseCommandResponse<Guid> { Success = false, FailureCode = "paid_event_policy_validation_failed", Message = "Paid-event policy is invalid.", Errors = ["Tenant is required."] }
            : await ReviseInstancePaidEventPolicyCommandHandler.ReviseAsync(request.TenantId, request.Policy, policies, unitOfWork, cancellationToken);
}

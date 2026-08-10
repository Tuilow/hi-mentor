using MediatR;

namespace Tuilow.Finance.Application.Queries.AdminListCreatorOnboardings;

public sealed record AdminListCreatorOnboardingsQuery(int Skip = 0, int Take = 50) : IRequest<IReadOnlyCollection<AdminCreatorOnboardingItem>>;

public sealed record AdminCreatorOnboardingItem(
    Guid Id, Guid CreatorId, string CreatorName, string CreatorEmail,
    string Status, string? CpfCnpjMasked, int PendingDocumentsCount,
    DateTime? ApprovedAt, string? RejectionReason
);

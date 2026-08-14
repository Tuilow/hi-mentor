using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.SharedKernel.Application.Interfaces;
using HiMentor.Catalog.Domain.Interfaces;
using HiMentor.Sales.Application.Interfaces;
using HiMentor.Sales.Domain.Interfaces;
using MediatR;
using CoursePurchaseEntity = HiMentor.Sales.Domain.Entities.CoursePurchase;

namespace HiMentor.Sales.Application.Commands.PurchaseCourse;

public sealed class PurchaseCourseCommandHandler(
    ICourseRepository courseRepository,
    ICoursePurchaseRepository coursePurchaseRepository,
    IUserProvisioningService userProvisioningService,
    IPaymentService paymentService,
    IMarketplacePaymentService marketplacePaymentService,
    ICreatorPaymentAccountLookup creatorPaymentAccountLookup,
    IMarketplaceFeatureFlag marketplaceFeatureFlag,
    IUnitOfWork uow
) : IRequestHandler<PurchaseCourseCommand, PurchaseCourseResponse>
{
    public async Task<PurchaseCourseResponse> Handle(PurchaseCourseCommand request, CancellationToken ct)
    {
        var course = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

        if (course.IsFree)
            throw new BusinessException("Este curso é gratuito — não é necessário comprá-lo, basta se matricular.");

        // Checkout anônimo: sem login, localiza ou cria a conta pelo e-mail informado. A conta
        // nova é persistida logo abaixo (ver comentário no SaveChangesAsync seguinte) — se o
        // pagamento falhar depois disso, só a compra fica pendente/inexistente; a conta em si
        // já existe, igual a alguém que se registra e some antes de pagar.
        var studentId = request.StudentId
            ?? await userProvisioningService.FindOrCreateStudentAsync(request.CustomerEmail, request.CustomerName, ct);

        // Bug encontrado em teste manual (mesmo em SubscribeToCourseCommandHandler): a FK real
        // "FK_course_purchases_users_StudentId" existe só no Postgres (migration
        // AddUserForeignKeys), sem relação equivalente configurada no EF — de propósito, pra não
        // dar navegação C# entre módulos. Sem isso, o EF não garante que o User novo seja
        // inserido antes do CoursePurchase na mesma SaveChanges, e o INSERT pode sair na ordem
        // errada (viola a FK, 23503). Este SaveChanges garante a ordem; é no-op se o usuário já
        // existia.
        await uow.SaveChangesAsync(ct);

        if (await coursePurchaseRepository.HasConfirmedPurchaseAsync(studentId, request.CourseId, ct))
            throw new BusinessException("Você já comprou este curso.");

        // Onboarding financeiro (subconta Asaas/BaaS, novo modelo) aprovado é obrigatório para
        // TODOS os criadores, sem exceção -- decisão explícita: corte imediato, sem manter o
        // fallback Legacy (conta da própria HiMentor) como rede de segurança para quem vendia pelo
        // modelo antigo. Na prática isso já deveria ser inatingível, porque a publicação do curso
        // (PublishCourseCommandHandler/PublishProductCommandHandler) exige a mesma aprovação --
        // este check aqui é defesa em profundidade, para nunca deixar uma compra cair
        // silenciosamente no caminho Legacy abaixo.
        if (!await creatorPaymentAccountLookup.HasApprovedFinancialOnboardingAsync(course.InstructorId, ct))
            throw new BusinessException(
                "Este curso está temporariamente indisponível para compra: o criador ainda não concluiu o onboarding financeiro.");

        // Marketplace de split: só entra se o flag global estiver ligado E o criador do curso
        // tiver uma conta Asaas própria conectada e validada (CanSell). Qualquer outro caso cai
        // no modelo Legacy abaixo, sem nenhuma mudança de comportamento — criadores que ainda
        // não conectaram uma conta continuam vendendo normalmente pela conta da HiMentor.
        var marketplaceAccount = marketplaceFeatureFlag.IsEnabled
            ? await creatorPaymentAccountLookup.GetMarketplaceAccountAsync(course.InstructorId, ct)
            : null;

        if (marketplaceAccount is { CanSell: true })
        {
            var commissionPercentage = await creatorPaymentAccountLookup
                .GetEffectiveCommissionPercentageAsync(course.InstructorId, ct);

            var marketplaceCustomer = await marketplacePaymentService.CreateOrGetCustomerAsync(
                course.InstructorId, studentId,
                new(request.CustomerName, request.CustomerEmail, request.CpfCnpj, request.Phone), ct);

            var marketplaceCharge = await marketplacePaymentService.CreateChargeAsync(
                course.InstructorId,
                new(marketplaceCustomer.AsaasCustomerId, course.Price.Amount, $"Curso: {course.Title}", course.Id.ToString()),
                commissionPercentage, ct);

            var marketplacePurchase = CoursePurchaseEntity.CreateForMarketplace(
                studentId, course.Id, course.InstructorId, course.Price.Amount,
                marketplaceAccount.CreatorAsaasAccountId, marketplaceCustomer.AsaasCustomerId,
                marketplaceCharge.AsaasPaymentId, commissionPercentage);

            await coursePurchaseRepository.AddAsync(marketplacePurchase, ct);
            await uow.SaveChangesAsync(ct);

            return new PurchaseCourseResponse(marketplacePurchase.Id, marketplaceCharge.AsaasPaymentId, marketplaceCharge.InvoiceUrl);
        }

        // Legacy — cobrança na própria conta Asaas da HiMentor. Mecânica de cobrança inalterada, mas
        // deixa de ser um "fallback silencioso": só é alcançado depois do guard de onboarding
        // financeiro acima, ou seja, só roda para criadores já aprovados no novo modelo cujo curso,
        // por algum motivo, não usa o caminho de marketplace (ex.: feature flag desligada).
        var customer = await paymentService.CreateOrGetCustomerAsync(
            new(request.CustomerName, request.CustomerEmail, request.CpfCnpj, request.Phone), ct);

        var charge = await paymentService.CreateChargeAsync(
            new(customer.Id, course.Price.Amount, $"Curso: {course.Title}", course.Id.ToString()), ct);

        var purchase = CoursePurchaseEntity.Create(
            studentId, course.Id, course.InstructorId, course.Price.Amount,
            customer.Id, charge.Id);

        await coursePurchaseRepository.AddAsync(purchase, ct);
        await uow.SaveChangesAsync(ct);

        return new PurchaseCourseResponse(purchase.Id, charge.Id, charge.InvoiceUrl);
    }
}

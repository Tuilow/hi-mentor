using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.Catalog.Domain.Interfaces;
using HiMentor.CreatorStudio.Domain.Interfaces;
using HiMentor.Finance.Domain.Interfaces;
using HiMentor.Learning.Domain.Interfaces;
using HiMentor.Sales.Domain.Enums;
using HiMentor.Sales.Domain.Interfaces;
using MediatR;

namespace HiMentor.CreatorStudio.Application.Queries.GetProductDashboard;

/// <summary>
/// Compõe dados de Catalog (views), CreatorStudio (leads), Learning (alunos), Sales (vendas)
/// e Finance (percentual de comissão vigente) — nenhuma dessas regras é duplicada, só somamos
/// o que cada módulo já expõe pelos próprios repositórios.
///
/// Comissão/receita líquida usam o percentual ATUAL da plataforma como estimativa — o extrato
/// exato, transação a transação (com o percentual histórico vigente em cada venda), já existe
/// no módulo Finance (WalletTransaction.AppliedFeePercentage) na carteira agregada do criador;
/// aqui o objetivo é uma visão rápida por produto, não substituir aquele extrato.
/// </summary>
public sealed class GetProductDashboardQueryHandler(
    ICourseRepository courseRepository,
    ILeadRepository leadRepository,
    IEnrollmentRepository enrollmentRepository,
    ICoursePurchaseRepository coursePurchaseRepository,
    IPlatformFeeConfigurationRepository platformFeeConfigurationRepository
) : IRequestHandler<GetProductDashboardQuery, ProductDashboardResponse>
{
    public async Task<ProductDashboardResponse> Handle(GetProductDashboardQuery request, CancellationToken ct)
    {
        var course = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

        if (course.InstructorId != request.InstructorId)
            throw new ForbiddenException("Apenas o criador pode ver o dashboard deste produto.");

        var leadsCount = await leadRepository.CountByCourseAsync(request.CourseId, ct);
        var studentsCount = await enrollmentRepository.CountByCourseAsync(request.CourseId, ct);

        var purchases = await coursePurchaseRepository.GetByCreatorAsync(course.InstructorId, null, null, ct);
        var confirmedPurchases = purchases
            .Where(p => p.CourseId == request.CourseId && p.Status == CoursePurchaseStatus.Confirmed)
            .ToList();

        var revenue = confirmedPurchases.Sum(p => p.Amount.Amount);
        var salesCount = confirmedPurchases.Count;

        var feeConfig = await platformFeeConfigurationRepository.GetActiveAsync(ct);
        var feePercentage = feeConfig?.Percentage ?? 0m;
        var platformFee = Math.Round(revenue * feePercentage / 100m, 2);
        var netRevenue = revenue - platformFee;

        return new ProductDashboardResponse(
            course.Id, course.Title, course.Slug.Value, course.ViewCount, leadsCount, studentsCount, salesCount,
            revenue, platformFee, netRevenue, feePercentage);
    }
}

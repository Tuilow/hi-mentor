using HiMentor.SharedKernel.Domain.Common;

namespace HiMentor.Learning.Domain.Entities;

/// <summary>
/// Código de acesso que libera matrícula em um programa sem passar pelo checkout (aluno que
/// ainda não tem nenhum programa digita um código recebido do criador/suporte — ver
/// RedeemAccessCodeCommandHandler, Learning.Application). Emitido pelo painel Admin da
/// plataforma (ver GenerateAccessCodeCommandHandler/AdminAccessCodesController) — não existe
/// tela de emissão para o próprio Creator/Mentor nesta primeira versão.
///
/// Não referencia Enrollment diretamente: quem cria a matrícula é o command handler, do mesmo
/// jeito que já faz para compra confirmada (CoursePurchaseConfirmedEventHandler) e matrícula
/// direta (EnrollStudentCommandHandler) — Enrollment.Create continua sendo o único ponto de
/// criação de matrícula, evitando duas fontes de verdade para "como uma matrícula nasce".
/// </summary>
public sealed class AccessCode : AggregateRoot
{
    // Sem O/0 e I/1 (fáceis de confundir ao digitar um código curto à mão).
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 8;

    private readonly List<AccessCodeRedemption> _redemptions = [];

    public string Code { get; private set; } = string.Empty;
    public Guid CourseId { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    /// <summary>Quantidade máxima de ativações. Null = ilimitado.</summary>
    public int? MaxUses { get; private set; }
    public int UsesCount { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<AccessCodeRedemption> Redemptions => _redemptions.AsReadOnly();

    private AccessCode() { }

    public static AccessCode Generate(Guid courseId, Guid createdByUserId, int? maxUses, DateTime? expiresAt)
    {
        // InvalidOperationException aqui pelo mesmo motivo de Course.Publish (Catalog.Domain):
        // violação de regra de negócio que DEVE chegar ao usuário como mensagem legível — ver
        // ExceptionHandlingMiddleware (Host), que mapeia InvalidOperationException para 422 com
        // a Message original, ao contrário do fallback genérico 500 para exceções não mapeadas.
        if (maxUses is <= 0)
            throw new InvalidOperationException("A quantidade máxima de usos deve ser maior que zero.");
        if (expiresAt is { } exp && exp <= DateTime.UtcNow)
            throw new InvalidOperationException("A data de expiração deve ser no futuro.");

        return new AccessCode
        {
            Code = GenerateCode(),
            CourseId = courseId,
            CreatedByUserId = createdByUserId,
            MaxUses = maxUses,
            ExpiresAt = expiresAt,
            IsActive = true
        };
    }

    private static string GenerateCode()
    {
        Span<char> buffer = stackalloc char[CodeLength];
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = Alphabet[Random.Shared.Next(Alphabet.Length)];
        return new string(buffer);
    }

    /// <summary>
    /// Resgata o código para o aluno informado. Retorna o AccessCodeRedemption criado (para o
    /// caller registrar explicitamente como Added — mesmo padrão de Enrollment.TrackLessonProgress,
    /// evita DbUpdateConcurrencyException quando o AccessCode pai já está tracked, ver achado do
    /// bug 1 do onboarding financeiro nesta mesma base).
    ///
    /// As mensagens abaixo são exibidas ao aluno como vieram (ExceptionHandlingMiddleware repassa
    /// InvalidOperationException.Message ao cliente) — texto e ordem seguem exatamente o pedido:
    /// código inválido/inativo, expirado, já usado por este aluno, sem mais usos disponíveis.
    /// </summary>
    public AccessCodeRedemption Redeem(Guid userId)
    {
        if (!IsActive)
            throw new InvalidOperationException("Este código não é válido.");
        if (ExpiresAt is { } exp && exp <= DateTime.UtcNow)
            throw new InvalidOperationException("Este código de acesso expirou.");
        if (_redemptions.Any(r => r.UserId == userId))
            throw new InvalidOperationException("Este código já foi utilizado.");
        if (MaxUses is { } max && UsesCount >= max)
            throw new InvalidOperationException("Este código não possui mais acessos disponíveis.");

        var redemption = AccessCodeRedemption.Create(Id, userId);
        _redemptions.Add(redemption);
        UsesCount++;
        Touch();
        return redemption;
    }

    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }
}

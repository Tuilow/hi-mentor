using Tuilow.SharedKernel.Domain.Common;

namespace Tuilow.Finance.Domain.Entities;

/// <summary>
/// Mapeia um aluno (StudentId) para o AsaasCustomerId dele DENTRO da conta Asaas de um creator
/// especifico. Necessario porque cada conta Asaas (a do creator) tem sua propria lista de
/// clientes, isolada das demais -- o mesmo aluno que compra cursos de dois creators diferentes
/// tem dois AsaasCustomerId distintos, um em cada conta. Sem este mapeamento nao haveria como
/// reaproveitar o cliente ja criado numa segunda compra do mesmo aluno com o mesmo creator.
/// </summary>
public sealed class CreatorAsaasCustomer : Entity
{
    public Guid CreatorAsaasAccountId { get; private set; }
    public Guid StudentId { get; private set; }
    public string AsaasCustomerId { get; private set; } = string.Empty;

    private CreatorAsaasCustomer() { }

    public static CreatorAsaasCustomer Create(Guid creatorAsaasAccountId, Guid studentId, string asaasCustomerId) => new()
    {
        CreatorAsaasAccountId = creatorAsaasAccountId,
        StudentId = studentId,
        AsaasCustomerId = asaasCustomerId
    };
}

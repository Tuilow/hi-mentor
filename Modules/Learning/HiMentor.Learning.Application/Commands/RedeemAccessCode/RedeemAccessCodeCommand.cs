using MediatR;

namespace HiMentor.Learning.Application.Commands.RedeemAccessCode;

/// <summary>
/// Ativação de acesso por código — aluno sem nenhum programa digita um código recebido do
/// criador/suporte (ver bloco "Tenho um código de acesso" no dashboard). Mesma ideia de
/// EnrollStudentCommand, mas a checagem de "pode entrar de graça" é a validade do AccessCode em
/// vez de preço do curso/assinatura. Ver AccessCode.Redeem (Domain) para as regras de
/// expiração/limite de usos/reuso indevido — sempre validadas aqui no backend, nunca só no cliente.
/// </summary>
public sealed record RedeemAccessCodeCommand(Guid UserId, string Code) : IRequest<RedeemAccessCodeResult>;

public sealed record RedeemAccessCodeResult(Guid EnrollmentId, Guid CourseId, string CourseTitle, string CourseSlug);

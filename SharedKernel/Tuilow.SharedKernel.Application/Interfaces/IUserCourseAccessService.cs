namespace Tuilow.SharedKernel.Application.Interfaces;

/// <summary>
/// ÚNICO serviço da plataforma que responde "o usuário X tem acesso para ASSISTIR ao curso Y
/// agora?". Antes desta sprint essa mesma pergunta era respondida de forma duplicada (e
/// ligeiramente diferente) em pelo menos três lugares: inline em
/// Streaming.Application.GetLessonPlayUrlQueryHandler, inline em
/// Channel.Application.GetPublicChannelQueryHandler (só olhava matrícula, sem considerar compra/
/// assinatura diretamente) e em Learning.Application.Interfaces.ICourseAccessChecker (só o
/// pedaço "acesso PAGO", usado para liberar a auto-matrícula em curso pago).
///
/// A implementação real (<see cref="Tuilow.Learning.Infrastructure.Services.LearningCourseAccessService"/>)
/// considera, nesta ordem: (1) matrícula ativa (Enrollment — já é criada automaticamente para
/// TODOS os caminhos de acesso hoje: matrícula direta em curso grátis, compra confirmada e
/// assinatura confirmada) e, como rede de segurança, (2) acesso pago direto (compra avulsa ou
/// assinatura ativa, via ICourseAccessChecker) — cobre o caso raro de um Enrollment ainda não
/// ter sido criado no exato instante da checagem.
///
/// Vive no SharedKernel (não em um módulo específico) porque é consumido por múltiplos módulos
/// (Learning, Streaming, Channel) — mesmo critério já usado para IEmailService/IWhatsAppService.
/// Extensível para "permissões futuras" (ex.: liberação manual de acesso por um admin) sem
/// quebrar nenhum chamador: um caminho a mais dentro da implementação, sem mudar a assinatura.
/// Nenhuma tela ou Controller deve reimplementar esta regra — sempre chamar HasAccessAsync.
/// </summary>
public interface IUserCourseAccessService
{
    Task<bool> HasAccessAsync(Guid userId, Guid courseId, CancellationToken ct = default);
}

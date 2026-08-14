namespace HiMentor.SharedKernel.Application.Exceptions;

/// <summary>
/// Achado M5 da avaliação: erro originado de uma integração externa (Asaas, Cloudflare Stream,
/// etc.) — antes essas falhas eram propagadas como InvalidOperationException, que o middleware
/// global trata como erro "de negócio" (422) e devolve ao cliente com a Message original,
/// incluindo texto cru vindo do provedor terceiro (corpo de erro HTTP, nomes de campo internos
/// etc.). Não dá pra simplesmente sanitizar TODA InvalidOperationException — o tipo também é
/// usado em vários pontos do domínio para mensagens de validação legítimas, feitas para chegar
/// ao usuário (ex.: Course.Publish, User.ConsumeMagicLink). Um tipo dedicado deixa o middleware
/// sanitizar só a integração externa: a Message completa continua no log interno (o middleware
/// já loga toda exceção não tratada antes de decidir a resposta), mas o cliente recebe uma
/// mensagem genérica.
/// </summary>
public sealed class ExternalServiceException(string message) : Exception(message);

using MediatR;

namespace HiMentor.IdentidadeAcesso.Application.Commands.ConfirmEmail;

// Antes era (Guid UserId, string Token) — trocado para (Email, Code) porque o e-mail de cadastro
// agora manda um código curto de 6 dígitos (ver User.Register) em vez de um GUID embutido num
// link; a pessoa digita o código manualmente na tela de confirmação, e ela só tem o e-mail à
// mão nesse momento, não o UserId.
public sealed record ConfirmEmailCommand(string Email, string Code) : IRequest<bool>;

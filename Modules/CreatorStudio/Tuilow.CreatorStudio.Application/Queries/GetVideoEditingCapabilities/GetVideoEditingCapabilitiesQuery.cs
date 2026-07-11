using Tuilow.CreatorStudio.Application.Interfaces;
using MediatR;

namespace Tuilow.CreatorStudio.Application.Queries.GetVideoEditingCapabilities;

/// <summary>Front chama antes de mostrar os botões de edição automática/clipes — sabe se deve exibi-los ou o aviso de "em breve".</summary>
public sealed record GetVideoEditingCapabilitiesQuery : IRequest<VideoEditingCapabilities>;

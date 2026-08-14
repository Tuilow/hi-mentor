using HiMentor.CreatorStudio.Application.Interfaces;
using MediatR;

namespace HiMentor.CreatorStudio.Application.Commands.GenerateMarketingCopy;

/// <summary>Central de Divulgação — botão "Gerar com IA" por canal (Instagram/Stories/WhatsApp/E-mail/Ads/Headline).</summary>
public sealed record GenerateMarketingCopyCommand(
    Guid CourseId,
    Guid InstructorId,
    MarketingChannel Channel
) : IRequest<MarketingCopySuggestion>;

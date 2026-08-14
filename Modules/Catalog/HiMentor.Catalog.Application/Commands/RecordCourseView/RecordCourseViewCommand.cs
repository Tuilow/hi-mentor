using MediatR;

namespace HiMentor.Catalog.Application.Commands.RecordCourseView;

/// <summary>
/// Incrementa o contador de visualizações da página de vendas pública. Endpoint anônimo —
/// alimenta o card "Views" do dashboard do produto (CreatorStudio).
/// </summary>
public sealed record RecordCourseViewCommand(string Slug) : IRequest;

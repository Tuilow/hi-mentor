namespace Tuilow.Catalog.Domain.Enums;

/// <summary>
/// Tipo de produto digital. Hoje toda a plataforma só sabe entregar conteúdo em vídeo
/// (Course = módulos/aulas/vídeo — o modelo já existente). Os demais valores existem para
/// não fechar a porta a outros formatos no futuro sem precisar de migração de enum.
/// </summary>
public enum ProductType { Course, Ebook, Bundle }

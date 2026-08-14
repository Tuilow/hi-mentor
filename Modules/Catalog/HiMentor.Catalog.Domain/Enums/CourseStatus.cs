namespace HiMentor.Catalog.Domain.Enums;

/// <summary>
/// InReview: estado opcional para moderação futura (o criador pode enviar um produto para
/// análise manualmente). Não é obrigatório no fluxo — por padrão o assistente de criação
/// publica direto (Draft → Published), consistente com "plataforma aberta, sem aprovação".
/// </summary>
public enum CourseStatus { Draft, InReview, Published, Archived }

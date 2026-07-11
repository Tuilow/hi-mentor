namespace Tuilow.CreatorStudio.Domain.Enums;

/// <summary>
/// Nível dos alunos do nicho do criador — usado só como contexto para a geração de estrutura/
/// roteiro por IA. Valores espelham Catalog.Domain.Enums.CourseLevel de propósito (mesmo
/// vocabulário para o criador), mas é um tipo próprio: Domain não referencia Domain de outro
/// módulo (a ligação com o Course real, se o criador optar por criar o curso, acontece só a
/// partir do front, via os commands já existentes de Catalog).
/// </summary>
public enum AudienceLevel { Beginner, Intermediate, Advanced }

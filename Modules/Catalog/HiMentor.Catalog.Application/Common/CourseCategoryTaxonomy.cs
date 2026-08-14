namespace HiMentor.Catalog.Application.Common;

/// <summary>
/// Lista curada de categorias/subcategorias sugeridas para o passo 1 do assistente de criação
/// (Categoria/Subcategoria eram campos de texto livre — ver GetCategoriesQueryHandler, que mescla
/// esta lista com o que os criadores já digitaram em <see cref="HiMentor.Catalog.Domain.Entities.Course"/>,
/// para o autocomplete nunca "esconder" uma categoria real só porque não está aqui).
/// Não é uma tabela/entidade — Category/Subcategory continuam texto livre no Course, sem
/// migração de banco: isto é só dado de referência para popular as sugestões.
/// </summary>
public static class CourseCategoryTaxonomy
{
    public static readonly IReadOnlyDictionary<string, string[]> Seed = new Dictionary<string, string[]>
    {
        ["Tecnologia"] = ["Programação", "Design (UX/UI)", "Marketing Digital", "Data Science e IA", "Cibersegurança", "Redes e Infraestrutura", "No-Code / Low-Code"],
        ["Negócios e Empreendedorismo"] = ["Empreendedorismo", "Vendas", "Finanças e Investimentos", "Gestão e Liderança", "Produtividade", "E-commerce", "Recursos Humanos"],
        ["Saúde e Bem-estar"] = ["Nutrição", "Fitness e Exercício", "Yoga e Meditação", "Saúde Mental", "Emagrecimento"],
        ["Educação e Idiomas"] = ["Inglês", "Espanhol", "Outros Idiomas", "Concursos Públicos", "Reforço Escolar"],
        ["Estilo de Vida"] = ["Culinária", "Moda e Beleza", "Decoração e Organização", "Maternidade e Paternidade", "Jardinagem"],
        ["Música"] = ["Violão e Guitarra", "Piano e Teclado", "Canto", "Produção Musical", "Teoria Musical"],
        ["Fotografia e Vídeo"] = ["Fotografia", "Edição de Vídeo", "Produção Audiovisual", "Animação"],
        ["Arte e Criatividade"] = ["Desenho e Ilustração", "Pintura", "Artesanato", "Design Gráfico"],
        ["Desenvolvimento Pessoal"] = ["Inteligência Emocional", "Carreira", "Espiritualidade", "Hábitos e Rotina"],
        ["Esportes"] = ["Artes Marciais", "Corrida", "Futebol", "Esportes Radicais"],
    };
}

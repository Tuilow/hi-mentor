namespace HiMentor.SharedKernel.Application.Common;

/// <summary>
/// Único lugar da plataforma que decide o <see cref="CourseCommercializationState"/> de um
/// curso, cruzando o preço bruto do Course (módulo Catalog) com a existência de um Plan de
/// assinatura ativo para o produto (módulo Sales) — os dois únicos fatores que hoje determinam
/// como um curso é comercializado. Antes desta sprint, cada tela (página de vendas pública,
/// listagem do catálogo, Canal do Criador) repetia essa mesma conta isoladamente no front-end
/// (e cada uma esquecia de checar o Plan em algum momento) — daqui em diante, todo response de
/// Application que precisa exibir preço/"Grátis" chama <see cref="Resolve"/> uma única vez no
/// backend e devolve o resultado pronto; o front-end só exibe, nunca deriva de novo.
///
/// Fica no SharedKernel (em vez de Catalog ou Sales) para poder ser chamado por qualquer módulo
/// sem criar uma dependência cruzada Catalog↔Sales além da que já existe (Sales.Application já
/// referencia Catalog.Domain) — este resolver não conhece nenhuma entidade de domínio, só os
/// booleans primitivos que cada chamador já tem em mãos.
/// </summary>
public static class CourseCommercializationResolver
{
    /// <param name="isPublished">Course.Status == Published — cursos não publicados nunca devem aparecer em vitrines/canais.</param>
    /// <param name="courseIsFree">Course.IsFree (Course.Price == 0).</param>
    /// <param name="hasActiveSubscriptionPlan">Existe algum Plan (Sales) com CourseId = este curso e IsActive = true.</param>
    public static CourseCommercializationState Resolve(
        bool isPublished, bool courseIsFree, bool hasActiveSubscriptionPlan)
    {
        if (!isPublished) return CourseCommercializationState.Hidden;

        // Assinatura tem prioridade: no modo "Assinatura" do assistente, Course.Price é
        // deliberadamente 0 (o preço real mora no Plan) — sem essa prioridade, esses cursos
        // seriam lidos como Free, que é exatamente o bug relatado ("curso pago aparece como
        // Grátis").
        if (hasActiveSubscriptionPlan) return CourseCommercializationState.Subscription;

        return courseIsFree ? CourseCommercializationState.Free : CourseCommercializationState.Paid;
    }
}

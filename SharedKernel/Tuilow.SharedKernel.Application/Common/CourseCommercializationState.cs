namespace Tuilow.SharedKernel.Application.Common;

/// <summary>
/// Estado real de comercialização de um curso — a interface deve refletir exatamente um destes
/// estados, nunca derivar "Grátis" olhando só Course.Price/Course.IsFree isoladamente (essa
/// checagem isolada é a causa raiz do bug "curso pago aparece como Grátis": um curso no modo
/// "Assinatura" grava Course.Price = 0 por design — ver Course.SetPrice —, então qualquer tela
/// que só olhasse IsFree via Price==0 lia esse curso como gratuito).
/// </summary>
public enum CourseCommercializationState
{
    /// <summary>Course.Price = 0 e nenhum Plan de assinatura ativo para o produto.</summary>
    Free,

    /// <summary>Course.Price > 0 (compra avulsa) e nenhum Plan de assinatura ativo.</summary>
    Paid,

    /// <summary>Existe um Plan de assinatura ativo para este produto (módulo Sales) — o preço real está no Plan, não em Course.Price.</summary>
    Subscription,

    /// <summary>
    /// Reservado para quando existir um modelo de preço promocional (ex.: "De R$ X / Por R$ Y").
    /// Não há hoje nenhum campo de preço promocional no domínio (Course não tem
    /// preço-original/preço-com-desconto) — este estado nunca é retornado pelo resolver
    /// enquanto esse dado não existir, para não inventar uma funcionalidade sem lastro de dados
    /// nem exigir migração de banco sem necessidade. Ver CourseCommercializationResolver.
    /// </summary>
    Promotional,

    /// <summary>Curso não publicado (Draft/InReview/Archived) — não deve aparecer em vitrines/canais públicos.</summary>
    Hidden
}

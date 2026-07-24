/**
 * Único lugar do front-end que sabe COMO exibir o estado de comercialização de um curso
 * ("Grátis" / preço / "Disponível na assinatura"). O estado em si (QUAL é) já vem calculado do
 * backend em `commercializationState` (ver CourseCommercializationResolver, SharedKernel.Application.Common)
 * — nenhuma tela deve voltar a derivar isso sozinha comparando `price === 0` ou `isFree` puro,
 * que foi exatamente a causa raiz do bug "curso pago aparece como Grátis": um curso no modo
 * "Assinatura" grava price=0 por design (o preço real mora no Plan, módulo Sales), então
 * qualquer tela que só olhasse price/isFree isoladamente lia esse curso como gratuito.
 *
 * `isFreeFallback` existe só como rede de segurança para uma resposta antiga em cache que ainda
 * não tenha o campo novo — nunca deve ser a fonte de verdade quando `state` está presente.
 */
export type CommercializationState = 'Free' | 'Paid' | 'Subscription' | 'Promotional' | 'Hidden';

export function formatPrice(price: number): string {
  return price.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

function resolveState(state: string | undefined, isFreeFallback: boolean): CommercializationState {
  if (state === 'Free' || state === 'Paid' || state === 'Subscription'
    || state === 'Promotional' || state === 'Hidden') return state;
  return isFreeFallback ? 'Free' : 'Paid';
}

/** Rótulo pronto para exibir no lugar do preço (badge/linha de preço em cards e listas). */
export function commercializationLabel(
  state: string | undefined, price: number, isFreeFallback: boolean,
): string {
  switch (resolveState(state, isFreeFallback)) {
    case 'Free': return 'Grátis';
    case 'Subscription': return 'Disponível na assinatura';
    case 'Hidden': return '';
    case 'Paid':
    case 'Promotional':
    default: return formatPrice(price);
  }
}

/** true somente quando o curso é REALMENTE gratuito (nunca para um curso em modo Assinatura). */
export function isFreeState(state: string | undefined, isFreeFallback: boolean): boolean {
  return resolveState(state, isFreeFallback) === 'Free';
}

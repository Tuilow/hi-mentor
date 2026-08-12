'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';

/**
 * Destino do redirecionamento automático da Asaas depois que o cliente paga por PIX ou cartão
 * (campo `callback.successUrl`, enviado ao criar a cobrança — ver
 * AsaasPaymentService/AsaasMarketplacePaymentService.CreateChargeAsync no backend). Boleto não
 * redireciona sozinho: a Asaas só confirma boleto depois da compensação bancária, dias depois —
 * o cliente já saiu da tela nesse momento.
 *
 * Como o webhook de confirmação de pagamento pode não ter processado ainda no exato instante
 * desse redirect (corrida entre o navegador voltando pra cá e o webhook da Asaas chegando no
 * backend), esta página NUNCA promete acesso liberado na hora — só confirma o pagamento e
 * aponta pro e-mail com o Magic Link (ver EmailService.SendMagicLinkAccessAsync), que é o
 * mecanismo real de acesso pós-compra em todo o app (mesma mensagem já usada em
 * app/c/[slug]/page.tsx e app/acesso).
 */
export default function PagamentoConfirmadoPage() {
  const [isLoggedIn, setIsLoggedIn] = useState<boolean | null>(null);

  useEffect(() => {
    setIsLoggedIn(!!localStorage.getItem('access_token'));
  }, []);

  return (
    <div className="min-h-screen bg-white flex flex-col items-center justify-center gap-4 px-4 text-center">
      <p className="text-5xl">🎉</p>
      <h1 className="text-2xl font-bold text-gray-800">Pagamento confirmado!</h1>
      <p className="text-sm text-gray-500 max-w-sm leading-relaxed">
        Enviamos um e-mail com o link de acesso ao seu curso — confira sua caixa de entrada
        (e a pasta de spam, por garantia). Assim que abrir o link, você entra direto, sem senha.
      </p>

      <div className="flex flex-col sm:flex-row items-center gap-3 mt-2">
        {isLoggedIn ? (
          <Link href="/dashboard" className="btn-primary px-6 py-2.5">
            Ir para meus programas →
          </Link>
        ) : (
          <Link href="/login" className="btn-primary px-6 py-2.5">
            Já tenho conta — Entrar
          </Link>
        )}
        <Link href="/" className="text-sm text-gray-500 hover:text-gray-800 transition-colors">
          Voltar para o site
        </Link>
      </div>

      <p className="text-xs text-gray-400 mt-6 max-w-xs">
        Não recebeu o e-mail em alguns minutos?{' '}
        <Link href="/acesso" className="underline">Solicite um novo link de acesso</Link>.
      </p>
    </div>
  );
}

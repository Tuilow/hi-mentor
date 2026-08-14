'use client';

import { useSyncExternalStore } from 'react';
import { subscribeToApiLoading } from '@/lib/api';

// Barra de progresso fixa no topo da tela, visível sempre que há pelo menos uma
// requisição à API em andamento — cobre qualquer botão/link/ação da aplicação sem
// depender de cada tela lembrar de implementar seu próprio estado de loading.
// useSyncExternalStore evita duplicar o contador em estado React local (a "fonte da
// verdade" é o contador em lib/api.ts, compartilhado por toda a aplicação).
function subscribe(onStoreChange: () => void) {
  return subscribeToApiLoading(() => onStoreChange());
}

function getSnapshot() {
  return pendingCountRef.current;
}

function getServerSnapshot() {
  return 0;
}

// subscribeToApiLoading já entrega o valor atual na primeira chamada — guardamos aqui
// para que getSnapshot (síncrono, sem parâmetros) sempre tenha o número mais recente.
const pendingCountRef = { current: 0 };
subscribeToApiLoading((count) => {
  pendingCountRef.current = count;
});

export function GlobalLoadingBar() {
  const pending = useSyncExternalStore(subscribe, getSnapshot, getServerSnapshot);
  const isLoading = pending > 0;

  return (
    <div
      aria-hidden={!isLoading}
      role="status"
      aria-live="polite"
      style={{
        position: 'fixed',
        top: 0,
        left: 0,
        right: 0,
        height: 3,
        zIndex: 9999,
        pointerEvents: 'none',
        opacity: isLoading ? 1 : 0,
        transition: 'opacity 200ms ease',
      }}
    >
      <div
        style={{
          height: '100%',
          width: '40%',
          background: 'linear-gradient(90deg, transparent, #6366f1, transparent)',
          animation: isLoading ? 'himentor-loading-slide 1.1s ease-in-out infinite' : 'none',
        }}
      />
      <style>{`
        @keyframes himentor-loading-slide {
          0% { transform: translateX(-100%); }
          100% { transform: translateX(250%); }
        }
      `}</style>
    </div>
  );
}

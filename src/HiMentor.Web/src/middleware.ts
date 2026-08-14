import { NextRequest, NextResponse } from 'next/server';

/**
 * Achado M4 da avaliação www/app: até aqui não existia nenhuma proteção de rota em
 * middleware.ts — todo o guard de autenticação era client-side (cada página decidia, depois de
 * já ter renderizado, se redirecionava pra /login). Isso expõe o HTML/JS da tela protegida a
 * qualquer requisição não autenticada e é o pré-requisito citado na recomendação de
 * arquitetura www/app antes de apontar um subdomínio dedicado (app.dominio.com) pra área do
 * aluno: sem esse gate, mover as rotas de lugar não muda nada sobre quem consegue alcançá-las.
 *
 * IMPORTANTE — isto é só um gate de UX (redireciona quem claramente não tem sessão), não a
 * autorização de verdade: a autorização real continua no backend, via [Authorize] + validação
 * de JWT em cada chamada de API — um "has_session" presente mas com refresh token expirado
 * ainda passa por aqui, mas cai em 401 nas chamadas reais e o interceptor do axios (ver
 * lib/api.ts) redireciona pra /login do mesmo jeito.
 *
 * Achado de teste manual (deploy real: front no Vercel, API no Railway — domínios registráveis
 * DE FATO diferentes): a versão original checava a PRESENÇA do cookie HttpOnly "refresh_token".
 * Isso só funciona quando front e API dividem o mesmo domínio registrável — esse cookie é
 * gravado pelo BACKEND com Domain = host da API, então o navegador nunca o envia em requisições
 * pro Vercel (domínio diferente), e este middleware (que roda no domínio do front) sempre via
 * "sem sessão", mesmo logo após um login bem-sucedido — resultado: loop de redirecionamento de
 * volta pro /login. Trocado para "has_session", um cookie NÃO-HttpOnly gravado pelo próprio
 * front via document.cookie (ver setAccessToken em lib/api.ts) assim que o login/refresh
 * funciona — por ser gravado na origem do front, ele sempre acompanha as requisições que este
 * middleware intercepta, não importa em qual domínio a API esteja.
 */
const SESSION_HINT_COOKIE = 'has_session';

// Prefixos protegidos — qualquer rota que comece com um destes exige sessão.
const PROTECTED_PREFIXES = [
  '/dashboard',
  '/cursos',
  '/estudio',
  '/historico',
  '/meus-cursos',
  '/admin',
  '/plataforma',
  '/assinatura',
];

// Match EXATO (não prefixo): /canal é o editor do próprio canal do criador
// ((dashboard)/canal/page.tsx) — só isso é protegido. /canal/[handle] (vitrine pública do
// canal de um criador, em app/canal/[handle]/page.tsx) tem que continuar acessível sem login;
// um prefixo ingênuo (`pathname.startsWith('/canal')`) bloquearia essa página pública por engano.
const PROTECTED_EXACT = ['/canal'];

function isProtected(pathname: string): boolean {
  if (PROTECTED_EXACT.includes(pathname)) return true;
  return PROTECTED_PREFIXES.some(
    (prefix) => pathname === prefix || pathname.startsWith(`${prefix}/`)
  );
}

export function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;

  if (!isProtected(pathname)) {
    return NextResponse.next();
  }

  const hasSession = request.cookies.has(SESSION_HINT_COOKIE);
  if (hasSession) {
    return NextResponse.next();
  }

  const loginUrl = new URL('/login', request.url);
  loginUrl.searchParams.set('returnUrl', pathname);
  return NextResponse.redirect(loginUrl);
}

export const config = {
  matcher: [
    '/dashboard/:path*',
    '/cursos/:path*',
    '/estudio/:path*',
    '/historico/:path*',
    '/meus-cursos/:path*',
    '/admin/:path*',
    '/plataforma/:path*',
    '/assinatura/:path*',
    '/canal',
  ],
};

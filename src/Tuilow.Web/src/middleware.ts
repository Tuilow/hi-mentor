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
 * autorização de verdade: o middleware só enxerga a PRESENÇA do cookie HttpOnly
 * "refresh_token" (não pode ler nem validar o JWT em si, que fica no header Authorization,
 * fora do alcance de cookies/middleware). A autorização real continua no backend, via
 * [Authorize] + validação de JWT em cada chamada de API — um cookie presente mas com refresh
 * token expirado ainda passa por aqui, mas cai em 401 nas chamadas reais e o interceptor do
 * axios (ver lib/api.ts) redireciona pra /login do mesmo jeito.
 */
const REFRESH_COOKIE = 'refresh_token';

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

  const hasSession = request.cookies.has(REFRESH_COOKIE);
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

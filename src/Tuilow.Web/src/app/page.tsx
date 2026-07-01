import Link from 'next/link';

const features = [
  {
    icon: '🎥',
    title: 'Cursos em Vídeo HD',
    desc: 'Mais de 100 aulas com instrutores certificados. Assista no seu ritmo, onde quiser.',
  },
  {
    icon: '🎓',
    title: 'Perfis de Aprendizado',
    desc: 'Crie perfis de estudo e acompanhe a evolução de cada um com histórico completo.',
  },
  {
    icon: '🏆',
    title: 'Certificados Oficiais',
    desc: 'Receba certificados reconhecidos ao concluir cada curso.',
  },
  {
    icon: '📊',
    title: 'Progresso em Tempo Real',
    desc: 'Dashboard com métricas de progresso, tempo de estudo e conquistas desbloqueadas.',
  },
  {
    icon: '🔒',
    title: 'Conteúdo Protegido',
    desc: 'Vídeos criptografados via Cloudflare Stream. Acesso exclusivo para assinantes.',
  },
  {
    icon: '💬',
    title: 'Suporte Especializado',
    desc: 'Tire dúvidas diretamente com os instrutores via comentários nas aulas.',
  },
];

const levels = [
  { label: 'Iniciante', color: 'text-emerald-600', desc: 'Fundamentos e primeiros passos' },
  { label: 'Intermediário', color: 'text-blue-600', desc: 'Prática avançada e projetos reais' },
  { label: 'Avançado', color: 'text-orange-600', desc: 'Especialização e domínio completo' },
];

export default function HomePage() {
  return (
    <div className="min-h-screen bg-white text-gray-800">

      {/* Navbar */}
      <header className="fixed top-0 inset-x-0 z-50 bg-white/80 backdrop-blur-md border-b border-gray-200">
        <div className="max-w-6xl mx-auto px-4 h-16 flex items-center justify-between">
          <span className="text-lg font-bold gradient-text">🎓 Tuilow</span>
          <nav className="hidden md:flex items-center gap-6 text-sm text-gray-500">
            <Link href="#features" className="hover:text-gray-800 transition-colors">Recursos</Link>
            <Link href="#cursos" className="hover:text-gray-800 transition-colors">Cursos</Link>
          </nav>
          <div className="flex items-center gap-3">
            <Link href="/login" className="btn-ghost text-sm">Entrar</Link>
            <Link href="/registro" className="btn-primary text-sm">Começar grátis</Link>
          </div>
        </div>
      </header>

      {/* Hero */}
      <section className="pt-32 pb-24 px-4 text-center relative overflow-hidden">
        {/* Glow background */}
        <div className="absolute inset-0 pointer-events-none">
          <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2
                          w-[800px] h-[500px] bg-blue-100/50 rounded-full blur-3xl" />
          <div className="absolute top-1/3 left-1/4 w-64 h-64 bg-blue-50 rounded-full blur-3xl" />
          <div className="absolute top-1/3 right-1/4 w-64 h-64 bg-orange-50 rounded-full blur-3xl" />
        </div>

        <div className="relative max-w-4xl mx-auto animate-fade-in">
          <div className="inline-flex items-center gap-2 bg-blue-50 border border-blue-200
                          text-blue-700 text-xs font-medium px-4 py-1.5 rounded-full mb-8">
            <span className="w-1.5 h-1.5 rounded-full bg-blue-500 animate-pulse" />
            Plataforma completa de cursos online
          </div>

          <h1 className="text-5xl md:text-7xl font-extrabold tracking-tight mb-6 leading-tight text-gray-800">
            Aprenda e Ensine{' '}
            <span className="gradient-text">com a Tuilow</span>
          </h1>
          <p className="text-lg md:text-xl text-gray-500 mb-10 max-w-2xl mx-auto leading-relaxed">
            Crie, venda e gerencie cursos online em uma plataforma moderna, rápida e intuitiva.
          </p>

          <div className="flex flex-col sm:flex-row gap-4 justify-center">
            <Link href="/cursos" className="btn-primary text-base px-8 py-3">
              Explorar Cursos →
            </Link>
            <Link href="/registro" className="btn-secondary text-base px-8 py-3">
              Criar Meu Curso
            </Link>
          </div>

          <p className="text-xs text-gray-400 mt-6">Sem cartão de crédito · Cancele quando quiser</p>
        </div>

        {/* Mock dashboard preview */}
        <div className="relative max-w-4xl mx-auto mt-20 animate-slide-up">
          <div className="bg-white border border-gray-200 rounded-2xl overflow-hidden shadow-2xl shadow-blue-100/60">
            <div className="bg-gray-50 px-4 py-3 flex items-center gap-2 border-b border-gray-200">
              <div className="w-3 h-3 rounded-full bg-gray-300" />
              <div className="w-3 h-3 rounded-full bg-gray-300" />
              <div className="w-3 h-3 rounded-full bg-gray-300" />
              <span className="text-xs text-gray-400 ml-2">tuilow.com/dashboard</span>
            </div>
            <div className="p-8 grid grid-cols-3 gap-4">
              {[
                { label: 'Cursos em andamento', value: '3', color: 'text-blue-600' },
                { label: 'Aulas concluídas', value: '47', color: 'text-emerald-600' },
                { label: 'Progresso médio', value: '78%', color: 'text-orange-600' },
              ].map((stat) => (
                <div key={stat.label} className="bg-gray-50 rounded-xl p-4 border border-gray-200">
                  <p className={`text-2xl font-bold ${stat.color}`}>{stat.value}</p>
                  <p className="text-xs text-gray-500 mt-1">{stat.label}</p>
                </div>
              ))}
            </div>
          </div>
        </div>
      </section>

      {/* Features */}
      <section id="features" className="py-24 px-4 border-t border-gray-200">
        <div className="max-w-6xl mx-auto">
          <div className="text-center mb-16">
            <h2 className="text-3xl md:text-4xl font-bold mb-4 text-gray-800">Tudo que você precisa</h2>
            <p className="text-gray-500 max-w-xl mx-auto">
              Uma plataforma completa pensada para quem quer criar, vender e concluir cursos com resultados reais.
            </p>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {features.map((f) => (
              <div key={f.title} className="card-hover group">
                <div className="text-3xl mb-4 group-hover:scale-110 transition-transform duration-200">
                  {f.icon}
                </div>
                <h3 className="font-semibold text-gray-800 mb-2">{f.title}</h3>
                <p className="text-sm text-gray-500 leading-relaxed">{f.desc}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Cursos / Níveis */}
      <section id="cursos" className="py-24 px-4 border-t border-gray-200">
        <div className="max-w-6xl mx-auto">
          <div className="text-center mb-16">
            <h2 className="text-3xl md:text-4xl font-bold mb-4 text-gray-800">Para todos os níveis</h2>
            <p className="text-gray-500 max-w-xl mx-auto">
              Do iniciante ao avançado, temos o curso certo para cada fase.
            </p>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {levels.map((l) => (
              <div key={l.label}
                className="card border-gray-200 hover:border-blue-200 transition-colors text-center p-10">
                <p className={`text-4xl font-extrabold mb-2 ${l.color}`}>{l.label}</p>
                <p className="text-gray-500 text-sm">{l.desc}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* CTA */}
      <section className="py-24 px-4 border-t border-gray-200">
        <div className="max-w-2xl mx-auto text-center">
          <div className="card border-gray-200 bg-gradient-to-br from-gray-50 to-blue-50 p-12">
            <h2 className="text-3xl font-bold mb-4 text-gray-800">
              Pronto para começar?
            </h2>
            <p className="text-gray-500 mb-8">
              Crie sua conta gratuita e acesse as primeiras aulas agora mesmo.
            </p>
            <Link href="/registro" className="btn-primary text-base px-10 py-3">
              Criar conta gratuita →
            </Link>
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="py-8 px-4 border-t border-gray-200 text-center text-sm text-gray-400">
        © {new Date().getFullYear()} Tuilow. Todos os direitos reservados.
      </footer>
    </div>
  );
}

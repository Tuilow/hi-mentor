import Link from 'next/link';

const features = [
  {
    icon: '🎥',
    title: 'Cursos em Vídeo HD',
    desc: 'Mais de 100 aulas com instrutores certificados. Assista no seu ritmo, onde quiser.',
  },
  {
    icon: '🐕',
    title: 'Perfil do seu Cão',
    desc: 'Cadastre seus cães e acompanhe a evolução de cada um com histórico completo.',
  },
  {
    icon: '🏆',
    title: 'Certificados Oficiais',
    desc: 'Receba certificados reconhecidos ao concluir cada módulo de adestramento.',
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
  { label: 'Iniciante', color: 'text-emerald-400', desc: 'Comandos básicos e socialização' },
  { label: 'Intermediário', color: 'text-brand-400', desc: 'Obediência avançada e truques' },
  { label: 'Avançado', color: 'text-pink-400', desc: 'Agility, proteção e especialidades' },
];

export default function HomePage() {
  return (
    <div className="min-h-screen bg-zinc-950 text-zinc-100">

      {/* Navbar */}
      <header className="fixed top-0 inset-x-0 z-50 bg-zinc-950/80 backdrop-blur-md border-b border-zinc-800/60">
        <div className="max-w-6xl mx-auto px-4 h-16 flex items-center justify-between">
          <span className="text-lg font-bold gradient-text">🐕 DogMaster Pro</span>
          <nav className="hidden md:flex items-center gap-6 text-sm text-zinc-400">
            <Link href="#features" className="hover:text-zinc-100 transition-colors">Recursos</Link>
            <Link href="#cursos" className="hover:text-zinc-100 transition-colors">Cursos</Link>
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
                          w-[800px] h-[500px] bg-brand-600/10 rounded-full blur-3xl" />
          <div className="absolute top-1/3 left-1/4 w-64 h-64 bg-purple-700/10 rounded-full blur-3xl" />
          <div className="absolute top-1/3 right-1/4 w-64 h-64 bg-pink-700/10 rounded-full blur-3xl" />
        </div>

        <div className="relative max-w-4xl mx-auto animate-fade-in">
          <div className="inline-flex items-center gap-2 bg-brand-950 border border-brand-800
                          text-brand-300 text-xs font-medium px-4 py-1.5 rounded-full mb-8">
            <span className="w-1.5 h-1.5 rounded-full bg-brand-400 animate-pulse" />
            Plataforma completa de adestramento canino
          </div>

          <h1 className="text-5xl md:text-7xl font-extrabold tracking-tight mb-6 leading-tight">
            Adestre seu cão{' '}
            <span className="gradient-text">como um profissional</span>
          </h1>
          <p className="text-lg md:text-xl text-zinc-400 mb-10 max-w-2xl mx-auto leading-relaxed">
            Cursos em vídeo HD, progresso personalizado e suporte de especialistas.
            Transforme o relacionamento com seu cão — de onde você estiver.
          </p>

          <div className="flex flex-col sm:flex-row gap-4 justify-center">
            <Link href="/registro" className="btn-primary text-base px-8 py-3">
              Começar gratuitamente →
            </Link>
            <Link href="/login" className="btn-secondary text-base px-8 py-3">
              Já tenho conta
            </Link>
          </div>

          <p className="text-xs text-zinc-500 mt-6">Sem cartão de crédito · Cancele quando quiser</p>
        </div>

        {/* Mock dashboard preview */}
        <div className="relative max-w-4xl mx-auto mt-20 animate-slide-up">
          <div className="bg-zinc-900 border border-zinc-800 rounded-2xl overflow-hidden shadow-2xl shadow-brand-950/40">
            <div className="bg-zinc-800 px-4 py-3 flex items-center gap-2">
              <div className="w-3 h-3 rounded-full bg-zinc-600" />
              <div className="w-3 h-3 rounded-full bg-zinc-600" />
              <div className="w-3 h-3 rounded-full bg-zinc-600" />
              <span className="text-xs text-zinc-500 ml-2">dogmasterpro.com/dashboard</span>
            </div>
            <div className="p-8 grid grid-cols-3 gap-4">
              {[
                { label: 'Cursos em andamento', value: '3', color: 'text-brand-400' },
                { label: 'Aulas concluídas', value: '47', color: 'text-emerald-400' },
                { label: 'Progresso médio', value: '78%', color: 'text-pink-400' },
              ].map((stat) => (
                <div key={stat.label} className="bg-zinc-800/60 rounded-xl p-4 border border-zinc-700/50">
                  <p className={`text-2xl font-bold ${stat.color}`}>{stat.value}</p>
                  <p className="text-xs text-zinc-500 mt-1">{stat.label}</p>
                </div>
              ))}
            </div>
          </div>
        </div>
      </section>

      {/* Features */}
      <section id="features" className="py-24 px-4 border-t border-zinc-800/60">
        <div className="max-w-6xl mx-auto">
          <div className="text-center mb-16">
            <h2 className="text-3xl md:text-4xl font-bold mb-4">Tudo que você precisa</h2>
            <p className="text-zinc-400 max-w-xl mx-auto">
              Uma plataforma completa pensada para donos de cães que querem resultados reais.
            </p>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {features.map((f) => (
              <div key={f.title} className="card-hover group">
                <div className="text-3xl mb-4 group-hover:scale-110 transition-transform duration-200">
                  {f.icon}
                </div>
                <h3 className="font-semibold text-zinc-100 mb-2">{f.title}</h3>
                <p className="text-sm text-zinc-400 leading-relaxed">{f.desc}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Cursos / Níveis */}
      <section id="cursos" className="py-24 px-4 border-t border-zinc-800/60">
        <div className="max-w-6xl mx-auto">
          <div className="text-center mb-16">
            <h2 className="text-3xl md:text-4xl font-bold mb-4">Para todos os níveis</h2>
            <p className="text-zinc-400 max-w-xl mx-auto">
              Do filhote ao cão adulto, temos o curso certo para cada fase.
            </p>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {levels.map((l) => (
              <div key={l.label}
                className="card border-zinc-800 hover:border-zinc-700 transition-colors text-center p-10">
                <p className={`text-4xl font-extrabold mb-2 ${l.color}`}>{l.label}</p>
                <p className="text-zinc-400 text-sm">{l.desc}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* CTA */}
      <section className="py-24 px-4 border-t border-zinc-800/60">
        <div className="max-w-2xl mx-auto text-center">
          <div className="card border-zinc-700 bg-gradient-to-br from-zinc-900 to-brand-950/30 p-12">
            <h2 className="text-3xl font-bold mb-4">
              Pronto para começar?
            </h2>
            <p className="text-zinc-400 mb-8">
              Crie sua conta gratuita e acesse as primeiras aulas agora mesmo.
            </p>
            <Link href="/registro" className="btn-primary text-base px-10 py-3">
              Criar conta gratuita →
            </Link>
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="py-8 px-4 border-t border-zinc-800/60 text-center text-sm text-zinc-500">
        © {new Date().getFullYear()} DogMaster Pro. Todos os direitos reservados.
      </footer>
    </div>
  );
}

import Link from 'next/link';

export default function HomePage() {
  return (
    <main className="min-h-screen bg-gradient-to-br from-brand-900 via-brand-700 to-brand-500">
      <div className="max-w-6xl mx-auto px-4 py-20 text-center text-white">
        <h1 className="text-5xl md:text-7xl font-bold mb-6">
          🐕 DogMaster Pro
        </h1>
        <p className="text-xl md:text-2xl text-brand-100 mb-4 max-w-2xl mx-auto">
          Adestramento canino profissional com IA, cursos em vídeo e comunidade especializada.
        </p>
        <p className="text-brand-200 mb-10">
          Transforme o relacionamento com o seu cão — de onde você estiver.
        </p>
        <div className="flex flex-col sm:flex-row gap-4 justify-center">
          <Link href="/registro" className="btn-primary text-center text-lg">
            Começar Gratuitamente
          </Link>
          <Link href="/cursos" className="btn-secondary text-center text-lg">
            Ver Cursos
          </Link>
        </div>

        {/* Features */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-8 mt-24">
          {[
            { emoji: '🎥', title: 'Cursos em Vídeo', desc: 'Mais de 100 aulas com instrutores certificados' },
            { emoji: '🤖', title: 'IA Treinadora', desc: 'Análise de comportamento e plano personalizado por IA' },
            { emoji: '🏆', title: 'Certificados', desc: 'Certificados reconhecidos ao concluir cada curso' },
          ].map((f) => (
            <div key={f.title} className="bg-white/10 backdrop-blur rounded-2xl p-8">
              <div className="text-5xl mb-4">{f.emoji}</div>
              <h3 className="text-xl font-bold mb-2">{f.title}</h3>
              <p className="text-brand-100">{f.desc}</p>
            </div>
          ))}
        </div>
      </div>
    </main>
  );
}

'use client';

import { Suspense, useEffect, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useParams, useRouter, useSearchParams } from 'next/navigation';
import {
  Eye, Users, GraduationCap, ShoppingCart, DollarSign, Percent, Wallet, ChevronLeft,
  Copy, QrCode, Code2, Instagram, MessageCircle, Mail, Megaphone, Type,
} from 'lucide-react';
import toast from 'react-hot-toast';
import QRCode from 'qrcode';
import { creatorStudioApi } from '@/lib/api';
import type { ProductDashboard, MarketingChannel, MarketingCopySuggestion } from '@/types';

const currency = (v: number) =>
  v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });

function StatCard({ icon: Icon, label, value }: { icon: React.ElementType; label: string; value: string }) {
  return (
    <div className="card flex items-center gap-4">
      <div className="w-10 h-10 rounded-xl bg-brand-50 text-brand-600 flex items-center justify-center flex-shrink-0">
        <Icon className="w-5 h-5" />
      </div>
      <div>
        <p className="text-xs text-gray-400">{label}</p>
        <p className="text-lg font-bold text-gray-800">{value}</p>
      </div>
    </div>
  );
}

async function copyToClipboard(text: string, label: string) {
  try {
    await navigator.clipboard.writeText(text);
    toast.success(`${label} copiado!`);
  } catch {
    toast.error('Não foi possível copiar. Selecione o texto manualmente.');
  }
}

const CHANNELS: { key: MarketingChannel; label: string; icon: React.ElementType }[] = [
  { key: 'InstagramPost', label: 'Post Instagram', icon: Instagram },
  { key: 'InstagramStory', label: 'Story', icon: Instagram },
  { key: 'WhatsApp', label: 'WhatsApp', icon: MessageCircle },
  { key: 'Email', label: 'E-mail', icon: Mail },
  { key: 'MetaAds', label: 'Anúncio (Meta Ads)', icon: Megaphone },
  { key: 'Headline', label: 'Headlines', icon: Type },
];

function ProductDashboardContent() {
  const params = useParams<{ id: string }>();
  const router = useRouter();
  const searchParams = useSearchParams();
  // Permite abrir direto na aba "Divulgar" via link (?tab=divulgar) — usado pelo botão
  // "Divulgar" da tabela de Meus Produtos, sem precisar passar pela aba "Desempenho" primeiro.
  const [tab, setTab] = useState<'performance' | 'divulgar'>(
    searchParams.get('tab') === 'divulgar' ? 'divulgar' : 'performance'
  );
  const [origin, setOrigin] = useState('');
  const [qrDataUrl, setQrDataUrl] = useState<string | null>(null);
  const [marketingResults, setMarketingResults] = useState<Partial<Record<MarketingChannel, MarketingCopySuggestion>>>({});
  const [generatingChannel, setGeneratingChannel] = useState<MarketingChannel | null>(null);

  useEffect(() => {
    setOrigin(window.location.origin);
  }, []);

  const { data, isLoading, error } = useQuery<ProductDashboard>({
    queryKey: ['product-dashboard', params.id],
    queryFn: () => creatorStudioApi.dashboard(params.id).then(r => r.data),
  });

  const publicUrl = data && origin ? `${origin}/c/${data.slug}` : '';

  useEffect(() => {
    if (!publicUrl) return;
    QRCode.toDataURL(publicUrl, { width: 240, margin: 1 })
      .then(setQrDataUrl)
      .catch(() => setQrDataUrl(null));
  }, [publicUrl]);

  const embedCode = publicUrl
    ? `<iframe src="${publicUrl}" width="100%" height="800" style="border:0"></iframe>`
    : '';
  const buttonCode = publicUrl
    ? `<a href="${publicUrl}" style="background:#4f46e5;color:#fff;padding:12px 24px;border-radius:8px;text-decoration:none;font-weight:600">Quero este Curso</a>`
    : '';

  const handleGenerate = async (channel: MarketingChannel) => {
    setGeneratingChannel(channel);
    try {
      const { data: result } = await creatorStudioApi.generateMarketingCopy(params.id, channel);
      setMarketingResults(prev => ({ ...prev, [channel]: result }));
    } catch {
      toast.error('Não foi possível gerar o texto agora. Tente novamente.');
    } finally {
      setGeneratingChannel(null);
    }
  };

  return (
    <div className="max-w-5xl mx-auto">
      <button onClick={() => router.push('/admin/produtos')}
        className="btn-ghost flex items-center gap-2 text-sm mb-4">
        <ChevronLeft className="w-4 h-4" /> Meus Produtos
      </button>

      {isLoading && <p className="text-gray-400 text-sm">Carregando dashboard...</p>}
      {!!error && <p className="text-red-500 text-sm">Não foi possível carregar o dashboard deste produto.</p>}

      {data && (
        <>
          <h1 className="text-2xl font-bold text-gray-800 mb-1">{data.productName}</h1>
          <p className="text-gray-500 text-sm mb-6">Desempenho e divulgação do produto.</p>

          {/* Tabs */}
          <div className="flex items-center gap-2 mb-6 border-b border-gray-200">
            {([
              ['performance', 'Desempenho'],
              ['divulgar', 'Divulgar'],
            ] as const).map(([key, label]) => (
              <button
                key={key}
                onClick={() => setTab(key)}
                className={`px-4 py-2.5 text-sm font-medium border-b-2 -mb-px transition-colors ${
                  tab === key
                    ? 'border-brand-500 text-brand-600'
                    : 'border-transparent text-gray-400 hover:text-gray-600'
                }`}
              >
                {label}
              </button>
            ))}
          </div>

          {tab === 'performance' && (
            <>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
                <StatCard icon={Eye} label="Views" value={data.views.toLocaleString('pt-BR')} />
                <StatCard icon={Users} label="Leads" value={data.leads.toLocaleString('pt-BR')} />
                <StatCard icon={GraduationCap} label="Alunos" value={data.students.toLocaleString('pt-BR')} />
                <StatCard icon={ShoppingCart} label="Vendas" value={data.sales.toLocaleString('pt-BR')} />
              </div>

              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <StatCard icon={DollarSign} label="Receita bruta" value={currency(data.revenue)} />
                <StatCard icon={Percent} label={`Comissão da plataforma (${data.platformFeePercentage}%)`} value={currency(data.platformFee)} />
                <StatCard icon={Wallet} label="Receita líquida" value={currency(data.netRevenue)} />
              </div>
            </>
          )}

          {tab === 'divulgar' && (
            <div className="space-y-6">
              {/* Kit de Divulgação */}
              <div className="card border-gray-200">
                <h2 className="text-lg font-semibold text-gray-800 mb-1">Kit de Divulgação</h2>
                <p className="text-sm text-gray-400 mb-5">
                  Link, QR Code, embed e botão prontos para usar em qualquer lugar — sites, blogs, cartões, anúncios.
                </p>

                <div className="space-y-4">
                  <div>
                    <p className="text-xs font-medium text-gray-500 mb-1.5">Link público / de checkout</p>
                    <div className="flex items-center gap-2">
                      <input readOnly value={publicUrl} className="input-field flex-1 text-sm" />
                      <button onClick={() => copyToClipboard(publicUrl, 'Link')}
                        className="btn-ghost p-2.5 flex-shrink-0" title="Copiar link">
                        <Copy className="w-4 h-4" />
                      </button>
                    </div>
                    <p className="text-xs text-gray-400 mt-1">
                      Ao clicar em comprar nessa página, o visitante já entra direto no fluxo de checkout.
                    </p>
                  </div>

                  <div>
                    <p className="text-xs font-medium text-gray-500 mb-1.5 flex items-center gap-1.5">
                      <Code2 className="w-3.5 h-3.5" /> Código Embed (site, blog, WordPress)
                    </p>
                    <div className="flex items-start gap-2">
                      <textarea readOnly value={embedCode} rows={2}
                        className="input-field flex-1 text-xs font-mono resize-none" />
                      <button onClick={() => copyToClipboard(embedCode, 'Embed')}
                        className="btn-ghost p-2.5 flex-shrink-0" title="Copiar embed">
                        <Copy className="w-4 h-4" />
                      </button>
                    </div>
                  </div>

                  <div>
                    <p className="text-xs font-medium text-gray-500 mb-1.5">Botão HTML "Comprar"</p>
                    <div className="flex items-start gap-2">
                      <textarea readOnly value={buttonCode} rows={2}
                        className="input-field flex-1 text-xs font-mono resize-none" />
                      <button onClick={() => copyToClipboard(buttonCode, 'Botão')}
                        className="btn-ghost p-2.5 flex-shrink-0" title="Copiar botão">
                        <Copy className="w-4 h-4" />
                      </button>
                    </div>
                  </div>

                  <div>
                    <p className="text-xs font-medium text-gray-500 mb-1.5 flex items-center gap-1.5">
                      <QrCode className="w-3.5 h-3.5" /> QR Code (cartões, flyers, eventos)
                    </p>
                    {qrDataUrl ? (
                      <img src={qrDataUrl} alt="QR Code do curso" className="w-32 h-32 rounded-lg border border-gray-200" />
                    ) : (
                      <div className="w-32 h-32 rounded-lg border border-gray-200 bg-gray-50 animate-pulse" />
                    )}
                  </div>
                </div>
              </div>

              {/* Central de Divulgação — IA por canal */}
              <div className="card border-gray-200">
                <h2 className="text-lg font-semibold text-gray-800 mb-1">Central de Divulgação</h2>
                <p className="text-sm text-gray-400 mb-5">
                  Textos prontos por canal, gerados por IA. Você sempre pode editar antes de usar.
                </p>

                <div className="grid grid-cols-2 sm:grid-cols-3 gap-3 mb-5">
                  {CHANNELS.map(({ key, label, icon: Icon }) => (
                    <button
                      key={key}
                      onClick={() => handleGenerate(key)}
                      disabled={generatingChannel === key}
                      className="flex items-center gap-2 justify-center border border-gray-200 rounded-lg
                                 px-3 py-2.5 text-sm font-medium text-gray-600 hover:border-brand-300
                                 hover:text-brand-600 transition-colors disabled:opacity-50"
                    >
                      <Icon className="w-4 h-4" />
                      {generatingChannel === key ? 'Gerando...' : label}
                    </button>
                  ))}
                </div>

                <div className="space-y-4">
                  {CHANNELS.filter(c => marketingResults[c.key]).map(({ key, label }) => {
                    const result = marketingResults[key]!;
                    const fullText = result.cta ? `${result.content}\n\n${result.cta}` : result.content;
                    return (
                      <div key={key} className="border border-gray-200 rounded-lg p-4">
                        <div className="flex items-center justify-between mb-2">
                          <p className="text-sm font-semibold text-gray-800">{label}</p>
                          <button onClick={() => copyToClipboard(fullText, label)}
                            className="btn-ghost p-1.5" title="Copiar texto">
                            <Copy className="w-3.5 h-3.5" />
                          </button>
                        </div>
                        <p className="text-sm text-gray-600 whitespace-pre-wrap">{result.content}</p>
                        {result.cta && <p className="text-sm text-brand-600 font-medium mt-2">{result.cta}</p>}
                      </div>
                    );
                  })}
                </div>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  );
}

export default function ProductDashboardPage() {
  return (
    <Suspense fallback={<p className="text-gray-400 text-sm">Carregando...</p>}>
      <ProductDashboardContent />
    </Suspense>
  );
}

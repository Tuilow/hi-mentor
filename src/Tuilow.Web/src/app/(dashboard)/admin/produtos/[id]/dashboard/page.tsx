'use client';

import { useQuery } from '@tanstack/react-query';
import { useParams, useRouter } from 'next/navigation';
import { Eye, Users, GraduationCap, ShoppingCart, DollarSign, Percent, Wallet, ChevronLeft } from 'lucide-react';
import { creatorStudioApi } from '@/lib/api';
import type { ProductDashboard } from '@/types';

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

export default function ProductDashboardPage() {
  const params = useParams<{ id: string }>();
  const router = useRouter();

  const { data, isLoading, error } = useQuery<ProductDashboard>({
    queryKey: ['product-dashboard', params.id],
    queryFn: () => creatorStudioApi.dashboard(params.id).then(r => r.data),
  });

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
          <p className="text-gray-500 text-sm mb-6">Desempenho do produto após a publicação.</p>

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
    </div>
  );
}

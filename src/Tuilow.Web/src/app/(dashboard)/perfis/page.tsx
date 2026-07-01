'use client';

import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { learnerProfilesApi } from '@/lib/api';
import { LearnerProfile } from '@/types';
import toast from 'react-hot-toast';

export default function LearnerProfilesPage() {
  const qc = useQueryClient();
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({ name: '', category: '' });

  const { data: profiles = [], isLoading } = useQuery<LearnerProfile[]>({
    queryKey: ['my-learner-profiles'],
    queryFn: () => learnerProfilesApi.getMyProfiles().then(r => r.data),
  });

  const addProfile = useMutation({
    mutationFn: (data: typeof form) => learnerProfilesApi.register(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['my-learner-profiles'] });
      toast.success('Perfil cadastrado com sucesso!');
      setShowForm(false);
      setForm({ name: '', category: '' });
    },
    onError: () => toast.error('Erro ao cadastrar perfil.'),
  });

  return (
    <div className="max-w-3xl mx-auto">
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-2xl font-bold text-gray-800">Meus Perfis</h1>
          <p className="text-gray-500 mt-1">Gerencie seus perfis de aprendizado.</p>
        </div>
        <button onClick={() => setShowForm(true)} className="btn-primary">
          + Adicionar
        </button>
      </div>

      {/* Formulário */}
      {showForm && (
        <div className="card border-gray-200 mb-6 animate-slide-up">
          <h2 className="font-semibold text-gray-800 mb-5">Cadastrar novo perfil</h2>
          <div className="space-y-3">
            <div>
              <label className="block text-sm font-medium text-gray-600 mb-1.5">
                Nome do perfil <span className="text-red-500">*</span>
              </label>
              <input
                placeholder="Ex: João"
                value={form.name}
                onChange={e => setForm(p => ({ ...p, name: e.target.value }))}
                className="input-field"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-600 mb-1.5">Área de interesse</label>
              <input
                placeholder="Ex: Programação, Design, Idiomas"
                value={form.category}
                onChange={e => setForm(p => ({ ...p, category: e.target.value }))}
                className="input-field"
              />
            </div>
          </div>
          <div className="flex gap-3 mt-5">
            <button
              onClick={() => addProfile.mutate(form)}
              disabled={!form.name || addProfile.isPending}
              className="btn-primary"
            >
              {addProfile.isPending ? 'Salvando...' : 'Salvar'}
            </button>
            <button onClick={() => setShowForm(false)} className="btn-ghost">
              Cancelar
            </button>
          </div>
        </div>
      )}

      {/* Loading */}
      {isLoading && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {[1,2].map(i => (
            <div key={i} className="card border-gray-200 animate-pulse h-24" />
          ))}
        </div>
      )}

      {/* Empty state */}
      {profiles.length === 0 && !isLoading && !showForm && (
        <div className="card border-gray-200 text-center py-16">
          <div className="text-6xl mb-4">🎓</div>
          <h3 className="font-semibold text-gray-800 mb-2">Nenhum perfil cadastrado ainda</h3>
          <p className="text-gray-500 mb-6 text-sm">
            Cadastre um perfil para personalizar sua experiência de aprendizado.
          </p>
          <button onClick={() => setShowForm(true)} className="btn-primary">
            Cadastrar meu primeiro perfil
          </button>
        </div>
      )}

      {/* Lista */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {profiles.map((profile) => (
          <div key={profile.id} className="card border-gray-200 hover:border-blue-200 transition-colors">
            <div className="flex items-center gap-4">
              <div className="w-14 h-14 bg-blue-50 rounded-full flex items-center justify-center text-3xl flex-shrink-0">
                🎓
              </div>
              <div className="flex-1 min-w-0">
                <h3 className="font-semibold text-gray-800">{profile.name}</h3>
                {profile.category && <p className="text-sm text-gray-500">{profile.category}</p>}
                <div className="flex items-center gap-3 mt-1">
                  {profile.level && (
                    <span className="text-xs text-gray-400">{profile.level}</span>
                  )}
                  {profile.ageMonths && (
                    <span className="text-xs text-gray-400">
                      {Math.floor(profile.ageMonths / 12)}a {profile.ageMonths % 12}m
                    </span>
                  )}
                </div>
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

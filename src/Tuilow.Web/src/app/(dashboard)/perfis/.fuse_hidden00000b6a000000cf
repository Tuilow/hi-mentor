'use client';

import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { dogsApi } from '@/lib/api';
import { Dog } from '@/types';
import toast from 'react-hot-toast';

export default function DogsPage() {
  const qc = useQueryClient();
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({ name: '', breed: '', sex: '' });

  const { data: dogs = [], isLoading } = useQuery<Dog[]>({
    queryKey: ['my-dogs'],
    queryFn: () => dogsApi.getMyDogs().then(r => r.data),
  });

  const addDog = useMutation({
    mutationFn: (data: typeof form) => dogsApi.register(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['my-dogs'] });
      toast.success('Cão cadastrado com sucesso!');
      setShowForm(false);
      setForm({ name: '', breed: '', sex: '' });
    },
    onError: () => toast.error('Erro ao cadastrar cão.'),
  });

  return (
    <div className="max-w-3xl mx-auto">
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-2xl font-bold text-zinc-100">Meus Cães</h1>
          <p className="text-zinc-400 mt-1">Gerencie o perfil dos seus companheiros.</p>
        </div>
        <button onClick={() => setShowForm(true)} className="btn-primary">
          + Adicionar
        </button>
      </div>

      {/* Formulário */}
      {showForm && (
        <div className="card border-zinc-700 mb-6 animate-slide-up">
          <h2 className="font-semibold text-zinc-100 mb-5">Cadastrar novo cão</h2>
          <div className="space-y-3">
            <div>
              <label className="block text-sm font-medium text-zinc-300 mb-1.5">
                Nome do cão <span className="text-red-400">*</span>
              </label>
              <input
                placeholder="Ex: Rex"
                value={form.name}
                onChange={e => setForm(p => ({ ...p, name: e.target.value }))}
                className="input-field"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-zinc-300 mb-1.5">Raça</label>
              <input
                placeholder="Ex: Golden Retriever"
                value={form.breed}
                onChange={e => setForm(p => ({ ...p, breed: e.target.value }))}
                className="input-field"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-zinc-300 mb-1.5">Sexo</label>
              <select
                value={form.sex}
                onChange={e => setForm(p => ({ ...p, sex: e.target.value }))}
                className="input-field"
              >
                <option value="">Não informar</option>
                <option value="Male">Macho</option>
                <option value="Female">Fêmea</option>
              </select>
            </div>
          </div>
          <div className="flex gap-3 mt-5">
            <button
              onClick={() => addDog.mutate(form)}
              disabled={!form.name || addDog.isPending}
              className="btn-primary"
            >
              {addDog.isPending ? 'Salvando...' : 'Salvar'}
            </button>
            <button onClick={() => setShowForm(false)} className="btn-secondary">
              Cancelar
            </button>
          </div>
        </div>
      )}

      {/* Loading */}
      {isLoading && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {[1,2].map(i => (
            <div key={i} className="card border-zinc-800 animate-pulse h-24" />
          ))}
        </div>
      )}

      {/* Empty state */}
      {dogs.length === 0 && !isLoading && !showForm && (
        <div className="card border-zinc-800 text-center py-16">
          <div className="text-6xl mb-4">🐾</div>
          <h3 className="font-semibold text-zinc-100 mb-2">Nenhum cão cadastrado ainda</h3>
          <p className="text-zinc-400 mb-6 text-sm">
            Cadastre seu cão para personalizar sua experiência de aprendizado.
          </p>
          <button onClick={() => setShowForm(true)} className="btn-primary">
            Cadastrar meu primeiro cão
          </button>
        </div>
      )}

      {/* Lista */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {dogs.map((dog) => (
          <div key={dog.id} className="card border-zinc-800 hover:border-zinc-700 transition-colors">
            <div className="flex items-center gap-4">
              <div className="w-14 h-14 bg-zinc-800 rounded-full flex items-center justify-center text-3xl flex-shrink-0">
                🐕
              </div>
              <div className="flex-1 min-w-0">
                <h3 className="font-semibold text-zinc-100">{dog.name}</h3>
                {dog.breed && <p className="text-sm text-zinc-400">{dog.breed}</p>}
                <div className="flex items-center gap-3 mt-1">
                  {dog.sex && (
                    <span className="text-xs text-zinc-500">
                      {dog.sex === 'Male' ? '♂ Macho' : '♀ Fêmea'}
                    </span>
                  )}
                  {dog.ageMonths && (
                    <span className="text-xs text-zinc-500">
                      {Math.floor(dog.ageMonths / 12)}a {dog.ageMonths % 12}m
                    </span>
                  )}
                  {dog.isNeutered && (
                    <span className="text-xs text-emerald-400">Castrado</span>
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

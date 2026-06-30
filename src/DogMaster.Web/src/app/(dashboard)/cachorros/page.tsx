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
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Meus Cães 🐕</h1>
        <button onClick={() => setShowForm(true)} className="btn-primary">
          + Adicionar Cão
        </button>
      </div>

      {showForm && (
        <div className="card mb-6">
          <h2 className="font-semibold text-gray-900 mb-4">Novo cão</h2>
          <div className="space-y-3">
            <input placeholder="Nome do cão *" value={form.name}
              onChange={e => setForm(p => ({ ...p, name: e.target.value }))}
              className="input-field" />
            <input placeholder="Raça (opcional)" value={form.breed}
              onChange={e => setForm(p => ({ ...p, breed: e.target.value }))}
              className="input-field" />
            <select value={form.sex}
              onChange={e => setForm(p => ({ ...p, sex: e.target.value }))}
              className="input-field">
              <option value="">Sexo (opcional)</option>
              <option value="Male">Macho</option>
              <option value="Female">Fêmea</option>
            </select>
          </div>
          <div className="flex gap-3 mt-4">
            <button onClick={() => addDog.mutate(form)} disabled={!form.name || addDog.isPending}
              className="btn-primary">
              {addDog.isPending ? 'Salvando...' : 'Salvar'}
            </button>
            <button onClick={() => setShowForm(false)} className="btn-secondary">
              Cancelar
            </button>
          </div>
        </div>
      )}

      {isLoading && <p className="text-gray-500">Carregando...</p>}

      {dogs.length === 0 && !isLoading && !showForm && (
        <div className="card text-center py-12">
          <div className="text-6xl mb-4">🐾</div>
          <h3 className="font-semibold text-gray-700 mb-2">Nenhum cão cadastrado ainda</h3>
          <p className="text-gray-500 mb-4">Cadastre seu cão para personalizar sua experiência.</p>
          <button onClick={() => setShowForm(true)} className="btn-primary">
            Cadastrar meu primeiro cão
          </button>
        </div>
      )}

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {dogs.map((dog) => (
          <div key={dog.id} className="card">
            <div className="flex items-center gap-4">
              <div className="w-16 h-16 bg-dog-paw/20 rounded-full flex items-center justify-center text-3xl">
                🐕
              </div>
              <div>
                <h3 className="font-semibold text-gray-900">{dog.name}</h3>
                {dog.breed && <p className="text-sm text-gray-500">{dog.breed}</p>}
                {dog.ageMonths && (
                  <p className="text-sm text-gray-500">
                    {Math.floor(dog.ageMonths / 12)} anos e {dog.ageMonths % 12} meses
                  </p>
                )}
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

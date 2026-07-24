'use client';

import { useEffect, useRef, useState } from 'react';
import { Check } from 'lucide-react';

interface CategoryAutocompleteProps {
  value: string;
  onChange: (value: string) => void;
  options: string[];
  placeholder?: string;
  label: string;
}

/**
 * Campo de Categoria/Subcategoria do assistente de criação de produtos — antes era um
 * <input> de texto livre (ver histórico de admin/produtos/novo/page.tsx). Continua aceitando
 * texto livre (não força escolher só o que está na lista, o back-end também não obriga isso),
 * mas agora sugere e filtra em tempo real as opções já existentes, pra reduzir duplicidade por
 * grafia (ex.: escrever "Produtividade" em vez de criar uma nova "produtividade").
 */
export function CategoryAutocomplete({ value, onChange, options, placeholder, label }: CategoryAutocompleteProps) {
  const [open, setOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const filtered = value.trim()
    ? options.filter(o => o.toLowerCase().includes(value.trim().toLowerCase()))
    : options;

  // Se o que já foi digitado bater (ignorando maiúsculas/acentuação de caixa) com uma opção
  // existente, usa a grafia canônica dela ao selecionar em vez de manter a digitada — é a parte
  // que evita duplicidade tipo "Produtividade" vs "produtividade" virarem duas categorias.
  const exactMatch = options.find(o => o.toLowerCase() === value.trim().toLowerCase());

  return (
    <div ref={containerRef} className="relative">
      <label className="text-sm font-medium text-gray-600">{label}</label>
      <input
        className="input-field mt-1"
        value={value}
        onChange={e => onChange(e.target.value)}
        onFocus={() => setOpen(true)}
        placeholder={placeholder}
        autoComplete="off"
      />
      {open && filtered.length > 0 && (
        <ul className="absolute z-10 mt-1 w-full max-h-56 overflow-auto rounded-lg border border-gray-200
                       bg-white shadow-lg py-1">
          {filtered.map(option => (
            <li key={option}>
              <button
                type="button"
                onMouseDown={e => e.preventDefault()}
                onClick={() => { onChange(option); setOpen(false); }}
                className="w-full text-left px-3 py-2 text-sm hover:bg-brand-50 flex items-center justify-between gap-2"
              >
                {option}
                {exactMatch === option && <Check className="w-3.5 h-3.5 text-brand-600 shrink-0" />}
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

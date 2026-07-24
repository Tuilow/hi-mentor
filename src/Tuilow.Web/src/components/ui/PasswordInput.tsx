'use client';

import { forwardRef, useState, InputHTMLAttributes } from 'react';
import { Eye, EyeOff } from 'lucide-react';

type PasswordInputProps = InputHTMLAttributes<HTMLInputElement>;

/**
 * Input de senha com botão de mostrar/ocultar (ícone de olho) — usado nos campos de senha e
 * confirmar senha do login, registro e redefinição de senha (Sprint Item 3). Aceita as mesmas
 * props de um <input> comum e encaminha o ref via forwardRef, então continua funcionando
 * normalmente com o registro do react-hook-form (`{...register('password')}`).
 */
export const PasswordInput = forwardRef<HTMLInputElement, PasswordInputProps>(
  ({ className, ...props }, ref) => {
    const [visible, setVisible] = useState(false);

    return (
      <div className="relative">
        <input
          {...props}
          ref={ref}
          type={visible ? 'text' : 'password'}
          className={`${className ?? ''} pr-10`}
        />
        <button
          type="button"
          onClick={() => setVisible((v) => !v)}
          // Evita que o botão entre na ordem de Tab do formulário (não é um campo, é um atalho visual).
          tabIndex={-1}
          aria-label={visible ? 'Ocultar senha' : 'Mostrar senha'}
          className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600
                     transition-colors"
        >
          {visible ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
        </button>
      </div>
    );
  }
);

PasswordInput.displayName = 'PasswordInput';

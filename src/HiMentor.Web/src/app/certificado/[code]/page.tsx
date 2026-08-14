'use client';

import { useQuery } from '@tanstack/react-query';
import { useParams } from 'next/navigation';
import Link from 'next/link';
import { certificatesApi } from '@/lib/api';

interface CertificateVerification {
  code: string;
  learnerName: string;
  courseTitle: string;
  issuedAt: string;
}

/**
 * Achado A4 da avaliação: página pública de verificação de autenticidade de certificado — não
 * exige login, é feita para ser conferida por qualquer pessoa (ex.: um recrutador validando um
 * certificado citado num currículo). Não há PDF para baixar aqui (ver nota no card abaixo) —
 * isto confirma que o certificado é real, não é o documento em si.
 */
export default function CertificateVerificationPage() {
  const { code } = useParams<{ code: string }>();

  const { data, isLoading, isError } = useQuery<CertificateVerification>({
    queryKey: ['certificate-verify', code],
    queryFn: () => certificatesApi.verify(code).then(r => r.data),
    enabled: !!code,
    retry: false,
  });

  return (
    <div className="min-h-screen bg-white flex items-center justify-center px-4">
      <div className="w-full max-w-md">
        <div className="text-center mb-8">
          <Link href="/" className="inline-block">
            <h1 className="text-2xl font-bold gradient-text">🎓 HiMentor</h1>
          </Link>
          <p className="text-gray-500 mt-2 text-sm">Verificação de certificado</p>
        </div>

        {isLoading && (
          <div className="card border-gray-200 text-center py-10">
            <p className="text-gray-400 text-sm">Verificando...</p>
          </div>
        )}

        {isError && !isLoading && (
          <div className="card border-red-200 bg-red-50/60 text-center py-10">
            <div className="text-4xl mb-3">⚠️</div>
            <h2 className="text-lg font-semibold text-gray-800">Certificado não encontrado</h2>
            <p className="text-sm text-gray-500 mt-1 max-w-xs mx-auto">
              O código <span className="font-mono">{code}</span> não corresponde a nenhum
              certificado emitido pela HiMentor.
            </p>
          </div>
        )}

        {data && !isLoading && !isError && (
          <div className="card border-emerald-100 bg-emerald-50/40 text-center py-10 px-6">
            <div className="text-4xl mb-3">🏆</div>
            <p className="text-xs font-semibold text-emerald-600 uppercase tracking-wider mb-3">
              Certificado autêntico
            </p>
            <h2 className="text-xl font-bold text-gray-800">{data.learnerName}</h2>
            <p className="text-gray-600 mt-1">concluiu o curso</p>
            <p className="text-lg font-semibold text-gray-800 mt-1">{data.courseTitle}</p>
            <p className="text-sm text-gray-400 mt-4">
              Emitido em {new Date(data.issuedAt).toLocaleDateString('pt-BR')}
            </p>
            <p className="text-xs text-gray-400 mt-1 font-mono">{data.code}</p>
          </div>
        )}

        <p className="text-center text-xs text-gray-400 mt-6">
          Esta página confirma a autenticidade do certificado — ainda não geramos um PDF para
          download automático.
        </p>
      </div>
    </div>
  );
}

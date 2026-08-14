'use client';

// Feature 12/08/2026 — aba "Certificados" do sidebar (ver (dashboard)/layout.tsx). Certificado é
// emitido automaticamente pelo backend quando um curso chega a 100% de progresso (ver
// CourseCompletedEventHandler, Learning.Application) — esta tela só lista o que já foi emitido
// (GET /certificates/me) e baixa o PDF gerado na hora (GET /certificates/{id}/download).
//
// Mesmo padrão de download por blob já usado em materialsApi.download
// ((dashboard)/cursos/[slug]/[lessonId]/page.tsx): um <a href> comum não manda o Bearer token
// exigido pelo endpoint, então o download passa pelo client autenticado (responseType 'blob') e
// o front-end monta um link temporário para disparar o "Salvar como".

import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { Award, Download, Loader2 } from 'lucide-react';
import { certificatesApi } from '@/lib/api';
import type { MyCertificate } from '@/types';
import { Card, EmptyState } from '@/components/ui/design-system';

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('pt-BR', { day: '2-digit', month: 'long', year: 'numeric' });
}

export default function CertificatesPage() {
  const [downloadingId, setDownloadingId] = useState<string | null>(null);

  const { data: certificates = [], isLoading } = useQuery<MyCertificate[]>({
    queryKey: ['my-certificates'],
    queryFn: () => certificatesApi.getMine().then(r => r.data),
  });

  const handleDownload = async (certificate: MyCertificate) => {
    setDownloadingId(certificate.certificateId);
    try {
      const { data } = await certificatesApi.download(certificate.certificateId);
      const blobUrl = URL.createObjectURL(data as Blob);
      const link = document.createElement('a');
      link.href = blobUrl;
      link.download = `certificado-${certificate.courseTitle}.pdf`;
      document.body.appendChild(link);
      link.click();
      link.remove();
      URL.revokeObjectURL(blobUrl);
    } catch {
      toast.error('Não foi possível baixar o certificado agora. Tente de novo em instantes.');
    } finally {
      setDownloadingId(null);
    }
  };

  return (
    <div className="max-w-5xl mx-auto">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-ink mb-1" style={{ letterSpacing: '-0.03em' }}>Certificados</h1>
        <p className="text-ink-3">Certificados emitidos automaticamente quando você conclui 100% de um programa.</p>
      </div>

      {isLoading && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
          {[1, 2, 3].map(i => (
            <Card key={i} className="animate-pulse">
              <div className="w-full h-32 bg-subtle rounded-xl mb-4" />
              <div className="h-4 bg-subtle rounded w-2/3 mb-2" />
              <div className="h-3 bg-subtle rounded w-1/3" />
            </Card>
          ))}
        </div>
      )}

      {!isLoading && certificates.length === 0 && (
        <EmptyState
          icon={<Award size={28} />}
          title="Nenhum certificado ainda"
          description="Assim que você concluir 100% de um programa, o certificado aparece aqui automaticamente."
        />
      )}

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
        {certificates.map((cert) => (
          <Card key={cert.certificateId} padding="none" className="overflow-hidden flex flex-col h-full">
            <div className="h-32 relative overflow-hidden bg-gradient-to-br from-violet-700 to-violet-900 flex items-center justify-center">
              {cert.thumbnailUrl ? (
                // eslint-disable-next-line @next/next/no-img-element
                <img src={cert.thumbnailUrl} alt={cert.courseTitle} className="absolute inset-0 w-full h-full object-cover opacity-40" />
              ) : null}
              <Award size={36} className="text-amber-300 relative" />
            </div>
            <div className="p-4 flex flex-col flex-1">
              <h3 className="font-bold text-ink mb-1 line-clamp-2">{cert.courseTitle}</h3>
              <p className="text-xs text-ink-3 mb-4">Concluído em {formatDate(cert.issuedAt)}</p>

              <button
                type="button"
                onClick={() => handleDownload(cert)}
                disabled={downloadingId === cert.certificateId}
                className="mt-auto flex items-center justify-center gap-2 bg-violet-600 hover:bg-violet-700 disabled:opacity-60 text-white font-semibold text-sm px-4 py-2.5 rounded-xl transition-all"
              >
                {downloadingId === cert.certificateId ? (
                  <>
                    <Loader2 size={16} className="animate-spin" /> Gerando PDF...
                  </>
                ) : (
                  <>
                    <Download size={16} /> Baixar certificado
                  </>
                )}
              </button>
            </div>
          </Card>
        ))}
      </div>
    </div>
  );
}

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using HiMentor.Learning.Application.Interfaces;

namespace HiMentor.Learning.Infrastructure.Services;

/// <summary>
/// Feature 12/08/2026 ("Baixar certificado"): renderização 100% vetorial com QuestPDF — sem
/// dependência de nenhum arquivo de logo (não existe nenhum asset de logo no repositório, ver
/// achado desta rodada). O wordmark "Hi Mentor" no topo é desenhado em código reproduzindo as
/// cores oficiais de src/components/brand/Logo.tsx (roxo #7c3aed / amarelo #fbbf24), em vez de
/// tentar embutir o SVG real (evita depender de parsing de SVG, que é uma API mais nova/menos
/// estável do QuestPDF).
///
/// QuestPDF.Settings.License = Community é gratuito para uso comercial com faturamento anual
/// abaixo do teto da licença (ver https://www.questpdf.com/license/) — plataforma deste porte se
/// enquadra; se a Hi Mentor crescer muito, vale reconferir o teto antes de assumir que continua
/// gratuito.
///
/// Nota para deploy em Linux (o projeto roda localmente em Windows hoje): QuestPDF usa
/// SkiaSharp por baixo dos panos — em uma imagem Linux "slim"/Alpine sem fontconfig, pode ser
/// necessário referenciar também o pacote `SkiaSharp.NativeAssets.Linux.NoDependencies` para o
/// PDF renderizar (no Windows isso não é necessário, os assets nativos já vêm com o pacote
/// principal do QuestPDF).
/// </summary>
public sealed class QuestPdfCertificateGenerator : ICertificatePdfGenerator
{
    private const string Purple = "#7c3aed";
    private const string Yellow = "#fbbf24";
    private const string InkDark = "#1e1b2e";
    private const string InkGray = "#6b6478";

    private static readonly string[] MonthNamesPtBr =
    [
        "janeiro", "fevereiro", "março", "abril", "maio", "junho",
        "julho", "agosto", "setembro", "outubro", "novembro", "dezembro"
    ];

    static QuestPdfCertificateGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Generate(CertificatePdfData data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(0);
                page.DefaultTextStyle(x => x.FontFamily("Arial"));
                page.Background(Colors.White);

                page.Content()
                    .Padding(26)
                    .Border(2).BorderColor(Purple)
                    .Padding(6)
                    .Border(1).BorderColor(Yellow)
                    .Padding(34)
                    .Column(column =>
                    {
                        // Wordmark "Hi Mentor" — mesmas cores de components/brand/Logo.tsx.
                        column.Item().AlignCenter().Row(row =>
                        {
                            // Sem CornerRadius aqui — não existe como extensão de IContainer na versão do
                            // QuestPDF restaurada pelo `dotnet restore` do usuário (erro de build real
                            // reportado por ele: "'IContainer' does not contain a definition for
                            // 'CornerRadius'"). Quadrado reto no lugar do quadrado arredondado do logo real
                            // — diferença puramente cosmética, não vale arriscar outra tentativa às cegas
                            // de API sem conseguir compilar neste ambiente.
                            row.AutoItem().Width(34).Height(34).Background(Purple)
                                .AlignCenter().AlignMiddle()
                                .Text("Hi").FontSize(15).Bold().FontColor(Yellow);
                            row.AutoItem().PaddingLeft(8).AlignMiddle()
                                .Text("Mentor").FontSize(20).Bold().FontColor(Purple);
                        });

                        column.Item().PaddingTop(24).AlignCenter()
                            .Text("Certificado").FontSize(40).Bold().FontColor(InkDark);

                        column.Item().PaddingTop(14).AlignCenter()
                            .Text("Este certificado comprova que").FontSize(13).Italic().FontColor(InkGray);

                        column.Item().PaddingTop(16).AlignCenter()
                            .Text(data.LearnerName).FontSize(28).Bold().FontColor(Purple);

                        column.Item().PaddingTop(6).AlignCenter().Width(360).Height(1).Background(Yellow);

                        // Container.Text(Action<TextDescriptor>) (em vez do atalho .Text(string)) porque o
                        // título do curso não tem tamanho fixo — pode quebrar em mais de uma linha, e só
                        // esta forma permite centralizar o parágrafo inteiro (text.AlignCenter()), não só a
                        // caixa que o envolve.
                        column.Item().PaddingTop(20).AlignCenter().Width(560).Text(text =>
                        {
                            text.AlignCenter();
                            text.DefaultTextStyle(x => x.FontSize(13).FontColor(InkDark));
                            text.Span($"concluiu com êxito o curso de “{data.CourseTitle}”, demonstrando comprometimento e dedicação em todas as aulas do programa.");
                        });

                        column.Item().PaddingTop(8).AlignCenter()
                            .Text($"Emitido em {FormatDate(data.IssuedAt)}").FontSize(11).FontColor(InkGray);

                        column.Item().PaddingTop(44).AlignCenter().Width(220).Height(1).Background(InkGray);
                        column.Item().PaddingTop(4).AlignCenter()
                            .Text("Direção Hi Mentor").FontSize(11).FontColor(InkGray);

                        column.Item().PaddingTop(26).AlignCenter()
                            .Text($"Código de verificação: {data.Code}").FontSize(9).FontColor(InkGray);
                    });
            });
        });

        return document.GeneratePdf();
    }

    private static string FormatDate(DateTime date) =>
        $"{date.Day} de {MonthNamesPtBr[date.Month - 1]} de {date.Year}";
}

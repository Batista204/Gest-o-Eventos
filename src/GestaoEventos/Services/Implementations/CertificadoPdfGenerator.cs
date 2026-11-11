using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GestaoEventos.Services.Implementations;

public static class CertificadoPdfGenerator
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static byte[] Gerar(DadosCertificado d, InstituicaoOptions inst, byte[]? logo, string urlValidacao)
    {
        var data = d.DataEvento.ToString("dd 'de' MMMM 'de' yyyy", PtBr);
        var carga = d.CargaHoraria.ToString("0.#", PtBr);
        var unidade = d.CargaHoraria == 1m ? "hora" : "horas";
        var emissao = d.DataEmissao.ToLocalTime().ToString("dd/MM/yyyy", PtBr);
        var azul = Colors.Blue.Darken3;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(28);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(t => t.FontSize(14).FontColor(Colors.Grey.Darken4));

                page.Content()
                    .Border(3).BorderColor(azul)
                    .Padding(24)
                    .AlignMiddle()
                    .Column(col =>
                    {
                        col.Spacing(10);

                        // Cabeçalho: logotipo (opcional) e nome da instituição
                        col.Item().Row(row =>
                        {
                            if (logo is not null)
                                row.ConstantItem(150).Height(56).Image(logo).FitArea();

                            row.RelativeItem().AlignMiddle().AlignCenter()
                               .Text(inst.Nome).FontSize(15).Bold().FontColor(azul);

                            if (logo is not null)
                                row.ConstantItem(150);   // equilibra o espaço do logotipo
                        });

                        col.Item().PaddingTop(12).AlignCenter()
                           .Text("CERTIFICADO").FontSize(32).Bold().FontColor(azul);

                        col.Item().AlignCenter().Text("Certificamos que");

                        col.Item().AlignCenter().Text(d.Nome).FontSize(26).Bold();

                        col.Item().AlignCenter().Text(t =>
                        {
                            t.AlignCenter();
                            t.Span("participou do evento ");
                            t.Span(d.Titulo).Bold();
                            t.Span($", realizado em {data}, com carga horária de {carga} {unidade}.");
                        });

                        // Assinatura (os dados vêm do appsettings; vazios, aparece só a linha)
                        col.Item().PaddingTop(14).AlignCenter().Width(280).Column(ass =>
                        {
                            ass.Item().PaddingTop(26).LineHorizontal(1).LineColor(Colors.Grey.Darken1);

                            ass.Item().AlignCenter()
                               .Text(string.IsNullOrWhiteSpace(inst.SignatarioNome) ? "Assinatura" : inst.SignatarioNome)
                               .FontSize(12).Bold();

                            if (!string.IsNullOrWhiteSpace(inst.SignatarioCargo))
                                ass.Item().AlignCenter().Text(inst.SignatarioCargo).FontSize(11);
                        });
                    });

                page.Footer().PaddingTop(6).Column(f =>
                {
                    f.Item().AlignCenter().Text(t =>
                    {
                        t.Span("Código de verificação: ").FontSize(11);
                        t.Span(d.Codigo).FontSize(11).Bold();
                    });

                    f.Item().AlignCenter()
                       .Text($"Emitido em {emissao}. Para conferir a autenticidade, acesse {urlValidacao} e informe o código.")
                       .FontSize(9).FontColor(Colors.Grey.Darken2);
                });
            });
        }).GeneratePdf();
    }
}
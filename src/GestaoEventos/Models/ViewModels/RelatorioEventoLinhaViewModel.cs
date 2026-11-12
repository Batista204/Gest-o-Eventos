using GestaoEventos.Models.Enums;

namespace GestaoEventos.Models.ViewModels;

public class RelatorioEventoLinhaViewModel
{
    public int EventoId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public DateOnly DataEvento { get; set; }
    public StatusEvento Status { get; set; }
    public int LimiteVagas { get; set; }

    public int InscritosAtivos { get; set; }
    public int Cancelados { get; set; }
    public int Presentes { get; set; }
    public int Ausentes { get; set; }
    public int CertificadosEmitidos { get; set; }

    // Só tem sentido com o evento encerrado e a presença registrada
    public decimal? TaxaPresenca =>
        Status == StatusEvento.Encerrado && Presentes + Ausentes > 0 && InscritosAtivos > 0
            ? Math.Round(Presentes * 100m / InscritosAtivos, 0)
            : null;
}
namespace GestaoEventos.Models.ViewModels;

public class HistoricoItemViewModel
{
    public int InscricaoId { get; set; }
    public int EventoId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Local { get; set; } = string.Empty;
    public DateOnly DataEvento { get; set; }
    public decimal CargaHoraria { get; set; }
    public bool? Presente { get; set; }
    public bool CertificadosLiberados { get; set; }
    public string? CertificadoCodigo { get; set; }   // só se o certificado for válido
}

public class HistoricoViewModel
{
    public List<HistoricoItemViewModel> Itens { get; set; } = new();

    public int EventosComPresenca => Itens.Count(i => i.Presente == true);
    public int CertificadosEmitidos => Itens.Count(i => i.CertificadoCodigo is not null);

    // RN35: só certificados válidos entram na soma
    public decimal HorasCertificadas => Itens.Where(i => i.CertificadoCodigo is not null).Sum(i => i.CargaHoraria);
}
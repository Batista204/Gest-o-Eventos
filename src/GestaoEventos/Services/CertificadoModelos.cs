namespace GestaoEventos.Services;

// Dados que entram no desenho do PDF
public class DadosCertificado
{
    public string Nome { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public DateOnly DataEvento { get; set; }
    public decimal CargaHoraria { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public DateTime DataEmissao { get; set; }   // UTC
}

// Resultado da geração: o arquivo e o nome sugerido para o download
public record CertificadoPdf(byte[] Conteudo, string NomeArquivo);
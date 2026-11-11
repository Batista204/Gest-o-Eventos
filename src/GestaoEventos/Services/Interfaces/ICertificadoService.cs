using GestaoEventos.Models.ViewModels;

namespace GestaoEventos.Services.Interfaces;

public interface ICertificadoService
{
    Task<CertificadoResumoViewModel?> ObterResumoAsync(int eventoId);
    Task<Resultado> LiberarAsync(int eventoId);
    Task<CertificadoPdf?> GerarPdfAsync(int inscricaoId, string? usuarioId, string urlValidacao);
    Task<CertificadoValidacaoViewModel> ValidarAsync(string? codigoDigitado);
}
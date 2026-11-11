using GestaoEventos.Models.ViewModels;

namespace GestaoEventos.Services.Interfaces;

public interface ICertificadoService
{
    Task<CertificadoResumoViewModel?> ObterResumoAsync(int eventoId);
    Task<Resultado> LiberarAsync(int eventoId);
}
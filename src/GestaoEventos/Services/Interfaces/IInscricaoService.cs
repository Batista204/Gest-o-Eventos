using GestaoEventos.Models.ViewModels;

namespace GestaoEventos.Services.Interfaces;

public interface IInscricaoService
{
    Task<List<EventoDisponivelViewModel>> ListarEventosAbertosAsync(string usuarioId);
    Task<EventoDisponivelDetalhesViewModel?> ObterEventoAsync(int eventoId, string usuarioId);
    Task<Resultado> InscreverAsync(int eventoId, string usuarioId);
    Task<Resultado> CancelarAsync(int inscricaoId, string usuarioId);
    Task<List<MinhaInscricaoViewModel>> ListarMinhasAsync(string usuarioId);
}
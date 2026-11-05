using GestaoEventos.Models.Enums;
using GestaoEventos.Models.ViewModels;

namespace GestaoEventos.Services.Interfaces;

public interface IEventoService
{
    Task<List<EventoListaViewModel>> ListarAsync(StatusEvento? status);
    Task<EventoDetalhesViewModel?> ObterDetalhesAsync(int id);
    Task<EventoFormViewModel?> ObterParaEdicaoAsync(int id);
    Task<Resultado> CriarAsync(EventoFormViewModel modelo, string organizadorId);
    Task<Resultado> AtualizarAsync(EventoFormViewModel modelo);
    Task<Resultado> AlterarStatusAsync(int id, StatusEvento novoStatus);
    Task<Resultado> ExcluirAsync(int id);
}
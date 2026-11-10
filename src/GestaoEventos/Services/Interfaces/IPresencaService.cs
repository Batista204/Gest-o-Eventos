using GestaoEventos.Models.ViewModels;

namespace GestaoEventos.Services.Interfaces;

public interface IPresencaService
{
    Task<PresencaListaViewModel?> ObterListaAsync(int eventoId);
    Task<Resultado> SalvarAsync(int eventoId, IReadOnlyList<PresencaItemViewModel> itens, string registradoPorId);
}
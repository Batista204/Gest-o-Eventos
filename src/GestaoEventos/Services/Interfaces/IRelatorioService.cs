using GestaoEventos.Models.Enums;
using GestaoEventos.Models.ViewModels;

namespace GestaoEventos.Services.Interfaces;

public interface IRelatorioService
{
    Task<List<RelatorioEventoLinhaViewModel>> ResumoEventosAsync(StatusEvento? status);
    Task<RelatorioEventoDetalheViewModel?> DetalheEventoAsync(int eventoId);
    Task<ArquivoCsv?> GerarCsvAsync(int eventoId);
    Task<HistoricoViewModel> HistoricoAsync(string usuarioId);
}
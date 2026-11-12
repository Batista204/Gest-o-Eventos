using GestaoEventos.Models.Constants;
using GestaoEventos.Models.Enums;
using GestaoEventos.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestaoEventos.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Perfis.Admin)]
public class RelatoriosController : Controller
{
    private readonly IRelatorioService _service;

    public RelatoriosController(IRelatorioService service) => _service = service;

    public async Task<IActionResult> Index(StatusEvento? status)
    {
        ViewData["Filtro"] = status;
        return View(await _service.ResumoEventosAsync(status));
    }

    // O "id" da rota é o id do evento
    public async Task<IActionResult> Evento(int id)
    {
        var relatorio = await _service.DetalheEventoAsync(id);
        return relatorio is null ? NotFound() : View(relatorio);
    }

    public async Task<IActionResult> Csv(int id)
    {
        var arquivo = await _service.GerarCsvAsync(id);
        return arquivo is null
            ? NotFound()
            : File(arquivo.Conteudo, "text/csv; charset=utf-8", arquivo.NomeArquivo);
    }
}
using System.Security.Claims;
using GestaoEventos.Models.Constants;
using GestaoEventos.Models.ViewModels;
using GestaoEventos.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestaoEventos.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Perfis.Admin)]
public class PresencaController : Controller
{
    private readonly IPresencaService _service;

    public PresencaController(IPresencaService service) => _service = service;

    // O "id" da rota é o id do evento
    public async Task<IActionResult> Index(int id)
    {
        var lista = await _service.ObterListaAsync(id);
        return lista is null ? NotFound() : View(lista);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Salvar(int eventoId, List<PresencaItemViewModel> itens)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var resultado = await _service.SalvarAsync(eventoId, itens, adminId);

        if (resultado.Sucesso)
            TempData["Sucesso"] = "Presenças salvas com sucesso.";
        else
            TempData["Erro"] = resultado.Erros[0].Mensagem;

        return RedirectToAction(nameof(Index), new { id = eventoId });
    }
}
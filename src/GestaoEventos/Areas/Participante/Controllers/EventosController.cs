using System.Security.Claims;
using GestaoEventos.Models.Constants;
using GestaoEventos.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestaoEventos.Areas.Participante.Controllers;

[Area("Participante")]
[Authorize(Roles = Perfis.Participante)]
public class EventosController : Controller
{
    private readonly IInscricaoService _service;

    public EventosController(IInscricaoService service) => _service = service;

    private string UsuarioId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index()
        => View(await _service.ListarEventosAbertosAsync(UsuarioId));

    public async Task<IActionResult> Details(int id)
    {
        var evento = await _service.ObterEventoAsync(id, UsuarioId);
        return evento is null ? NotFound() : View(evento);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Inscrever(int id)
    {
        var resultado = await _service.InscreverAsync(id, UsuarioId);

        if (resultado.Sucesso)
            TempData["Sucesso"] = "Inscrição realizada com sucesso.";
        else
            TempData["Erro"] = resultado.Erros[0].Mensagem;

        return RedirectToAction(nameof(Details), new { id });
    }
}
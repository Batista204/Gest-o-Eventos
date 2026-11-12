using System.Security.Claims;
using GestaoEventos.Models.Constants;
using GestaoEventos.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestaoEventos.Areas.Participante.Controllers;

[Area("Participante")]
[Authorize(Roles = Perfis.Participante)]
public class HistoricoController : Controller
{
    private readonly IRelatorioService _service;

    public HistoricoController(IRelatorioService service) => _service = service;

    public async Task<IActionResult> Index()
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return View(await _service.HistoricoAsync(usuarioId));
    }
}
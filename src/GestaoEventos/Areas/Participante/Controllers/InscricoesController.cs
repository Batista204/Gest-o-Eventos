using System.Security.Claims;
using GestaoEventos.Models.Constants;
using GestaoEventos.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestaoEventos.Areas.Participante.Controllers;

[Area("Participante")]
[Authorize(Roles = Perfis.Participante)]
public class InscricoesController : Controller
{
    private readonly IInscricaoService _service;

    public InscricoesController(IInscricaoService service) => _service = service;

    private string UsuarioId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index()
        => View(await _service.ListarMinhasAsync(UsuarioId));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(int id, string? origem)
    {
        var resultado = await _service.CancelarAsync(id, UsuarioId);

        if (resultado.Sucesso)
            TempData["Sucesso"] = "Inscrição cancelada.";
        else
            TempData["Erro"] = resultado.Erros[0].Mensagem;

        return RedirectToAction(nameof(Index));
    }
}
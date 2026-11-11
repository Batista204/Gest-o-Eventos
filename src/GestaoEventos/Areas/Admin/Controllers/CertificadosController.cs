using GestaoEventos.Models.Constants;
using GestaoEventos.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestaoEventos.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Perfis.Admin)]
public class CertificadosController : Controller
{
    private readonly ICertificadoService _service;

    public CertificadosController(ICertificadoService service) => _service = service;

    // O "id" da rota é o id do evento
    public async Task<IActionResult> Index(int id)
    {
        var resumo = await _service.ObterResumoAsync(id);
        return resumo is null ? NotFound() : View(resumo);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Liberar(int id)
    {
        var resultado = await _service.LiberarAsync(id);

        if (resultado.Sucesso)
            TempData["Sucesso"] = $"{resultado.Quantidade} certificado(s) liberado(s) com sucesso.";
        else
            TempData["Erro"] = resultado.Erros[0].Mensagem;

        return RedirectToAction(nameof(Index), new { id });
    }
}
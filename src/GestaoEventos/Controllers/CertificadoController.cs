using GestaoEventos.Models.ViewModels;
using GestaoEventos.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GestaoEventos.Controllers;

[AllowAnonymous]
public class CertificadoController : Controller
{
    private readonly ICertificadoService _service;

    public CertificadoController(ICertificadoService service) => _service = service;

    // Consulta por GET para o link poder ser compartilhado; limitada por IP
    [HttpGet, EnableRateLimiting("validacao")]
    public async Task<IActionResult> Validar(string? codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            return View(new CertificadoValidacaoViewModel());

        return View(await _service.ValidarAsync(codigo));
    }
}
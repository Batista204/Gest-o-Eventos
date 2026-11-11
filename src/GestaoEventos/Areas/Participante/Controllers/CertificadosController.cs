using System.Security.Claims;
using GestaoEventos.Models.Constants;
using GestaoEventos.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestaoEventos.Areas.Participante.Controllers;

[Area("Participante")]
[Authorize(Roles = Perfis.Participante)]
public class CertificadosController : Controller
{
    private readonly ICertificadoService _service;

    public CertificadosController(ICertificadoService service) => _service = service;

    public async Task<IActionResult> Baixar(int id)
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var urlValidacao = Url.Action("Validar", "Certificado", new { area = "" }, Request.Scheme)!;

        var pdf = await _service.GerarPdfAsync(id, usuarioId, urlValidacao);

        // Não existe, não é seu ou foi invalidado: mesma resposta, sem dar pistas
        return pdf is null ? NotFound() : File(pdf.Conteudo, "application/pdf", pdf.NomeArquivo);
    }
}
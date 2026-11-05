using System.Security.Claims;
using GestaoEventos.Models.Constants;
using GestaoEventos.Models.Enums;
using GestaoEventos.Models.ViewModels;
using GestaoEventos.Services;
using GestaoEventos.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestaoEventos.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Perfis.Admin)]
public class EventosController : Controller
{
    private readonly IEventoService _service;

    public EventosController(IEventoService service) => _service = service;

    public async Task<IActionResult> Index(StatusEvento? status)
    {
        ViewData["Filtro"] = status;
        return View(await _service.ListarAsync(status));
    }

    public async Task<IActionResult> Details(int id)
    {
        var evento = await _service.ObterDetalhesAsync(id);
        return evento is null ? NotFound() : View(evento);
    }

    [HttpGet]
    public IActionResult Create() => View(new EventoFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventoFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var organizadorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var resultado = await _service.CriarAsync(model, organizadorId);

        if (!resultado.Sucesso)
        {
            AdicionarErros(resultado);
            return View(model);
        }

        TempData["Sucesso"] = "Evento criado como Planejado. Quando estiver pronto, abra as inscrições.";
        return RedirectToAction(nameof(Details), new { id = resultado.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var evento = await _service.ObterParaEdicaoAsync(id);
        if (evento is null)
            return NotFound();

        if (evento.Status is StatusEvento.Encerrado or StatusEvento.Cancelado)
        {
            TempData["Erro"] = "Eventos encerrados ou cancelados não podem ser editados.";
            return RedirectToAction(nameof(Details), new { id });
        }

        return View(evento);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EventoFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var resultado = await _service.AtualizarAsync(model);

        if (!resultado.Sucesso)
        {
            AdicionarErros(resultado);
            return View(model);
        }

        TempData["Sucesso"] = "Evento atualizado com sucesso.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AlterarStatus(int id, StatusEvento novoStatus)
    {
        var resultado = await _service.AlterarStatusAsync(id, novoStatus);

        if (resultado.Sucesso)
            TempData["Sucesso"] = $"Status alterado para {novoStatus}.";
        else
            TempData["Erro"] = resultado.Erros[0].Mensagem;

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Excluir(int id)
    {
        var resultado = await _service.ExcluirAsync(id);

        if (!resultado.Sucesso)
        {
            TempData["Erro"] = resultado.Erros[0].Mensagem;
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Sucesso"] = "Evento excluído.";
        return RedirectToAction(nameof(Index));
    }

    private void AdicionarErros(Resultado resultado)
    {
        foreach (var erro in resultado.Erros)
            ModelState.AddModelError(erro.Campo ?? string.Empty, erro.Mensagem);
    }
}
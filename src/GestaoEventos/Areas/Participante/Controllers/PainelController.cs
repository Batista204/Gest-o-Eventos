using GestaoEventos.Models.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestaoEventos.Areas.Participante.Controllers;

[Area("Participante")]
[Authorize(Roles = Perfis.Participante)]
public class PainelController : Controller
{
    public IActionResult Index() => View();
}
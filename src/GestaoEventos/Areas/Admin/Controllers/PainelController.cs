using GestaoEventos.Models.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestaoEventos.Areas.Admin.Controllers;

// [Area] associa o controller à área; [Authorize] restringe ao perfil Admin
[Area("Admin")]
[Authorize(Roles = Perfis.Admin)]
public class PainelController : Controller
{
    public IActionResult Index() => View();
}
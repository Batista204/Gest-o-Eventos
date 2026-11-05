using GestaoEventos.Models.Constants;
using GestaoEventos.Models.Entities;
using GestaoEventos.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GestaoEventos.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<Usuario> _userManager;
    private readonly SignInManager<Usuario> _signInManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<Usuario> userManager,
        SignInManager<Usuario> signInManager,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    // ---------- Cadastro ----------

    [HttpGet, AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        return View();
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var email = model.Email.Trim();
        var usuario = new Usuario
        {
            UserName = email,
            Email = email,
            NomeCompleto = model.NomeCompleto.Trim(),
            Matricula = string.IsNullOrWhiteSpace(model.Matricula) ? null : model.Matricula.Trim(),
            DataCadastro = DateTime.UtcNow
        };

        var resultado = await _userManager.CreateAsync(usuario, model.Senha);
        if (!resultado.Succeeded)
        {
            foreach (var mensagem in resultado.Errors.Select(TraduzirErro).Distinct())
                ModelState.AddModelError(string.Empty, mensagem);
            return View(model);
        }

        // Cadastro público sempre cria Participante (RN22)
        var resultadoPerfil = await _userManager.AddToRoleAsync(usuario, Perfis.Participante);
        if (!resultadoPerfil.Succeeded)
        {
            _logger.LogError("Falha ao atribuir o perfil Participante ao usuário {Email}.", email);
            await _userManager.DeleteAsync(usuario);   // evita usuário sem perfil
            ModelState.AddModelError(string.Empty, "Não foi possível concluir o cadastro. Tente novamente.");
            return View(model);
        }

        await _signInManager.SignInAsync(usuario, isPersistent: false);
        return RedirectToAction("Index", "Home");
    }

    // ---------- Login ----------

    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
            return View(model);

        var usuario = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (usuario is not null)
        {
            var resultado = await _signInManager.PasswordSignInAsync(
                usuario, model.Senha, model.LembrarMe, lockoutOnFailure: true);

            if (resultado.Succeeded)
                return RedirecionarAposLogin(returnUrl);

            if (resultado.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty,
                    "Muitas tentativas inválidas. Tente novamente em alguns minutos.");
                return View(model);
            }
        }

        // Mensagem genérica: não revela se o e-mail existe
        ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos.");
        return View(model);
    }

    // ---------- Logout ----------

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    // ---------- Acesso negado ----------

    [HttpGet, AllowAnonymous]
    public IActionResult AccessDenied() => View();

    // ---------- Auxiliares ----------

    private IActionResult RedirecionarAposLogin(string? returnUrl)
    {
        // Só aceita URLs locais, evitando redirecionamento para sites externos (open redirect)
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction("Index", "Home");   // ajustado por perfil na etapa 2.4
    }

    // O Identity devolve mensagens em inglês; aqui traduzimos as mais comuns.
    private static string TraduzirErro(IdentityError erro) => erro.Code switch
    {
        "DuplicateUserName" or "DuplicateEmail" => "Este e-mail já está cadastrado.",
        "InvalidEmail" or "InvalidUserName" => "E-mail inválido.",
        "PasswordTooShort" => "A senha deve ter no mínimo 8 caracteres.",
        "PasswordRequiresDigit" => "A senha deve conter ao menos um número.",
        "PasswordRequiresLower" => "A senha deve conter ao menos uma letra minúscula.",
        "PasswordRequiresUpper" => "A senha deve conter ao menos uma letra maiúscula.",
        "PasswordRequiresNonAlphanumeric" => "A senha deve conter ao menos um caractere especial.",
        _ => "Não foi possível concluir a operação. Verifique os dados informados."
    };
}
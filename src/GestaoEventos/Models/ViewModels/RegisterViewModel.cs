using System.ComponentModel.DataAnnotations;

namespace GestaoEventos.Models.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Informe o nome completo.")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 150 caracteres.")]
    [Display(Name = "Nome completo")]
    public string NomeCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [StringLength(256)]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [StringLength(30, ErrorMessage = "A matrícula deve ter no máximo 30 caracteres.")]
    [Display(Name = "Matrícula (opcional)")]
    public string? Matricula { get; set; }

    [Required(ErrorMessage = "Informe a senha.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "A senha deve ter no mínimo 8 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Senha")]
    public string Senha { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme a senha.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Senha), ErrorMessage = "As senhas não conferem.")]
    [Display(Name = "Confirmar senha")]
    public string ConfirmarSenha { get; set; } = string.Empty;
}
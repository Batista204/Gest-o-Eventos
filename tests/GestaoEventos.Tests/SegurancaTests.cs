using System.Reflection;
using GestaoEventos.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace GestaoEventos.Tests;

public class SegurancaTests
{
    private static IEnumerable<Type> Controllers() =>
        typeof(AppDbContext).Assembly.GetTypes()
            .Where(t => typeof(Controller).IsAssignableFrom(t) && !t.IsAbstract);

    [Fact]
    public void Todo_controller_de_area_exige_login_e_perfil()
    {
        var semProtecao = Controllers()
            .Where(t => t.GetCustomAttribute<AreaAttribute>() is not null)
            .Where(t => string.IsNullOrWhiteSpace(t.GetCustomAttribute<AuthorizeAttribute>()?.Roles))
            .Select(t => t.FullName)
            .ToList();

        Assert.Empty(semProtecao);
    }

    [Fact]
    public void Toda_acao_post_tem_protecao_contra_csrf()
    {
        var semProtecao = new List<string>();

        foreach (var controller in Controllers())
        {
            var protegidoNaClasse = controller.GetCustomAttribute<AutoValidateAntiforgeryTokenAttribute>() is not null;

            foreach (var acao in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (acao.GetCustomAttribute<HttpPostAttribute>() is null)
                    continue;

                var protegido = protegidoNaClasse
                    || acao.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>() is not null;

                if (!protegido)
                    semProtecao.Add($"{controller.Name}.{acao.Name}");
            }
        }

        Assert.Empty(semProtecao);
    }
}
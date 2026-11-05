using GestaoEventos.Models.Constants;
using GestaoEventos.Models.Entities;
using Microsoft.AspNetCore.Identity;

namespace GestaoEventos.Data;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeeder");

        // 1. Perfis
        foreach (var perfil in new[] { Perfis.Admin, Perfis.Participante })
        {
            if (!await roleManager.RoleExistsAsync(perfil))
                await roleManager.CreateAsync(new IdentityRole(perfil));
        }

        // 2. Administrador inicial (credenciais vêm dos User Secrets, nunca do código)
        var email = config["SeedAdmin:Email"];
        var senha = config["SeedAdmin:Senha"];
        var nome = config["SeedAdmin:Nome"] ?? "Administrador";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
        {
            logger.LogWarning("SeedAdmin não configurado: nenhum administrador foi criado.");
            return;
        }

        var admin = await userManager.FindByEmailAsync(email);
        if (admin is null)
        {
            admin = new Usuario
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                NomeCompleto = nome,
                DataCadastro = DateTime.UtcNow
            };

            var resultado = await userManager.CreateAsync(admin, senha);
            if (!resultado.Succeeded)
            {
                var erros = string.Join("; ", resultado.Errors.Select(e => e.Description));
                logger.LogError("Falha ao criar o administrador inicial: {Erros}", erros);
                return;
            }
        }

        if (!await userManager.IsInRoleAsync(admin, Perfis.Admin))
            await userManager.AddToRoleAsync(admin, Perfis.Admin);
    }
}
using GestaoEventos.Data;
using Microsoft.EntityFrameworkCore;

namespace GestaoEventos.Tests;

public sealed class BancoDeTestes : IAsyncDisposable
{
    private readonly string _conexao;

    private BancoDeTestes(string conexao) => _conexao = conexao;

    public static async Task<BancoDeTestes> CriarAsync()
    {
        var servidor = Environment.GetEnvironmentVariable("TESTES_SQLSERVER") ?? @"localhost\SQLEXPRESS";
        var nome = $"GestaoEventos_Teste_{Guid.NewGuid():N}";
        var banco = new BancoDeTestes(
            $"Server={servidor};Database={nome};Trusted_Connection=True;TrustServerCertificate=True;");

        // Cria as tabelas a partir do modelo (com índices únicos, restrições e rowversion)
        await using var db = banco.NovoContexto();
        await db.Database.EnsureCreatedAsync();
        return banco;
    }

    // Cada operação simultânea precisa do seu próprio contexto (o DbContext não é thread-safe)
    public AppDbContext NovoContexto() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(_conexao).Options);

    public async ValueTask DisposeAsync()
    {
        await using var db = NovoContexto();
        await db.Database.EnsureDeletedAsync();
    }
}
using GestaoEventos.Data;
using GestaoEventos.Models.Entities;
using GestaoEventos.Models.Enums;

namespace GestaoEventos.Tests;

public record CenarioEvento(Evento Evento, Usuario Organizador, List<Usuario> Participantes);

public static class Cenario
{
    public static Usuario NovoUsuario(string nome)
    {
        var email = $"{Guid.NewGuid():N}@teste.local";
        return new Usuario
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NomeCompleto = nome
        };
    }

    // hoje = true: o evento é de hoje (permite registrar presença); false: é de amanhã
    public static async Task<CenarioEvento> CriarEventoAsync(
        AppDbContext db, int vagas, int participantes,
        StatusEvento status = StatusEvento.Aberto, bool hoje = false)
    {
        var organizador = NovoUsuario("Organizador");
        var lista = Enumerable.Range(1, participantes)
            .Select(i => NovoUsuario($"Participante {i:00}")).ToList();

        db.Users.Add(organizador);
        db.Users.AddRange(lista);

        var evento = new Evento
        {
            Titulo = "Evento de teste",
            Descricao = "Descrição de teste",
            Local = "Sala de teste",
            DataEvento = DateOnly.FromDateTime(hoje ? DateTime.Today : DateTime.Today.AddDays(1)),
            HoraInicio = new TimeOnly(19, 0),
            HoraFim = new TimeOnly(21, 0),
            CargaHoraria = 2,
            LimiteVagas = vagas,
            InicioInscricao = DateTime.Now.AddHours(-2),
            FimInscricao = DateTime.Now.AddHours(2),
            Status = status,
            OrganizadorId = organizador.Id
        };
        db.Eventos.Add(evento);

        await db.SaveChangesAsync();
        return new CenarioEvento(evento, organizador, lista);
    }

    // Inscreve os participantes na ordem. Em "presencas": true = presente, false = ausente, null = sem registro
    public static async Task<List<Inscricao>> InscreverAsync(
        AppDbContext db, CenarioEvento cenario, params bool?[] presencas)
    {
        var inscricoes = cenario.Participantes.Select(p => new Inscricao
        {
            EventoId = cenario.Evento.Id,
            UsuarioId = p.Id,
            DataInscricao = DateTime.UtcNow,
            Status = StatusInscricao.Ativa
        }).ToList();

        db.Inscricoes.AddRange(inscricoes);
        await db.SaveChangesAsync();

        for (var i = 0; i < inscricoes.Count && i < presencas.Length; i++)
        {
            if (presencas[i] is bool presente)
            {
                db.Presencas.Add(new Presenca
                {
                    InscricaoId = inscricoes[i].Id,
                    Presente = presente,
                    DataRegistro = DateTime.UtcNow,
                    RegistradaPorId = cenario.Organizador.Id
                });
            }
        }

        await db.SaveChangesAsync();
        return inscricoes;
    }
}
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using GestaoEventos.Data;
using GestaoEventos.Models.Entities;
using GestaoEventos.Models.Enums;
using GestaoEventos.Models.ViewModels;
using GestaoEventos.Services.Interfaces;
using Microsoft.EntityFrameworkCore;


namespace GestaoEventos.Services.Implementations;

public class CertificadoService : ICertificadoService
{
    // Alfabeto sem caracteres ambíguos (sem 0, O, 1, I, L): 31 símbolos
    private const string Alfabeto = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    private readonly AppDbContext _db;
    private readonly InstituicaoOptions _instituicao;
    private readonly IWebHostEnvironment _env;

    public CertificadoService(AppDbContext db, IOptions<InstituicaoOptions> instituicao, IWebHostEnvironment env)
    {
        _db = db;
        _instituicao = instituicao.Value;
        _env = env;
    }

    public CertificadoService(AppDbContext db) => _db = db;

    // ---------- Consulta ----------

    public async Task<CertificadoResumoViewModel?> ObterResumoAsync(int eventoId)
    {
        var evento = await _db.Eventos.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventoId);
        if (evento is null)
            return null;

        // RN12: só inscrições ativas
        var itens = await _db.Inscricoes
            .AsNoTracking()
            .Where(i => i.EventoId == eventoId && i.Status == StatusInscricao.Ativa)
            .OrderBy(i => i.Usuario.NomeCompleto)
            .Select(i => new CertificadoItemViewModel
            {
                InscricaoId = i.Id,
                Nome = i.Usuario.NomeCompleto,
                Email = i.Usuario.Email ?? string.Empty,
                Matricula = i.Usuario.Matricula,
                Presente = i.Presenca == null ? (bool?)null : i.Presenca.Presente,
                Codigo = i.Certificado == null ? null : i.Certificado.CodigoVerificacao,
                DataEmissao = i.Certificado == null ? (DateTime?)null : i.Certificado.DataEmissao
            })
            .ToListAsync();

        var resumo = new CertificadoResumoViewModel
        {
            EventoId = evento.Id,
            Titulo = evento.Titulo,
            DataEvento = evento.DataEvento,
            HoraInicio = evento.HoraInicio,
            Status = evento.Status,
            LiberadosEm = evento.CertificadosLiberadosEm,
            Itens = itens
        };

        resumo.Aviso = evento.Status switch
        {
            StatusEvento.Cancelado => "Evento cancelado: não gera certificados.",
            StatusEvento.Encerrado => null,
            _ => "Os certificados só podem ser liberados depois que o evento for encerrado."
        };

        if (resumo.Aviso is null && evento.CertificadosLiberadosEm is null && resumo.Elegiveis == 0)
            resumo.Aviso = "Nenhum participante é elegível: é preciso ter presença confirmada.";

        resumo.PodeLiberar = resumo.Aviso is null && evento.CertificadosLiberadosEm is null;
        return resumo;
    }

    // ---------- Liberação ----------

    public async Task<Resultado> LiberarAsync(int eventoId)
    {
        var evento = await _db.Eventos.FirstOrDefaultAsync(e => e.Id == eventoId);
        if (evento is null)
            return Resultado.Falha("Evento não encontrado.");

        // As regras são conferidas de novo aqui, e não só na tela
        if (evento.Status != StatusEvento.Encerrado)
            return Resultado.Falha("Os certificados só podem ser liberados depois que o evento for encerrado.");

        // RN31: liberação única
        if (evento.CertificadosLiberadosEm is not null)
            return Resultado.Falha("Os certificados deste evento já foram liberados.");

        // RN16: inscrição ativa + evento encerrado + presença confirmada
        var elegiveis = await _db.Inscricoes
            .Where(i => i.EventoId == eventoId
                        && i.Status == StatusInscricao.Ativa
                        && i.Presenca != null && i.Presenca.Presente
                        && i.Certificado == null)
            .Select(i => i.Id)
            .ToListAsync();

        if (elegiveis.Count == 0)
            return Resultado.Falha("Nenhum participante é elegível: é preciso ter presença confirmada.");

        var agora = DateTime.UtcNow;
        var usados = new HashSet<string>();

        foreach (var inscricaoId in elegiveis)
        {
            _db.Certificados.Add(new Certificado
            {
                InscricaoId = inscricaoId,
                CodigoVerificacao = await GerarCodigoUnicoAsync(usados),
                DataEmissao = agora,
                Valido = true
            });
        }

        evento.CertificadosLiberadosEm = agora;

        try
        {
            // Uma única gravação: ou todos os certificados são criados, ou nenhum
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Resultado.Falha("O evento foi alterado por outra pessoa. Recarregue a página e tente novamente.");
        }
        catch (DbUpdateException)
        {
            return Resultado.Falha("Não foi possível liberar os certificados. Recarregue a página e confira o resultado.");
        }

        var ok = Resultado.Ok();
        ok.Quantidade = elegiveis.Count;
        return ok;
    }

    // ---------- Código de verificação (RN19 e RN32) ----------

    private async Task<string> GerarCodigoUnicoAsync(HashSet<string> usados)
    {
        while (true)
        {
            var codigo = GerarCodigo();

            if (usados.Contains(codigo))
                continue;
            if (await _db.Certificados.AnyAsync(c => c.CodigoVerificacao == codigo))
                continue;

            usados.Add(codigo);
            return codigo;
        }
    }

    // Formato XXXX-XXXX-XXXX (14 caracteres). RandomNumberGenerator é criptograficamente seguro.
    private static string GerarCodigo()
    {
        var chars = new char[14];
        var pos = 0;

        for (var i = 0; i < 12; i++)
        {
            if (i > 0 && i % 4 == 0)
                chars[pos++] = '-';
            chars[pos++] = Alfabeto[RandomNumberGenerator.GetInt32(Alfabeto.Length)];
        }

        return new string(chars);
    }

    // ---------- PDF (RF22 e RF23) ----------

    // usuarioId = null: acesso do administrador. Preenchido: só o dono do certificado (RN33).
    public async Task<CertificadoPdf?> GerarPdfAsync(int inscricaoId, string? usuarioId, string urlValidacao)
    {
        var consulta = _db.Certificados.AsNoTracking()
            .Where(c => c.InscricaoId == inscricaoId
                        && c.Valido
                        && c.Inscricao.Status == StatusInscricao.Ativa
                        && c.Inscricao.Evento.Status == StatusEvento.Encerrado
                        && c.Inscricao.Evento.CertificadosLiberadosEm != null);

        if (usuarioId is not null)
            consulta = consulta.Where(c => c.Inscricao.UsuarioId == usuarioId);

        var dados = await consulta
            .Select(c => new DadosCertificado
            {
                Nome = c.Inscricao.Usuario.NomeCompleto,
                Titulo = c.Inscricao.Evento.Titulo,
                DataEvento = c.Inscricao.Evento.DataEvento,
                CargaHoraria = c.Inscricao.Evento.CargaHoraria,
                Codigo = c.CodigoVerificacao,
                DataEmissao = c.DataEmissao
            })
            .FirstOrDefaultAsync();

        if (dados is null)
            return null;

        var conteudo = CertificadoPdfGenerator.Gerar(dados, _instituicao, CarregarLogo(), urlValidacao);
        return new CertificadoPdf(conteudo, $"certificado-{dados.Codigo}.pdf");
    }

    // ---------- Validação pública (RF24, RN34) ----------

    public async Task<CertificadoValidacaoViewModel> ValidarAsync(string? codigoDigitado)
    {
        var resultado = new CertificadoValidacaoViewModel
        {
            Consultado = true,
            Codigo = codigoDigitado is null ? null : codigoDigitado[..Math.Min(codigoDigitado.Length, 30)]
        };

        var codigo = NormalizarCodigo(codigoDigitado);
        if (codigo is null)
            return resultado;

        var certificado = await _db.Certificados.AsNoTracking()
            .Where(c => c.CodigoVerificacao == codigo
                        && c.Valido
                        && c.Inscricao.Status == StatusInscricao.Ativa
                        && c.Inscricao.Evento.Status == StatusEvento.Encerrado)
            .Select(c => new
            {
                Nome = c.Inscricao.Usuario.NomeCompleto,
                Evento = c.Inscricao.Evento.Titulo,
                c.Inscricao.Evento.DataEvento,
                c.Inscricao.Evento.CargaHoraria,
                c.DataEmissao
            })
            .FirstOrDefaultAsync();

        // Inexistente e invalidado dão o mesmo resultado
        if (certificado is null)
            return resultado;

        resultado.Encontrado = true;
        resultado.Codigo = codigo;
        resultado.Nome = certificado.Nome;
        resultado.Evento = certificado.Evento;
        resultado.DataEvento = certificado.DataEvento;
        resultado.CargaHoraria = certificado.CargaHoraria;
        resultado.DataEmissao = certificado.DataEmissao;
        return resultado;
    }

    // Aceita minúsculas, espaços e código sem hífens; devolve XXXX-XXXX-XXXX ou null
    private static string? NormalizarCodigo(string? entrada)
    {
        if (string.IsNullOrWhiteSpace(entrada) || entrada.Length > 40)
            return null;

        var limpo = new string(entrada.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        if (limpo.Length != 12)
            return null;

        return $"{limpo[..4]}-{limpo[4..8]}-{limpo[8..]}";
    }

    private byte[]? CarregarLogo()
    {
        var caminho = Path.Combine(_env.ContentRootPath, "Assets", "logo-instituicao.png");
        return File.Exists(caminho) ? File.ReadAllBytes(caminho) : null;
    }
    
}
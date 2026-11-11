namespace GestaoEventos.Services;

public record ErroValidacao(string? Campo, string Mensagem);

public class Resultado
{
    public List<ErroValidacao> Erros { get; } = new();
    public bool Sucesso => Erros.Count == 0;

    // Id do registro criado, quando aplicável
    public int? Id { get; set; }

    // Quantidade de registros afetados, quando aplicável
    public int Quantidade { get; set; }

    public static Resultado Ok(int? id = null) => new() { Id = id };

    public static Resultado Falha(string mensagem, string? campo = null)
    {
        var resultado = new Resultado();
        resultado.Erros.Add(new ErroValidacao(campo, mensagem));
        return resultado;
    }

    public void Adicionar(string mensagem, string? campo = null)
        => Erros.Add(new ErroValidacao(campo, mensagem));
}
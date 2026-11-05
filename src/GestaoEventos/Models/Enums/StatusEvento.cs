namespace GestaoEventos.Models.Enums;

// Ciclo de vida do evento (RN04). Por ser byte, vira "tinyint" no SQL Server.
public enum StatusEvento : byte
{
    Planejado = 0,
    Aberto = 1,
    Encerrado = 2,
    Cancelado = 3
}
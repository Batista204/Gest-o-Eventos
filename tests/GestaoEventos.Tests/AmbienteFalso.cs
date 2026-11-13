using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace GestaoEventos.Tests;

public class AmbienteFalso : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "Testes";
    public string EnvironmentName { get; set; } = "Testing";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public string WebRootPath { get; set; } = AppContext.BaseDirectory;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
}
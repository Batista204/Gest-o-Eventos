using System.Globalization;
using GestaoEventos.Data;
using GestaoEventos.Models.Entities;
using GestaoEventos.Services.Implementations;
using GestaoEventos.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using GestaoEventos.Services;
using Microsoft.AspNetCore.RateLimiting;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ---------- Banco de dados ----------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' não encontrada.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// ---------- Identity ----------
builder.Services.AddIdentity<Usuario, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;

    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;

    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddControllersWithViews();

builder.Services.AddScoped<IEventoService, EventoService>(); 
builder.Services.AddScoped<IInscricaoService, InscricaoService>();
builder.Services.AddScoped<IPresencaService, PresencaService>();
builder.Services.AddScoped<ICertificadoService, CertificadoService>();

// Licença Community do QuestPDF (uso acadêmico); detalhes em docs/
QuestPDF.Settings.License = LicenseType.Community;

builder.Services.Configure<InstituicaoOptions>(builder.Configuration.GetSection("Instituicao"));

// Limite de consultas na página pública de validação: 10 por minuto por IP
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (contexto, token) =>
    {
        contexto.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        await contexto.HttpContext.Response.WriteAsync(
            "Muitas tentativas. Aguarde um minuto e tente novamente.", token);
    };
    options.AddPolicy("validacao", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
var culturaFixa = CultureInfo.InvariantCulture;
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culturaFixa),
    SupportedCultures = new List<CultureInfo> { culturaFixa },
    SupportedUICultures = new List<CultureInfo> { culturaFixa }
});
app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();   // identifica quem é o usuário (deve vir antes da autorização)
app.UseAuthorization();    // verifica o que o usuário pode acessar

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Painel}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Cria perfis e administrador inicial
await IdentitySeeder.SeedAsync(app.Services);

app.Run();
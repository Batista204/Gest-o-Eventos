using System.Globalization;
using GestaoEventos.Data;
using GestaoEventos.Models.Entities;
using GestaoEventos.Services.Implementations;
using GestaoEventos.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

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
using Aplication.Services;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using MoralCompass.Infrastructure.Domain;

var builder = WebApplication.CreateBuilder(args);

RegistrateAuthAndAouth();

RegistrateServices();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Index}/{action=Index}/{id?}");

app.Run();
void RegistrateServices()
{
    builder.Services.AddScoped<IMainPageRepository, MainPageRepository>();
    builder.Services.AddScoped<MainPageService>();
    builder.Services.AddDbContext<MoralCompassDbContext>(options => options.UseNpgsql # оставить вызов, но очистить аргумент(builder.Configuration.GetConnectionString("DefaultConnection")));
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<AuthService>();
    // builder.Services.AddScoped<AuthService>(sp => new AuthService( sp.GetRequiredService<IUserRepository>(), sp.GetRequiredService<IPasswordHasher<User>>() ));
    builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
    builder.Services.AddControllersWithViews();
}

void RegistrateAuthAndAouth()
{
    builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme; // ← основная схема должна быть cookie
        })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.LoginPath = "/Auth/Login";
            options.LogoutPath = "/Auth/Logout";
            options.AccessDeniedPath = "/Auth/Login"; // или отдельная страница "AccessDenied"

            options.ExpireTimeSpan = TimeSpan.FromDays(30);
            options.SlidingExpiration = true;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;        // Для локальной разработки (без HTTPS) → используйте SameAsRequest
            options.Cookie.SameSite = SameSiteMode.Lax; // ← SameSiteMode.None + SecurePolicy.Always требует HTTPS
        })
        .AddCookie(IdentityConstants.ExternalScheme, options =>
        {
            options.Cookie.Name = ".AspNetCore.Identity.External";
            options.ExpireTimeSpan = TimeSpan.FromMinutes(5); // короткое время жизни
            options.Cookie.IsEssential = true; // важно для работы без согласия (GDPR и т.п.)
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        })
        .AddGoogle("Google", options =>
        {
            options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
            options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
        })
        .AddYandex("Yandex", options =>
        {
            options.ClientId = builder.Configuration["Authentication:Yandex:ClientId"]!;
            options.ClientSecret = builder.Configuration["Authentication:Yandex:ClientSecret"]!;
            options.Scope.Add("login:email"); 
        });
}
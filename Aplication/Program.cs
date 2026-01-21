using Aplication.Services;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using MoralCompass.Infrastructure.Domain;

var builder = WebApplication.CreateBuilder(args);

RegistrateAuthAndAouth();

RegistrateServices();

var app = builder.Build();

ConfigureApp();

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://*:{port}");

app.Run();


void RegistrateServices()
{
    builder.Services.AddScoped<IMainPageRepository, MainPageRepository>();
    builder.Services.AddScoped<MainPageService>();
    builder.Services.AddDbContext<MoralCompassDbContext>(options => options.UseNpgsql # оставить вызов, но очистить аргумент(builder.Configuration.GetConnectionString("DefaultConnection")));
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<AuthService>();
    builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
    builder.Services.AddControllersWithViews();
    builder.Services.AddAuthorization(options => { options.AddPolicy("AdminOnly", policy => 
            policy.RequireClaim("IsAdmin", "true")); });
}


void ConfigureApp()
{
    app.Use((context, next) =>
    {
        if (context.Request.Host.Host.EndsWith(".twc1.net"))
        {
            context.Request.Scheme = "https";
        }
        return next();
    });

    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Index}/{action=Index}/{id?}");
}

void RegistrateAuthAndAouth()
{
    builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme; 
        })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.LoginPath = "/Auth/Login";
            options.LogoutPath = "/Auth/Logout";
            options.AccessDeniedPath = "/Auth/Login"; 

            options.ExpireTimeSpan = TimeSpan.FromDays(30);
            options.SlidingExpiration = true;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;        
            options.Cookie.SameSite = SameSiteMode.Lax; 
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



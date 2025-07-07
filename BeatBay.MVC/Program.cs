using System;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// 1) Añade MVC y Razor
builder.Services.AddControllersWithViews();

// 2) Configura HttpClient para la API (BaseUrl sin "/api")
builder.Services.AddHttpClient("BeatBay.API", client =>
{
    // Lee la URL base de la API (sin barra al final o con ella)
    var apiBaseRaw = builder.Configuration.GetValue<string>("ApiSettings:BaseUrl")
                  ?? throw new InvalidOperationException("Falta ApiSettings:BaseUrl en appsettings.json");
    // Asegura que termine con '/'
    var apiBase = apiBaseRaw.EndsWith('/') ? apiBaseRaw : apiBaseRaw + '/';
    client.BaseAddress = new Uri(apiBase);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// 3) Sesión en memoria para guardar el JWT
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "BeatBaySession";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromHours(2);
});

// 4) Autenticación con cookies (envuelve el JWT)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/VAuth/Login";
        options.LogoutPath = "/VAuth/Logout";
        options.Cookie.Name = "BeatBayAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
    });

// 5) CORS (opcional)
builder.Services.AddCors(o => o.AddPolicy("AllowAPI", policy =>
{
    var apiBase = builder.Configuration.GetValue<string>("ApiSettings:BaseUrl");
    if (!string.IsNullOrEmpty(apiBase))
    {
        policy.WithOrigins(apiBase)
              .AllowAnyHeader()
              .AllowAnyMethod();
    }
}));

var app = builder.Build();

// Pipeline
if(app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseCors("AllowAPI");

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

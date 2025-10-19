using GanaderiaControl.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;

var builder = WebApplication.CreateBuilder(args);

// ---- Puerto (Render/Containers) ----
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// ---- DB + Identity ----
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString)
);

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequiredLength = 6;
        options.Password.RequireDigit = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Autorización global: todo MVC protegido
builder.Services.AddControllersWithViews(opt =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    opt.Filters.Add(new AuthorizeFilter(policy));
});

// Razor Pages: deja anónimas las de Identity (login, register, etc.)
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AllowAnonymousToAreaFolder("Identity", "/Account");
});

// Cookies
builder.Services.ConfigureApplicationCookie(opt =>
{
    opt.LoginPath = "/Identity/Account/Login";
    opt.AccessDeniedPath = "/Identity/Account/AccessDenied";
    opt.SlidingExpiration = true;
    opt.ExpireTimeSpan = TimeSpan.FromHours(8);
});

// Proxy/CDN (Render/NGINX/Cloudflare)
builder.Services.Configure<ForwardedHeadersOptions>(opt =>
{
    opt.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Confía en proxies desconocidos si estás en PaaS tipo Render/Heroku
    opt.KnownNetworks.Clear();
    opt.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint(); // detalle de errores solo en dev
}
else
{
    // IMPORTANTE: Apunta a un endpoint real que exista
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();

    // Páginas de estado (404/403/500…) que re-ejecutan una ruta visible
    app.UseStatusCodePagesWithReExecute("/Home/StatusCode", "?code={0}");
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Health
app.MapGet("/health", () => Results.Ok("OK"));

// Migraciones + Seed
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var db = sp.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    await SeedIdentityAsync(sp);
}

// Redirige raíz al Dashboard
app.MapGet("/", () => Results.Redirect("/Dashboard"));

// Rutas MVC + Razor Pages
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();

// ======= Seed =======
static async Task SeedIdentityAsync(IServiceProvider services)
{
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    foreach (var role in new[] { "Admin", "Operador" })
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));

    var email = "admin@local.com";
    var password = "Admin123!";
    var user = await userManager.FindByEmailAsync(email);

    if (user == null)
    {
        user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
        var create = await userManager.CreateAsync(user, password);
        if (!create.Succeeded)
        {
            var errors = string.Join("; ", create.Errors.Select(e => $"{e.Code}:{e.Description}"));
            throw new Exception("Seed user failed: " + errors);
        }
    }

    if (!await userManager.IsInRoleAsync(user, "Admin"))
        await userManager.AddToRoleAsync(user, "Admin");
}

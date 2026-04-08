using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using T20Master.Web.Components;
using T20Master.Web.Data;

var builder = WebApplication.CreateBuilder(args);

// Database — support Railway's DATABASE_URL or appsettings ConnectionString
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (string.IsNullOrEmpty(connectionString) && !string.IsNullOrEmpty(databaseUrl))
{
    // Convert postgresql://user:pass@host:port/db to Npgsql format
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');
    connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true;";
}
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString,
        npgsqlOptions => npgsqlOptions.UseVector()));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.AccessDeniedPath = "/access-denied";
});

// Services
builder.Services.AddScoped<T20Master.Web.Services.CampaignService>();
builder.Services.AddScoped<T20Master.Web.Services.CharacterService>();
builder.Services.AddScoped<T20Master.Web.Services.AI.PdfProcessor>();
builder.Services.AddScoped<T20Master.Web.Services.AI.EmbeddingService>();
builder.Services.AddScoped<T20Master.Web.Services.AI.RagIndexer>();
builder.Services.AddScoped<T20Master.Web.Services.AI.RagService>();
builder.Services.AddScoped<T20Master.Web.Services.AI.GeminiChatService>();
builder.Services.AddScoped<T20Master.Web.Services.GameSessionService>();
builder.Services.AddScoped<T20Master.Web.Services.MapService>();
builder.Services.AddScoped<T20Master.Web.Services.CombatBoardService>();

// SignalR
builder.Services.AddSignalR();

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

// Auto-migrate
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}

// CLI: --clear-chunks
if (args.Contains("--clear-chunks"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.RuleChunks.RemoveRange(db.RuleChunks);
    await db.SaveChangesAsync();
    Console.WriteLine("All RuleChunks deleted.");
    return;
}

// CLI: --retry-failed
if (args.Contains("--retry-failed"))
{
    using var scope = app.Services.CreateScope();
    var indexer = scope.ServiceProvider.GetRequiredService<T20Master.Web.Services.AI.RagIndexer>();
    await indexer.RetryFailedAsync();
    return;
}

// CLI: --index-pdf
if (args.Contains("--index-pdf"))
{
    using var scope = app.Services.CreateScope();
    var indexer = scope.ServiceProvider.GetRequiredService<T20Master.Web.Services.AI.RagIndexer>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var pdfPath = config["PdfPath"] ?? throw new InvalidOperationException("PdfPath not configured");
    await indexer.IndexPdfAsync(pdfPath);
    return;
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// Health check
app.MapGet("/health", () => "OK");

if (app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

// Auth endpoints
app.MapPost("/account/login", async (HttpContext context, SignInManager<ApplicationUser> signInManager) =>
{
    var form = await context.Request.ReadFormAsync();
    var email = form["email"].ToString();
    var password = form["password"].ToString();

    var result = await signInManager.PasswordSignInAsync(email, password, isPersistent: false, lockoutOnFailure: false);
    if (result.Succeeded)
        return Results.Redirect("/dashboard");

    return Results.Redirect("/login?error=1");
});

app.MapPost("/account/register", async (HttpContext context, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) =>
{
    var form = await context.Request.ReadFormAsync();
    var displayName = form["displayName"].ToString();
    var email = form["email"].ToString();
    var password = form["password"].ToString();

    var user = new ApplicationUser
    {
        UserName = email,
        Email = email,
        DisplayName = displayName
    };

    var result = await userManager.CreateAsync(user, password);
    if (result.Succeeded)
    {
        await signInManager.SignInAsync(user, isPersistent: false);
        return Results.Redirect("/dashboard");
    }

    var errors = string.Join(". ", result.Errors.Select(e => e.Description));
    return Results.Redirect($"/register?error={Uri.EscapeDataString(errors)}");
});

app.MapPost("/logout", async (SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/");
});

app.MapHub<T20Master.Web.Hubs.CampaignSessionHub>("/hubs/session");

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

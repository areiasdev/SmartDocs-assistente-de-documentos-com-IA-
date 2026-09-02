using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartDocs.Web.Components;
using SmartDocs.Web.Data;
using SmartDocs.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSignalR();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.")));

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();

builder.Services.AddAuthorization();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IPdfTextExtractor, PdfPigTextExtractor>();
builder.Services.AddSingleton<InMemoryVectorStore>();                 // guarda estado -> singleton
builder.Services.AddSingleton<IDocumentService, LocalDocumentService>();

builder.Services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>(); // typed HttpClient
builder.Services.AddScoped<DocumentIngestionService>();              // nao faz sentido como Singleton

builder.Services.AddHttpClient<IChatService, OllamaChatService>();
builder.Services.AddScoped<RagService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // Se tiver prod tenho que mudar, ver https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapHub<SmartDocs.Web.Hubs.ChatHub>("/hubs/chat");

app.Run();

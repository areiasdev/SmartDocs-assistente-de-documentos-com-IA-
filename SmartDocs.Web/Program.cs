using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using SmartDocs.Web.Components;
using SmartDocs.Web.Components.Account;   // helpers do Identity (IdentityUserAccessor, etc.)
using SmartDocs.Web.Data;
using SmartDocs.Web.Interfaces;
using SmartDocs.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ---- Blazor + SignalR ----
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSignalR();

// ---- Autenticação (ASP.NET Core Identity, cookie-based, EF Core/SQLite store) ----
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
.AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// Não tenho nenhum provider de email a sério configurado, o RequireConfirmedAccount
// está a false lá em cima, por isso este sender que não faz nada só serve para o
// Identity não rebentar no DI (ele pede sempre um IEmailSender), já que não uso
// reset de password nem confirmação de email aqui.
builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// ---- Serviços do SmartDocs ----
// Cada coisa externa (extrair PDF, embeddings, chat com o LLM) fica escondida
// atrás de uma interface, e é só aqui que registo a implementação a sério. O
// RagService e o DocumentIngestionService só conhecem as interfaces, nunca as
// classes Ollama/PdfPig diretamente, se um dia trocar para Azure OpenAI ou
// Azure AI Document Intelligence, mudo só a linha do registo aqui, o resto do
// código nem dá por isso.
builder.Services.AddSingleton<IPdfTextExtractor, PdfPigTextExtractor>();
builder.Services.AddSingleton<InMemoryVectorStore>();          // guarda estado -> singleton, mas só em memória (ver comentário na classe)
builder.Services.AddScoped<IDocumentService, LocalDocumentService>();     // depende do DbContext (scoped), por isso tem de ser scoped também
builder.Services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>();
builder.Services.AddHttpClient<IChatService, OllamaChatService>();
builder.Services.AddScoped<DocumentIngestionService>();
builder.Services.AddScoped<RagService>();
builder.Services.AddScoped<ConversationService>();

var app = builder.Build();

// ---- Pipeline ----
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapHub<SmartDocs.Web.Hubs.ChatHub>("/hubs/chat");
app.MapAdditionalIdentityEndpoints();

// NOTAS PARA MIM, coisas que dava para melhorar se tivesse mais tempo:
// - Rate limiting nos endpoints de upload e chat (app.UseRateLimiter() + policy),
//   para não deixar ninguém dar spam ao Ollama/à API de embeddings.
// - Um endpoint de health check (services.AddHealthChecks() + app.MapHealthChecks("/health")),
//   barato de fazer e normalmente esperado numa app "operational ready".
// - Logging estruturado com correlation id por pedido, para conseguir seguir um
//   upload/pergunta específico nos logs de ponta a ponta.
// - Resolver o [Authorize] do ChatHub (ver nota na classe), o userId devia vir
//   do Context.User autenticado, não de um parâmetro que o cliente manda.
// - InMemoryVectorStore é só em memória (ver comentário na classe), passar para
//   uma base de dados vetorial a sério (Azure AI Search / Cosmos DB vector search)
//   antes de isto ser usado a sério por mais que uma pessoa.
app.Run();
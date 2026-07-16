using MongoDB.Driver;
using SupportAgent.API.Data;
using SupportAgent.API.Middleware;
using SupportAgent.API.Services;

var builder = WebApplication.CreateBuilder(args);

// ── MongoDB ───────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<IMongoClient>(_ =>
    new MongoClient(builder.Configuration["MongoDB:ConnectionString"]));
builder.Services.AddSingleton<MongoDbContext>();

// ── OpenAI + domain services ─────────────────────────────────────────────────
builder.Services.AddSingleton<OpenAIService>();
builder.Services.AddSingleton<KnowledgeService>();
builder.Services.AddSingleton<ChatService>();

// ── Controllers ───────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ── Swagger ───────────────────────────────────────────────────────────────────
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new() { Title = "AI Support Agent API", Version = "v1" });
});

// ── CORS ──────────────────────────────────────────────────────────────────────
var allowedOrigins = builder.Configuration
    .GetSection("AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:3000"];

builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(p => p
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()));

// ── Health checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

// ═════════════════════════════════════════════════════════════════════════════
var app = builder.Build();

// ── Seed knowledge base ───────────────────────────────────────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
    await KnowledgeSeeder.SeedAsync(db);
}
catch (Exception ex)
{
    app.Logger.LogWarning("Seeder skipped — MongoDB not reachable: {Message}", ex.Message);
}

// ── Middleware pipeline ───────────────────────────────────────────────────────
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(opt => opt.SwaggerEndpoint("/swagger/v1/swagger.json", "AI Support Agent API v1"));

app.UseCors();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

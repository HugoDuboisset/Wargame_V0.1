using Wargame.API.Middlewares;
using Wargame.Application;
using Wargame.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration de l'Injection de Dépendances ---

// Enregistrement de notre logique métier (CQRS, MediatR, FluentValidation)
builder.Services.AddApplication();

// Enregistrement de notre persistance (Repositories JSON)
builder.Services.AddInfrastructure(options => 
{
    // Le dossier "data" sera créé à la racine d'exécution de l'API
    options.DataDirectory = "data";
});

// Enregistrement des Controllers
builder.Services.AddControllers();

// Gestion globale des exceptions avec ProblemDetails
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(); 

// Configuration OpenAPI (Swagger natif .NET)
builder.Services.AddOpenApi();

var app = builder.Build();

// --- Configuration du Pipeline HTTP ---

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Le visualiseur natif de OpenAPI ou Swagger UI peut s'ajouter ici
}

// Intercepte les erreurs (ValidationException, etc.) et retourne proprement du ProblemDetails
app.UseExceptionHandler(); 

// app.UseHttpsRedirection();

// Routage vers nos classes Controllers
app.MapControllers();

app.Run();

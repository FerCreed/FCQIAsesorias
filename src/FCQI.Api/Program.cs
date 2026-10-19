using System.Text;
using FCQI.Application.Administration;
using FCQI.Application.Advisors.Queries;
using FCQI.Application.Auth;
using FCQI.Application.Sessions.Commands;
using FCQI.Application.Sessions.Queries;
using FCQI.Application.Subjects.Queries;
using FCQI.Infrastructure;
using FCQI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<GetSubjectsQueryHandler>();
builder.Services.AddScoped<GetAdvisorsQueryHandler>();
builder.Services.AddScoped<GetAvailabilitiesQueryHandler>();
builder.Services.AddScoped<GetSessionsQueryHandler>();
builder.Services.AddScoped<CreateSessionCommandHandler>();
builder.Services.AddScoped<UpdateSessionStatusCommandHandler>();
builder.Services.AddScoped<GetAdminAdvisorsQueryHandler>();
builder.Services.AddScoped<ReplaceAdvisorSubjectsCommandHandler>();
builder.Services.AddScoped<ResolveProfileHandler>();

// Identidad de quien llama, leída del JWT.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<FCQI.Api.Security.CurrentUser>();

var jwtKey = builder.Configuration["Authentication:Jwt:Key"] ?? "FCQI-dev-jwt-key-change-me-32chars!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Authentication:Jwt:Issuer"] ?? "FCQI.Api",
            ValidAudience = builder.Configuration["Authentication:Jwt:Audience"] ?? "FCQI.Web",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.SetIsOriginAllowed(origin => Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.Host == "localhost")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// El esquema y los datos NO los crea la aplicación. Los definen db/01-schema.sql
// y db/02-seed.sql, que el contenedor ejecuta contra MySQL antes de arrancar la
// API (ver fcqi-init-db.sh en el Dockerfile). Aquí solo se comprueba que la base
// esté lista, para fallar con un mensaje claro en vez de en la primera consulta.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (!await db.Database.CanConnectAsync())
    {
        throw new InvalidOperationException(
            "No se pudo conectar a MySQL. Revisa ConnectionStrings:DefaultConnection.");
    }

    if (!await db.Terms.AnyAsync(t => t.IsCurrent))
    {
        throw new InvalidOperationException(
            "La base de datos no está inicializada: no hay ciclo escolar vigente. " +
            "Ejecuta db/01-schema.sql y db/02-seed.sql antes de arrancar la API.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

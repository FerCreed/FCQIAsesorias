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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await Catalog20262Seeder.SeedAsync(db);
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

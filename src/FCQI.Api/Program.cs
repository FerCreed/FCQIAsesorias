using FCQI.Api.Configuration;
using FCQI.Application;
using FCQI.Infrastructure;
using FCQI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApi(builder.Configuration)
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

var app = builder.Build();

// El esquema y los datos NO los crea la aplicación. Los definen db/01-schema.sql
// y db/02-seed.sql, que el contenedor ejecuta contra MySQL antes de arrancar la
// API (ver docker/fcqi-init-db.sh). Aquí solo se comprueba que la base esté
// lista, para fallar con un mensaje claro en vez de en la primera consulta.
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
app.UseRateLimiter();
app.MapControllers();

app.Run();

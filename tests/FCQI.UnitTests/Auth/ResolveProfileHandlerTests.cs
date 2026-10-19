using FCQI.Application.Auth;
using FCQI.Domain;
using FCQI.Domain.Entities;
using FCQI.Infrastructure.Persistence;
using FCQI.UnitTests.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FCQI.UnitTests.Auth;

public class ResolveProfileHandlerTests
{
    /// <summary>
    /// El caso que el modelo anterior no podía representar: Jimena es asesora
    /// y alumna. Con tres tablas de identidad y una cadena de SELECT, el
    /// primer acierto ganaba y nunca podía agendar como alumna.
    /// </summary>
    [Fact]
    public async Task PeerAdvisor_GetsBothRoles()
    {
        await using var context = await SeedAsync();
        var handler = new ResolveProfileHandler(context);

        var result = await handler.ResolveAsync("j2207105@uabc.edu.mx", "t");

        Assert.NotNull(result);
        Assert.Contains(UserRoles.Advisor, result.Roles);
        Assert.Contains(UserRoles.Student, result.Roles);
        Assert.Equal("Jimena Beltrán Zepeda", result.FullName);
    }

    [Fact]
    public async Task StudentOnly_GetsOneRole()
    {
        await using var context = await SeedAsync();
        var handler = new ResolveProfileHandler(context);

        var result = await handler.ResolveAsync("yesua.diaz@uabc.edu.mx", "t");

        Assert.NotNull(result);
        Assert.Equal(new[] { UserRoles.Student }, result.Roles);
    }

    [Fact]
    public async Task UnknownEmail_IsRejected()
    {
        await using var context = await SeedAsync();
        var handler = new ResolveProfileHandler(context);

        Assert.Null(await handler.ResolveAsync("nadie@uabc.edu.mx", "t"));
    }

    [Fact]
    public async Task NonInstitutionalEmail_IsRejected()
    {
        await using var context = await SeedAsync();
        var handler = new ResolveProfileHandler(context);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.ResolveAsync("alguien@gmail.com", "t"));
    }

    [Fact]
    public async Task DemoProfiles_ListAPeerAdvisorTwice_OncePerRole()
    {
        await using var context = await SeedAsync();
        var handler = new ResolveProfileHandler(context);

        var profiles = await handler.ListDemoProfilesAsync();
        var jimena = profiles.Where(p => p.Email == "j2207105@uabc.edu.mx").ToList();

        Assert.Equal(2, jimena.Count);
        Assert.Contains(jimena, p => p.Role == UserRoles.Advisor);
        Assert.Contains(jimena, p => p.Role == UserRoles.Student);
    }

    private static async Task<AppDbContext> SeedAsync()
    {
        var context = TestContext.Create();

        context.Programs.Add(new AcademicProgram { Id = 1, Code = "QFB", Name = "Química Farmacéutica Biológica" });
        context.Modalities.Add(new Modality { Id = 1, Code = "IN_PERSON", Name = "Presencial" });

        // DisplayName lo genera MySQL; el proveedor en memoria no lo calcula,
        // así que se fija por reflexión para que las pruebas lo vean.
        var jimena = Person("Jimena", "Beltrán", "Zepeda", "j2207105@uabc.edu.mx");
        var yesua = Person("Yesua Fernando", "Díaz", "Hernández", "yesua.diaz@uabc.edu.mx");
        context.People.AddRange(jimena, yesua);
        await context.SaveChangesAsync();

        context.AdvisorProfiles.Add(new AdvisorProfile
        {
            PersonId = jimena.Id, ProgramId = 1, DefaultModalityId = 1, IsActive = true
        });
        context.StudentProfiles.AddRange(
            new StudentProfile { PersonId = jimena.Id, StudentNumber = "2207105", IsActive = true },
            new StudentProfile { PersonId = yesua.Id, StudentNumber = "2208134", IsActive = true });

        await context.SaveChangesAsync();
        return context;
    }

    private static Person Person(string first, string paternal, string maternal, string email)
    {
        var person = new Person
        {
            FirstName = first, LastNamePaternal = paternal,
            LastNameMaternal = maternal, Email = email, IsActive = true
        };

        typeof(Person).GetProperty(nameof(Domain.Entities.Person.DisplayName))!
            .SetValue(person, $"{first} {paternal} {maternal}");

        return person;
    }
}

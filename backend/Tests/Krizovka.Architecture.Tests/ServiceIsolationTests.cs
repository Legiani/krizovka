using Krizovka.Api.Data;
using Krizovka.Users.Domain.Entities;
using Krizovka.Users.Infrastructure.Persistence;
using NetArchTest.Rules;
using Xunit;

namespace Krizovka.Architecture.Tests;

// Recycled from City+ ServiceIsolationTests; only the two deployed services.
public sealed class ServiceIsolationTests
{
    [Fact]
    public void Reports_api_does_not_reference_users_business_implementation()
    {
        var types = Types.InAssembly(typeof(KrizovkaDbContext).Assembly);
        Assert.NotEmpty(types.GetTypes());
        Assert.True(types.ShouldNot().HaveDependencyOn("Krizovka.Users").GetResult().IsSuccessful);
    }

    [Fact]
    public void Users_service_does_not_reference_reports_api()
    {
        var types = Types.InAssemblies([
            typeof(BackofficeUser).Assembly,
            typeof(Krizovka.Users.Application.Core.DependencyInjection).Assembly,
            typeof(UsDbContext).Assembly]);
        Assert.NotEmpty(types.GetTypes());
        Assert.True(types.ShouldNot().HaveDependencyOn("Krizovka.Api").GetResult().IsSuccessful);
    }
}

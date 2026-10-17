using Krizovka.Users.Domain.Entities;
using NetArchTest.Rules;
using Xunit;

namespace Krizovka.Architecture.Tests;

// Recycled from City+ DomainRulesTests; inspect an explicit, nonempty assembly.
public sealed class DomainRulesTests
{
    [Theory]
    [InlineData("Microsoft.EntityFrameworkCore")]
    [InlineData("System.Net.Http")]
    [InlineData("Microsoft.AspNetCore")]
    [InlineData("MassTransit")]
    [InlineData("Serilog")]
    [InlineData("Krizovka.Users.Infrastructure")]
    [InlineData("Krizovka.Users.Application")]
    public void Users_domain_does_not_depend_on_infrastructure(string forbidden)
    {
        var types = Types.InAssembly(typeof(BackofficeUser).Assembly)
            .That().ResideInNamespaceStartingWith("Krizovka.Users.Domain");
        Assert.NotEmpty(types.GetTypes());
        var result = types.ShouldNot().HaveDependencyOn(forbidden).GetResult();
        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }
}

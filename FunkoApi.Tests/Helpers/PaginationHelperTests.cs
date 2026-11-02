using FluentAssertions;
using FunkoApi.Back.Helpers;
using NUnit.Framework;

namespace FunkoApi.Tests.Helpers;

[TestFixture]
public class PaginationHelperTests
{
    [TestCase(1, 1, false, false)]
    [TestCase(1, 3, false, true)]
    [TestCase(2, 3, true, true)]
    [TestCase(3, 3, true, false)]
    public void CreateLinkHeader_PrimeraIntermediaOUltimaPagina_ConstruyeEnlaces(int page, int total, bool anteriores, bool siguientes)
    {
        // Arrange
        const string url = "https://localhost/api/funkos/paged";
        const int size = 10;

        // Act
        var resultado = PaginationHelper.CreateLinkHeader(page, total, size, url);

        // Assert
        if (anteriores)
        {
            resultado.Should().Contain($"<{url}?page=1&pageSize=10>; rel=\"first\"");
            resultado.Should().Contain($"<{url}?page={page - 1}&pageSize=10>; rel=\"prev\"");
        }
        else resultado.Should().NotContain("rel=\"first\"").And.NotContain("rel=\"prev\"");
        if (siguientes)
        {
            resultado.Should().Contain($"<{url}?page={page + 1}&pageSize=10>; rel=\"next\"");
            resultado.Should().Contain($"<{url}?page={total}&pageSize=10>; rel=\"last\"");
        }
        else resultado.Should().NotContain("rel=\"next\"").And.NotContain("rel=\"last\"");
        if (!anteriores && !siguientes) resultado.Should().BeEmpty();
    }
}

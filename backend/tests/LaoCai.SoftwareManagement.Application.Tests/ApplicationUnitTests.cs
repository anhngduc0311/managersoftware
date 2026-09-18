using LaoCai.SoftwareManagement.Application.Common.Exceptions;
using LaoCai.SoftwareManagement.Application.Common.Models;

namespace LaoCai.SoftwareManagement.Application.Tests;

public class PaginationTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(3, 3)]
    public void PagedQuery_Page_ShouldBeAtLeastOne(int inputPage, int expectedPage)
    {
        var query = new PagedQuery { Page = inputPage };
        query.Page.Should().Be(expectedPage);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(-10, 20)]
    [InlineData(50, 50)]
    [InlineData(150, 100)] // Max page size limit
    public void PagedQuery_PageSize_ShouldBeBoundedBetween1And100(int inputSize, int expectedSize)
    {
        var query = new PagedQuery { PageSize = inputSize };
        query.PageSize.Should().Be(expectedSize);
    }

    [Fact]
    public void PagedResult_ShouldCalculateTotalPagesCorrectly()
    {
        var items = new List<string> { "A", "B", "C" };
        var result = PagedResult<string>.Create(items, 25, 1, 10);

        result.TotalCount.Should().Be(25);
        result.TotalPages.Should().Be(3);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }
}

public class AppExceptionTests
{
    [Fact]
    public void NotFoundException_ShouldSet404AndResourceCode()
    {
        var ex = new NotFoundException("Deployment", "DEP-01");
        ex.StatusCode.Should().Be(404);
        ex.Code.Should().Be("resource.not_found");
        ex.Message.Should().Contain("Deployment");
    }

    [Fact]
    public void ConcurrencyException_ShouldSet412AndCode()
    {
        var ex = new ConcurrencyException();
        ex.StatusCode.Should().Be(412);
        ex.Code.Should().Be("concurrency.version_mismatch");
    }
}

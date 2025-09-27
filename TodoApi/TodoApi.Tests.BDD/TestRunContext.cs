using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using TodoApi.WebApi;

namespace TodoApi.Tests.BDD;

[TestFixture]
[Parallelizable(ParallelScope.Fixtures)]
public class TestRunContext : IDisposable
{
    public WebApplicationFactory<Program> Factory { get; private set; }

    public TestRunContext()
    {
        Factory = new WebApplicationFactory<Program>();
    }

    public void Dispose()
    {
        Factory?.Dispose();
    }
}
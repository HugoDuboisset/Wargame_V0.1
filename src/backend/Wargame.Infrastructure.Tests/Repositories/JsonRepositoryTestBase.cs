using Microsoft.Extensions.Options;
using Wargame.Infrastructure.Configuration;

namespace Wargame.Infrastructure.Tests.Repositories;

public abstract class JsonRepositoryTestBase : IDisposable
{
    protected readonly string TestDataDirectory;
    protected readonly IOptions<JsonRepositoryOptions> Options;

    protected JsonRepositoryTestBase()
    {
        TestDataDirectory = Path.Combine(Path.GetTempPath(), "WargameTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(TestDataDirectory);
        Options = Microsoft.Extensions.Options.Options.Create(new JsonRepositoryOptions { DataDirectory = TestDataDirectory });
    }

    public void Dispose()
    {
        if (Directory.Exists(TestDataDirectory))
        {
            Directory.Delete(TestDataDirectory, true);
        }
    }
}

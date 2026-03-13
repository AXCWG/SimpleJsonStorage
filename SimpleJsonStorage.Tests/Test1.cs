namespace SimpleJsonStorage.Tests;

[TestClass]
public sealed class Test1
{
    [TestMethod]
    public void DelayedStoragePool()
    {
        var s = new AppStorage();
        for (int i = 0; i < 10; i++)
        {
            s.TestClassEntries.Add(new()
            {
                Uuid = Guid.NewGuid().ToString(),
                Yes = Random.Shared.NextSingle() >= 0.5,
            });
        }
    }
}


public sealed class AppStorage : StoragePool
{
    public class TestClass
    {
        public required string Uuid { get; set; }
        public required bool Yes { get; set; }
    }

    public DelayedProgramStorageSet<TestClass> TestClassEntries { get; set; } = null!;

    public AppStorage()
    {
        OnConfiguring("com.axcwg.test", new()
        {
            WriteIndented = true
        }); 
    }
}
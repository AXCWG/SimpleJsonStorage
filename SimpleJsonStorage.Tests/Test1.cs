using System.Diagnostics;

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
            Task.Delay(400).GetAwaiter().GetResult();
            s.NonDelayedTestClassEntries.Add(new()
            {
                Uuid = Guid.NewGuid().ToString(),
                Yes = Random.Shared.NextSingle() >= 0.5,
            });
        }
        
        
    }

    [TestMethod]
    public void DelayedInteractfulStoragePool()
    {
        var s = new AppStorage();
        for (int i = 0; i < 10; i++)
        {
            s.TestClassEntries.Add(new()
            {
                Uuid = Guid.NewGuid().ToString(),
                Yes = Random.Shared.NextSingle() >= 0.5,
            });
            Console.ReadLine();
        }
        s.SaveChanges();

    }

    [TestMethod]
    public void DelayedStorageRemove()
    {
        var s = new DelayedProgramStorageSet<AppStorage.TestClassR>("com.axcwg.test", "TestClassEntries", options: new()
        {
            WriteIndented = true
        });
        s.Remove(new()
        {
            Uuid = "15abac1a-34b7-4d24-9b8b-c742a089f800",
            Yes = true
        });
        s.SaveChanges();
        

    }

    [TestMethod]
    public void StorageRemove()
    {
        var s = new ProgramStorageSet<AppStorage.TestClassR>("com.axcwg.test", "NonDelayedTestClassEntries",
            options: new()
            {
                WriteIndented = true
            });
        s.RemoveAll(i => i.Uuid == "542f0cdc-22f8-4546-bd2e-f409e56d0b1c");
        

    }

    [TestMethod]
    public void Equal()
    {
        var a = new AppStorage.TestClass
        {
            Uuid = "2",
            Yes = false
        };
        var b = new AppStorage.TestClass
        {
            Uuid = "2",
            Yes = false
        };
        Console.WriteLine($"{a == b}");
        Console.WriteLine($"{a.Equals(b) }");
    }

    [TestMethod]
    public void VerySimpleTest()
    {
        var timer = new Timer((_) =>
        {
            Debug.WriteLine("Hello. ");
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        Console.ReadLine(); 

    }

    [TestMethod]
    public void Combined()
    {
        var s = new AppStorage();

        Console.ReadLine();
    }
}


public sealed class AppStorage : StoragePool
{
    public class TestClass
    {
        public required string Uuid { get; set; }
        public required bool Yes { get; set; }
    }

    public record TestClassR
    {
        public required string Uuid { get; set; }
        public required bool Yes { get; set; }
    }

    public class TestSetting
    {
        public bool IsAdmin { get; set; }
        public bool IsOp { get; set; }
    }

    public DelayedProgramStorageSet<TestClass> TestClassEntries { get; set; } = null!;
    public ProgramStorageSet<TestClass> NonDelayedTestClassEntries { get; set; } = null!;
    public ProgramStorage<TestSetting> TestSettings { get; set; } = null!;

    public AppStorage()
    {
        OnConfiguring(identifier: "com.axcwg.test", options:new()
        {
            WriteIndented = true
        }, builder: builder =>
            builder.UseAutoSaveChanges<AppStorage, DelayedProgramStorageSet<TestClass>>(i => TestClassEntries, TimeSpan.FromSeconds(1) )
                .UseCheckOnSaveChanges<AppStorage, ProgramStorageSet<TestClass>>(o=>o.NonDelayedTestClassEntries)
                .UseCheckOnSaveChanges<AppStorage, DelayedProgramStorageSet<TestClass>>(i=>i.TestClassEntries)
                .UseCheckOnSaveChanges<AppStorage, ProgramStorage<TestSetting>>(o=>o.TestSettings)
        ); 
    }
}
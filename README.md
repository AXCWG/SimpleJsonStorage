# Simple JSON Storage
Simple JSON-powered storage interface. 

### Forewords
- This project is not meant for high performance. ~~(It runs purely on pure JSON so what could you really expect)~~
- Further documentation is properly documented in source code.
- Whilst utilizing ProgramStorage<T>, you might always want to initialize your DB first. You could achieve it through `ProgramStorageSet<T>.Set(obj)`, where `Set()` is an instance method of `ProgramStorageSet<>`, and `obj` is an instance of `T`

## Quick Start
```csharp
public class Settings
{
    public bool IsOn{get;set; }
    ...
}
var storage = new ProgramStorage<Settings>(identifier: "com.org.app", name: "Settings"); 
storage.Set(i=>i.IsOn, true); 
storage.Get(i=>i.IsOn); // Will be true. 

// Seperate example
public class Person
{
    public string Name{get;set; }
}
var storage = new ProgramStorageSet<Person>(identifier: "com.org.app", name: "People"); 
storage.Add(new()
{
    Name = "Andrew"
});
storage.Add(new()
{
    Name = "Alexander"
});

// Now storage will look something like: [{Name: "Andrew"}, {Name: "Alexander"}]. 
// Please check Intellisense for more overloads. 
```

## Storage Pool
Storage pool is something resembles DbContext in EF Core by deriving custom "contexts" from class `StoragePool`. 

```csharp
public sealed class SampleStoragePool : StoragePool
{
    public ProgramStorageSet<string> Strings { get; set; } = null!;

    public SampleStoragePool(string identifier, JsonSerializerOptions? options = null)
    {
        OnConfiguring(identifier, options);
    }
}
```

## Where It's Stored
The default is basically the evaluation of 

```csharp
Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),identifier, $"{name}.json")
```
Windows default: `%APPDATA%/[identifier]/[name].json`

    In storage pools [name] is your property name. 

Mac: 
`/Users/[username]/Library/Application Support/[identifier]/[name].json` (I think so)

Don't know about Linux. 
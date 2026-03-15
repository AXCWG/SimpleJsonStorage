using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Unicode;

namespace SimpleJsonStorage;

[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#if NET7_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
#endif
public class ProgramStorage<T> : IProgramStorage<T> where T: new()
{
    public FileStream FileStream { get; }
    public bool CheckOnSave { get; init; }
    
    public IProgramStorage<T> Configure(string identifier, string name, string? path = null, JsonSerializerOptions? options = null)
    {
        if (Configured)
        {
            return this; 
        }
        JsonSerializerOptions = options ?? new JsonSerializerOptions
        {
            IncludeFields = false
        };
        if (path == null)
        {
            if (!File.Exists(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    identifier, $"{name}.json")))
            {
                var dataPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), identifier);
                Directory.CreateDirectory(dataPath); 
                ProgramStoragePath = Path.Join(dataPath,($"{name}.json"));
                File.WriteAllText(ProgramStoragePath, JsonSerializer.Serialize(new T()));
                StorageName = name; 
            }
            else
            {
                var dataPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), identifier);
                ProgramStoragePath = Path.Join(dataPath,$"{name}.json");
                StorageName = name;

            }
            return this; 
        }

        if (!File.Exists(Path.Join(path, identifier, $"{name}.json")))
        {
            var d = Path.Join(path, identifier);
            Directory.CreateDirectory(d); 
            ProgramStoragePath = Path.Join(d,$"{name}.json");
            File.WriteAllText(ProgramStoragePath, JsonSerializer.Serialize(new T()));
            StorageName = name; 
        }
        else
        {
            var d = Path.Join(path, identifier);
            ProgramStoragePath = Path.Join(d, $"{name}.json");
            StorageName = name; 
        }

        Configured = true;
        return this; 
    }

    public bool Configured { get; private set;  }

    /// <summary>
    /// Name of the storage
    /// </summary>
    public string StorageName { get; private set;  }
    /// <summary>
    /// Storage path of the storage. 
    /// </summary>
    public string ProgramStoragePath { get; private set; }
    /// <summary>
    /// Options that'll be used every serialization and deserialization. 
    /// </summary>
    public JsonSerializerOptions JsonSerializerOptions { get; private set;  }
    /// <summary>
    /// Directory structure goes like: [path] / [identifier] / [name].json
    /// </summary>
    /// <param name="identifier">Program identifier.</param>
    /// <param name="name">Storage name. </param>
    /// <param name="path">Folder path. Defaults to <c>Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)</c></param>
    /// <param name="options"></param>
    public ProgramStorage(string identifier, string name, string? path = null, JsonSerializerOptions? options = null, bool checkOnSave = false)
    {
        Configure(identifier, name, path, options);
        if (ProgramStoragePath is null || JsonSerializerOptions is null || StorageName is null)
        {
            throw new Exception("Init failed for unknown reason. ");
        }

        FileStream = File.Open(ProgramStoragePath, FileMode.OpenOrCreate); 
        CheckOnSave = checkOnSave; 
    }

    public T? Get()
    {
        Stream.Synchronized(FileStream).Position = 0;
        return JsonSerializer.Deserialize<T>(Stream.Synchronized(FileStream), JsonSerializerOptions);
    }

    /// <summary>
    /// Set the entire object. 
    /// </summary>
    /// <param name="obj"></param>
    public void Set(T obj)
    {
        var s = JsonSerializer.Serialize(obj, JsonSerializerOptions);
        Stream.Synchronized(FileStream).Position = 0; 
        

        
        if (CheckOnSave )
        {
            var b = new byte[Stream.Synchronized(FileStream).Length];
            for (int i = 0; i < b.Length; i++)
            {
                b[i] = (byte)Stream.Synchronized(FileStream).ReadByte();
            }

            if (s == Encoding.UTF8.GetString(b))
            {
#if DEBUG
                Debug.WriteLine($"{DateTime.Now.ToLongTimeString()} No need to serialize: data matches. ");
#endif
                return; 
            }
            
        }
        Stream.Synchronized(FileStream).SetLength(0);
        foreach (var c in s)
        {
            Stream.Synchronized(FileStream).WriteByte((byte)c);
        }
        Stream.Synchronized(FileStream).Flush();
    }
    /// <summary>
    /// Set the property / field in storage. 
    /// </summary>
    /// <param name="expression">Expression that points to the property / field</param>
    /// <param name="value">Value to set. </param>
    /// <typeparam name="T2">Type of the specified property / field</typeparam>
    /// <exception cref="InvalidOperationException">Either its occured by specifying field name with no JsonSerializerOptions which has IncludeFields property set to true or giving something like <c>(i)=>i</c>. </exception>
    public void Set<T2>(Expression<Func<T, T2>> expression, T2 value)
    {
        Stream.Synchronized(FileStream).Position = 0; 
        T? i = JsonSerializer.Deserialize<T>(Stream.Synchronized(FileStream),  JsonSerializerOptions);
        if (i == null)
        {
            throw new NullReferenceException("The object is null. "); 
        }
        if (expression.Body is MemberExpression memberExpression)
        {
            if (memberExpression.Member is PropertyInfo propInfo)
            {
                typeof(T).GetProperty(propInfo.Name)?.SetValue(i, value);
                goto SERIALIZE; 
            }
            if (memberExpression.Member is FieldInfo fieldInfo)
            {
                if (!JsonSerializerOptions.IncludeFields)
                {
                    throw new InvalidOperationException(
                        "While specifying fields, please ensure a JsonSerializerOption with IncludeFields = true is passed in the construction of this object. ");
                }
                typeof(T).GetField(fieldInfo.Name)?.SetValue(i, value);
            }
        }
        else
        {
            throw new InvalidOperationException("Use Set(T obj) instead. ");
        }
        SERIALIZE:
        Stream.Synchronized(FileStream).Position = 0; 
        
        var s = JsonSerializer.Serialize(i, JsonSerializerOptions);
        
        if (CheckOnSave )
        {
            var b = new byte[Stream.Synchronized(FileStream).Length];
            for (int j = 0; j < b.Length; j++)
            {
                b[j] = (byte)Stream.Synchronized(FileStream).ReadByte();
            }

            if (s == Encoding.UTF8.GetString(b))
            {
#if DEBUG
                Debug.WriteLine($"{DateTime.Now.ToLongTimeString()} No need to serialize: data matches. ");
#endif
                return;
            }

        }
        Stream.Synchronized(FileStream).SetLength(0);
        foreach (var c in s)
        {
            Stream.Synchronized(FileStream).WriteByte((byte)c);
        }
        Stream.Synchronized(FileStream).Flush();
    }
    /// <summary>
    /// Get the property / field in storage. 
    /// </summary>
    /// <param name="expression">Expression that points to the property / field</param>
    /// <typeparam name="T2">Type of the specified property / field</typeparam>
    /// <returns>Value of specified property / field</returns>
    /// <exception cref="NullReferenceException">Object is null. </exception>
    /// <exception cref="InvalidOperationException">Specifying field name with no JsonSerializerOptions which has IncludeFields property set to true. </exception>
    public T2 Get<T2>(Expression<Func<T, T2>> expression)
    {
        Stream.Synchronized(FileStream).Position = 0;
        T? i = JsonSerializer.Deserialize<T>(FileStream, JsonSerializerOptions);
        if (i == null)
        {
            throw new NullReferenceException("The object is null. ");
        }

        if (expression.Body is MemberExpression memberExpression)
        {
            if (memberExpression.Member is PropertyInfo propInfo)
            {
                return (T2)typeof(T).GetProperty(propInfo.Name)?.GetValue(i)!;
            }

            if (memberExpression.Member is FieldInfo fieldInfo)
            {
                if (!JsonSerializerOptions.IncludeFields)
                {
                    throw new InvalidOperationException(
                        "While specifying fields, please ensure a JsonSerializerOption with IncludeFields = true is passed in the construction of this object. ");
                }

                return (T2)typeof(T).GetField(fieldInfo.Name)?.GetValue(i)!;
            }
        }
        return (T2)(dynamic)i; 
    }
}

[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#if NET7_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
#endif
public class ProgramStorageSet<T> : IProgramStorage<IEnumerable<T>>, ICollection<T>, IProgramStorageSet
{
    public FileStream FileStream { get; set; }
    public ProgramStorageSet(string identifier, string name, string? path = null, JsonSerializerOptions? options = null, bool checkOnSave = false)
    {
        Configure(identifier, name, path, options);
        if (StorageName is null || ProgramStoragePath is null || JsonSerializerOptions is null)
        {
            throw new Exception("Init failed for unknown reason. "); 
        }

        FileStream = File.Open(ProgramStoragePath, FileMode.OpenOrCreate);
        CheckOnSave = checkOnSave;
    }
    
    public IProgramStorage<IEnumerable<T>> Configure(string identifier, string name, string? path = null, JsonSerializerOptions? options = null)
    {
        if (Configured)
        {
            return this; 
        }
        JsonSerializerOptions = options ?? new JsonSerializerOptions
        {
            IncludeFields = false
        };
        if (path == null)
        {
            if (!File.Exists(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    identifier, $"{name}.json")))
            {
                var dataPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), identifier);
                Directory.CreateDirectory(dataPath); 
                ProgramStoragePath = Path.Join(dataPath,($"{name}.json"));
                File.WriteAllText(ProgramStoragePath, "[]");
                StorageName = name; 
            }
            else
            {
                var dataPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), identifier);
                ProgramStoragePath = Path.Join(dataPath,$"{name}.json");
                StorageName = name;

            }
            return this; 
        }

        if (!File.Exists(Path.Join(path, identifier, $"{name}.json")))
        {
            var d = Path.Join(path, identifier);
            Directory.CreateDirectory(d); 
            ProgramStoragePath = Path.Join(d,$"{name}.json");
            File.WriteAllText(ProgramStoragePath, "[]");
            StorageName = name; 
        }
        else
        {
            var d = Path.Join(path, identifier);
            ProgramStoragePath = Path.Join(d, $"{name}.json");
            StorageName = name; 
        }

        Configured = true;
        return this; 
    }

    public bool Configured { get; private set;  }
    public string StorageName { get; private set; }
    public string ProgramStoragePath { get; private set; }
    public bool CheckOnSave { get; }
    public JsonSerializerOptions JsonSerializerOptions { get; private set; }
    public virtual IEnumerable<T> Get()
    {
        Stream.Synchronized(FileStream).Position = 0;
        return JsonSerializer.Deserialize<List<T>>(Stream.Synchronized(FileStream), JsonSerializerOptions) ?? throw new NullReferenceException("The object is null. ");
    }

    public virtual void Set(IEnumerable<T> obj)
    {
        var s = JsonSerializer.Serialize(obj, JsonSerializerOptions);
        Stream.Synchronized(FileStream).Position = 0; 
        
        if (CheckOnSave)
        {
            var b = new  byte[Stream.Synchronized(FileStream) .Length];
            for (int i = 0; i < b.Length; i++)
            {
                b[i] = (byte)Stream.Synchronized(FileStream).ReadByte();
            }

            if (s == Encoding.UTF8.GetString(b))
            {
#if DEBUG
                Debug.WriteLine($"{DateTime.Now.ToLongTimeString()} No need to serialize: data matches. ");
#endif
                return;
            }
            
        }
        Stream.Synchronized(FileStream).SetLength(0);
        foreach (var c in s)
        {
            Stream.Synchronized(FileStream).WriteByte((byte)c);
        }
        Stream.Synchronized(FileStream).Flush();
    }

    public virtual async Task SetAsync(IEnumerable<T> obj)
    {
        using var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, obj, JsonSerializerOptions);
        var s = Encoding.UTF8.GetString(stream.ToArray());
        Stream.Synchronized(FileStream).Position = 0; 
        if (CheckOnSave)
        {
            var b = new  byte[Stream.Synchronized(stream) .Length];
            for (int i = 0; i < b.Length; i++)
            {
                b[i] = (byte)Stream.Synchronized(stream).ReadByte();
            }

            if (s == Encoding.UTF8.GetString(b))
            {
#if DEBUG
                Debug.WriteLine($"{DateTime.Now.ToLongTimeString()} No need to serialize: data matches. ");
#endif
                return;
            }
            
        }
        Stream.Synchronized(FileStream).SetLength(0);
        await Stream.Synchronized(FileStream).WriteAsync(s.Select(i=>(byte)i).ToArray());


        await Stream.Synchronized(FileStream).FlushAsync();
    }
    public virtual IEnumerator<T> GetEnumerator()
    {
        foreach (var inst in Get())
        {
            yield return inst; 
        }
    }
    
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(T obj)
    {
        Set([..Get(), obj]);
    }

    public void AddRange(IEnumerable<T> obj)
    {
        Set([..Get(), ..obj]);
    }

    public void Clear()
    {
        Set([]);
    }

    public bool Contains(T item)
    {
        return Get().Contains(item); 
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        int local = arrayIndex; 
        foreach (var x1 in Get())
        {
            array[local] = x1; 
            local++;
        }
    }

    public bool Remove(T item)
    {
        var l = Get().ToList();
        l.Remove(item); 
        Set(l);
        if (Get().Count() == l.Count) return false; 
        return true; 
    }

    public bool RemoveAll(Predicate<T> match)
    {
        var l = Get().ToList();
        l.RemoveAll(match.Invoke);
        Set(l);
        if (Get().Count() == l.Count) return false; 
        return true;
    }

    public int Count => Get().Count();

    public bool IsReadOnly
    {
        get => false;
    }
}

[RequiresUnreferencedCode(
    "JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#if NET7_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
#endif
public class DelayedProgramStorageSet<T> : ProgramStorageSet<T>, IDelayedProgramStorageSet, IDisposable, IAsyncDisposable
{
    private IEnumerable<T> UnderlyingStructure { get; set; }
    private Timer? Timer { get; set; }
    public DelayedProgramStorageSet(string identifier, string name, string? path = null, JsonSerializerOptions? options = null, TimeSpan? timespan = null, bool checkOnSave = false) : base(identifier, name, path, options,checkOnSave)
    {
        UnderlyingStructure = JsonSerializer.Deserialize<IEnumerable<T>>(FileStream) ??
                                  throw new NullReferenceException("The object is null. ");
        if (timespan is not null)
        {
            Timer = new(_=>
            { 
                #if DEBUG
                Debug.WriteLine("Test.");
                #endif
                SaveChanges();
            },null, dueTime:TimeSpan.Zero, timespan.Value);
        }
    }

    public override IEnumerable<T> Get()
    {
        return UnderlyingStructure; 
    }
    
    public override IEnumerator<T> GetEnumerator()
    {
        foreach (var x1 in Get())
        {
            yield return x1; 
        }
    }

    public override void Set(IEnumerable<T> obj)
    {
        UnderlyingStructure = obj; 
    }

    public async Task SaveChangesAsync()
    {
        if (CheckOnSave)
        {
            Stream.Synchronized(FileStream).Position = 0; 
            await using var m = new MemoryStream();
            await Stream.Synchronized(FileStream).CopyToAsync(m);
            await using var i = new MemoryStream();
            await JsonSerializer.SerializeAsync(i, UnderlyingStructure, JsonSerializerOptions);
            if (Encoding.UTF8.GetString(m.ToArray()) == Encoding.UTF8.GetString(i.ToArray()))
            {
                #if DEBUG
                Debug.WriteLine($"{DateTime.Now.ToShortTimeString()}No changes detected, skipping writing...");
                #endif
                return; 
            }
        }
        
        Stream.Synchronized(FileStream).SetLength(0);
        await JsonSerializer.SerializeAsync(Stream.Synchronized(FileStream), UnderlyingStructure, JsonSerializerOptions);
        await Stream.Synchronized(FileStream).FlushAsync();
    }
    public void SaveChanges()
    {
        if (CheckOnSave)
        {
            Stream.Synchronized(FileStream).Position = 0; 
            using var m = new MemoryStream();
            Stream.Synchronized(FileStream).CopyTo(m);
            if (JsonSerializer.Serialize(UnderlyingStructure, JsonSerializerOptions) ==
                Encoding.UTF8.GetString(m.ToArray()))
            {
                #if DEBUG
                Debug.WriteLine($"{DateTime.Now.ToShortTimeString()}No changes detected, skipping writing...");
                #endif
                return; 
            } 
        }
        Stream.Synchronized(FileStream).SetLength(0);
        JsonSerializer.Serialize(Stream.Synchronized(FileStream), UnderlyingStructure, JsonSerializerOptions);
        Stream.Synchronized(FileStream).Flush(); 
    }

    public void Dispose()
    {
        Timer?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (Timer != null) await Timer.DisposeAsync();
        await Stream.Synchronized(FileStream).DisposeAsync();
    }
}

public interface IDelayedProgramStorageSet
{
    void SaveChanges();
    Task SaveChangesAsync();
}

public interface IProgramStorageSet
{
    
}

public interface IProgramStorage<T> : IProgramStorage
{
    IProgramStorage<T> Configure(string identifier, string name, string? path = null, JsonSerializerOptions? options = null); 
    T? Get();
    void Set(T obj);
}
public interface IProgramStorage
{
    bool Configured { get; }
    string StorageName { get;   }
    JsonSerializerOptions JsonSerializerOptions { get; }
    string ProgramStoragePath { get;   }
    FileStream FileStream { get; }
    bool CheckOnSave { get; }
}
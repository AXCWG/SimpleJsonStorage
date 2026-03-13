using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace SimpleJsonStorage;

[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#if NET7_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
#endif
public class ProgramStorage<T> : IProgramStorage<T> where T: new()
{
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
    public ProgramStorage(string identifier, string name, string? path = null, JsonSerializerOptions? options = null)
    {
        Configure(identifier, name, path, options);
        if (ProgramStoragePath is null || JsonSerializerOptions is null || StorageName is null)
        {
            throw new Exception("Init failed for unknown reason. ");
        }
    }

    public T? Get()
    {
        return JsonSerializer.Deserialize<T>(File.ReadAllText(ProgramStoragePath), JsonSerializerOptions);
    }

    /// <summary>
    /// Set the entire object. 
    /// </summary>
    /// <param name="obj"></param>
    public void Set(T obj)
    {
        File.WriteAllText(ProgramStoragePath, JsonSerializer.Serialize(obj, JsonSerializerOptions));
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
        T? i = JsonSerializer.Deserialize<T>(File.ReadAllText(ProgramStoragePath),  JsonSerializerOptions);
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
        File.WriteAllText(ProgramStoragePath,JsonSerializer.Serialize(i, JsonSerializerOptions)); 
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
        T? i = JsonSerializer.Deserialize<T>(File.ReadAllText(ProgramStoragePath), JsonSerializerOptions);
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
public class ProgramStorageSet<T> : IProgramStorage<IEnumerable<T>>, ICollection<T>
{
    public ProgramStorageSet(string identifier, string name, string? path = null, JsonSerializerOptions? options = null)
    {
        Configure(identifier, name, path, options);
        if (StorageName is null || ProgramStoragePath is null || JsonSerializerOptions is null)
        {
            throw new Exception("Init failed for unknown reason. "); 
        }
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
    public JsonSerializerOptions JsonSerializerOptions { get; private set; }
    public virtual IEnumerable<T> Get()
    {
        return JsonSerializer.Deserialize<List<T>>(File.ReadAllText(ProgramStoragePath), JsonSerializerOptions) ?? throw new NullReferenceException("The object is null. ");
    }

    public virtual void Set(IEnumerable<T> obj)
    {
        File.WriteAllText(ProgramStoragePath, JsonSerializer.Serialize(obj, JsonSerializerOptions));
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
        var l = Get();
        var d = new List<T>(); 
        foreach (var x1 in l)
        {
            if (x1?.Equals(item) ?? x1 is null && item is null)
            {
                break; 
            }
            d.Add(x1);
        }

        if (d.Count == Count)
        {
            return false; 
        }
        Set(d);
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
public class DelayedProgramStorageSet<T> : ProgramStorageSet<T>
{
    private IEnumerable<T> UnderlyingStructure { get; set; }
    private Timer Timer { get; set; }
    public DelayedProgramStorageSet(string identifier, string name, string? path = null, JsonSerializerOptions? options = null, TimeSpan? timespan = null) : base(identifier, name, path, options)
    {
        if (timespan is null)
        {
            timespan = TimeSpan.FromSeconds(5); 
        }
        UnderlyingStructure = JsonSerializer.Deserialize<IEnumerable<T>>(File.ReadAllText(ProgramStoragePath)) ??
                                  throw new NullReferenceException("The object is null. ");
        Timer = new(_ =>
        {
            base.Set(UnderlyingStructure);
        }, null, dueTime: timespan.Value, );
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
}
public interface IProgramStorage<T>
{
    IProgramStorage<T> Configure(string identifier, string name, string? path = null, JsonSerializerOptions? options = null); 
    bool Configured { get; }
    string StorageName { get;   }
    string ProgramStoragePath { get;   }
    JsonSerializerOptions JsonSerializerOptions { get; }
    T? Get();
    void Set(T obj);
}
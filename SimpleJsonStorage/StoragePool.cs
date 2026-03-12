using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;

namespace SimpleJsonStorage;

[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#if NET7_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
#endif
public abstract class StoragePool
{
    public virtual void OnConfiguring(string identifier, JsonSerializerOptions? options = null)
    {
        foreach (var propertyInfo in GetType().GetProperties().Where(i => i.PropertyType == typeof(ProgramStorageSet<>).MakeGenericType(i.PropertyType.GenericTypeArguments[0])))
        {
            var t = propertyInfo.PropertyType.GenericTypeArguments[0];
            propertyInfo.SetValue(this, Activator.CreateInstance(typeof(ProgramStorageSet<>).MakeGenericType(t), args:
                [identifier, propertyInfo.Name, null, options]));
        }

        
        foreach (var propertyInfo in GetType().GetProperties().Where(i=>i.PropertyType == typeof(ProgramStorage<>).MakeGenericType(i.PropertyType.GenericTypeArguments[0])))
        {
            propertyInfo.SetValue(this, Activator.CreateInstance(typeof(ProgramStorage<>).MakeGenericType(propertyInfo.PropertyType.GenericTypeArguments[0]), args: [identifier, propertyInfo.Name, null, options]));
        }
    }
}

public sealed class SampleStoragePool : StoragePool
{
    public ProgramStorageSet<string> Strings { get; set; } = null!;

    public SampleStoragePool(string identifier, JsonSerializerOptions? options = null)
    {
        OnConfiguring(identifier, options);
    }
}
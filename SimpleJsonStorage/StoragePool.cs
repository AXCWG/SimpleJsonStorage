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
public abstract class StoragePool
{
    public virtual void OnConfiguring(string identifier, Func<ConfigurationBuilder, ConfigurationBuilder>? builder = null, JsonSerializerOptions? options = null)
    {
        var b = builder?.Invoke(new()) ?? new();
        foreach (var propertyInfo in GetType().GetProperties().Where(i => i.PropertyType == typeof(ProgramStorageSet<>).MakeGenericType(i.PropertyType.GenericTypeArguments[0])||
                                                                          i.PropertyType == typeof(ProgramStorage<>).MakeGenericType(i.PropertyType.GenericTypeArguments[0])||
                                                                          i.PropertyType == typeof(DelayedProgramStorageSet<>).MakeGenericType(i.PropertyType.GenericTypeArguments[0])))
        {
            propertyInfo.SetValue(this, b.Apply(propertyInfo,identifier, propertyInfo.Name, null, options ));
        }
        
    }

    
    public void SaveChanges()
    {
        foreach (var propertyInfo in GetType().GetProperties().Where(i =>
                     i.PropertyType ==
                     typeof(DelayedProgramStorageSet<>).MakeGenericType(i.PropertyType.GenericTypeArguments[0])))
        {

            ((IDelayedProgramStorageSet)propertyInfo.GetValue(this)!).SaveChanges(); 
        }
    }
}
public class ConfigurationBuilder
{
    private Dictionary<string, TimeSpan?> AutoSaveChanges { get; set; } = []; 
    public ConfigurationBuilder UseAutoSaveChanges<TStoragePool, TProperty>(Expression<Func<TStoragePool, TProperty>> expression,TimeSpan timeSpan) where TProperty : IDelayedProgramStorageSet
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            if (memberExpression.Member is PropertyInfo propInfo)
            {
                 AutoSaveChanges.Add(propInfo.Name ?? throw new NullReferenceException(), timeSpan);
            }
        }
        return this; 
    }

    internal object? Apply(PropertyInfo p, string identifier, string name, string? path, JsonSerializerOptions? options)
    {
        if (p.PropertyType ==
            typeof(DelayedProgramStorageSet<>).MakeGenericType(p.PropertyType.GenericTypeArguments[0]))
        {
            foreach (var autoSaveChange in AutoSaveChanges)
            {
                if (p.Name == autoSaveChange.Key)
                {
                    return Activator.CreateInstance(
                        typeof(DelayedProgramStorageSet<>).MakeGenericType(p.PropertyType.GenericTypeArguments[0]), identifier, name, path, options, autoSaveChange.Value);
                }
                
            }
            return Activator.CreateInstance(
                typeof(DelayedProgramStorageSet<>).MakeGenericType(p.PropertyType.GenericTypeArguments[0]),
                identifier, name, path, options);
        }
        if (p.PropertyType == typeof(ProgramStorage<>).MakeGenericType(p.PropertyType.GenericTypeArguments[0]))
        {
            return Activator.CreateInstance(
                typeof(ProgramStorage<>).MakeGenericType(p.PropertyType.GenericTypeArguments[0]), identifier, name, path, options);
        }
        if(p.PropertyType == typeof(ProgramStorageSet<>).MakeGenericType(p.PropertyType.GenericTypeArguments[0]))
        {
            return Activator.CreateInstance(
                typeof(ProgramStorageSet<>).MakeGenericType(p.PropertyType.GenericTypeArguments[0]), identifier, name, path, options);
        }

        throw new InvalidOperationException("Property Info does not match to any suitable type. "); 
    }
}
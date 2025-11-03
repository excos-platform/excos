using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Sdk;

namespace Excos.Platform.TestingUtilities;

/// <summary>
/// Custom xUnit DataAttribute that supports dependency injection from a service provider
/// with optional primitive inline data.
/// </summary>
/// <remarks>
/// This attribute enables test methods to receive both:
/// 1. Dependencies injected from a DI container (complex types)
/// 2. Primitive values provided inline via the attribute constructor
/// 
/// Services are resolved from the root service provider. For test isolation,
/// register services as Transient rather than Singleton or Scoped.
/// 
/// Thread-safety: The global service provider should be set once during test assembly
/// initialization before any tests run to avoid race conditions.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class InjectFromServicesDataAttribute : DataAttribute
{
    private static volatile IServiceProvider? _globalServiceProvider;
    private readonly object?[] _inlineData;

    /// <summary>
    /// Sets the global service provider used for dependency injection in tests.
    /// This should be called once during test initialization before any tests run.
    /// </summary>
    /// <param name="serviceProvider">The service provider to use for DI</param>
    /// <remarks>
    /// For thread-safety, call this method once during test assembly initialization
    /// before parallel test execution begins.
    /// </remarks>
    public static void SetServiceProvider(IServiceProvider serviceProvider)
    {
        _globalServiceProvider = serviceProvider;
    }

    /// <summary>
    /// Gets the global service provider (for testing purposes)
    /// </summary>
    internal static IServiceProvider? GetServiceProvider() => _globalServiceProvider;

    /// <summary>
    /// Initializes a new instance of the attribute with optional inline primitive data.
    /// </summary>
    /// <param name="data">Primitive values to be passed to corresponding test parameters</param>
    public InjectFromServicesDataAttribute(params object?[] data)
    {
        _inlineData = data ?? Array.Empty<object?>();
    }

    /// <inheritdoc/>
    public override IEnumerable<object?[]> GetData(MethodInfo testMethod)
    {
        if (_globalServiceProvider == null)
        {
            throw new InvalidOperationException(
                "GlobalServiceProvider not set. Call InjectFromServicesDataAttribute.SetServiceProvider() " +
                "during test initialization.");
        }

        var parameters = testMethod.GetParameters();
        var values = new object?[parameters.Length];

        int inlineDataIndex = 0;

        for (int i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            var parameterType = parameter.ParameterType;

            // Check if this is a primitive or simple type that should use inline data
            if (IsPrimitiveOrSimpleType(parameterType) && inlineDataIndex < _inlineData.Length)
            {
                values[i] = _inlineData[inlineDataIndex++];
            }
            else
            {
                // Try to resolve from DI container
                // Note: Services are resolved from the root provider. For test isolation,
                // register services as Transient rather than Singleton/Scoped.
                var dependency = _globalServiceProvider.GetService(parameterType);
                if (dependency == null)
                {
                    throw new InvalidOperationException(
                        $"Cannot resolve parameter '{parameter.Name}' of type '{parameterType.FullName}' " +
                        $"for test method '{testMethod.Name}'. Ensure the service is registered in the DI container " +
                        $"or provide inline data for primitive types.");
                }
                values[i] = dependency;
            }
        }

        yield return values;
    }

    /// <summary>
    /// Determines if a type is considered primitive or simple for the purpose of inline data.
    /// </summary>
    private static bool IsPrimitiveOrSimpleType(Type type)
    {
        return type.IsPrimitive
            || type.IsEnum
            || type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(TimeSpan)
            || type == typeof(Guid)
            || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) 
                && IsPrimitiveOrSimpleType(Nullable.GetUnderlyingType(type)!));
    }
}

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Sdk;

namespace Excos.Platform.TestingUtilities;

/// <summary>
/// Custom xUnit DataAttribute that supports dependency injection from a service provider.
/// </summary>
/// <remarks>
/// This attribute resolves test method parameters from a DI container and can be combined
/// with <see cref="InjectInlineAttribute"/> to create multiple test cases with different
/// inline data values.
/// 
/// Each test execution creates a new service scope for proper isolation of scoped services.
/// 
/// Thread-safety: The global service provider should be set once during test assembly
/// initialization before any tests run to avoid race conditions.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class InjectDataAttribute : DataAttribute
{
    private static volatile IServiceProvider? _globalServiceProvider;

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

    /// <inheritdoc/>
    public override IEnumerable<object?[]> GetData(MethodInfo testMethod)
    {
        if (_globalServiceProvider == null)
        {
            throw new InvalidOperationException(
                "GlobalServiceProvider not set. Call InjectDataAttribute.SetServiceProvider() " +
                "during test initialization.");
        }

        // Get all InjectInline attributes
        var inlineAttributes = testMethod.GetCustomAttributes<InjectInlineAttribute>().ToArray();
        
        // If there are no inline attributes, create a single test case with just DI services
        if (inlineAttributes.Length == 0)
        {
            yield return ResolveParameters(testMethod, Array.Empty<object?>());
        }
        else
        {
            // Create a test case for each InjectInline attribute
            foreach (var inlineAttr in inlineAttributes)
            {
                yield return ResolveParameters(testMethod, inlineAttr.Data);
            }
        }
    }

    /// <summary>
    /// Resolves test method parameters by combining inline data with DI services.
    /// </summary>
    private object?[] ResolveParameters(MethodInfo testMethod, object?[] inlineData)
    {
        // Create a new scope for this test execution
        using var scope = _globalServiceProvider!.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        // Check if there's an ITestMethodContainer and set the method info
        var methodContainer = scopedProvider.GetService<ITestMethodContainer>();
        if (methodContainer != null)
        {
            methodContainer.Method = testMethod;
        }

        var parameters = testMethod.GetParameters();
        var values = new object?[parameters.Length];

        int inlineDataIndex = 0;

        for (int i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            var parameterType = parameter.ParameterType;

            // Check if this is a primitive or simple type that should use inline data
            if (IsPrimitiveOrSimpleType(parameterType) && inlineDataIndex < inlineData.Length)
            {
                values[i] = inlineData[inlineDataIndex++];
            }
            else
            {
                // Try to resolve from DI container with the new scope
                var dependency = scopedProvider.GetService(parameterType);
                if (dependency == null)
                {
                    throw new InvalidOperationException(
                        $"Cannot resolve parameter '{parameter.Name}' of type '{parameterType.FullName}' " +
                        $"for test method '{testMethod.Name}'. Ensure the service is registered in the DI container " +
                        $"or provide inline data for primitive types using [InjectInline].");
                }
                values[i] = dependency;
            }
        }

        return values;
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

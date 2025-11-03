using Microsoft.Extensions.DependencyInjection;

namespace Excos.Platform.TestingUtilities;

/// <summary>
/// Legacy attribute for backward compatibility. Use <see cref="InjectDataAttribute"/> 
/// with <see cref="InjectInlineAttribute"/> instead.
/// </summary>
/// <remarks>
/// This attribute is maintained for backward compatibility with existing tests.
/// New tests should use [InjectData] with [InjectInline(a, b, c)] for better support
/// of multiple test case variants.
/// </remarks>
[Obsolete("Use InjectDataAttribute with InjectInlineAttribute instead for better multi-case support.")]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class InjectFromServicesDataAttribute : InjectDataAttribute
{
    /// <summary>
    /// Sets the global service provider used for dependency injection in tests.
    /// </summary>
    /// <param name="serviceProvider">The service provider to use for DI</param>
    public new static void SetServiceProvider(IServiceProvider serviceProvider)
    {
        InjectDataAttribute.SetServiceProvider(serviceProvider);
    }

    /// <summary>
    /// Initializes a new instance of the attribute with optional inline primitive data.
    /// </summary>
    /// <param name="data">Primitive values to be passed to corresponding test parameters</param>
    /// <remarks>
    /// This constructor is maintained for backward compatibility. For new tests,
    /// use [InjectData] with [InjectInline(a, b, c)] instead.
    /// </remarks>
    public InjectFromServicesDataAttribute(params object?[] data)
    {
        // For backward compatibility, if inline data is provided,
        // we'll apply it as a single InjectInline attribute would
        if (data != null && data.Length > 0)
        {
            // Store data to be used in GetData
            _legacyInlineData = data;
        }
    }

    private readonly object?[]? _legacyInlineData;

    /// <inheritdoc/>
    public override IEnumerable<object?[]> GetData(System.Reflection.MethodInfo testMethod)
    {
        // If legacy inline data was provided in constructor, use it
        if (_legacyInlineData != null && _legacyInlineData.Length > 0)
        {
            // Temporarily inject as if there was an InjectInline attribute
            yield return ResolveParametersLegacy(testMethod, _legacyInlineData);
        }
        else
        {
            // Use base implementation
            foreach (var data in base.GetData(testMethod))
            {
                yield return data;
            }
        }
    }

    private object?[] ResolveParametersLegacy(System.Reflection.MethodInfo testMethod, object?[] inlineData)
    {
        var provider = GetServiceProvider();
        if (provider == null)
        {
            throw new InvalidOperationException(
                "GlobalServiceProvider not set. Call InjectFromServicesDataAttribute.SetServiceProvider() " +
                "during test initialization.");
        }

        using var scope = provider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

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

            if (IsPrimitiveOrSimpleType(parameterType) && inlineDataIndex < inlineData.Length)
            {
                values[i] = inlineData[inlineDataIndex++];
            }
            else
            {
                var dependency = scopedProvider.GetService(parameterType);
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

        return values;
    }

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

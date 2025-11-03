namespace Excos.Platform.TestingUtilities;

/// <summary>
/// Attribute that provides inline data for test method parameters.
/// Multiple instances of this attribute can be applied to a single test method
/// to create multiple test cases with different inline values.
/// </summary>
/// <remarks>
/// This attribute should be used in conjunction with <see cref="InjectDataAttribute"/>.
/// The InjectDataAttribute will combine the inline data from all InjectInlineAttributes
/// with the services resolved from the DI container to create test case variants.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class InjectInlineAttribute : Attribute
{
    /// <summary>
    /// Gets the inline data values for this test case.
    /// </summary>
    public object?[] Data { get; }

    /// <summary>
    /// Initializes a new instance of the attribute with inline data values.
    /// </summary>
    /// <param name="data">The inline data values to be passed to test method parameters</param>
    public InjectInlineAttribute(params object?[] data)
    {
        Data = data ?? Array.Empty<object?>();
    }
}

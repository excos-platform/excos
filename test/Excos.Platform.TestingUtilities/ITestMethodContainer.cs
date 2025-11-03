using System.Reflection;

namespace Excos.Platform.TestingUtilities;

/// <summary>
/// Interface for services that need access to the test method being executed.
/// Used for dynamic dependency resolution based on test method metadata.
/// </summary>
public interface ITestMethodContainer
{
    /// <summary>
    /// Gets or sets the test method information.
    /// This property is set by the test framework before service resolution.
    /// </summary>
    MethodInfo? Method { get; set; }
}

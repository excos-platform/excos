# Excos.Platform.TestingUtilities

This library provides testing utilities for the Excos platform, including dependency injection support for xUnit tests.

## InjectDataAttribute and InjectInlineAttribute

A pair of custom xUnit attributes that enable dependency injection directly into test method parameters with support for multiple test case variants.

### Features

1. **Dependency Injection**: Automatically resolves complex types from a DI container
2. **Multiple Test Cases**: Use multiple `[InjectInline]` attributes to create test variants with different inline data
3. **Service Scope Management**: Creates a new service scope for each test execution to ensure proper isolation
4. **Mixed Parameters**: Allows mixing both primitive inline data and injected dependencies in the same test method
5. **ITestMethodContainer**: Services can access test method metadata for dynamic resolution

### Setup

Before running tests, you need to configure the service provider. This is typically done in a test fixture:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Excos.Platform.TestingUtilities;
using Xunit;

public class TestFixture
{
    public TestFixture()
    {
        var services = new ServiceCollection();
        
        // Register your services (use Transient or Scoped for test isolation)
        services.AddTransient<IMyService, MyService>();
        services.AddTransient<IAnotherService, AnotherService>();
        
        // Optional: Register ITestMethodContainer if services need test method info
        services.AddScoped<ITestMethodContainer, MyTestMethodContainer>();
        
        var provider = services.BuildServiceProvider();
        InjectDataAttribute.SetServiceProvider(provider);
    }
}

[CollectionDefinition("My Tests")]
public class MyTestCollection : ICollectionFixture<TestFixture> { }
```

### Usage Examples

#### Example 1: Inject Services Only

```csharp
[Collection("My Tests")]
public class MyTests
{
    [Theory]
    [InjectData]
    public void TestWithInjectedService(IMyService service)
    {
        // service is automatically resolved from the DI container
        var result = service.DoSomething();
        Assert.NotNull(result);
    }
}
```

#### Example 2: Multiple Test Cases with Inline Data

```csharp
[Theory]
[InjectData]
[InjectInline(10, 5)]
[InjectInline(20, 10)]
[InjectInline(7, 3)]
public void TestWithMultipleVariants(
    int x,                  // Comes from each InjectInline attribute
    int y,                  // Comes from each InjectInline attribute
    IMyService service)     // Resolved from DI container
{
    var result = service.Calculate(x, y);
    Assert.Equal(x + y, result);
}
```

This will create 3 separate test cases, one for each `[InjectInline]` attribute.

#### Example 3: Multiple Services with Inline Data

```csharp
[Theory]
[InjectData]
[InjectInline("test-id-1")]
[InjectInline("test-id-2")]
public void TestWithMultipleServices(
    string id,                      // Comes from InjectInline
    IMyService service1,            // Resolved from DI container
    IAnotherService service2)       // Resolved from DI container
{
    service1.Process(id);
    var result = service2.GetResult();
    Assert.NotNull(result);
}
```

### F# Usage

The attributes can be used from F# as well:

```fsharp
open Xunit
open Microsoft.Extensions.DependencyInjection
open Excos.Platform.TestingUtilities

type TestFixture() =
    do
        let services = ServiceCollection()
        services.AddTransient<IMyService, MyService>() |> ignore
        let provider = services.BuildServiceProvider()
        InjectDataAttribute.SetServiceProvider(provider)

[<CollectionDefinition("My Tests")>]
type MyTestCollection() =
    interface ICollectionFixture<TestFixture>

[<Collection("My Tests")>]
module MyTests =
    
    [<Theory>]
    [<InjectData>]
    let ``Test with injected service`` (service: IMyService) =
        let result = service.DoSomething()
        Assert.NotNull(result)
    
    [<Theory>]
    [<InjectData>]
    [<InjectInline(10, 5)>]
    [<InjectInline(20, 10)>]
    let ``Test with multiple variants`` (x: int) (y: int) (service: IMyService) =
        let result = service.Calculate x y
        Assert.Equal(x + y, result)
```

### ITestMethodContainer Interface

Services can implement `ITestMethodContainer` to receive information about the test method being executed:

```csharp
public class DynamicService : ITestMethodContainer
{
    public MethodInfo? Method { get; set; }
    
    public void ProcessBasedOnTestMethod()
    {
        if (Method != null)
        {
            // Use Method.Name, Method.GetCustomAttributes(), etc.
            Console.WriteLine($"Executing in test: {Method.Name}");
        }
    }
}
```

Register it as a scoped service:

```csharp
services.AddScoped<ITestMethodContainer, DynamicService>();
services.AddScoped<IDynamicService>(sp => (DynamicService)sp.GetRequiredService<ITestMethodContainer>());
```

### How It Works

1. **Parameter Resolution**: The `InjectDataAttribute` examines each test method parameter:
   - **Primitive types** (int, string, bool, DateTime, etc.) are filled from the inline data in `[InjectInline]` attributes
   - **Complex types** (interfaces, classes) are resolved from the DI container

2. **Service Scopes**: Each test execution creates a new service scope:
   - Scoped services are unique per test case
   - Proper disposal of disposable services
   - Complete test isolation

3. **Multiple Test Cases**: Each `[InjectInline]` attribute creates a separate test case:
   ```csharp
   [InjectData]
   [InjectInline(1, "test")]   // Test case 1
   [InjectInline(2, "demo")]   // Test case 2
   ```

4. **Parameter Order**: Primitive parameters should appear first, followed by injected services:
   ```csharp
   [InjectData]
   [InjectInline(1, "test")]
   void MyTest(
       int primitiveParam1,      // ← From InjectInline
       string primitiveParam2,   // ← From InjectInline
       IService injectedService) // ← From DI container
   ```

### Supported Primitive Types

The following types are considered "primitive" and will be sourced from inline data:

- All built-in primitive types (int, long, bool, etc.)
- string
- decimal
- DateTime, DateTimeOffset, TimeSpan
- Guid
- Enums
- Nullable versions of the above

### Error Handling

- If the service provider is not set, tests will fail with: `GlobalServiceProvider not set`
- If a service cannot be resolved, tests will fail with: `Cannot resolve parameter 'X' of type 'Y'`
- If no `[InjectInline]` attributes are present, a single test case with only injected services is created

### Best Practices

1. **Set up the service provider once** per test collection using a fixture
2. **Order parameters** with primitives first, then injected services
3. **Use Transient or Scoped services** for proper test isolation with the new scope-per-test behavior
4. **Use multiple [InjectInline] attributes** to create test variants instead of multiple test methods
5. **Thread-safety** - call `SetServiceProvider` once during assembly initialization before parallel tests run
6. **Dispose properly** - scopes are automatically disposed, but ensure your fixture disposes the root provider

### Important Notes

- **Service Lifetime**: Each test creates a new scope. Use Transient or Scoped lifetime for proper test isolation.
- **Thread Safety**: The service provider is stored in a static field. Set it once during test assembly initialization.
- **Automatic Scoping**: Unlike the legacy approach, each test now automatically gets its own scope for proper isolation.

### Legacy InjectFromServicesDataAttribute

For backward compatibility, the old `InjectFromServicesDataAttribute` is still available but deprecated. It's recommended to migrate to the new `[InjectData]` + `[InjectInline]` pattern for better multi-case support.

```csharp
// Old (deprecated):
[Theory]
[InjectFromServicesData(10, 5)]
public void OldStyleTest(int x, int y, IMyService service) { }

// New (recommended):
[Theory]
[InjectData]
[InjectInline(10, 5)]
public void NewStyleTest(int x, int y, IMyService service) { }
```

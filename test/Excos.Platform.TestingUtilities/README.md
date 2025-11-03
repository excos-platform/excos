# Excos.Platform.TestingUtilities

This library provides testing utilities for the Excos platform, including dependency injection support for xUnit tests.

## InjectFromServicesDataAttribute

A custom xUnit `DataAttribute` that enables dependency injection directly into test method parameters, with support for mixed primitive and injected dependencies.

### Features

1. **Dependency Injection**: Automatically resolves complex types from a DI container
2. **Primitive Inline Data**: Supports passing primitive values directly via the attribute constructor
3. **Service Scope Management**: Creates a new service scope for each test execution to ensure test isolation
4. **Mixed Parameters**: Allows mixing both primitive inline data and injected dependencies in the same test method

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
        
        // Register your services
        services.AddSingleton<IMyService, MyService>();
        services.AddTransient<IAnotherService, AnotherService>();
        
        var provider = services.BuildServiceProvider();
        InjectFromServicesDataAttribute.SetServiceProvider(provider);
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
    [InjectFromServicesData]
    public void TestWithInjectedService(IMyService service)
    {
        // service is automatically resolved from the DI container
        var result = service.DoSomething();
        Assert.NotNull(result);
    }
}
```

#### Example 2: Mix Primitive Data and Injected Services

```csharp
[Theory]
[InjectFromServicesData(10, 5)]
public void TestWithPrimitiveAndInjectedData(
    int x,                  // Comes from inline data (10)
    int y,                  // Comes from inline data (5)
    IMyService service)     // Resolved from DI container
{
    var result = service.Calculate(x, y);
    Assert.Equal(15, result);
}
```

#### Example 3: Multiple Services with Primitive Data

```csharp
[Theory]
[InjectFromServicesData("test-id")]
public void TestWithMultipleServices(
    string id,                      // Comes from inline data
    IMyService service1,            // Resolved from DI container
    IAnotherService service2)       // Resolved from DI container
{
    service1.Process(id);
    var result = service2.GetResult();
    Assert.NotNull(result);
}
```

### F# Usage

The attribute can be used from F# as well:

```fsharp
open Xunit
open Microsoft.Extensions.DependencyInjection
open Excos.Platform.TestingUtilities

type TestFixture() =
    do
        let services = ServiceCollection()
        services.AddSingleton<IMyService, MyService>() |> ignore
        let provider = services.BuildServiceProvider()
        InjectFromServicesDataAttribute.SetServiceProvider(provider)

[<CollectionDefinition("My Tests")>]
type MyTestCollection() =
    interface ICollectionFixture<TestFixture>

[<Collection("My Tests")>]
module MyTests =
    
    [<Theory>]
    [<InjectFromServicesData>]
    let ``Test with injected service`` (service: IMyService) =
        let result = service.DoSomething()
        Assert.NotNull(result)
    
    [<Theory>]
    [<InjectFromServicesData(10, 5)>]
    let ``Test with mixed data`` (x: int) (y: int) (service: IMyService) =
        let result = service.Calculate x y
        Assert.Equal(15, result)
```

### How It Works

1. **Parameter Resolution**: The attribute examines each test method parameter:
   - **Primitive types** (int, string, bool, DateTime, etc.) are filled from the inline data passed to the attribute constructor
   - **Complex types** (interfaces, classes) are resolved from the DI container

2. **Service Scopes**: Each test execution creates a new service scope, ensuring:
   - Scoped services are unique per test
   - Proper disposal of disposable services
   - Test isolation

3. **Parameter Order**: Primitive parameters should appear first, followed by injected services:
   ```csharp
   [InjectFromServicesData(1, "test")]  // Inline data
   void MyTest(
       int primitiveParam1,      // ← From inline data
       string primitiveParam2,   // ← From inline data
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
- If inline data is provided for fewer primitive parameters than exist in the method, remaining primitives will attempt DI resolution (and likely fail)

### Best Practices

1. **Set up the service provider once** per test collection using a fixture
2. **Order parameters** with primitives first, then injected services
3. **Keep primitive data simple** - use inline data for test case variations
4. **Use scoped or transient services** when tests need isolation
5. **Dispose properly** - the attribute handles scope disposal, but ensure your fixture disposes the root provider if needed

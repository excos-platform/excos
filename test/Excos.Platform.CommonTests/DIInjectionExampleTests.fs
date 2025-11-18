namespace Excos.Platform.CommonTests

open System
open Microsoft.Extensions.DependencyInjection
open Xunit
open Excos.Platform.TestingUtilities

// Example services for testing DI injection
type ICalculatorService =
    abstract member Add: int -> int -> int
    abstract member Multiply: int -> int -> int

type CalculatorService() =
    interface ICalculatorService with
        member _.Add x y = x + y
        member _.Multiply x y = x * y

type ILoggerService =
    abstract member Log: string -> unit

type LoggerService() =
    let mutable logs = []
    interface ILoggerService with
        member _.Log message = logs <- message :: logs
    member _.GetLogs() = List.rev logs

// Test fixture to set up DI container
type DITestFixture() =
    do
        let services = ServiceCollection()
        services.AddTransient<ICalculatorService, CalculatorService>() |> ignore
        services.AddTransient<ILoggerService, LoggerService>() |> ignore
        let provider = services.BuildServiceProvider()
        InjectDataAttribute.SetServiceProvider(provider)

    interface IDisposable with
        member _.Dispose() = ()

// Test class that uses the fixture
[<CollectionDefinition("DI Injection Tests")>]
type DICollectionFixture() =
    interface ICollectionFixture<DITestFixture>

[<Collection("DI Injection Tests")>]
module DIInjectionExampleTests =

    // Example test using DI injection only (new style)
    [<Theory>]
    [<InjectData>]
    let ``Calculator service can add numbers`` (calculator: ICalculatorService) =
        let result = calculator.Add 5 3
        Assert.Equal(8, result)

    // Example test with multiple test cases using InjectInline (new style)
    [<Theory>]
    [<InjectData>]
    [<InjectInline(10, 5)>]
    [<InjectInline(20, 10)>]
    [<InjectInline(7, 3)>]
    let ``Calculator service operations with multiple inline variants`` (x: int) (y: int) (calculator: ICalculatorService) =
        let sum = calculator.Add x y
        let product = calculator.Multiply x y
        Assert.Equal(x + y, sum)
        Assert.Equal(x * y, product)

    // Example test with multiple injected services and inline data (new style)
    [<Theory>]
    [<InjectData>]
    [<InjectInline("test operation")>]
    [<InjectInline("another test")>]
    let ``Multiple services with inline message variants`` (message: string) (calculator: ICalculatorService) (logger: ILoggerService) =
        logger.Log(message)
        let result = calculator.Add 2 3
        Assert.Equal(5, result)

    // Legacy test using old attribute (for backward compatibility testing)
    [<Theory>]
    [<InjectFromServicesData(100, 50)>]
    let ``Legacy attribute still works`` (x: int) (y: int) (calculator: ICalculatorService) =
        let sum = calculator.Add x y
        Assert.Equal(150, sum)

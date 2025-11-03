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
        services.AddSingleton<ICalculatorService, CalculatorService>() |> ignore
        services.AddTransient<ILoggerService, LoggerService>() |> ignore
        let provider = services.BuildServiceProvider()
        InjectFromServicesDataAttribute.SetServiceProvider(provider)

    interface IDisposable with
        member _.Dispose() = ()

// Test class that uses the fixture
[<CollectionDefinition("DI Injection Tests")>]
type DICollectionFixture() =
    interface ICollectionFixture<DITestFixture>

[<Collection("DI Injection Tests")>]
module DIInjectionExampleTests =

    // Example test using DI injection only
    [<Theory>]
    [<InjectFromServicesData>]
    let ``Calculator service can add numbers`` (calculator: ICalculatorService) =
        let result = calculator.Add 5 3
        Assert.Equal(8, result)

    // Example test using both DI injection and inline primitive data
    [<Theory>]
    [<InjectFromServicesData(10, 5)>]
    let ``Calculator service operations with inline data`` (x: int) (y: int) (calculator: ICalculatorService) =
        let sum = calculator.Add x y
        let product = calculator.Multiply x y
        Assert.Equal(15, sum)
        Assert.Equal(50, product)

    // Example test with multiple injected services and primitive data
    [<Theory>]
    [<InjectFromServicesData("test operation")>]
    let ``Multiple services with inline message`` (message: string) (calculator: ICalculatorService) (logger: ILoggerService) =
        logger.Log(message)
        let result = calculator.Add 2 3
        Assert.Equal(5, result)
        // Note: In a real test, you'd need a way to verify the log was called

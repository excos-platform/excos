namespace Excos.Platform.WebApiHostTests

open Xunit
open Excos.Platform.AppHostTests
open Excos.Platform.ApiClient.V1
open Excos.Testing.OpenTelemetry.Asserts

module Tests =

    let appTask = AppHost.StartAsync()

    [<Fact>]
    let ``Api / returns "Hello World!"`` () = task {
        let! app = appTask
        let! client = app.GetWebApiClientAsync()

        let! response = client.GetAsync("/")
        let! content = response.Content.ReadAsStringAsync()
 
        Assert.Equal("Hello World!", content)
    }

    [<Fact>]
    let ``Api /counter performs increases over managed state`` () = task {
        use! app = appTask
        let! httpClient = app.GetWebApiClientAsync()
        let client = CountersClient(httpClient)
        client.ReadResponseAsString <- true // for debugging
        let counterRef = "'my-counter'"

        try
            let! _ = client.GetByKeyAsync(counterRef)
            Assert.Fail("Expected exception not thrown: 404 NotFound")
        with
        | :? ExcosApiException as ex ->
            Assert.Equal(404, ex.StatusCode)
 
        let! response = client.IncreaseByKeyAsync(counterRef)
        Assert.Equal("Counter increased", response.Value)

        let! response = client.GetByKeyAsync(counterRef)
        Assert.Equal(1, response.Value)

        let! _ = client.IncreaseByKeyAsync(counterRef)
        let! response = client.GetByKeyAsync(counterRef)
        Assert.Equal(2, response.Value)
    }

    [<Fact>]
    let ``Api /counter logs event`` () = task {
        use! app = appTask
        let! httpClient = app.GetWebApiClientAsync()
        let client = CountersClient(httpClient)
        let! _ = client.IncreaseByKeyAsync("'my-counter'")

        do! AppHost.TestOtlpServer.WaitForEvents()

        AppHost.TestOtlpServer.Should()
            .HaveLog("Increased") |> ignore
    }
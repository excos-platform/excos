namespace Excos.Platform.WebApiHostTests

open System.Net
open System.Net.Http
open Xunit
open Excos.Platform.AppHostTests

module Tests =

    [<Fact>]
    let ``Api / returns "Hello World!"`` () = task {
        use! app = AppHost.StartAsync()
        let! client = app.GetWebApiClientAsync()

        let! response = client.GetAsync("/")
        let! content = response.Content.ReadAsStringAsync()
 
        Assert.Equal("Hello World!", content)
    }

    [<Fact>]
    let ``Api /counter performs increases over managed state`` () = task {
        use! app = AppHost.StartAsync()
        let! client = app.GetWebApiClientAsync()

        let! response = client.GetAsync("/api/Counters('my-counter')")
        let! content = response.Content.ReadAsStringAsync()
 
        Assert.Equal("0", content)

        use content = new StringContent("")
        let! response = client.PostAsync("/api/Counters('my-counter')/Increase", content)

        Assert.Equal(HttpStatusCode.OK, response.StatusCode)

        let! response = client.GetAsync("/api/Counters('my-counter')")
        let! content = response.Content.ReadAsStringAsync()

        Assert.Equal("1", content)
    }

namespace Excos.Platform.WebApiHostTests

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

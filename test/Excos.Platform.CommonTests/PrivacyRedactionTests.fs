namespace Excos.Platform.CommonTests

open System.Collections.Generic
open Xunit
open Excos.Platform.Common.Privacy
open Excos.Platform.Common.Privacy.Redaction

module PrivacyRedactionTests =

    type TestEvent() =
        [<UII>]
        member val UIIProperty = "UII" with get, set

        [<CC>]
        member val CCProperty = "CC" with get, set

        [<UPI>]
        member val UPIProperty = "UPI" with get, set
        [<UPI;UserHashed>]
        member val UPIHashedProperty = "UPIHashed" with get, set
        [<UPI;TenantHashed>]
        member val UPIHashedTenantProperty = "UPIHashedTenant" with get, set
        [<UDI>]
        member val UDIProperty = "UDI" with get, set
        [<OI>]
        member val OIProperty = "OI" with get, set
        [<OI;TenantHashed>]
        member val OIHashedTenantProperty = "OIHashedTenant" with get, set
        [<SYS>]
        member val SYSProperty = "SYS" with get, set

    [<Fact>]
    let ``CC and UII fields are not logged`` () =
        let descriptors = PrivacyValueDescriptor.GetDescriptors(typeof<TestEvent>)
        let redactor = PrivacyValueRedactor()
        let testEvent = TestEvent()
        let dataAttributes = Dictionary<string, string>();

        for descriptor in descriptors do
            dataAttributes.Add(descriptor.OpenTelemetryName, redactor.Redact(descriptor.GetValue(testEvent), descriptor.Redaction));

        Assert.Equal(7, dataAttributes.Count)
        Assert.Equal("UPI", dataAttributes.[nameof Unchecked.defaultof<TestEvent>.UPIProperty])
        Assert.Equal("132d04f2-8030-5fd1-8577-9e768f6be383", dataAttributes.[nameof Unchecked.defaultof<TestEvent>.UPIHashedProperty])
        Assert.Equal("d9e3b13e-1d2c-5907-92a1-d24627fda02e", dataAttributes.[nameof Unchecked.defaultof<TestEvent>.UPIHashedTenantProperty])
        Assert.Equal("UDI", dataAttributes.[nameof Unchecked.defaultof<TestEvent>.UDIProperty])
        Assert.Equal("OI", dataAttributes.[nameof Unchecked.defaultof<TestEvent>.OIProperty])
        Assert.Equal("525aa953-03bc-5b95-b69c-41909a6d43b6", dataAttributes.[nameof Unchecked.defaultof<TestEvent>.OIHashedTenantProperty])
        Assert.Equal("SYS", dataAttributes.[nameof Unchecked.defaultof<TestEvent>.SYSProperty])

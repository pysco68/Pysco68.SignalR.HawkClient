# Pysco68.SignalR.HawkHandler

Enables you to use the Hawk authentication scheme for SignalR connections from .NET client. No server-side modification are required (beside taking care of the order in the OWIN pipeline => not include SignalR prior to the Hawk authentication middleware).

## Usage

You can install the HawkHandler from Nuget:

```
Install-Package Pysco68.SignalR.HawkClient
```

Please take note that this library relies on `Thinktecture.IdentityModel.Hawk` for implementing the actual Hawk authentication.

```C#
var hubConnection = new HubConnection(url);
            
/* Hub registration */

var credential = = new Credential
{
    Id = "id",
    Key = key,
    Algorithm = SupportedAlgorithms.SHA256,
    User = "user"
};

var httpClient = new HawkHttpClient(credential);
await hubConnection.Start(httpClient);
```

## Licence

See LICENCE file.

The class `Disposer` is original work from Microsoft Open Technologies, Inc. and can be found in the SignalR project. It is licensed under Apache License, Version 2.0.

## Help / Contribution

If you found a bug, please create an issue. Want to contribute? Yes, please! Create a pull request!
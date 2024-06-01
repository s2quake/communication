# Communication

## Summary

The Communication library is network module using <https://github.com/grpc/grpc>.

## Development Environment

```plain
.NET 8.0
c# 12.0
```

## Clone

```bash
git clone https://github.com/s2quake/communication.git
cd communication
```

## Build

```bash
dotnet build
```

## Run server

```bash
dotnet run --project src/JSSoft.Communication.Server
```

## Run client

```bash
dotnet run --project src/JSSoft.Communication.Client
```

## Quick start

### 1. Create solution

Run terminal and execute the command line below to create a solution and project

```plain
mkdir .example
dotnet new sln -o .example
dotnet new console -o .example/Server-Test
dotnet new console -o .example/Client-Test
dotnet sln .example add .example/Server-Test
dotnet sln .example add .example/Client-Test
dotnet add .example/Server-Test reference src/JSSoft.Communication
dotnet add .example/Client-Test reference src/JSSoft.Communication
```

### 2. Define service

Create `IMyService.cs` file in the path .example/Server-Test.

Then copy and paste the code below into the file `IMyService.cs`.

```csharp
namespace Services;

public interface IMyService
{
    Task<Guid> LoginAsync(string userID, CancellationToken cancellationToken);

    Task<(string product, string version)> GetVersionAsync(CancellationToken cancellationToken);
}

```

### 3. Implement service

Create `MyService.cs` file in the path .example/Server-Test.

Then copy and paste the code below into the file `MyService.cs`.

```csharp
using JSSoft.Communication;

namespace Services;

sealed class MyService : ServerService<IMyService>, IMyService
{
    public Task<Guid> LoginAsync(string userID, CancellationToken cancellationToken)
    {
        return Task.Run(() => 
        {
            Console.WriteLine($"logged in: '{userID}'");
            return Guid.NewGuid();
        }, cancellationToken);
    }

    public Task<(string product, string version)> GetVersionAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            return ("MyServer", "1.0.0.0");
        }, cancellationToken);
    }
}

```

## 4. Implement code to run the server

Create `Program.cs` file in the path .example/Server-Test.

Then copy and paste the code below into the file `Program.cs`.

```csharp
using JSSoft.Communication;
using JSSoft.Communication.Extensions;
using Services;

var service = new MyService();
var serviceContext = new ServerContext([service]);
var token = await serviceContext.OpenAsync(cancellationToken: default);

Console.WriteLine("Server has been started.");
Console.WriteLine("Press any key to exit.");
Console.ReadKey();

var exitCode = await serviceContext.ReleaseAsync(token);
Environment.Exit(exitCode);

```

## 5. Implement code to run the client

First, you need to include the file `IMyService.cs` that you created when you created the Server-Test project.

Open `.example/Client-Test/Client-Test.csproj` and insert `<Compile Include="..\Server-Test\IMyService.cs" />` into the file as shown below.

```xml
...
  <ItemGroup>
    <ProjectReference Include="..\..\src\JSSoft.Communication\JSSoft.Communication.csproj" />
    <!-- Add this line -->
    <Compile Include="..\Server-Test\IMyService.cs" />
  </ItemGroup>
...
```

And create `Program.cs` file in the path .example/Server-Test.

Then copy and paste the code below into the file `Program.cs`.

```csharp
using JSSoft.Communication;
using JSSoft.Communication.Extensions;
using Services;

var service = new ClientService<IMyService>();
var serviceContext = new ClientContext([service]);

var token = await serviceContext.OpenAsync(cancellationToken: default);
var server = service.Server;

var id = await server.LoginAsync("admin", cancellationToken: default);
var (product, version) = await server.GetVersionAsync(cancellationToken: default);
Console.WriteLine($"logged in: {id}");
Console.WriteLine($"product: {product}");
Console.WriteLine($"version: {version}");

Console.WriteLine("Press any key to exit.");
Console.ReadKey();

var exitCode = await serviceContext.ReleaseAsync(token);
Environment.Exit(exitCode);

```

## 6. Build and Run example

Build example solution

```bash
dotnet build .example
```

Run Server-Test example project in terminal

```bash
dotnet run --project .example/Server-Test
```

Run Client-Test example project in a new terminal

```bash
dotnet run --project .example/Client-Test
```

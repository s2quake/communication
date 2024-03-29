# Communication

## 개요

<https://github.com/grpc/grpc> 를 사용하여 구현한 통신 모듈입니다.

## 개발 환경

```plain
Visual Studio Code
.NET 8.0
c# 12.0
```

## 빌드

```plain
git clone https://github.com/s2quake/communication.git
cd communication
dotnet build
```

## 실행

```plain
dotnet run --project JSSoft.Communication.Server --framework net8.0

dotnet run --project JSSoft.Communication.Client --framework net8.0
```

## 솔루션 구성

솔루션은 서버와 클라이언트 예제 그리고 통신을 담당하는 라이브러리, 총 3개의 프로젝트로 구성되어 있습니다.

## JSSoft.Communication

gRPC 을(를) 사용하여 서버와 통신을 할 수 있는 라이브러리 입니다.

서버와 클라이언트 간의 동일한 인터페이스(C#)를 사용하여 기능을 구현할 목적이기 때문에

gRPC 은(는)낮은 수준으로만 사용되었습니다.

따라서 gRPC 은(는) 내부에 감추어져 있으며 직접적으로 사용 하지는 못합니다.

gPRC 을(를) 사용하기 위한 프로토콜은 [adaptor.proto](JSSoft.Communication/Grpc/adaptor.proto) 에 정의되어 있습니다.

## 처음부터 실행까지

### 1. 도구 설치

아래의 링크로 이동하여 .NET 8.0과 Visual Studio Code를 설치합니다.

[.NET 8.0](https://dotnet.microsoft.com/download)

[Visual Studio Code](https://code.visualstudio.com/)

만약 설치가 이미 되어 있다면 이 과정을 건너 뛰셔도 됩니다.

### 2. 소스코드 받기

git 이 설치되어 있지 않다면 <https://git-scm.com/> 에서 git을 설치하시기 바랍니다.

macOS 또는 linux 운영체제에서는 **terminal**을

Windows 에서는 **PowerShell**을 실행합니다.

```plain
git clone https://github.com/s2quake/communication.git
```

### 3. 소스 경로로 이동

```plain
cd communication
```

## 4. 소스 빌드

```plain
dotnet build --framework net8.0
```

## 5. 서버 실행

```plain
dotnet run --project JSSoft.Communication.Server --framework net8.0
```

## 6. 클라이언트 실행

새로운 terminal이나 PowerShell을 실행하여 소스 경로로 이동하여 아래의 명령을 실행합니다.

```plain
dotnet run --project JSSoft.Communication.Client --framework net8.0
```

## 간단한 예제를 작성해보면서 무엇인지 알아보기

### 1. Visual Studio Code 폴더 열기

Visual Studio Code 를 실행후 폴더 열기로 소스 위치를 선택합니다.

### 2. 서버 프로젝트 만들기

상단의 보기 메뉴에서 터미널을 선택하여 터미널 창을 띄웁니다.

아래 명령을 실행하여 프로젝트를 생성하고 필요한 설정을 수행합니다.

```plain
mkdir Server-Test
dotnet new console -o Server-Test
dotnet sln add Server-Test
dotnet add Server-Test reference JSSoft.Communication
```

### 3. 나만의 서비스 정의하기

서버를 구축하기 전에 클라이언트에서 사용할 간단한 서비스를 제작해봅니다.

Server-Test 경로내에 `IMyService.cs` 파일을 만들고 아래와 같은 인터페이스를 정의합니다.

```csharp
using JSSoft.Communication;

namespace Services;

public interface IMyService
{
    [ServerMethod]
    string Login(string userID);

    [ServerMethod]
    Task<(string product, string version)> GetVersionAsync(CancellationToken cancellationToken);
}

```

아주 간단한 Login 메소드와 복잡해보이고 웬지 코딩 숙련도가 높아질것 같은 비동기 메소드 GetVersionAsync 를 정의하였습니다.

만약 인터페이스를 `internal` 으로 사용하고자 한다면 코드 상단에 다음 구문을 추가 해야 합니다.

```plain
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("JSSoft.Communication.Runtime")]
```

### 4. 나만의 서비스 구현하기

이제 인터페이스를 정의했으니 실제 작업을 구현해봅니다.

Server-Test 경로내에 `MyService.cs` 파일을 만들고 아래와 같이 작업을 구현합니다.

```csharp
using JSSoft.Communication;

namespace Services;

sealed class MyService : ServerService<IMyService>, IMyService
{
    public string Login(string userID)
    {
        Console.WriteLine($"logged in: '{userID}'");
        return $"{Guid.NewGuid()}";
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

## 5. 서버 실행하기

Server-Test 경로내에 Program.cs 내용을 다음과 같이 작성합니다.

```csharp
using JSSoft.Communication;
using Services;

var service = new MyService();
var serviceContext = new ServerContext([service]);
try
{
    var token = await serviceContext.OpenAsync(CancellationToken.None);

    Console.WriteLine("서버가 시작되었습니다.");
    Console.WriteLine("종료하려면 아무 키나 누르세요.");
    Console.ReadKey();

    await serviceContext.CloseAsync(token, CancellationToken.None);
}
catch
{
    await serviceContext.AbortAsync();
    Environment.Exit(1);
}

```

## 7. 클라이언트 프로젝트 만들기

서버 프로젝트를 생성할때와 마찬가지로 터미널 창을 열고 아래 명령을 실행합니다.

```plain
mkdir Client-Test
dotnet new console -o Client-Test
dotnet sln add Client-Test
dotnet add Client-Test reference JSSoft.Communication
```

## 8. 클라이언트 구현하기

Client-Test 경로내에 Program.cs 내용을 다음과 같이 작성합니다.

> Server-Test 에서 생성한 `IMyService.cs` 파일을 포함시킵니다.

```csharp
using JSSoft.Communication;
using Services;

var service = new ClientService<IMyService>();
var serviceContext = new ClientContext([service]);

try
{
    var token = await serviceContext.OpenAsync(CancellationToken.None);
    var server = service.Server;

    var id = server.Login("admin");
    var (product, version) = await server.GetVersionAsync(CancellationToken.None);
    Console.WriteLine($"logged in: {id}");
    Console.WriteLine($"product: {product}");
    Console.WriteLine($"version: {version}");

    Console.WriteLine("종료하려면 아무 키나 누르세요.");
    Console.ReadKey();

    await serviceContext.CloseAsync(token, CancellationToken.None);
}
catch
{
    await serviceContext.AbortAsync();
    Environment.Exit(1);
}

```

## 9. 빌드 및 실행하기

새로운 terminal이나 PowerShell 실행후 소스 경로에서 아래의 명령을 실행하여 빌드합니다.

```plain
dotnet build --framework net8.0
```

빌드가 완료된 후에 아래의 명령을 실행하여 서버를 실행합니다.

```plain
dotnet run --project Server-Test --framework net8.0
```

다시 새로운 terminal이나 PowerShell 실행후 소스 경로에서 아래의 명령을 실행하여 클라이언트를 실행합니다.

```plain
dotnet run --project Client-Test --framework net8.0
```

# Directives

Use this reference when the `.cs` file needs packages, SDK changes, MSBuild properties, project references, or executable script behavior.

Directives must appear at the top of the file before normal code.

## `#:package`

Use `#:package` to restore a NuGet package for a single-file app.

```csharp
#:package Humanizer@2.14.1

using Humanizer;

Console.WriteLine(TimeSpan.FromMinutes(90).Humanize());
```

## `#:sdk`

Use `#:sdk` when the file needs a different SDK, such as ASP.NET Core.

```csharp
#:sdk Microsoft.NET.Sdk.Web

var builder = WebApplication.CreateBuilder();
var app = builder.Build();
app.MapGet("/", () => "hello");
app.Run();
```

## `#:property`

Use `#:property` for project-style properties, for example language version.

```csharp
#:property LangVersion=preview

Console.WriteLine("preview enabled");
```

## `#:project`

Use `#:project` to reference an existing project from the file-based app.

```csharp
#:project ../MyLibrary/MyLibrary.csproj

using MyLibrary;
```

This is useful when a single-file app should call into local project code without becoming a full project itself.

## Shebang

On Unix-like systems, a file-based app can be made directly executable:

```csharp
#!/usr/bin/dotnet run
Console.WriteLine("hello");
```

Make it executable with `chmod +x app.cs`, then run `./app.cs`.

## Example With Multiple Directives

```csharp
#:sdk Microsoft.NET.Sdk
#:package Humanizer@2.14.1
#:property LangVersion=preview

using Humanizer;

var date = DateTimeOffset.Parse("2024-12-03");
Console.WriteLine($"{(DateTimeOffset.Now - date).Humanize()} since .NET 9.");
```

## Teaching Notes

- Explain that directives are part of the file, not separate CLI flags.
- Keep examples runnable as-is.
- If another agent is unlikely to know the feature, be explicit that these directives are understood by the .NET CLI for file-based apps.

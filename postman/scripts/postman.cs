#!/usr/bin/env dotnet
#:package Newtonsoft.Json@13.0.3

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

return await new PostmanTool().RunAsync(args);

sealed class PostmanTool
{
    private const string ApiBase = "https://api.getpostman.com";
    private const string SchemaUrl = "https://schema.getpostman.com/json/collection/v2.1.0/collection.json";
    private static readonly string TempDir = Path.Combine(Path.GetTempPath(), "postman-skill");
    private static readonly HashSet<string> HttpMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS"
    };

    public async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0) return Usage("No command supplied.");

        try
        {
            var cmd = args[0].ToLowerInvariant();
            var rest = args.Skip(1).ToArray();
            return cmd switch
            {
                "add" or "add-request" => await CmdAddRequest(rest),
                "add-folder" => await CmdAddFolder(rest),
                "create" => await CmdCreate(rest),
                "delete" => await CmdDelete(rest),
                "list" => await CmdList(rest),
                "list-remote" or "collections" => await CmdListRemote(rest),
                "pull" or "export" => await CmdPull(rest),
                "remove" => await CmdRemove(rest),
                "run" => await CmdRun(rest),
                "self-test" => CmdSelfTest(),
                "validate" => await CmdValidate(rest),
                _ => Usage($"Unknown command: {cmd}")
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    async Task<int> CmdCreate(string[] args)
    {
        var name = Opt(args, "--name", "-n");
        var workspace = Opt(args, "--workspace", "-w");
        if (string.IsNullOrWhiteSpace(name)) return Usage("create: --name is required.");

        using var api = NewApiClient();
        var collection = NewCollection(name);
        var endpoint = "/collections" + (string.IsNullOrWhiteSpace(workspace) ? "" : $"?workspace={Uri.EscapeDataString(workspace)}");
        var response = await api.SendJson(HttpMethod.Post, endpoint, Wrap(collection));
        var result = response["collection"] as JObject;
        Console.WriteLine($"Created remote collection: {result?["uid"] ?? result?["id"]}");
        return 0;
    }

    async Task<int> CmdAddRequest(string[] args)
    {
        var collectionId = Required(args, "--collection", "-c", "add-request");
        var name = Required(args, "--name", "-n", "add-request");
        var url = Required(args, "--url", "-u", "add-request");
        if (collectionId == null || name == null || url == null) return 1;

        var method = (Opt(args, "--method", "-m") ?? "GET").ToUpperInvariant();
        if (!HttpMethods.Contains(method))
        {
            Console.Error.WriteLine($"Invalid method: {method}. Use: {string.Join(", ", HttpMethods.OrderBy(x => x))}");
            return 1;
        }

        var folder = Opt(args, "--folder", "-f");
        var body = Opt(args, "--body", "-b");
        var headers = Opt(args, "--header", "-h");
        var description = Opt(args, "--description", "-d");
        var useVariable = Opt(args, "--use-variable", null);

        using var api = NewApiClient();
        var collection = await api.GetCollection(collectionId);
        var finalUrl = ApplyVariable(url, collection["variable"] as JArray, useVariable);
        var request = new JObject(
            new JProperty("name", name),
            new JProperty("request", BuildRequest(method, finalUrl, body, headers, description)),
            new JProperty("response", new JArray())
        );

        var items = string.IsNullOrWhiteSpace(folder)
            ? EnsureItems(collection)
            : FindOrCreateFolder(EnsureItems(collection), folder);

        items.Add(request);
        await api.UpdateCollection(collectionId, collection);
        Console.WriteLine($"Added request '{name}' to remote collection: {collectionId}");
        return 0;
    }

    async Task<int> CmdAddFolder(string[] args)
    {
        var collectionId = Required(args, "--collection", "-c", "add-folder");
        var name = Required(args, "--name", "-n", "add-folder");
        if (collectionId == null || name == null) return 1;

        using var api = NewApiClient();
        var collection = await api.GetCollection(collectionId);
        FindOrCreateFolder(EnsureItems(collection), name);
        await api.UpdateCollection(collectionId, collection);
        Console.WriteLine($"Added folder '{name}' to remote collection: {collectionId}");
        return 0;
    }

    async Task<int> CmdRemove(string[] args)
    {
        var collectionId = Required(args, "--collection", "-c", "remove");
        var name = Required(args, "--name", "-n", "remove");
        if (collectionId == null || name == null) return 1;

        using var api = NewApiClient();
        var collection = await api.GetCollection(collectionId);
        if (!RemoveItem(collection["item"] as JArray, name))
        {
            Console.Error.WriteLine($"Item not found: {name}");
            return 1;
        }

        await api.UpdateCollection(collectionId, collection);
        Console.WriteLine($"Removed '{name}' from remote collection: {collectionId}");
        return 0;
    }

    async Task<int> CmdList(string[] args)
    {
        var collectionId = Required(args, "--collection", "-c", "list");
        if (collectionId == null) return 1;

        using var api = NewApiClient();
        var collection = await api.GetCollection(collectionId);
        PrintCollection(collection, remote: true);
        return 0;
    }

    async Task<int> CmdListRemote(string[] args)
    {
        using var api = NewApiClient();
        var json = await api.SendJson(HttpMethod.Get, "/collections", null);
        Console.WriteLine("Remote collections:");
        foreach (var col in json["collections"] as JArray ?? new JArray())
        {
            Console.WriteLine($"  {col["uid"] ?? col["id"]} - {col["name"]}");
        }
        return 0;
    }

    async Task<int> CmdPull(string[] args)
    {
        var collectionId = CollectionRef(args, "pull");
        var output = Required(args, "--output", "-o", "pull");
        if (collectionId == null || output == null) return 1;

        using var api = NewApiClient();
        var collection = await api.GetCollection(collectionId);
        WriteJsonFile(output, HasFlag(args, "--raw") ? collection : Wrap(collection));
        Console.WriteLine($"Exported remote collection to: {output}");
        return 0;
    }

    async Task<int> CmdValidate(string[] args)
    {
        var collectionId = CollectionRef(args, "validate");
        if (collectionId == null) return 1;

        using var api = NewApiClient();
        var collection = await api.GetCollection(collectionId);
        return ValidateCollection(collection);
    }

    async Task<int> CmdDelete(string[] args)
    {
        var collectionId = CollectionRef(args, "delete");
        if (collectionId == null) return 1;

        using var api = NewApiClient();
        await api.SendJson(HttpMethod.Delete, $"/collections/{Uri.EscapeDataString(collectionId)}", null);
        Console.WriteLine($"Deleted remote collection: {collectionId}");
        return 0;
    }

    async Task<int> CmdRun(string[] args)
    {
        var collectionId = Required(args, "--collection", "-c", "run");
        if (collectionId == null) return 1;

        using var api = NewApiClient();
        var collection = await api.GetCollection(collectionId);
        Directory.CreateDirectory(TempDir);
        var tempFile = Path.Combine(TempDir, $"{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, collection.ToString(Formatting.Indented), Encoding.UTF8);

        var newmanArgs = new List<string> { "run", tempFile };
        AddNewmanOption(args, newmanArgs, "--environment", "-e");
        AddNewmanOption(args, newmanArgs, "--iteration-data", "-d");
        AddNewmanOption(args, newmanArgs, "--reporters", "-r");
        AddNewmanOption(args, newmanArgs, "--folder", "-f");
        AddNewmanOption(args, newmanArgs, "--export-environment", null);
        AddNewmanOption(args, newmanArgs, "--export-collection", null);
        AddNewmanOption(args, newmanArgs, "--iteration-count", null);
        AddNewmanOption(args, newmanArgs, "--timeout", null);
        AddNewmanOption(args, newmanArgs, "--delay-request", null);

        Console.WriteLine($"Running remote collection with newman: {collectionId}");
        return await RunProcess("newman", newmanArgs);
    }

    int CmdSelfTest()
    {
        var variableUrl = BuildUrl("{{baseUrl}}/api/notes");
        Assert(variableUrl["raw"]?.ToString() == "{{baseUrl}}/api/notes", "variable URL raw is preserved");
        Assert(variableUrl["protocol"] == null, "variable URL has no protocol");
        Assert((variableUrl["host"] as JArray)?[0]?.ToString() == "{{baseUrl}}", "variable URL host keeps braces");
        Assert((variableUrl["path"] as JArray)?.Count == 2, "variable URL path parsed");

        var parsedUrl = BuildUrl("https://api.example.com/users?page=1");
        Assert(parsedUrl["protocol"]?.ToString() == "https", "absolute URL protocol parsed");
        Assert((parsedUrl["host"] as JArray)?.Count == 3, "absolute URL host parsed");
        Assert((parsedUrl["query"] as JArray)?.Count == 1, "absolute URL query parsed");

        var collection = NewCollection("Self Test");
        var folderItems = FindOrCreateFolder(EnsureItems(collection), "Notes");
        folderItems.Add(new JObject(new JProperty("name", "List Notes")));
        Assert(((JArray)collection["item"]!).Count == 1, "folder attached to collection");

        Console.WriteLine("Self-test: PASSED");
        return 0;
    }

    static ApiClient NewApiClient()
    {
        var key = GetApiKey();
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("POSTMAN_API_KEY is not set. Remote Postman API operations require an API key.");
        }
        return new ApiClient(key);
    }

    static JObject NewCollection(string name) => new(
        new JProperty("info", new JObject(
            new JProperty("name", name),
            new JProperty("schema", SchemaUrl)
        )),
        new JProperty("item", new JArray()),
        new JProperty("variable", new JArray())
    );

    static JObject Wrap(JObject collection) => new(new JProperty("collection", collection));

    static void WriteJsonFile(string path, JObject json)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(path, json.ToString(Formatting.Indented), Encoding.UTF8);
    }

    static int ValidateCollection(JObject collection)
    {
        var info = collection["info"] as JObject;
        if (info == null) return FailValidation("Missing required field: info");
        if (string.IsNullOrWhiteSpace(info["name"]?.ToString())) return FailValidation("Missing required field: info.name");
        if (collection["item"] is not JArray items) return FailValidation("Missing required field: item");

        Console.WriteLine($"Collection: {info["name"]}");
        Console.WriteLine($"Requests: {CountRequests(items)}");
        Console.WriteLine($"Folders: {CountFolders(items)}");
        Console.WriteLine("Validation: PASSED");
        return 0;
    }

    static int FailValidation(string message)
    {
        Console.Error.WriteLine($"Validation failed: {message}");
        return 1;
    }

    static JObject BuildRequest(string method, string url, string? body, string? headers, string? description)
    {
        var request = new JObject(
            new JProperty("method", method),
            new JProperty("header", ParseHeaders(headers)),
            new JProperty("url", BuildUrl(url))
        );

        if (!string.IsNullOrWhiteSpace(description))
        {
            request["description"] = description;
        }

        if (!string.IsNullOrWhiteSpace(body))
        {
            request["body"] = new JObject(
                new JProperty("mode", "raw"),
                new JProperty("raw", body)
            );
        }

        return request;
    }

    static JObject BuildUrl(string url)
    {
        var result = new JObject(new JProperty("raw", url));

        if (TryBuildVariableUrl(url, out var variableUrl))
        {
            return variableUrl;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return result;
        }

        result["protocol"] = uri.Scheme;
        result["host"] = new JArray(uri.Host.Split('.', StringSplitOptions.RemoveEmptyEntries));

        var path = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (path.Length > 0) result["path"] = new JArray(path);

        if (!string.IsNullOrEmpty(uri.Query))
        {
            result["query"] = new JArray(uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries).Select(pair =>
            {
                var parts = pair.Split('=', 2);
                return new JObject(
                    new JProperty("key", WebUtility.UrlDecode(parts[0])),
                    new JProperty("value", parts.Length > 1 ? WebUtility.UrlDecode(parts[1]) : "")
                );
            }));
        }

        return result;
    }

    static bool TryBuildVariableUrl(string url, out JObject result)
    {
        result = new JObject(new JProperty("raw", url));

        if (!url.StartsWith("{{", StringComparison.Ordinal))
        {
            return false;
        }

        var end = url.IndexOf("}}", StringComparison.Ordinal);
        if (end < 0)
        {
            return false;
        }

        var variable = url[..(end + 2)];
        result["host"] = new JArray(variable);

        var remainder = url[(end + 2)..];
        var queryIndex = remainder.IndexOf('?', StringComparison.Ordinal);
        var pathPart = queryIndex >= 0 ? remainder[..queryIndex] : remainder;
        var queryPart = queryIndex >= 0 ? remainder[(queryIndex + 1)..] : "";

        var path = pathPart.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (path.Length > 0) result["path"] = new JArray(path);

        if (!string.IsNullOrWhiteSpace(queryPart))
        {
            result["query"] = new JArray(queryPart.Split('&', StringSplitOptions.RemoveEmptyEntries).Select(pair =>
            {
                var parts = pair.Split('=', 2);
                return new JObject(
                    new JProperty("key", WebUtility.UrlDecode(parts[0])),
                    new JProperty("value", parts.Length > 1 ? WebUtility.UrlDecode(parts[1]) : "")
                );
            }));
        }

        return true;
    }

    static JArray ParseHeaders(string? headers)
    {
        var result = new JArray();
        if (string.IsNullOrWhiteSpace(headers)) return result;

        foreach (var header in headers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = header.Split(':', 2);
            if (parts.Length != 2) continue;
            result.Add(new JObject(
                new JProperty("key", parts[0].Trim()),
                new JProperty("value", parts[1].Trim()),
                new JProperty("type", "text")
            ));
        }

        return result;
    }

    static string ApplyVariable(string url, JArray? variables, string? useVariable)
    {
        if (variables == null || url.Contains("{{")) return url;

        foreach (var variable in variables.OfType<JObject>())
        {
            var key = variable["key"]?.ToString();
            var value = variable["value"]?.ToString();
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value)) continue;
            if (!string.IsNullOrWhiteSpace(useVariable) && key != useVariable) continue;
            if (url.StartsWith(value, StringComparison.Ordinal))
            {
                return $"{{{{{key}}}}}{url[value.Length..]}";
            }
        }

        return url;
    }

    static JArray EnsureItems(JObject collection)
    {
        if (collection["item"] is JArray items) return items;
        items = new JArray();
        collection["item"] = items;
        return items;
    }

    static JArray FindOrCreateFolder(JArray items, string folderName)
    {
        foreach (var item in items.OfType<JObject>())
        {
            if (item["request"] == null && item["item"] is JArray subItems && item["name"]?.ToString() == folderName)
            {
                return subItems;
            }
        }

        var folder = new JObject(
            new JProperty("name", folderName),
            new JProperty("item", new JArray())
        );
        items.Add(folder);
        return (JArray)folder["item"]!;
    }

    static bool RemoveItem(JArray? items, string name)
    {
        if (items == null) return false;

        for (var i = items.Count - 1; i >= 0; i--)
        {
            if (items[i] is not JObject item) continue;
            if (item["name"]?.ToString() == name)
            {
                items.RemoveAt(i);
                return true;
            }
            if (RemoveItem(item["item"] as JArray, name)) return true;
        }

        return false;
    }

    static void PrintCollection(JObject collection, bool remote)
    {
        Console.WriteLine($"Collection: {collection["info"]?["name"] ?? "Unnamed"}{(remote ? " (remote)" : "")}");
        if (collection["variable"] is JArray variables && variables.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Variables:");
            foreach (var variable in variables.OfType<JObject>())
            {
                Console.WriteLine($"  {variable["key"]} = {variable["value"]}");
            }
        }
        Console.WriteLine();
        PrintItems(collection["item"] as JArray, "");
    }

    static void PrintItems(JArray? items, string prefix)
    {
        if (items == null) return;
        foreach (var item in items.OfType<JObject>())
        {
            var name = item["name"]?.ToString() ?? "(unnamed)";
            if (item["request"] is JObject request)
            {
                Console.WriteLine($"{prefix}[{request["method"] ?? "?"}] {name}");
                Console.WriteLine($"{prefix}  URL: {request["url"]?["raw"]}");
            }
            else
            {
                Console.WriteLine($"{prefix}[Folder] {name}");
                PrintItems(item["item"] as JArray, prefix + "  ");
            }
        }
    }

    static int CountRequests(JArray items) => items.OfType<JObject>().Sum(item =>
        item["request"] != null ? 1 : item["item"] is JArray subItems ? CountRequests(subItems) : 0);

    static int CountFolders(JArray items) => items.OfType<JObject>().Sum(item =>
        item["request"] == null && item["item"] is JArray subItems ? 1 + CountFolders(subItems) : 0);

    static async Task<int> RunProcess(string fileName, IReadOnlyList<string> args)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var arg in args) psi.ArgumentList.Add(arg);

        using var process = System.Diagnostics.Process.Start(psi);
        if (process == null)
        {
            Console.Error.WriteLine($"Failed to start process: {fileName}");
            return 1;
        }

        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        Console.Write(await stdout);
        Console.Error.Write(await stderr);
        return process.ExitCode;
    }

    static void AddNewmanOption(string[] args, List<string> target, string longName, string? shortName)
    {
        var value = Opt(args, longName, shortName);
        if (value == null) return;
        target.Add(longName);
        target.Add(value);
    }

    static bool HasFlag(string[] args, string name) => args.Any(arg => arg == name);

    static string? Opt(string[] args, string key, string? shortKey)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == key || (!string.IsNullOrWhiteSpace(shortKey) && args[i] == shortKey))
            {
                return i + 1 < args.Length ? args[i + 1] : null;
            }
        }

        foreach (var arg in args)
        {
            if (arg.StartsWith(key + "=", StringComparison.Ordinal)) return arg[(key.Length + 1)..];
            if (!string.IsNullOrWhiteSpace(shortKey) && arg.StartsWith(shortKey + "=", StringComparison.Ordinal))
            {
                return arg[(shortKey.Length + 1)..];
            }
        }

        return null;
    }

    static string? Required(string[] args, string key, string shortKey, string command)
    {
        var value = Opt(args, key, shortKey);
        if (!string.IsNullOrWhiteSpace(value)) return value;
        Console.Error.WriteLine($"{command}: {key} is required.");
        return null;
    }

    static string? CollectionRef(string[] args, string command)
    {
        var value = Opt(args, "--collection", "-c") ?? Opt(args, "--id", "-i");
        if (!string.IsNullOrWhiteSpace(value)) return value;
        Console.Error.WriteLine($"{command}: --collection is required.");
        return null;
    }

    static string GetApiKey()
    {
        var processKey = Environment.GetEnvironmentVariable("POSTMAN_API_KEY");
        if (!string.IsNullOrWhiteSpace(processKey)) return processKey;
        var userKey = Environment.GetEnvironmentVariable("POSTMAN_API_KEY", EnvironmentVariableTarget.User);
        if (!string.IsNullOrWhiteSpace(userKey)) return userKey;
        var machineKey = Environment.GetEnvironmentVariable("POSTMAN_API_KEY", EnvironmentVariableTarget.Machine);
        return machineKey ?? "";
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException($"Self-test failed: {message}");
    }

    static int Usage(string message)
    {
        Console.Error.WriteLine(message);
        Console.Error.WriteLine();
        Console.Error.WriteLine("Usage: postman <command> [options]");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Commands:");
        Console.Error.WriteLine("  create          Create a remote collection");
        Console.Error.WriteLine("  add-request     Add a request to a remote collection");
        Console.Error.WriteLine("  add-folder      Add a folder to a remote collection");
        Console.Error.WriteLine("  list            List a remote collection");
        Console.Error.WriteLine("  list-remote     List remote collections");
        Console.Error.WriteLine("  pull            Export a remote collection to a local file");
        Console.Error.WriteLine("  remove          Remove a request or folder from a remote collection");
        Console.Error.WriteLine("  delete          Delete a remote collection");
        Console.Error.WriteLine("  run             Run a remote collection through newman");
        Console.Error.WriteLine("  validate        Validate a remote collection");
        return 1;
    }

    sealed class ApiClient : IDisposable
    {
        private readonly HttpClient _client = new();

        public ApiClient(string apiKey)
        {
            _client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        }

        public async Task<JObject> GetCollection(string collectionId)
        {
            var json = await SendJson(HttpMethod.Get, $"/collections/{Uri.EscapeDataString(collectionId)}", null);
            return json["collection"] as JObject ?? throw new InvalidOperationException("Postman API response did not include a collection object.");
        }

        public async Task UpdateCollection(string collectionId, JObject collection)
        {
            await SendJson(HttpMethod.Put, $"/collections/{Uri.EscapeDataString(collectionId)}", Wrap(collection));
        }

        public async Task<JObject> SendJson(HttpMethod method, string path, JObject? body)
        {
            using var request = new HttpRequestMessage(method, ApiBase + path);
            if (body != null)
            {
                request.Content = new StringContent(body.ToString(Formatting.None), Encoding.UTF8, "application/json");
            }

            using var response = await _client.SendAsync(request);
            var text = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Postman API returned {(int)response.StatusCode} {response.ReasonPhrase}: {text}");
            }

            if (string.IsNullOrWhiteSpace(text)) return new JObject();
            return JObject.Parse(text);
        }

        public void Dispose() => _client.Dispose();
    }
}

#!/usr/bin/env dotnet

#pragma warning disable IL2026, IL3050, SYSLIB0013

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

var exitCode = await new SiyuanTool().RunAsync(args);
return exitCode;

[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(bool))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
partial class SiyuanJsonContext : JsonSerializerContext { }

class SiyuanTool
{
    string BaseUrl { get; } = Environment.GetEnvironmentVariable("SIYUAN_URL") ?? "http://127.0.0.1:6806";
    string Token { get; } = Environment.GetEnvironmentVariable("SIYUAN_TOKEN") ?? "";
    HttpClient Http { get; } = new() { BaseAddress = null! };
    Dictionary<string, CommandSpec> Commands { get; }

    public SiyuanTool()
    {
        Commands = new Dictionary<string, CommandSpec>
        {
            ["ls"] = new("List all notebooks", _ => CmdLs()),
            ["tree"] = new("Document tree with titles (args: notebook [path])", CmdTree),
            ["docs"] = new("Flat doc listing (args: notebook [path])", CmdDocs),
            ["cat"] = new("Export doc as markdown (args: id)", CmdCat),
            ["info"] = new("Get block metadata (args: id)", CmdInfo),
            ["rm"] = new("Remove docs by ID (args: id [id ...])", CmdRm),
            ["rm-path"] = new("Remove doc by path (args: notebook path)", CmdRmPath),
            ["mv"] = new("Move docs (args: toID fromID [fromID ...])", CmdMv),
            ["sql"] = new("Execute SQL query (args: statement)", CmdSql),
            ["search"] = new("Full-text search (args: query [page])", CmdSearch),
            ["search-docs"] = new("Search document titles (args: query)", CmdSearchDocs),
            ["path"] = new("Get HPath by ID (args: id)", CmdPath),
            ["full-path"] = new("Get full HPath including parent docs (args: id)", CmdFullPath),
            ["breadcrumb"] = new("Get block breadcrumb (args: id)", CmdBreadcrumb),
            ["outline"] = new("Get doc outline (args: id)", CmdOutline),
            ["create"] = new("Create doc from stdin markdown (args: notebook path)", CmdCreate),
            ["update-md"] = new("Update doc from stdin markdown (args: id)", CmdUpdateMd),
            ["rename"] = new("Rename doc by ID (args: id title)", CmdRename),
            ["tags"] = new("List/search tags (args: [keyword])", CmdTags),
            ["raw"] = new("Call any API endpoint (args: endpoint [json])", CmdRaw),
            ["duplicate"] = new("Duplicate a document (args: id)", CmdDuplicate),
            ["set-attrs-batch"] = new("Batch set block attributes (args: key=value ... --where sql_where)", CmdSetAttrsBatch),
            ["open-nb"] = new("Open/mount notebook (args: notebook)", CmdOpenNb),
            ["close-nb"] = new("Close/unmount notebook (args: notebook)", CmdCloseNb),
            ["remove-nb"] = new("Remove notebook (args: notebook)", CmdRemoveNb),
            ["create-nb"] = new("Create notebook (args: name)", CmdCreateNb),
            ["rename-nb"] = new("Rename notebook (args: notebook name)", CmdRenameNb),
            ["get-nb-conf"] = new("Get notebook config (args: notebook)", CmdGetNbConf),
            ["set-nb-conf"] = new("Set notebook config (args: notebook json_conf)", CmdSetNbConf),
            ["get-doc"] = new("Get doc content JSON (args: id)", CmdGetDoc),
            ["insert-block"] = new("Insert block (args: parentID dataType data [--previous ID] [--next ID])", CmdInsertBlock),
            ["move-block"] = new("Move block (args: id [--parent ID] [--previous ID])", CmdMoveBlock),
            ["export"] = new("Export doc in various formats (args: id format)", CmdExport),
            ["attrs"] = new("Get block attributes (args: id)", CmdAttrs),
            ["set-attrs"] = new("Set block attributes (args: id key=value ...)", CmdSetAttrs),
            ["recent"] = new("Recently updated blocks (no args)", CmdRecent),
            ["history"] = new("Get doc history (args: notebook path)", CmdHistory),
            ["templates"] = new("Search templates (args: [keyword])", CmdTemplates),
            ["backlinks"] = new("Get backlinks for a block (args: id)", CmdBacklinks),
        };
    }

    public async Task<int> RunAsync(string[] args)
    {
        Http.BaseAddress = new Uri(BaseUrl);
        Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", Token);

        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            PrintHelp(args.Length > 1 ? args[1] : null);
            return 0;
        }

        var command = args[0];
        var cmdArgs = args.Skip(1).ToList();

        try
        {
            if (!Commands.TryGetValue(command, out var spec))
            {
                return UnknownCommand(command);
            }

            return await spec.Handler(cmdArgs);
        }
        catch (HttpRequestException ex)
        {
            Console.Error.WriteLine($"HTTP error: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    delegate Task<int> CommandHandler(List<string> args);
    sealed record CommandSpec(string Description, CommandHandler Handler);

    int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"Unknown command: {command}");
        return 1;
    }

    bool RequireArgs(List<string> args, int count, string usage)
    {
        if (args.Count >= count)
        {
            return true;
        }

        Console.Error.WriteLine($"Usage: {usage}");
        return false;
    }

    void PrintJson(JsonElement value) =>
        Console.WriteLine(JsonSerializer.Serialize(value, SiyuanJsonContext.Default.Options));

    string SerializeBody(object? body)
    {
        if (body is null) return "{}";
        if (body is JsonElement je) return je.GetRawText();
        if (body is string s) return s;
        if (body is System.Collections.IDictionary dict)
        {
            var pairs = new List<string>();
            foreach (System.Collections.DictionaryEntry e in dict)
                pairs.Add($"\"{e.Key}\":{SerializeValue(e.Value)}");
            return "{" + string.Join(",", pairs) + "}";
        }
        var type = body.GetType();
        var props = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var values = new List<string>();
        foreach (var prop in props)
            values.Add($"\"{prop.Name}\":{SerializeValue(prop.GetValue(body))}");
        return "{" + string.Join(",", values) + "}";
    }

    string SerializeValue(object? value)
    {
        if (value is null) return "null";
        if (value is string s) return $"\"{s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t")}\"";
        if (value is JsonElement je) return je.GetRawText();
        if (value is int or long or bool or double) return value.ToString()?.ToLower() ?? "null";
        if (value is System.Collections.IDictionary nestedDict)
        {
            var pairs = new List<string>();
            foreach (System.Collections.DictionaryEntry e in nestedDict)
                pairs.Add($"\"{e.Key}\":{SerializeValue(e.Value)}");
            return "{" + string.Join(",", pairs) + "}";
        }
        if (value is System.Collections.IEnumerable arr && value.GetType() != typeof(string))
        {
            var items = new List<string>();
            foreach (var item in arr)
                items.Add(SerializeValue(item));
            return "[" + string.Join(",", items) + "]";
        }
        return $"\"{value}\"";
    }

    async Task<JsonElement> Api(string endpoint, object? body = null)
    {
        var json = SerializeBody(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var resp = await Http.PostAsync($"/api{endpoint}", content);
        var responseBody = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize(responseBody, SiyuanJsonContext.Default.JsonElement);
        var code = result.GetProperty("code").GetInt32();
        if (code != 0)
        {
            var msg = result.TryGetProperty("msg", out var m) ? m.GetString() : "unknown";
            Console.Error.WriteLine($"API error [{code}]: {msg}");
            Environment.Exit(1);
        }
        return result.GetProperty("data");
    }

    string GetNotebookId(string nameOrId)
    {
        var data = Api("/notebook/lsNotebooks").Result;
        foreach (var nb in data.GetProperty("notebooks").EnumerateArray())
        {
            if (nb.GetProperty("id").GetString() == nameOrId ||
                nb.GetProperty("name").GetString() == nameOrId)
                return nb.GetProperty("id").GetString()!;
        }
        Console.Error.WriteLine($"Notebook not found: {nameOrId}");
        Environment.Exit(1);
        return "";
    }

    string SanitizePath(string path)
    {
        if (path.Length >= 2 && path[1] == ':')
        {
            var gitIndex = path.IndexOf("/Git/", StringComparison.OrdinalIgnoreCase);
            if (gitIndex != -1)
                path = path[(gitIndex + 4)..];
            else
            {
                var parts = path.Replace('\\', '/').Split('/');
                parts = parts.Where(p =>
                    p.Length != 2 || p[1] != ':' && !p.Equals("Program Files", StringComparison.OrdinalIgnoreCase) && !p.Equals("Git", StringComparison.OrdinalIgnoreCase)).ToArray();
                if (parts.Length > 0)
                    path = "/" + parts[^1];
            }
        }
        path = path.TrimStart('.');
        if (!path.StartsWith('/'))
            path = "/" + path;
        return path;
    }

    async Task<int> CmdLs()
    {
        var data = await Api("/notebook/lsNotebooks");
        foreach (var nb in data.GetProperty("notebooks").EnumerateArray())
            Console.WriteLine($"{nb.GetProperty("name").GetString(),-12} {nb.GetProperty("id").GetString()}  closed={nb.GetProperty("closed").GetBoolean()}  sort={nb.GetProperty("sort").GetInt32()}");
        return 0;
    }

    async Task<int> CmdTree(List<string> args)
    {
        if (!RequireArgs(args, 1, "tree <notebook> [path]")) return 1;
        var box = GetNotebookId(args[0]);
        var path = args.Count > 1 ? SanitizePath(args[1]) : "/";
        var treeData = await Api("/filetree/listDocTree", new { notebook = box, path });
        var sqlData = await Api("/query/sql", new { stmt = $"SELECT id, content FROM blocks WHERE type='d' AND box='{box}' LIMIT 9999" });
        var titles = new Dictionary<string, string>();
        foreach (var row in sqlData.EnumerateArray())
            titles[row.GetProperty("id").GetString()!] = row.GetProperty("content").GetString() ?? "???";

        void PrintNode(JsonElement node, int depth = 0)
        {
            var nid = node.GetProperty("id").GetString()!;
            var title = titles.GetValueOrDefault(nid, "???");
            var prefix = depth == 0 ? "" : new string(' ', (depth - 1) * 2) + "└─ ";
            Console.WriteLine($"{prefix}{title}  ({nid})");
            if (node.TryGetProperty("children", out var children))
                foreach (var child in children.EnumerateArray())
                    PrintNode(child, depth + 1);
        }

        var nodes = treeData.TryGetProperty("tree", out var t) ? t : treeData;
        if (nodes.ValueKind == JsonValueKind.Array)
            foreach (var node in nodes.EnumerateArray())
                PrintNode(node);
        return 0;
    }

    async Task<int> CmdDocs(List<string> args)
    {
        if (!RequireArgs(args, 1, "docs <notebook> [path]")) return 1;
        var box = GetNotebookId(args[0]);
        var path = args.Count > 1 ? SanitizePath(args[1]) : "/";
        var data = await Api("/filetree/listDocsByPath", new { notebook = box, path, sort = 0, maxListCount = 999999 });
        foreach (var f in data.GetProperty("files").EnumerateArray())
            Console.WriteLine($"{f.GetProperty("path").GetString(),-50} {f.GetProperty("name").GetString()}");
        return 0;
    }

    async Task<int> CmdCat(List<string> args)
    {
        if (!RequireArgs(args, 1, "cat <id>")) return 1;
        var data = await Api("/export/exportMdContent", new { id = args[0] });
        Console.WriteLine($"# {data.GetProperty("hPath").GetString()}\n");
        Console.WriteLine(data.GetProperty("content").GetString());
        return 0;
    }

    async Task<int> CmdInfo(List<string> args)
    {
        if (!RequireArgs(args, 1, "info <id>")) return 1;
        var data = await Api("/block/getBlockInfo", new { id = args[0] });
        PrintJson(data);
        return 0;
    }

    async Task<int> CmdRm(List<string> args)
    {
        if (!RequireArgs(args, 1, "rm <id> [id ...]")) return 1;
        foreach (var id in args)
        {
            await Api("/filetree/removeDocByID", new { id });
            Console.WriteLine($"Removed: {id}");
        }
        return 0;
    }

    async Task<int> CmdRmPath(List<string> args)
    {
        if (!RequireArgs(args, 2, "rm-path <notebook> <path>")) return 1;
        var box = GetNotebookId(args[0]);
        await Api("/filetree/removeDoc", new { notebook = box, path = args[1] });
        Console.WriteLine($"Removed: {args[0]}:{args[1]}");
        return 0;
    }

    async Task<int> CmdMv(List<string> args)
    {
        if (!RequireArgs(args, 2, "mv <toID> <fromID...>")) return 1;
        var toId = args[0];
        var fromIds = args.Skip(1).ToList();
        await Api("/filetree/moveDocsByID", new { fromIDs = fromIds, toID = toId });
        Console.WriteLine($"Moved {fromIds.Count} doc(s) to {toId}");
        return 0;
    }

    async Task<int> CmdSql(List<string> args)
    {
        if (!RequireArgs(args, 1, "sql <statement>")) return 1;
        var stmt = string.Join(" ", args);
        var data = await Api("/query/sql", new { stmt });
        if (data.ValueKind == JsonValueKind.Array)
            foreach (var row in data.EnumerateArray())
            {
                var values = new List<string>();
                foreach (var prop in row.EnumerateObject())
                    values.Add(prop.Value.ToString());
                Console.WriteLine(string.Join("\t", values));
            }
        else
            PrintJson(data);
        return 0;
    }

    async Task<int> CmdSearch(List<string> args)
    {
        if (!RequireArgs(args, 1, "search <query> [page]")) return 1;
        var page = args.Count > 1 ? int.Parse(args[1]) : 1;
        var data = await Api("/search/fullTextSearchBlock", new { query = args[0], method = 0, page, pageSize = 32, orderBy = 0, groupBy = 1 });
        Console.WriteLine($"Total matches: {data.GetProperty("matchedBlockCount").GetInt32()}");
        foreach (var blk in data.GetProperty("blocks").EnumerateArray())
        {
            var hPath = blk.TryGetProperty("hPath", out var hp) ? hp.GetString() : "";
            var type = blk.TryGetProperty("type", out var tp) ? tp.GetString() : "";
            var content = blk.TryGetProperty("content", out var ct) ? ct.GetString() : "";
            var display = (content ?? "").Length > 120 ? content![..120] : content;
            Console.WriteLine($"{hPath}  [{type}]  {display}");
        }
        return 0;
    }

    async Task<int> CmdSearchDocs(List<string> args)
    {
        if (!RequireArgs(args, 1, "search-docs <query>")) return 1;
        var data = await Api("/filetree/searchDocs", new { k = args[0] });
        foreach (var doc in data.EnumerateArray())
        {
            var hPath = doc.TryGetProperty("hPath", out var hp) ? hp.GetString() : "";
            var path = doc.TryGetProperty("path", out var p) ? p.GetString()?.TrimStart('/').Replace(".sy", "") : "";
            Console.WriteLine($"{hPath,-50} {path}");
        }
        return 0;
    }

    async Task<int> CmdPath(List<string> args)
    {
        if (!RequireArgs(args, 1, "path <id>")) return 1;
        var data = await Api("/filetree/getHPathByID", new { id = args[0] });
        Console.WriteLine(data.ToString());
        return 0;
    }

    async Task<int> CmdFullPath(List<string> args)
    {
        if (!RequireArgs(args, 1, "full-path <id>")) return 1;
        var data = await Api("/filetree/getFullHPathByID", new { id = args[0] });
        Console.WriteLine(data.ToString());
        return 0;
    }

    async Task<int> CmdBreadcrumb(List<string> args)
    {
        if (!RequireArgs(args, 1, "breadcrumb <id>")) return 1;
        var data = await Api("/block/getBlockBreadcrumb", new { id = args[0] });
        PrintJson(data);
        return 0;
    }

    async Task<int> CmdOutline(List<string> args)
    {
        if (!RequireArgs(args, 1, "outline <id>")) return 1;
        var data = await Api("/outline/getDocOutline", new { id = args[0] });
        if (data.ValueKind == JsonValueKind.Null) { Console.WriteLine("(no outline)"); return 0; }
        foreach (var h in data.EnumerateArray())
        {
            var depth = h.TryGetProperty("depth", out var d) ? d.GetInt32() : 0;
            var content = h.TryGetProperty("content", out var c) ? c.GetString() : "";
            var id = h.TryGetProperty("id", out var i) ? i.GetString() : "";
            Console.WriteLine($"{new string(' ', depth * 2)}{content}  ({id})");
        }
        return 0;
    }

    async Task<int> CmdCreate(List<string> args)
    {
        if (!RequireArgs(args, 2, "create <notebook> <path> [--parent <parentID>]")) return 1;
        var box = GetNotebookId(args[0]);
        var childName = SanitizePath(args[1]).TrimStart('/');

        string? parentId = null;
        var parentIdx = args.IndexOf("--parent");
        if (parentIdx != -1 && parentIdx + 1 < args.Count)
            parentId = args[parentIdx + 1];

        string path;
        if (parentId is not null)
        {
            var parentInfo = await Api("/block/getBlockInfo", new { id = parentId });
            var parentTitle = parentInfo.GetProperty("rootTitle").GetString() ?? parentId;
            path = $"/{parentTitle}/{childName}";
        }
        else
        {
            path = $"/{childName}";
        }

        var md = Console.IsInputRedirected ? await Console.In.ReadToEndAsync() : "";
        if (string.IsNullOrWhiteSpace(md)) { Console.Error.WriteLine("No markdown content provided on stdin"); return 1; }

        var body = new Dictionary<string, object> { ["notebook"] = box, ["path"] = path, ["markdown"] = md };
        if (parentId is not null) body["parentID"] = parentId;
        var data = await Api("/filetree/createDocWithMd", body);
        Console.WriteLine($"Created: {data}");
        return 0;
    }

    async Task<int> CmdUpdateMd(List<string> args)
    {
        if (!RequireArgs(args, 1, "update-md <id>")) return 1;
        var id = args[0];

        var md = Console.IsInputRedirected ? await Console.In.ReadToEndAsync() : "";
        if (string.IsNullOrWhiteSpace(md)) { Console.Error.WriteLine("No markdown content provided on stdin"); return 1; }

        await Api("/block/updateBlock", new { dataType = "markdown", data = md, id });

        var firstH1 = System.Text.RegularExpressions.Regex.Match(md, @"^#\s+(.+)$", System.Text.RegularExpressions.RegexOptions.Multiline);
        if (firstH1.Success)
        {
            var title = firstH1.Groups[1].Value.Trim();
            await Api("/filetree/renameDocByID", new { id, title });
        }

        Console.WriteLine($"Updated: {id}");
        return 0;
    }

    async Task<int> CmdRename(List<string> args)
    {
        if (!RequireArgs(args, 2, "rename <id> <title>")) return 1;
        await Api("/filetree/renameDocByID", new { id = args[0], title = args[1] });
        Console.WriteLine($"Renamed {args[0]} -> {args[1]}");
        return 0;
    }

    async Task<int> CmdTags(List<string> args)
    {
        var kw = args.Count > 0 ? args[0] : "";
        var data = await Api("/search/searchTag", new { k = kw });
        if (data.TryGetProperty("tags", out var tags))
            foreach (var tag in tags.EnumerateArray())
                Console.WriteLine(tag.ToString());
        else
            PrintJson(data);
        return 0;
    }

    async Task<int> CmdRaw(List<string> args)
    {
        if (!RequireArgs(args, 1, "raw <endpoint> [json]")) return 1;
        var endpoint = args[0].TrimStart('.');
        if (!endpoint.StartsWith('/')) endpoint = "/" + endpoint;
        object payload = "{}";
        if (args.Count > 1)
        {
            var jsonStr = string.Join(" ", args.Skip(1));
            try { payload = JsonSerializer.Deserialize(jsonStr, SiyuanJsonContext.Default.JsonElement); }
            catch { payload = jsonStr; }
        }
        var result = await Api(endpoint, payload);
        PrintJson(result);
        return 0;
    }

    async Task<int> CmdDuplicate(List<string> args)
    {
        if (!RequireArgs(args, 1, "duplicate <id>")) return 1;
        var data = await Api("/filetree/duplicateDoc", new { id = args[0] });
        Console.WriteLine($"Duplicated: {data}");
        return 0;
    }

    async Task<int> CmdSetAttrsBatch(List<string> args)
    {
        var whereIdx = args.FindIndex(a => a == "--where");
        if (whereIdx < 0)
        {
            RequireArgs([], 1, "set-attrs-batch <key=value ...> --where <sql where clause>");
            return 1;
        }

        var attrArgs = args.Take(whereIdx).ToList();
        var whereClause = string.Join(" ", args.Skip(whereIdx + 1));

        var attrs = new Dictionary<string, string>();
        foreach (var a in attrArgs)
        {
            var parts = a.Split('=', 2);
            if (parts.Length == 2) attrs[parts[0]] = parts[1];
        }
        if (attrs.Count == 0) { Console.Error.WriteLine("No attributes specified"); return 1; }

        var ids = new List<string>();
        int offset = 0;
        while (true)
        {
            var sql = $"SELECT id FROM blocks WHERE {whereClause} ORDER BY path LIMIT 64 OFFSET {offset}";
            var data = await Api("/query/sql", new { stmt = sql });
            var batch = data.EnumerateArray().Select(r => r.GetProperty("id").GetString()!).ToList();
            if (batch.Count == 0) break;
            ids.AddRange(batch);
            offset += 64;
        }

        if (ids.Count == 0) { Console.WriteLine("No matching blocks found."); return 0; }

        Console.Error.WriteLine($"Found {ids.Count} blocks. Updating...");
        var success = 0;
        var failed = 0;
        for (int i = 0; i < ids.Count; i++)
        {
            try
            {
                await Api("/attr/setBlockAttrs", new { id = ids[i], attrs });
                success++;
            }
            catch
            {
                Console.Error.WriteLine($"Failed: {ids[i]}");
                failed++;
            }
            if ((i + 1) % 20 == 0) Console.Error.WriteLine($"Progress: {i + 1}/{ids.Count}");
        }
        Console.WriteLine($"Done: {success} updated, {failed} failed.");
        return 0;
    }

    async Task<int> CmdOpenNb(List<string> args)
    {
        if (!RequireArgs(args, 1, "open-nb <notebook>")) return 1;
        var box = GetNotebookId(args[0]);
        await Api("/notebook/openNotebook", new { notebook = box });
        Console.WriteLine($"Opened: {args[0]}");
        return 0;
    }

    async Task<int> CmdCloseNb(List<string> args)
    {
        if (!RequireArgs(args, 1, "close-nb <notebook>")) return 1;
        var box = GetNotebookId(args[0]);
        await Api("/notebook/closeNotebook", new { notebook = box });
        Console.WriteLine($"Closed: {args[0]}");
        return 0;
    }

    async Task<int> CmdRemoveNb(List<string> args)
    {
        if (!RequireArgs(args, 1, "remove-nb <notebook>")) return 1;
        var box = GetNotebookId(args[0]);
        await Api("/notebook/closeNotebook", new { notebook = box });
        await Api("/notebook/removeNotebook", new { notebook = box });
        Console.WriteLine($"Removed: {args[0]}");
        return 0;
    }

    async Task<int> CmdCreateNb(List<string> args)
    {
        if (!RequireArgs(args, 1, "create-nb <name>")) return 1;
        var data = await Api("/notebook/createNotebook", new { name = args[0] });
        Console.WriteLine($"Created: {data}");
        return 0;
    }

    async Task<int> CmdRenameNb(List<string> args)
    {
        if (!RequireArgs(args, 2, "rename-nb <notebook> <name>")) return 1;
        var box = GetNotebookId(args[0]);
        await Api("/notebook/renameNotebook", new { notebook = box, name = args[1] });
        Console.WriteLine($"Renamed {args[0]} -> {args[1]}");
        return 0;
    }

    async Task<int> CmdGetNbConf(List<string> args)
    {
        if (!RequireArgs(args, 1, "get-nb-conf <notebook>")) return 1;
        var box = GetNotebookId(args[0]);
        var data = await Api("/notebook/getNotebookConf", new { notebook = box });
        PrintJson(data);
        return 0;
    }

    async Task<int> CmdSetNbConf(List<string> args)
    {
        if (!RequireArgs(args, 2, "set-nb-conf <notebook> <confJSON>")) return 1;
        var box = GetNotebookId(args[0]);
        var conf = JsonSerializer.Deserialize(string.Join(" ", args.Skip(1)), SiyuanJsonContext.Default.JsonElement);
        await Api("/notebook/setNotebookConf", new { notebook = box, conf });
        Console.WriteLine($"Config updated for: {args[0]}");
        return 0;
    }

    async Task<int> CmdGetDoc(List<string> args)
    {
        if (!RequireArgs(args, 1, "get-doc <id>")) return 1;
        var data = await Api("/filetree/getDoc", new { id = args[0] });
        PrintJson(data);
        return 0;
    }

    async Task<int> CmdInsertBlock(List<string> args)
    {
        if (!RequireArgs(args, 3, "insert-block <parentID> <dataType> <data> [--previous ID] [--next ID]")) return 1;
        var parentID = args[0];
        var dataType = args[1];
        var data = args[2];
        string? previousID = null, nextID = null;
        for (int i = 3; i < args.Count; i++)
        {
            if (args[i] == "--previous" && i + 1 < args.Count) previousID = args[++i];
            if (args[i] == "--next" && i + 1 < args.Count) nextID = args[++i];
        }
        var body = new Dictionary<string, object> { ["data"] = data, ["dataType"] = dataType, ["parentID"] = parentID };
        if (previousID != null) body["previousID"] = previousID;
        if (nextID != null) body["nextID"] = nextID;
        var result = await Api("/block/insertBlock", body);
        Console.WriteLine($"Inserted: {result}");
        return 0;
    }

    async Task<int> CmdMoveBlock(List<string> args)
    {
        if (!RequireArgs(args, 1, "move-block <id> [--parent ID] [--previous ID]")) return 1;
        var id = args[0];
        string? parentID = null, previousID = null;
        for (int i = 1; i < args.Count; i++)
        {
            if (args[i] == "--parent" && i + 1 < args.Count) parentID = args[++i];
            if (args[i] == "--previous" && i + 1 < args.Count) previousID = args[++i];
        }
        var body = new Dictionary<string, object> { ["id"] = id };
        if (parentID != null) body["parentID"] = parentID;
        if (previousID != null) body["previousID"] = previousID;
        await Api("/block/moveBlock", body);
        Console.WriteLine($"Moved block: {id}");
        return 0;
    }

    async Task<int> CmdExport(List<string> args)
    {
        if (!RequireArgs(args, 2, "export <id> <format>")) return 1;
        var id = args[0];
        var format = args[1].ToLower();
        var formats = new Dictionary<string, string>
        {
            ["md"] = "/export/exportMd", ["sy"] = "/export/exportSY",
            ["html"] = "/export/exportPreviewHTML", ["docx"] = "/export/exportDocx",
            ["pdf"] = "/export/processPDF", ["epub"] = "/export/exportEPUB",
            ["textile"] = "/export/exportTextile", ["org"] = "/export/exportOrgMode",
            ["odt"] = "/export/exportODT", ["rtf"] = "/export/exportRTF",
            ["asciidoc"] = "/export/exportReStructuredText"
        };
        if (!formats.TryGetValue(format, out var endpoint))
        {
            Console.Error.WriteLine($"Unsupported format: {format}. Supported: {string.Join(", ", formats.Keys)}");
            return 1;
        }
        var data = await Api(endpoint, new { id });
        PrintJson(data);
        return 0;
    }

    async Task<int> CmdAttrs(List<string> args)
    {
        if (!RequireArgs(args, 1, "attrs <id>")) return 1;
        var data = await Api("/attr/getBlockAttrs", new { id = args[0] });
        PrintJson(data);
        return 0;
    }

    async Task<int> CmdSetAttrs(List<string> args)
    {
        if (!RequireArgs(args, 2, "set-attrs <id> <key=value...>")) return 1;
        var id = args[0];
        var attrs = new Dictionary<string, string>();
        foreach (var a in args.Skip(1))
        {
            var parts = a.Split('=', 2);
            if (parts.Length == 2) attrs[parts[0]] = parts[1];
        }
        await Api("/attr/setBlockAttrs", new { id, attrs });
        Console.WriteLine($"Attrs set on: {id}");
        return 0;
    }

    async Task<int> CmdRecent(List<string> args)
    {
        var data = await Api("/block/getRecentUpdatedBlocks");
        foreach (var blk in data.EnumerateArray())
        {
            var hPath = blk.TryGetProperty("hPath", out var hp) ? hp.GetString() : "";
            var id = blk.TryGetProperty("id", out var i) ? i.GetString() : "";
            Console.WriteLine($"{hPath}  ({id})");
        }
        return 0;
    }

    async Task<int> CmdHistory(List<string> args)
    {
        if (!RequireArgs(args, 2, "history <notebook> <path>")) return 1;
        var box = GetNotebookId(args[0]);
        var data = await Api("/history/getDocHistoryContent", new { notebook = box, historyPath = args[1] });
        PrintJson(data);
        return 0;
    }

    async Task<int> CmdTemplates(List<string> args)
    {
        var kw = args.Count > 0 ? args[0] : "";
        var data = await Api("/search/searchTemplate", new { k = kw });
        if (data.ValueKind == JsonValueKind.Array)
            foreach (var t in data.EnumerateArray())
            {
                var name = t.TryGetProperty("name", out var n) ? n.GetString() : "";
                var path = t.TryGetProperty("path", out var p) ? p.GetString() : "";
                Console.WriteLine($"{name}  {path}");
            }
        else if (data.TryGetProperty("templates", out var tmplArr) && tmplArr.ValueKind == JsonValueKind.Array)
            foreach (var t in tmplArr.EnumerateArray())
            {
                var name = t.TryGetProperty("name", out var n) ? n.GetString() : "";
                var path = t.TryGetProperty("path", out var p) ? p.GetString() : "";
                Console.WriteLine($"{name}  {path}");
            }
        else
            PrintJson(data);
        return 0;
    }

    async Task<int> CmdBacklinks(List<string> args)
    {
        if (!RequireArgs(args, 1, "backlinks <id>")) return 1;
        var data = await Api("/ref/getBacklink2", new { id = args[0] });
        PrintJson(data);
        return 0;
    }

    void PrintHelp(string? cmd)
    {
        if (cmd != null && Commands.TryGetValue(cmd, out var spec))
            Console.WriteLine($"  {cmd} — {spec.Description}");
        else
        {
            Console.WriteLine("SiYuan CLI helper — C# file app");
            Console.WriteLine("Usage: dotnet run --file siyuan.cs <command> [args]\n");
            Console.WriteLine("Commands:");
            foreach (var (name, commandSpec) in Commands)
                Console.WriteLine($"  {name,-14} {commandSpec.Description}");
        }
    }
}

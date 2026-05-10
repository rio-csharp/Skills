#!/usr/bin/env dotnet-script
// SiYuan CLI helper in C# — equivalent to siyuan.py
// Usage: dotnet-script siyuan.csx <command> [args]

#nullable enable

using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

var baseUrl = Environment.GetEnvironmentVariable("SIYUAN_URL") ?? "http://127.0.0.1:6806";
var token = Environment.GetEnvironmentVariable("SIYUAN_TOKEN") ?? "";

var http = new HttpClient();
http.BaseAddress = new Uri(baseUrl);
http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", token);

var argsList = Args.ToList();

if (argsList.Count == 0 || argsList[0] is "-h" or "--help" or "help")
{
    PrintHelp(argsList.Count > 1 ? argsList[1] : null);
    return;
}

var command = argsList[0];
var cmdArgs = argsList.Skip(1).ToList();

try
{
    switch (command)
    {
        case "ls": await CmdLs(); break;
        case "tree": await CmdTree(cmdArgs); break;
        case "docs": await CmdDocs(cmdArgs); break;
        case "cat": await CmdCat(cmdArgs); break;
        case "info": await CmdInfo(cmdArgs); break;
        case "rm": await CmdRm(cmdArgs); break;
        case "rm-path": await CmdRmPath(cmdArgs); break;
        case "mv": await CmdMv(cmdArgs); break;
        case "sql": await CmdSql(cmdArgs); break;
        case "search": await CmdSearch(cmdArgs); break;
        case "search-docs": await CmdSearchDocs(cmdArgs); break;
        case "path": await CmdPath(cmdArgs); break;
        case "full-path": await CmdFullPath(cmdArgs); break;
        case "breadcrumb": await CmdBreadcrumb(cmdArgs); break;
        case "outline": await CmdOutline(cmdArgs); break;
        case "create": await CmdCreate(cmdArgs); break;
        case "update-md": await CmdUpdateMd(cmdArgs); break;
        case "rename": await CmdRename(cmdArgs); break;
        case "tags": await CmdTags(cmdArgs); break;
        case "raw": await CmdRaw(cmdArgs); break;
        case "duplicate": await CmdDuplicate(cmdArgs); break;
        case "set-attrs-batch": await CmdSetAttrsBatch(cmdArgs); break;
        case "open-nb": await CmdOpenNb(cmdArgs); break;
        case "close-nb": await CmdCloseNb(cmdArgs); break;
        case "remove-nb": await CmdRemoveNb(cmdArgs); break;
        case "create-nb": await CmdCreateNb(cmdArgs); break;
        case "rename-nb": await CmdRenameNb(cmdArgs); break;
        case "get-nb-conf": await CmdGetNbConf(cmdArgs); break;
        case "set-nb-conf": await CmdSetNbConf(cmdArgs); break;
        case "get-doc": await CmdGetDoc(cmdArgs); break;
        case "insert-block": await CmdInsertBlock(cmdArgs); break;
        case "move-block": await CmdMoveBlock(cmdArgs); break;
        case "export": await CmdExport(cmdArgs); break;
        case "attrs": await CmdAttrs(cmdArgs); break;
        case "set-attrs": await CmdSetAttrs(cmdArgs); break;
        case "recent": await CmdRecent(cmdArgs); break;
        case "history": await CmdHistory(cmdArgs); break;
        case "templates": await CmdTemplates(cmdArgs); break;
        case "backlinks": await CmdBacklinks(cmdArgs); break;
        default:
            Console.Error.WriteLine($"Unknown command: {command}");
            Environment.Exit(1);
            break;
    }
}
catch (HttpRequestException ex)
{
    Console.Error.WriteLine($"HTTP error: {ex.Message}");
    Environment.Exit(1);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}

// ── API helpers ──────────────────────────────────────────

async Task<JsonElement> Api(string endpoint, object? body = null)
{
    var json = body is null ? "{}" : JsonSerializer.Serialize(body);
    var content = new StringContent(json, Encoding.UTF8, "application/json");
    var resp = await http.PostAsync($"/api{endpoint}", content);
    var responseBody = await resp.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<JsonElement>(responseBody);
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
    // Handle paths that were mangled by Git Bash (e.g., C:/Program Files/Git/xxx)
    // Pattern: matches Windows absolute paths like C:/... or C:\...
    if (path.Length >= 2 && path[1] == ':')
    {
        // Find the last segment after the Git prefix
        // Typically: C:/Program Files/Git/actual/path
        var gitIndex = path.IndexOf("/Git/", StringComparison.OrdinalIgnoreCase);
        if (gitIndex != -1)
        {
            path = path[(gitIndex + 4)..]; // +4 to skip "/Git"
        }
        else
        {
            // If not a Git-bash path, just take the last meaningful segment
            var parts = path.Replace('\\', '/').Split('/');
            // Remove drive letter prefix, Program Files, Git from the path
            parts = parts.Where(p =>
                p.Length != 2 || p[1] != ':' && !p.Equals("Program Files", StringComparison.OrdinalIgnoreCase) && !p.Equals("Git", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (parts.Length > 0)
                path = "/" + parts[^1];
        }
    }

    // Strip ./ prefix (used to avoid bash mangling)
    path = path.TrimStart('.');
    if (!path.StartsWith('/'))
        path = "/" + path;

    return path;
}

// ── Commands ─────────────────────────────────────────────

async Task CmdLs()
{
    var data = await Api("/notebook/lsNotebooks");
    foreach (var nb in data.GetProperty("notebooks").EnumerateArray())
    {
        Console.WriteLine($"{nb.GetProperty("name").GetString(),-12} {nb.GetProperty("id").GetString()}  closed={nb.GetProperty("closed").GetBoolean()}  sort={nb.GetProperty("sort").GetInt32()}");
    }
}

async Task CmdTree(List<string> args)
{
    if (args.Count == 0) { Console.Error.WriteLine("Usage: tree <notebook> [path]"); return; }
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
    {
        foreach (var node in nodes.EnumerateArray())
            PrintNode(node);
    }
}

async Task CmdDocs(List<string> args)
{
    if (args.Count == 0) { Console.Error.WriteLine("Usage: docs <notebook> [path]"); return; }
    var box = GetNotebookId(args[0]);
    var path = args.Count > 1 ? SanitizePath(args[1]) : "/";
    var data = await Api("/filetree/listDocsByPath", new { notebook = box, path, sort = 0, maxListCount = 999999 });
    foreach (var f in data.GetProperty("files").EnumerateArray())
        Console.WriteLine($"{f.GetProperty("path").GetString(),-50} {f.GetProperty("name").GetString()}");
}

async Task CmdCat(List<string> args)
{
    if (args.Count == 0) { Console.Error.WriteLine("Usage: cat <id>"); return; }
    var data = await Api("/export/exportMdContent", new { id = args[0] });
    Console.WriteLine($"# {data.GetProperty("hPath").GetString()}\n");
    Console.WriteLine(data.GetProperty("content").GetString());
}

async Task CmdInfo(List<string> args)
{
    if (args.Count == 0) { Console.Error.WriteLine("Usage: info <id>"); return; }
    var data = await Api("/block/getBlockInfo", new { id = args[0] });
    Console.WriteLine(JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
}

async Task CmdRm(List<string> args)
{
    foreach (var id in args)
    {
        await Api("/filetree/removeDocByID", new { id });
        Console.WriteLine($"Removed: {id}");
    }
}

async Task CmdRmPath(List<string> args)
{
    if (args.Count < 2) { Console.Error.WriteLine("Usage: rm-path <notebook> <path>"); return; }
    var box = GetNotebookId(args[0]);
    await Api("/filetree/removeDoc", new { notebook = box, path = args[1] });
    Console.WriteLine($"Removed: {args[0]}:{args[1]}");
}

async Task CmdMv(List<string> args)
{
    if (args.Count < 2) { Console.Error.WriteLine("Usage: mv <toID> <fromID...>"); return; }
    var toId = args[0];
    var fromIds = args.Skip(1).ToList();
    await Api("/filetree/moveDocsByID", new { fromIDs = fromIds, toID = toId });
    Console.WriteLine($"Moved {fromIds.Count} doc(s) to {toId}");
}

async Task CmdSql(List<string> args)
{
    if (args.Count == 0) { Console.Error.WriteLine("Usage: sql <statement>"); return; }
    var stmt = string.Join(" ", args);
    var data = await Api("/query/sql", new { stmt });
    if (data.ValueKind == JsonValueKind.Array)
    {
        foreach (var row in data.EnumerateArray())
        {
            var values = new List<string>();
            foreach (var prop in row.EnumerateObject())
                values.Add(prop.Value.ToString());
            Console.WriteLine(string.Join("\t", values));
        }
    }
    else
    {
        Console.WriteLine(JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
    }
}

async Task CmdSearch(List<string> args)
{
    if (args.Count == 0) { Console.Error.WriteLine("Usage: search <query> [page]"); return; }
    var page = args.Count > 1 ? int.Parse(args[1]) : 1;
    var data = await Api("/search/fullTextSearchBlock", new
    {
        query = args[0], method = 0, page, pageSize = 32, orderBy = 0, groupBy = 1
    });
    Console.WriteLine($"Total matches: {data.GetProperty("matchedBlockCount").GetInt32()}");
    foreach (var blk in data.GetProperty("blocks").EnumerateArray())
    {
        var hPath = blk.TryGetProperty("hPath", out var hp) ? hp.GetString() : "";
        var type = blk.TryGetProperty("type", out var tp) ? tp.GetString() : "";
        var content = blk.TryGetProperty("content", out var ct) ? ct.GetString() : "";
        var display = (content ?? "").Length > 120 ? content![..120] : content;
Console.WriteLine($"{hPath}  [{type}]  {display}");
    }
}

async Task CmdSearchDocs(List<string> args)
{
    if (args.Count == 0) { Console.Error.WriteLine("Usage: search-docs <query>"); return; }
    var data = await Api("/filetree/searchDocs", new { k = args[0] });
    foreach (var doc in data.EnumerateArray())
    {
        var hPath = doc.TryGetProperty("hPath", out var hp) ? hp.GetString() : "";
        var path = doc.TryGetProperty("path", out var p) ? p.GetString()?.TrimStart('/').Replace(".sy", "") : "";
        Console.WriteLine($"{hPath,-50} {path}");
    }
}

async Task CmdPath(List<string> args)
{
    if (args.Count == 0) { Console.Error.WriteLine("Usage: path <id>"); return; }
    var data = await Api("/filetree/getHPathByID", new { id = args[0] });
    Console.WriteLine(data.ToString());
}

async Task CmdFullPath(List<string> args)
{
    if (args.Count == 0) { Console.Error.WriteLine("Usage: full-path <id>"); return; }
    var data = await Api("/filetree/getFullHPathByID", new { id = args[0] });
    Console.WriteLine(data.ToString());
}

async Task CmdBreadcrumb(List<string> args)
{
    if (args.Count == 0) { Console.Error.WriteLine("Usage: breadcrumb <id>"); return; }
    var data = await Api("/block/getBlockBreadcrumb", new { id = args[0] });
    Console.WriteLine(JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
}

async Task CmdOutline(List<string> args)
{
    if (args.Count == 0) { Console.Error.WriteLine("Usage: outline <id>"); return; }
    var data = await Api("/outline/getDocOutline", new { id = args[0] });
    if (data.ValueKind == JsonValueKind.Null)
    {
        Console.WriteLine("(no outline)");
        return;
    }
    foreach (var h in data.EnumerateArray())
    {
        var depth = h.TryGetProperty("depth", out var d) ? d.GetInt32() : 0;
        var content = h.TryGetProperty("content", out var c) ? c.GetString() : "";
        var id = h.TryGetProperty("id", out var i) ? i.GetString() : "";
        Console.WriteLine($"{new string(' ', depth * 2)}{content}  ({id})");
    }
}

async Task CmdCreate(List<string> args)
{
    if (args.Count < 2) { Console.Error.WriteLine("Usage: create <notebook> <path> [--parent <parentID>]"); return; }
    var box = GetNotebookId(args[0]);
    var childName = SanitizePath(args[1]).TrimStart('/');

    string? parentId = null;
    var parentIdx = args.IndexOf("--parent");
    if (parentIdx != -1 && parentIdx + 1 < args.Count)
        parentId = args[parentIdx + 1];

    // When creating under a parent, prefix path with parent title so SiYuan nests correctly
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
    if (string.IsNullOrWhiteSpace(md))
    {
        Console.Error.WriteLine("No markdown content provided on stdin");
        Environment.Exit(1);
    }

    var body = new Dictionary<string, object> { ["notebook"] = box, ["path"] = path, ["markdown"] = md };
    if (parentId is not null) body["parentID"] = parentId;
    var data = await Api("/filetree/createDocWithMd", body);
    Console.WriteLine($"Created: {data}");
}

async Task CmdUpdateMd(List<string> args)
{
    if (args.Count < 1) { Console.Error.WriteLine("Usage: update-md <id>"); return; }
    var id = args[0];

    var md = Console.IsInputRedirected ? await Console.In.ReadToEndAsync() : "";
    if (string.IsNullOrWhiteSpace(md))
    {
        Console.Error.WriteLine("No markdown content provided on stdin");
        Environment.Exit(1);
    }

    // Use updateBlock — preserves parent-child relationships and doc path
    await Api("/block/updateBlock", new { dataType = "markdown", data = md, id });

    // Rename to match the new heading
    var firstH1 = System.Text.RegularExpressions.Regex.Match(md, @"^#\s+(.+)$", System.Text.RegularExpressions.RegexOptions.Multiline);
    if (firstH1.Success)
    {
        var title = firstH1.Groups[1].Value.Trim();
        await Api("/filetree/renameDocByID", new { id, title });
    }

    Console.WriteLine($"Updated: {id}");
}

async Task CmdRename(List<string> args)
{
    if (args.Count < 2) { Console.Error.WriteLine("Usage: rename <id> <title>"); return; }
    await Api("/filetree/renameDocByID", new { id = args[0], title = args[1] });
    Console.WriteLine($"Renamed {args[0]} -> {args[1]}");
}

async Task CmdTags(List<string> args)
{
    var kw = args.Count > 0 ? args[0] : "";
    var data = await Api("/search/searchTag", new { k = kw });
    if (data.TryGetProperty("tags", out var tags))
    {
        foreach (var tag in tags.EnumerateArray())
            Console.WriteLine(tag.ToString());
    }
    else
    {
        Console.WriteLine(JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
    }
}

async Task CmdRaw(List<string> args)
{
    if (args.Count == 0) { Console.Error.WriteLine("Usage: raw <endpoint> [json]"); return; }
    var endpoint = args[0].TrimStart('.');
    if (!endpoint.StartsWith('/')) endpoint = "/" + endpoint;
    object payload = "{}";
    if (args.Count > 1)
    {
        var jsonStr = string.Join(" ", args.Skip(1));
        try { payload = JsonSerializer.Deserialize<JsonElement>(jsonStr); }
        catch { payload = jsonStr; }
    }
    var result = await Api(endpoint, payload);
    Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
}

async Task CmdDuplicate(List<string> args)
{
    if (args.Count == 0) { Console.Error.WriteLine("Usage: duplicate <id>"); return; }
    var data = await Api("/filetree/duplicateDoc", new { id = args[0] });
    Console.WriteLine($"Duplicated: {JsonSerializer.Serialize(data)}");
}

async Task CmdSetAttrsBatch(List<string> args)
{
    // Usage: set-attrs-batch key1=value1 key2=value2 ... --where where_clause
    // Example: set-attrs-batch custom-sy-fullwidth=false custom-sy-readonly=true --where box='...' AND path LIKE '/...%' AND type='d'
    var whereIdx = args.FindIndex(a => a == "--where");
    if (whereIdx < 0) { Console.Error.WriteLine("Usage: set-attrs-batch <key=value ...> --where <sql where clause>"); return; }

    var attrArgs = args.Take(whereIdx).ToList();
    var whereClause = string.Join(" ", args.Skip(whereIdx + 1));

    var attrs = new Dictionary<string, string>();
    foreach (var a in attrArgs)
    {
        var parts = a.Split('=', 2);
        if (parts.Length == 2) attrs[parts[0]] = parts[1];
    }

    if (attrs.Count == 0) { Console.Error.WriteLine("No attributes specified"); return; }

    // Collect all IDs with pagination
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

    if (ids.Count == 0) { Console.WriteLine("No matching blocks found."); return; }

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
}

async Task CmdOpenNb(List<string> args)
{
    if (args.Count == 0) { Console.Error.WriteLine("Usage: open-nb <notebook>"); return; }
    var box = GetNotebookId(args[0]);
    await Api("/notebook/openNotebook", new { notebook = box });
    Console.WriteLine($"Opened: {args[0]}");
}

async Task CmdCloseNb(List<string> args)
{
    if (args.Count == 0) { Console.Error.WriteLine("Usage: close-nb <notebook>"); return; }
    var box = GetNotebookId(args[0]);
    await Api("/notebook/closeNotebook", new { notebook = box });
    Console.WriteLine($"Closed: {args[0]}");
}

async Task CmdRemoveNb(List<string> args)
{
    if (args.Count == 0) { Console.Error.WriteLine("Usage: remove-nb <notebook>"); return; }
    var box = GetNotebookId(args[0]);
    await Api("/notebook/closeNotebook", new { notebook = box });
    await Api("/notebook/removeNotebook", new { notebook = box });
    Console.WriteLine($"Removed: {args[0]}");
}

async Task CmdCreateNb(List<string> args)
{
    if (args.Count == 0) { Console.Error.WriteLine("Usage: create-nb <name>"); return; }
    var data = await Api("/notebook/createNotebook", new { name = args[0] });
    Console.WriteLine($"Created: {data}");
}

async Task CmdRenameNb(List<string> args)
{
    if (args.Count < 2) { Console.Error.WriteLine("Usage: rename-nb <notebook> <name>"); return; }
    var box = GetNotebookId(args[0]);
    await Api("/notebook/renameNotebook", new { notebook = box, name = args[1] });
    Console.WriteLine($"Renamed {args[0]} -> {args[1]}");
}

async Task CmdGetNbConf(List<string> args)
{
    if (args.Count == 0) { Console.Error.WriteLine("Usage: get-nb-conf <notebook>"); return; }
    var box = GetNotebookId(args[0]);
    var data = await Api("/notebook/getNotebookConf", new { notebook = box });
    Console.WriteLine(JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
}

async Task CmdSetNbConf(List<string> args)
{
    if (args.Count < 2) { Console.Error.WriteLine("Usage: set-nb-conf <notebook> <confJSON>"); return; }
    var box = GetNotebookId(args[0]);
    var conf = JsonSerializer.Deserialize<JsonElement>(string.Join(" ", args.Skip(1)));
    await Api("/notebook/setNotebookConf", new { notebook = box, conf });
    Console.WriteLine($"Config updated for: {args[0]}");
}

async Task CmdGetDoc(List<string> args)
{
    if (args.Count == 0) { Console.Error.WriteLine("Usage: get-doc <id>"); return; }
    var data = await Api("/filetree/getDoc", new { id = args[0] });
    Console.WriteLine(JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
}

async Task CmdInsertBlock(List<string> args)
{
    if (args.Count < 3) { Console.Error.WriteLine("Usage: insert-block <parentID> <dataType> <data> [--previous ID] [--next ID]"); return; }
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
}

async Task CmdMoveBlock(List<string> args)
{
    if (args.Count < 1) { Console.Error.WriteLine("Usage: move-block <id> [--parent ID] [--previous ID]"); return; }
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
}

async Task CmdExport(List<string> args)
{
    if (args.Count < 2) { Console.Error.WriteLine("Usage: export <id> <format>"); return; }
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
        Environment.Exit(1);
    }
    var data = await Api(endpoint, new { id });
    Console.WriteLine(JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
}

async Task CmdAttrs(List<string> args)
{
    if (args.Count == 0) { Console.Error.WriteLine("Usage: attrs <id>"); return; }
    var data = await Api("/attr/getBlockAttrs", new { id = args[0] });
    Console.WriteLine(JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
}

async Task CmdSetAttrs(List<string> args)
{
    if (args.Count < 2) { Console.Error.WriteLine("Usage: set-attrs <id> <key=value...>"); return; }
    var id = args[0];
    var attrs = new Dictionary<string, string>();
    foreach (var a in args.Skip(1))
    {
        var parts = a.Split('=', 2);
        if (parts.Length == 2) attrs[parts[0]] = parts[1];
    }
    await Api("/attr/setBlockAttrs", new { id, attrs });
    Console.WriteLine($"Attrs set on: {id}");
}

async Task CmdRecent(List<string> args)
{
    var data = await Api("/block/getRecentUpdatedBlocks");
    foreach (var blk in data.EnumerateArray())
    {
        var hPath = blk.TryGetProperty("hPath", out var hp) ? hp.GetString() : "";
        var id = blk.TryGetProperty("id", out var i) ? i.GetString() : "";
        Console.WriteLine($"{hPath}  ({id})");
    }
}

async Task CmdHistory(List<string> args)
{
    if (args.Count < 2) { Console.Error.WriteLine("Usage: history <notebook> <path>"); return; }
    var box = GetNotebookId(args[0]);
    var data = await Api("/history/getDocHistoryContent", new { notebook = box, path = args[1] });
    Console.WriteLine(JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
}

async Task CmdTemplates(List<string> args)
{
    var kw = args.Count > 0 ? args[0] : "";
    var data = await Api("/search/searchTemplate", new { k = kw });
    if (data.ValueKind == JsonValueKind.Array)
    {
        foreach (var t in data.EnumerateArray())
        {
            var name = t.TryGetProperty("name", out var n) ? n.GetString() : "";
            var path = t.TryGetProperty("path", out var p) ? p.GetString() : "";
            Console.WriteLine($"{name}  {path}");
        }
    }
    else if (data.TryGetProperty("templates", out var tmplArr) && tmplArr.ValueKind == JsonValueKind.Array)
    {
        foreach (var t in tmplArr.EnumerateArray())
        {
            var name = t.TryGetProperty("name", out var n) ? n.GetString() : "";
            var path = t.TryGetProperty("path", out var p) ? p.GetString() : "";
            Console.WriteLine($"{name}  {path}");
        }
    }
    else
    {
        Console.WriteLine(JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
    }
}

async Task CmdBacklinks(List<string> args)
{
    if (args.Count == 0) { Console.Error.WriteLine("Usage: backlinks <id>"); return; }
    var data = await Api("/ref/getBacklink2", new { id = args[0] });
    Console.WriteLine(JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
}

void PrintHelp(string? cmd)
{
    var help = new Dictionary<string, string>
    {
        ["ls"] = "List all notebooks",
        ["tree"] = "Document tree with titles (args: notebook [path])",
        ["docs"] = "Flat doc listing (args: notebook [path])",
        ["cat"] = "Export doc as markdown (args: id)",
        ["info"] = "Get block metadata (args: id)",
        ["rm"] = "Remove docs by ID (args: id [id ...])",
        ["rm-path"] = "Remove doc by path (args: notebook path)",
        ["mv"] = "Move docs (args: toID fromID [fromID ...])",
        ["sql"] = "Execute SQL query (args: statement)",
        ["search"] = "Full-text search (args: query [page])",
        ["search-docs"] = "Search document titles (args: query)",
        ["path"] = "Get HPath by ID (args: id)",
        ["full-path"] = "Get full HPath including parent docs (args: id)",
        ["breadcrumb"] = "Get block breadcrumb (args: id)",
        ["outline"] = "Get doc outline (args: id)",
        ["create"] = "Create doc from stdin markdown (args: notebook path)",
        ["rename"] = "Rename doc by ID (args: id title)",
        ["tags"] = "List/search tags (args: [keyword])",
        ["raw"] = "Call any API endpoint (args: endpoint [json])",
        ["duplicate"] = "Duplicate a document (args: id)",
        ["set-attrs-batch"] = "Batch set block attributes (args: key=value ... --where sql_where)",
        ["open-nb"] = "Open/mount notebook (args: notebook)",
        ["close-nb"] = "Close/unmount notebook (args: notebook)",
        ["remove-nb"] = "Remove notebook (args: notebook)",
        ["create-nb"] = "Create notebook (args: name)",
        ["rename-nb"] = "Rename notebook (args: notebook name)",
        ["get-nb-conf"] = "Get notebook config (args: notebook)",
        ["set-nb-conf"] = "Set notebook config (args: notebook json_conf)",
        ["get-doc"] = "Get doc content JSON (args: id)",
        ["insert-block"] = "Insert block (args: parentID dataType data [--previous ID] [--next ID])",
        ["move-block"] = "Move block (args: id [--parent ID] [--previous ID])",
        ["export"] = "Export doc in various formats (args: id format)",
        ["attrs"] = "Get block attributes (args: id)",
        ["set-attrs"] = "Set block attributes (args: id key=value ...)",
        ["recent"] = "Recently updated blocks (no args)",
        ["history"] = "Get doc history (args: notebook path)",
        ["templates"] = "Search templates (args: [keyword])",
        ["backlinks"] = "Get backlinks for a block (args: id)",
    };

    if (cmd != null && help.TryGetValue(cmd, out var desc))
    {
        Console.WriteLine($"  {cmd} — {desc}");
    }
    else
    {
        Console.WriteLine("SiYuan CLI helper — C# edition");
        Console.WriteLine("Usage: dotnet-script siyuan.csx <command> [args]\n");
        Console.WriteLine("Commands:");
        foreach (var (name, description) in help)
            Console.WriteLine($"  {name,-14} {description}");
    }
}

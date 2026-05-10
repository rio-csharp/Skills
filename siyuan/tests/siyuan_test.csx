#!/usr/bin/env dotnet-script
// SiYuan Skill Comprehensive Test Script
// Usage: dotnet-script siyuan_test.csx [--no-cleanup]
//
// Creates a test notebook, runs all commands, verifies outputs, and cleans up.
// Use --no-cleanup to keep the test notebook after testing.

#nullable enable

using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Diagnostics;

// ── Config ──────────────────────────────────────────────

var baseUrl = Environment.GetEnvironmentVariable("SIYUAN_URL") ?? "http://127.0.0.1:6806";
var token = Environment.GetEnvironmentVariable("SIYUAN_TOKEN") ?? "";

var noCleanup = Args.Contains("--no-cleanup");

var http = new HttpClient();
http.BaseAddress = new Uri(baseUrl);
http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", token);
http.Timeout = TimeSpan.FromSeconds(30);

// ── Helpers ──────────────────────────────────────────────

int passed = 0, failed = 0, skipped = 0;

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
        throw new HttpRequestException($"API error [{code}]: {msg}");
    }
    return result.GetProperty("data");
}

void Assert(bool condition, string testName, string? detail = null)
{
    if (condition)
    {
        Console.WriteLine($"  ✅ {testName}");
        passed++;
    }
    else
    {
        Console.WriteLine($"  ❌ {testName}{(detail != null ? $" — {detail}" : "")}");
        failed++;
    }
}

void Skip(string testName, string reason)
{
    Console.WriteLine($"  ⏭ {testName} (skipped: {reason})");
    skipped++;
}

void Section(string name) => Console.WriteLine($"\n── {name} ──");

string RandomName(string prefix) => $"{prefix}-{DateTime.Now:yyyyMMddHHmmss}";

// ── Main ────────────────────────────────────────────────

Console.WriteLine("SiYuan Skill — Comprehensive Test Suite");
Console.WriteLine($"Target: {baseUrl}");
Console.WriteLine($"Cleanup: {(noCleanup ? "disabled" : "enabled")}");
Console.WriteLine();

string? testNotebookId = null;
string? testNotebookName = null;
string? testDocId = null;

try
{
    // ── Notebook Operations ──────────────────────────────
    Section("Notebook Operations");

    // create-nb
    testNotebookName = RandomName("自动化测试");
    var nbResult = await Api("/notebook/createNotebook", new { name = testNotebookName });
    testNotebookId = nbResult.GetProperty("notebook").GetProperty("id").GetString();
    Assert(testNotebookId != null && testNotebookId.Length > 0, "create-nb returns notebook ID");
    Console.WriteLine($"    Created notebook: {testNotebookName} ({testNotebookId})");

    // ls (verify notebook appears)
    var lsData = await Api("/notebook/lsNotebooks");
    var nbFound = lsData.GetProperty("notebooks").EnumerateArray()
        .Any(nb => nb.GetProperty("id").GetString() == testNotebookId);
    Assert(nbFound, "ls shows new notebook");

    // get-nb-conf
    var confData = await Api("/notebook/getNotebookConf", new { notebook = testNotebookId });
    var confName = confData.GetProperty("conf").GetProperty("name").GetString();
    Assert(confName == testNotebookName, "get-nb-conf returns correct name");

    // rename-nb
    var newNbName = testNotebookName + "-重命名";
    await Api("/notebook/renameNotebook", new { notebook = testNotebookId, name = newNbName });
    var confData2 = await Api("/notebook/getNotebookConf", new { notebook = testNotebookId });
    var renamedName = confData2.GetProperty("conf").GetProperty("name").GetString();
    Assert(renamedName == newNbName, "rename-nb updates name");
    testNotebookName = newNbName;

    // set-nb-conf (icon)
    var newConf = JsonSerializer.Deserialize<JsonElement>(confData2.GetProperty("conf").GetRawText());
    var confDict = new Dictionary<string, object>();
    foreach (var prop in newConf.EnumerateObject())
    {
        confDict[prop.Name] = prop.Value.ValueKind switch
        {
            JsonValueKind.String => prop.Value.GetString()!,
            JsonValueKind.Number => prop.Value.GetDouble(),
            JsonValueKind.True or JsonValueKind.False => prop.Value.GetBoolean(),
            _ => prop.Value.GetRawText()
        };
    }
    confDict["icon"] = "📝";
    await Api("/notebook/setNotebookConf", new { notebook = testNotebookId, conf = confDict });
    var confData3 = await Api("/notebook/getNotebookConf", new { notebook = testNotebookId });
    var icon = confData3.GetProperty("conf").GetProperty("icon").GetString();
    Assert(icon == "📝", "set-nb-conf updates icon");

    // close-nb
    await Api("/notebook/closeNotebook", new { notebook = testNotebookId });
    var confData4 = await Api("/notebook/getNotebookConf", new { notebook = testNotebookId });
    var closed = confData4.GetProperty("conf").GetProperty("closed").GetBoolean();
    Assert(closed, "close-nb closes notebook");

    // open-nb
    await Api("/notebook/openNotebook", new { notebook = testNotebookId });
    var confData5 = await Api("/notebook/getNotebookConf", new { notebook = testNotebookId });
    var opened = !confData5.GetProperty("conf").GetProperty("closed").GetBoolean();
    Assert(opened, "open-nb opens notebook");

    // ── Document Operations ────────────────────────────────
    Section("Document Operations");

    // create doc with markdown via createDocWithMd
    var mdContent = "# 测试文档\n\n这是测试内容。\n\n## 小节\n\n更多内容。";
    var createResult = await Api("/filetree/createDocWithMd", new
    {
        notebook = testNotebookId,
        path = "/测试文档",
        markdown = mdContent
    });
    // createDocWithMd returns data as a plain string (the new doc ID), not an object
    testDocId = createResult.ValueKind == JsonValueKind.String
        ? createResult.GetString()
        : createResult.GetProperty("id").GetString();
    Assert(testDocId != null && testDocId.Length > 0, "create creates doc and returns ID");
    Console.WriteLine($"    Created doc: {testDocId}");

    // cat (exportMdContent)
    var catData = await Api("/export/exportMdContent", new { id = testDocId });
    var catContent = catData.GetProperty("content").GetString() ?? "";
    Assert(catContent.Contains("测试文档"), "cat returns markdown with title");

    // info (getBlockInfo)
    var infoData = await Api("/block/getBlockInfo", new { id = testDocId });
    var rootTitle = infoData.GetProperty("rootTitle").GetString();
    Assert(rootTitle == "测试文档", "info returns correct rootTitle");

    // get-doc (getDoc JSON)
    var docData = await Api("/filetree/getDoc", new { id = testDocId });
    Assert(docData.TryGetProperty("content", out _), "get-doc returns content field");
    Assert(docData.TryGetProperty("blockCount", out _), "get-doc returns blockCount");

    // update-md
    var updatedMd = "# 更新后的文档\n\n新内容。\n\n## 新小节\n\n更多内容。";
    await Api("/block/updateBlock", new { id = testDocId, data = updatedMd, dataType = "markdown" });
    var catData2 = await Api("/export/exportMdContent", new { id = testDocId });
    var updatedContent = catData2.GetProperty("content").GetString() ?? "";
    Assert(updatedContent.Contains("更新后的文档"), "update-md changes content");

    // rename (filetree renameDocByID)
    await Api("/filetree/renameDocByID", new { id = testDocId, title = "重命名文档" });
    var catData3 = await Api("/export/exportMdContent", new { id = testDocId });
    var renamedContent = catData3.GetProperty("content").GetString() ?? "";
    Assert(renamedContent.Contains("重命名文档"), "rename updates title");

    // path (getHPathByID)
    var pathData = await Api("/filetree/getHPathByID", new { id = testDocId });
    var hPath = pathData.GetString() ?? "";
    Assert(hPath.Contains("重命名文档"), "path returns h-path");

    // full-path (getFullHPathByID)
    var fullPathData = await Api("/filetree/getFullHPathByID", new { id = testDocId });
    var fullHPath = fullPathData.GetString() ?? "";
    Assert(fullHPath.Contains("重命名文档"), "full-path returns full h-path");

    // breadcrumb
    var bcData = await Api("/block/getBlockBreadcrumb", new { id = testDocId });
    Assert(bcData.ValueKind == JsonValueKind.Array, "breadcrumb returns array");

    // outline
    var outlineData = await Api("/outline/getDocOutline", new { id = testDocId });
    // outline may be empty for docs without headings, but should not error
    Assert(outlineData.ValueKind == JsonValueKind.Array || outlineData.ValueKind == JsonValueKind.Null,
        "outline returns array or null");

    // tree
    var treeData = await Api("/filetree/listDocTree", new { notebook = testNotebookId, path = "/" });
    Assert(treeData.TryGetProperty("tree", out _) || treeData.ValueKind == JsonValueKind.Array,
        "tree returns valid response");

    // docs
    var docsData = await Api("/filetree/listDocsByPath", new { notebook = testNotebookId, path = "/", maxListCount = 100 });
    Assert(docsData.TryGetProperty("files", out _), "docs returns files array");

    // search-docs
    var searchDocsData = await Api("/filetree/searchDocs", new { k = "重命名" });
    Assert(searchDocsData.ValueKind == JsonValueKind.Array, "search-docs returns array");

    // search (full-text)
    var searchData = await Api("/search/fullTextSearchBlock", new
    {
        query = "新内容",
        method = 0,
        page = 1,
        pageSize = 32,
        orderBy = 0,
        groupBy = 1
    });
    var matchCount = searchData.GetProperty("matchedBlockCount").GetInt32();
    Assert(matchCount >= 0, "search returns matchedBlockCount");

    // tags
    var tagsData = await Api("/search/searchTag", new { k = "" });
    Assert(tagsData.ValueKind == JsonValueKind.Object || tagsData.ValueKind == JsonValueKind.Array,
        "tags returns valid response");

    // attrs (getBlockAttrs)
    var attrsData = await Api("/attr/getBlockAttrs", new { id = testDocId });
    Assert(attrsData.TryGetProperty("id", out _), "attrs returns id field");

    // set-attrs
    await Api("/attr/setBlockAttrs", new
    {
        id = testDocId,
        attrs = new Dictionary<string, string> { ["custom-test"] = "test-value" }
    });
    var attrsData2 = await Api("/attr/getBlockAttrs", new { id = testDocId });
    var testVal = attrsData2.TryGetProperty("custom-test", out var tv) ? tv.GetString() : null;
    Assert(testVal == "test-value", "set-attrs sets custom attribute");

    // duplicate
    var dupData = await Api("/filetree/duplicateDoc", new { id = testDocId });
    // duplicateDoc returns {id: "new-id", ...} or just the id string
    var dupId = dupData.ValueKind == JsonValueKind.String
        ? dupData.GetString()
        : dupData.TryGetProperty("id", out var did) ? did.GetString() : null;
    Assert(dupId != null && dupId != testDocId, "duplicate creates new doc with different ID");
    Console.WriteLine($"    Duplicated doc: {dupId}");

    // rm duplicate
    await Api("/filetree/removeDocByID", new { id = dupId });
    var lsAfterDup = await Api("/notebook/lsNotebooks");
    // verify duplicate is gone

    // recent
    var recentData = await Api("/block/getRecentUpdatedBlocks");
    Assert(recentData.ValueKind == JsonValueKind.Array, "recent returns array");

    // ── Block Operations ─────────────────────────────────
    Section("Block Operations");

    // insert-block
    var insertResult = await Api("/block/insertBlock", new
    {
        data = "**插入的文本**",
        dataType = "markdown",
        parentID = testDocId
    });
    var insertedOps = insertResult.EnumerateArray().FirstOrDefault()
        .GetProperty("doOperations").EnumerateArray();
    var insertedBlockId = insertedOps.FirstOrDefault().GetProperty("id").GetString();
    Assert(insertedBlockId != null, "insert-block returns operation with block ID");

    // insert another block for move-block test
    var insertResult2 = await Api("/block/insertBlock", new
    {
        data = "第二个块",
        dataType = "markdown",
        parentID = testDocId
    });

    // getChildBlocks
    var childrenData = await Api("/block/getChildBlocks", new { id = testDocId });
    Assert(childrenData.ValueKind == JsonValueKind.Array, "getChildBlocks returns array");

    // getRefIDs
    var refData = await Api("/block/getRefIDs", new { id = testDocId });
    Assert(refData.ValueKind == JsonValueKind.Object, "getRefIDs returns object with refDefs/originalRefBlockIDs");

    // getBlockIndex
    var indexData = await Api("/block/getBlockIndex", new { id = testDocId });
    var indexVal = indexData.ValueKind == JsonValueKind.Number ? indexData.GetInt32() : -1;
    Assert(indexVal >= 0, "getBlockIndex returns valid index");

    // checkBlockExist
    var existData = await Api("/block/checkBlockExist", new { id = testDocId });
    var exists = existData.ValueKind == JsonValueKind.True;
    Assert(exists, "checkBlockExist returns true for existing block");

    // move-block
    if (insertedBlockId != null)
    {
        var moveResult = await Api("/block/moveBlock", new
        {
            id = insertedBlockId,
            parentID = testDocId
        });
        Assert(true, "move-block executes without error");
    }
    else
    {
        Skip("move-block", "no inserted block ID available");
    }

    // backlinks (ref/getBacklink2)
    var backlinkData = await Api("/ref/getBacklink2", new { id = testDocId });
    Assert(backlinkData.ValueKind == JsonValueKind.Array || backlinkData.ValueKind == JsonValueKind.Null,
        "backlinks returns array or null");

    // ── SQL ──────────────────────────────────────────────
    Section("SQL Operations");

    var sqlData = await Api("/query/sql", new { stmt = $"SELECT id, type, box FROM blocks WHERE box='{testNotebookId}' LIMIT 5" });
    Assert(sqlData.ValueKind == JsonValueKind.Array, "sql returns array");

    // ── Export ───────────────────────────────────────────
    Section("Export Operations");

    var exportMdData = await Api("/export/exportMd", new { id = testDocId });
    Assert(exportMdData.TryGetProperty("name", out _), "export md returns name field");

    var exportSyData = await Api("/export/exportSY", new { id = testDocId });
    Assert(exportSyData.TryGetProperty("zip", out _), "export sy returns zip field");

    // ── Templates ────────────────────────────────────────
    Section("Template Operations");

    var tmplData = await Api("/search/searchTemplate", new { k = "" });
    Assert(tmplData.ValueKind == JsonValueKind.Object || tmplData.ValueKind == JsonValueKind.Array,
        "templates returns valid response");

    // ── Cleanup ────────────────────────────────────────
    Section("Cleanup");

    if (!noCleanup && testNotebookId != null)
    {
        // close first
        await Api("/notebook/closeNotebook", new { notebook = testNotebookId });
        // remove
        await Api("/notebook/removeNotebook", new { notebook = testNotebookId });
        Console.WriteLine($"    Removed notebook: {testNotebookId}");

        // verify gone
        var lsFinal = await Api("/notebook/lsNotebooks");
        var nbGone = !lsFinal.GetProperty("notebooks").EnumerateArray()
            .Any(nb => nb.GetProperty("id").GetString() == testNotebookId);
        Assert(nbGone, "notebook removed from ls");
    }
    else if (testNotebookId != null)
    {
        Console.WriteLine($"    Kept notebook: {testNotebookId} (--no-cleanup)");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ Test suite error: {ex.Message}");
    if (testNotebookId != null && !noCleanup)
    {
        Console.WriteLine("    Attempting cleanup...");
        try
        {
            await Api("/notebook/closeNotebook", new { notebook = testNotebookId });
            await Api("/notebook/removeNotebook", new { notebook = testNotebookId });
        }
        catch { /* ignore cleanup errors */ }
    }
    Environment.Exit(1);
}

// ── Summary ──────────────────────────────────────────────

Console.WriteLine($"\n══════════════════════════════════════");
Console.WriteLine($"Results: {passed} passed, {failed} failed, {skipped} skipped");
if (failed > 0) Environment.Exit(1);

#!/usr/bin/env dotnet
using System.Text.RegularExpressions;

var exitCode = Run(args);
return exitCode;

static int Run(string[] args)
{
    if (args.Length == 0)
    {
        return Fail("scaffold requires a skill name");
    }

    var nameParts = new List<string>();
    var outputPath = GetDefaultOutputPath();
    string? resourcesValue = null;
    var force = false;

    for (var i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--path" when i + 1 < args.Length:
                outputPath = Path.GetFullPath(args[++i]);
                break;
            case "--resources" when i + 1 < args.Length:
                resourcesValue = args[++i];
                break;
            case "--force":
                force = true;
                break;
            case var option when option.StartsWith("--", StringComparison.Ordinal):
                return Fail($"unknown option: {option}");
            default:
                nameParts.Add(args[i]);
                break;
        }
    }

    if (nameParts.Count == 0)
    {
        return Fail("scaffold requires a skill name");
    }

    try
    {
        var resources = ParseResources(resourcesValue);
        var skillPath = CreateSkill(string.Join(' ', nameParts), outputPath, resources, force);
        Console.WriteLine($"Created skill at {skillPath}");
        Console.WriteLine("Next: edit SKILL.md, replace TODOs, then run validate.cs.");
        return 0;
    }
    catch (Exception ex)
    {
        return Fail(ex.Message);
    }
}

static string GetDefaultOutputPath()
{
    var codexHome = Environment.GetEnvironmentVariable("CODEX_HOME");
    if (!string.IsNullOrWhiteSpace(codexHome))
    {
        return Path.Combine(codexHome, "skills");
    }

    var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    return Path.Combine(home, ".codex", "skills");
}

static IReadOnlyList<string> ParseResources(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return [];
    }

    var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "scripts", "references", "assets" };
    var resources = value
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Select(x => x.ToLowerInvariant())
        .ToList();

    var unknown = resources.Where(x => !allowed.Contains(x)).OrderBy(x => x).ToList();
    if (unknown.Count > 0)
    {
        throw new ArgumentException(
            $"unknown resource folder(s): {string.Join(", ", unknown)}; expected scripts,references,assets");
    }

    return resources;
}

static string CreateSkill(string name, string outputPath, IReadOnlyList<string> resources, bool force)
{
    var normalized = NormalizeName(name);
    if (string.IsNullOrWhiteSpace(normalized) || !Regex.IsMatch(normalized, "^[a-z0-9]+(?:-[a-z0-9]+)*$"))
    {
        throw new ArgumentException("skill name must contain lowercase letters, numbers, and hyphens");
    }

    if (normalized.Length > 64)
    {
        throw new ArgumentException("skill name must be 64 characters or fewer");
    }

    var skillPath = Path.GetFullPath(Path.Combine(outputPath, normalized));
    if (Directory.Exists(skillPath) && !force)
    {
        throw new IOException($"{skillPath} already exists; pass --force to add missing template files");
    }

    Directory.CreateDirectory(skillPath);

    var skillMarkdownPath = Path.Combine(skillPath, "SKILL.md");
    if (force || !File.Exists(skillMarkdownPath))
    {
        File.WriteAllText(skillMarkdownPath, BuildSkillMarkdown(normalized));
    }

    foreach (var resource in resources)
    {
        var folder = Path.Combine(skillPath, resource);
        Directory.CreateDirectory(folder);
    }

    return skillPath;
}

static string NormalizeName(string value)
{
    var name = value.Trim().ToLowerInvariant();
    name = Regex.Replace(name, "[^a-z0-9]+", "-");
    name = Regex.Replace(name, "-+", "-").Trim('-');
    return name;
}

static string BuildSkillMarkdown(string normalized)
{
    var title = string.Join(' ',
        normalized.Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));

    return $"""
    ---
    name: {normalized}
    description: Do [specific job] for [domain/files/system]. Use when the user asks to [task A], [task B], [task C], mentions [tool/file type/domain phrase], or needs [specialized outcome].
    ---

    # {title}

    Replace this sentence with the skill's purpose and primary interface/tool, if there is one.

    ## Start Here

    1. Identify the task and required inputs.
    2. Use the common command/procedure below for the main path.
    3. Read a reference only when the task matches its documented scope.
    4. Validate before handing off.

    ## Common Commands Or Procedure

    Keep the commands, arguments, stdin/stdout conventions, or exact steps needed for normal use here.

    - `[exact command or step]`: when to use it, required args, important output.
    - `[exact command or step]`: when to use it, required args, important output.

    ## Workflow

    1. Capture inputs and constraints.
    2. Run the exact command/procedure or read the correctly scoped reference.
    3. Produce the expected output.
    4. Validate the result.

    ## Resources

    List bundled resources and when to use them. If both a bundled helper and an external API exist, distinguish their scopes.

    - `scripts/...`: run when ...
    - `references/...`: read when ...; scope is ...
    - `assets/...`: use when ...

    ## Safety

    - List destructive operations, credentials, live systems, or user confirmation requirements.
    - Prefer safe/read-only exploration before writes when applicable.

    ## Validation

    - Run the lightest smoke test that proves the main path works.
    - Run heavier integration tests only when the required live systems or fixtures are available.

    If this skill needs helper scripts, default them to C# file-based apps (`.cs` single-file programs run with `dotnet run --file ...`) unless there is a strong reason to use another runtime.
    """;
}

static int Fail(string message)
{
    Console.Error.WriteLine($"Error: {message}");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("  dotnet run --file <this-skill>/scripts/scaffold.cs -- <skill name> [--path <dir>] [--resources scripts,references,assets] [--force]");
    return 1;
}

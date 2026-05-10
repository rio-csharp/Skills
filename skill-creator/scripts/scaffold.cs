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
        var gitKeep = Path.Combine(folder, ".gitkeep");
        if (force || !File.Exists(gitKeep))
        {
            File.WriteAllText(gitKeep, string.Empty);
        }
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
    description: TODO: Describe what this skill does and the concrete situations that should trigger it.
    ---

    # {title}

    Use this skill when TODO.

    ## Workflow

    1. TODO: Capture inputs and constraints.
    2. TODO: Run or read the relevant resources.
    3. TODO: Produce the expected output.
    4. TODO: Validate the result.

    ## Resources

    TODO: List bundled resources and when to use them.

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

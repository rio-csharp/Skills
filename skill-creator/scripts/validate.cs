#!/usr/bin/env dotnet
using System.Text.RegularExpressions;

var exitCode = Run(args);
return exitCode;

static int Run(string[] args)
{
    if (args.Length != 1)
    {
        return Fail("validate requires exactly one skill path");
    }

    var result = ValidateSkill(args[0]);
    PrintResult(result);
    return result.Ok ? 0 : 1;
}

static ValidationResult ValidateSkill(string skillPath)
{
    var path = Path.GetFullPath(skillPath);
    var result = new ValidationResult();
    var resourceDirectories = new HashSet<string>(StringComparer.Ordinal) { "scripts", "references", "assets" };
    var packageClutter = new HashSet<string>(StringComparer.Ordinal)
    {
        ".git", ".hg", ".svn", "__pycache__", "node_modules", ".venv", "venv", "dist", "build", "evals", "tmp", "temp"
    };
    var reservedNames = new[] { "anthropic", "claude" };
    var allowedFrontmatter = new HashSet<string>(StringComparer.Ordinal)
    {
        "name", "description", "allowed-tools", "disable-model-invocation", "user-invocable",
        "context", "agent", "paths", "license", "metadata", "compatibility", "shell", "model", "effort"
    };

    if (!Directory.Exists(path))
    {
        result.Errors.Add($"skill folder does not exist: {path}");
        return result;
    }

    var skillMarkdownPath = Path.Combine(path, "SKILL.md");
    if (!File.Exists(skillMarkdownPath))
    {
        result.Errors.Add("missing SKILL.md");
        return result;
    }

    Dictionary<string, object> frontmatter;
    string body;
    try
    {
        (frontmatter, body) = ReadFrontmatter(skillMarkdownPath);
    }
    catch (Exception ex)
    {
        result.Errors.Add(ex.Message);
        return result;
    }

    var unexpected = frontmatter.Keys.Except(allowedFrontmatter).OrderBy(x => x).ToList();
    if (unexpected.Count > 0)
    {
        result.Warnings.Add(
            $"unexpected frontmatter field(s): {string.Join(", ", unexpected)}; confirm the target platform supports them");
    }

    if (!frontmatter.TryGetValue("name", out var nameValue) ||
        nameValue is not string rawName ||
        string.IsNullOrWhiteSpace(rawName))
    {
        result.Errors.Add("frontmatter.name must be a non-empty string");
    }
    else
    {
        var name = rawName.Trim();
        if (!Regex.IsMatch(name, "^[a-z0-9]+(?:-[a-z0-9]+)*$"))
        {
            result.Errors.Add("frontmatter.name must use lowercase letters, numbers, and single hyphens");
        }

        if (name.Length > 64)
        {
            result.Errors.Add("frontmatter.name must be 64 characters or fewer");
        }

        var reservedHits = reservedNames.Where(name.Contains).OrderBy(x => x).ToList();
        if (reservedHits.Count > 0)
        {
            result.Errors.Add($"frontmatter.name must not contain reserved word(s): {string.Join(", ", reservedHits)}");
        }

        var folderName = new DirectoryInfo(path).Name;
        if (!string.Equals(name, folderName, StringComparison.Ordinal))
        {
            result.Warnings.Add($"frontmatter.name '{name}' does not match folder name '{folderName}'");
        }
    }

    if (!frontmatter.TryGetValue("description", out var descriptionValue) ||
        descriptionValue is not string rawDescription ||
        string.IsNullOrWhiteSpace(rawDescription))
    {
        result.Errors.Add("frontmatter.description must be a non-empty string");
    }
    else
    {
        var description = Regex.Replace(rawDescription, @"\s+", " ").Trim();
        if (description.Length > 1024)
        {
            result.Errors.Add("frontmatter.description must be 1024 characters or fewer");
        }

        if (description.Contains('<') || description.Contains('>'))
        {
            result.Errors.Add("frontmatter.description must not contain XML/HTML angle brackets");
        }

        if (description.Length < 50)
        {
            result.Warnings.Add("description is very short; include concrete trigger contexts");
        }

        var lowered = description.ToLowerInvariant();
        if (!lowered.Contains("use when", StringComparison.Ordinal) &&
            !lowered.Contains("use this skill", StringComparison.Ordinal))
        {
            result.Warnings.Add("description should explicitly say when to use the skill");
        }

        if (new[] { "anything", "everything", "all tasks", "all requests" }.Any(lowered.Contains))
        {
            result.Warnings.Add("description may be too broad; add boundaries or concrete triggers");
        }
    }

    if (string.IsNullOrWhiteSpace(body))
    {
        result.Errors.Add("SKILL.md body is empty");
    }

    if (body.Contains("TODO", StringComparison.Ordinal))
    {
        result.Warnings.Add("SKILL.md still contains TODO placeholders");
    }

    var lineCount = body.Split(["\r\n", "\n"], StringSplitOptions.None).Length;
    if (lineCount > 500)
    {
        result.Warnings.Add("SKILL.md body is over 500 lines; consider moving details to references/");
    }

    foreach (Match match in Regex.Matches(body, @"\[[^\]]+\]\(([^)]+)\)"))
    {
        var targetText = match.Groups[1].Value;
        if (targetText.Contains("://", StringComparison.Ordinal) ||
            targetText.StartsWith("#", StringComparison.Ordinal) ||
            targetText.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        var cleanTarget = targetText.Split('#')[0].Trim();
        if (string.IsNullOrEmpty(cleanTarget))
        {
            continue;
        }

        var fullTarget = Path.GetFullPath(Path.Combine(path, cleanTarget));
        if (!fullTarget.StartsWith(Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase))
        {
            result.Warnings.Add($"local link points outside the skill folder: {cleanTarget}");
            continue;
        }

        if (!File.Exists(fullTarget) && !Directory.Exists(fullTarget))
        {
            result.Errors.Add($"local link target does not exist: {cleanTarget}");
        }
    }

    foreach (var childDirectory in Directory.EnumerateDirectories(path))
    {
        var name = Path.GetFileName(childDirectory);
        if (resourceDirectories.Contains(name))
        {
            var visibleFiles = Directory.EnumerateFiles(childDirectory, "*", SearchOption.AllDirectories)
                .Where(file => !string.Equals(Path.GetFileName(file), ".gitkeep", StringComparison.Ordinal))
                .ToList();

            if (visibleFiles.Count == 0)
            {
                result.Warnings.Add($"{name}/ exists but has no useful files");
            }
        }
        else if (packageClutter.Contains(name))
        {
            result.Warnings.Add($"package clutter directory found: {name}/");
        }
    }

    foreach (var fileSystemEntry in Directory.EnumerateFileSystemEntries(path, "*", SearchOption.AllDirectories))
    {
        var relative = Path.GetRelativePath(path, fileSystemEntry);
        var parts = relative.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            if (packageClutter.Contains(part))
            {
                result.Warnings.Add($"package clutter appears under skill folder: {part}");
            }
        }
    }

    result.Warnings.Sort(StringComparer.Ordinal);
    return result;
}

static void PrintResult(ValidationResult result)
{
    Console.WriteLine(result.Ok ? "Skill validation passed." : "Skill validation failed.");

    foreach (var error in result.Errors)
    {
        Console.WriteLine($"ERROR: {error}");
    }

    foreach (var warning in result.Warnings)
    {
        Console.WriteLine($"WARNING: {warning}");
    }
}

static (Dictionary<string, object> Frontmatter, string Body) ReadFrontmatter(string skillMarkdownPath)
{
    var content = File.ReadAllText(skillMarkdownPath);
    if (!content.StartsWith("---", StringComparison.Ordinal))
    {
        throw new InvalidOperationException("SKILL.md must start with YAML frontmatter delimited by ---");
    }

    var match = Regex.Match(content, @"^---\r?\n(.*?)\r?\n---\r?\n?", RegexOptions.Singleline);
    if (!match.Success)
    {
        throw new InvalidOperationException("SKILL.md is missing closing frontmatter delimiter");
    }

    var rawYaml = match.Groups[1].Value.Trim('\n');
    var body = content[match.Length..].TrimStart('\r', '\n');
    return (ParseSimpleYaml(rawYaml), body);
}

static Dictionary<string, object> ParseSimpleYaml(string rawYaml)
{
    var data = new Dictionary<string, object>(StringComparer.Ordinal);
    var lines = rawYaml.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
    var index = 0;

    while (index < lines.Length)
    {
        var line = lines[index++];
        if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
        {
            continue;
        }

        if (line.StartsWith(' ') || line.StartsWith('\t'))
        {
            throw new InvalidOperationException($"unsupported indented frontmatter line: {line}");
        }

        var colonIndex = line.IndexOf(':');
        if (colonIndex < 0)
        {
            throw new InvalidOperationException($"frontmatter line must contain ':': {line}");
        }

        var key = line[..colonIndex].Trim();
        var value = line[(colonIndex + 1)..].Trim();
        if (string.IsNullOrEmpty(key))
        {
            throw new InvalidOperationException("frontmatter contains an empty key");
        }

        if (value is ">" or "|" or ">-" or "|-")
        {
            var blockLines = new List<string>();
            while (index < lines.Length)
            {
                var continuation = lines[index];
                if (!IsIndentedOrBlank(continuation))
                {
                    break;
                }

                index++;
                blockLines.Add(continuation.Trim());
            }

            data[key] = value.StartsWith("|", StringComparison.Ordinal)
                ? string.Join("\n", blockLines)
                : string.Join(" ", blockLines);
            continue;
        }

        if (string.IsNullOrEmpty(value) && index < lines.Length && IsIndented(lines[index]))
        {
            var nested = new Dictionary<string, string>(StringComparer.Ordinal);
            var nestedList = new List<string>();
            while (index < lines.Length && IsIndentedOrBlank(lines[index]))
            {
                var continuation = lines[index].Trim();
                index++;
                if (string.IsNullOrEmpty(continuation))
                {
                    continue;
                }

                if (continuation.StartsWith("- ", StringComparison.Ordinal))
                {
                    nestedList.Add(continuation[2..].Trim().Trim('"', '\''));
                    continue;
                }

                var nestedColonIndex = continuation.IndexOf(':');
                if (nestedColonIndex >= 0)
                {
                    var nestedKey = continuation[..nestedColonIndex].Trim();
                    var nestedValue = continuation[(nestedColonIndex + 1)..].Trim().Trim('"', '\'');
                    nested[nestedKey] = nestedValue;
                }
            }

            data[key] = nested.Count > 0 ? nested : nestedList;
            continue;
        }

        if (value.StartsWith("[", StringComparison.Ordinal) && value.EndsWith("]", StringComparison.Ordinal))
        {
            var inner = value[1..^1];
            var items = inner.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => x.Trim('"', '\''))
                .ToList();
            data[key] = items;
            continue;
        }

        data[key] = string.IsNullOrEmpty(value) ? string.Empty : value.Trim('"', '\'');
    }

    return data;
}

static bool IsIndented(string line) =>
    line.StartsWith(' ') || line.StartsWith('\t');

static bool IsIndentedOrBlank(string line) =>
    string.IsNullOrWhiteSpace(line) || IsIndented(line);

static int Fail(string message)
{
    Console.Error.WriteLine($"Error: {message}");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("  dotnet run --file <this-skill>/scripts/validate.cs -- <path-to-skill-folder>");
    return 1;
}

sealed class ValidationResult
{
    public List<string> Errors { get; } = [];
    public List<string> Warnings { get; } = [];
    public bool Ok => Errors.Count == 0;
}

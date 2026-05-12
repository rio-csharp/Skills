#!/usr/bin/env dotnet
#:package DocumentFormat.OpenXml@3.3.0

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

var exitCode = new DocxTool().Run(args);
return exitCode;

class DocxTool
{
    public int Run(string[] args)
    {
        if (args.Length == 0) return Fail("No command. Try: docx read --input file.docx");
        var cmd = args[0].ToLowerInvariant();
        var rest = args.Skip(1).ToArray();

        return cmd switch
        {
            "read" => CmdRead(rest),
            "create" => CmdCreate(rest),
            "modify" => CmdModify(rest),
            "convert" => CmdConvert(rest),
            _ => Fail($"Unknown command: {cmd}")
        };
    }

    int CmdRead(string[] args)
    {
        var input = Arg("--input", "-i", args) ?? Positional(args);
        if (string.IsNullOrEmpty(input)) return Fail("read: --input required");

        if (!File.Exists(input)) return Fail($"File not found: {input}");

        using var doc = WordprocessingDocument.Open(input, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null) return Fail("Invalid docx: no body found");

        var props = doc.PackageProperties;
        Console.WriteLine("file: " + Path.GetFullPath(input));
        Console.WriteLine("title: " + (props.Title ?? ""));
        Console.WriteLine("author: " + (props.Creator ?? ""));
        Console.WriteLine("subject: " + (props.Subject ?? ""));
        Console.WriteLine("keywords: " + (props.Keywords ?? ""));
        Console.WriteLine("created: " + (props.Created?.ToString() ?? ""));
        Console.WriteLine("modified: " + (props.Modified?.ToString() ?? ""));

        var text = string.Join(Environment.NewLine + Environment.NewLine, body.Elements<Paragraph>().Select(p => p.InnerText));
        Console.WriteLine();
        Console.WriteLine("=== Content ===");
        Console.WriteLine(text);
        return 0;
    }

    int CmdCreate(string[] args)
    {
        var output = Arg("--output", "-o", args);
        if (string.IsNullOrEmpty(output)) return Fail("create: --output required");
        var content = Arg("--content", "-c", args);

        var doc = WordprocessingDocument.Create(output, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document();
        var body = mainPart.Document.AppendChild(new Body());

        var lines = (content ?? "").Split('\n', StringSplitOptions.None);
        foreach (var line in lines)
        {
            var para = body.AppendChild(new Paragraph());
            var run = para.AppendChild(new Run());
            run.AppendChild(new Text(line));
        }

        doc.Save();
        Console.WriteLine("Created: " + Path.GetFullPath(output));
        return 0;
    }

    int CmdModify(string[] args)
    {
        var input = Arg("--input", "-i", args) ?? Positional(args);
        if (string.IsNullOrEmpty(input)) return Fail("modify: --input required");
        var output = Arg("--output", "-o", args);
        var content = Arg("--content", "-c", args);
        var appendText = Arg("--append", "", args) ?? content ?? "";

        if (!File.Exists(input)) return Fail($"File not found: {input}");

        // Copy to output first, or modify in place
        var targetPath = output ?? input;
        if (output != null && output != input)
        {
            File.Copy(input, output, true);
            input = output;
        }

        using var doc = WordprocessingDocument.Open(input, true);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null) return Fail("Invalid docx: no body found");

        var para = body.AppendChild(new Paragraph());
        var run = para.AppendChild(new Run());
        run.AppendChild(new Text(appendText));

        doc.Save();
        Console.WriteLine("Modified: " + Path.GetFullPath(targetPath));
        return 0;
    }

    int CmdConvert(string[] args)
    {
        var input = Arg("--input", "-i", args) ?? Positional(args);
        if (string.IsNullOrEmpty(input)) return Fail("convert: --input required");
        var output = Arg("--output", "-o", args);
        var format = Arg("--format", "-f", args)?.ToLowerInvariant() ?? "txt";

        if (!File.Exists(input)) return Fail($"File not found: {input}");

        using var doc = WordprocessingDocument.Open(input, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null) return Fail("Invalid docx: no body found");

        var text = string.Join(Environment.NewLine + Environment.NewLine, body.Elements<Paragraph>().Select(p => p.InnerText));

        if (format == "txt")
        {
            var outPath = output ?? Path.ChangeExtension(input, ".txt");
            File.WriteAllText(outPath, text);
            Console.WriteLine("Converted to TXT: " + Path.GetFullPath(outPath));
        }
        else if (format == "html")
        {
            var outPath = output ?? Path.ChangeExtension(input, ".html");
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html><head><meta charset=\"utf-8\"><title>");
            sb.Append(doc.PackageProperties.Title ?? "Document");
            sb.AppendLine("</title></head><body>");
            sb.AppendLine("<pre>");
            sb.Append(System.Net.WebUtility.HtmlEncode(text));
            sb.AppendLine("</pre>");
            sb.AppendLine("</body></html>");
            File.WriteAllText(outPath, sb.ToString());
            Console.WriteLine("Converted to HTML: " + Path.GetFullPath(outPath));
        }
        else if (format == "pdf")
        {
            return Fail("convert --format pdf: requires additional PDF rendering library not bundled in this helper. Use the pdf skill instead for PDF conversion from docx source.");
        }
        else
        {
            return Fail($"convert --format: unknown format '{format}'. Use txt, html, or pdf (requires external tool).");
        }
        return 0;
    }

    string? Arg(string longForm, string shortForm, string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == longForm || (!string.IsNullOrEmpty(shortForm) && args[i] == shortForm))
                return i + 1 < args.Length ? args[i + 1] : null;
        }
        return null;
    }

    string? Positional(string[] args)
    {
        var optionsWithValues = new HashSet<string> { "--input", "-i", "--output", "-o", "--content", "-c", "--append", "--format", "-f" };
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (optionsWithValues.Contains(arg))
            {
                i++;
                continue;
            }
            if (!arg.StartsWith("-")) return arg;
        }
        return null;
    }

    int Fail(string msg)
    {
        Console.Error.WriteLine("ERROR: " + msg);
        Console.Error.WriteLine();
        Console.Error.WriteLine("Usage: docx <command> [options]");
        Console.Error.WriteLine("Commands: read, create, modify, convert");
        return 1;
    }
}

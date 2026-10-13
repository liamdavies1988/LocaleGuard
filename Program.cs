using Spectre.Console;
using System.Drawing;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LocaleGuard;

/// <summary>
/// Console utility that compares two locale JSON files (source and target)
/// to ensure keys, translations and placeholder variables are consistent.
/// </summary>
public class Program
{
    /// <summary>
    /// Entry point. Compares two locale files and returns 0 on success, 1 on failure.
    /// </summary>
    static int Main(string[] args)
    {
        // Default paths if running straight from Visual Studio (F5)
        string sourcePath = args.Length > 0
            ? args[0]
            : Path.Combine(AppContext.BaseDirectory, "locales", "en.json");

        string targetPath = args.Length > 1
            ? args[1]
            : Path.Combine(AppContext.BaseDirectory, "locales", "cy.json");

        AnsiConsole.MarkupLine($"[cyan]Inspecting parity between:[/] [bold]{Path.GetFileName(sourcePath)}[/] and [bold]{Path.GetFileName(targetPath)}[/]\n");

        // Ensure both files exist before attempting to parse
        if (!File.Exists(sourcePath) || !File.Exists(targetPath))
        {
            AnsiConsole.MarkupLine("[bold red]Error:[/] One or both localization files could not be found.");
            return 1;
        }

        Dictionary<string, string> sourceData;
        Dictionary<string, string> targetData;

        // Load and flatten JSON into simple key->value dictionaries
        try
        {
            sourceData = LoadAndFlattenJson(sourcePath);
            targetData = LoadAndFlattenJson(targetPath);
        }
        catch (Exception ex)
        {
            // JSON parsing or file encoding issues
            AnsiConsole.MarkupLine($"[bold red]JSON/Encoding Error:[/] {ex.Message}");
            return 1;
        }

        // Collect any localization issues found
        var errors = new List<(string Key, string Issue, string Details)>();

        // Iterate every key in the source and validate the target
        foreach (var (key, sourceVal) in sourceData)
        {
            // Check 1: Missing Key in target
            if (!targetData.TryGetValue(key, out var targetVal))
            {
                errors.Add((key, "MISSING_KEY", "Key is present in source but missing in target"));
                continue;
            }

            // Normalize whitespace for comparison checks
            targetVal = targetVal.Trim();

            // Check 2: Empty Value in target
            if (string.IsNullOrEmpty(targetVal))
            {
                errors.Add((key, "EMPTY_VALUE", "Target value is empty"));
            }
            // Check 3: Untranslated long strings (simple heuristic)
            else if (targetVal.Equals(sourceVal, StringComparison.OrdinalIgnoreCase) && targetVal.Split(' ').Length > 2)
            {
                errors.Add((key, "UNTRANSLATED", "Target value appears identical to English source"));
            }

            // Check 4: Placeholder variable mismatches (e.g. {username} vs {user_name})
            var sourceVars = ExtractPlaceholders(sourceVal);
            var targetVars = ExtractPlaceholders(targetVal);

            // Compare as sets so order differences don't raise false positives
            if (!sourceVars.SetEquals(targetVars))
            {
                errors.Add((key, "VARIABLE_MISMATCH", $"Expected {{{string.Join(", ", sourceVars)}}}, found {{{string.Join(", ", targetVars)}}}"));
            }
        }

        // If we found issues, render a table and exit with failure code for CI pipelines
        if (errors.Count > 0)
        {
            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Spectre.Console.Color.Red);

            table.AddColumn(new TableColumn("[bold white]Key[/]"));
            table.AddColumn(new TableColumn("[bold yellow]Issue Type[/]"));
            table.AddColumn(new TableColumn("[bold dim]Details[/]"));

            foreach (var (key, issue, details) in errors)
            {
                table.AddRow($"[white]{key}[/]", $"[yellow]{issue}[/]", $"[grey]{details}[/]");
            }

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"\n[bold red]FAILED:[/] Found {errors.Count} localization issue(s).\n");
            return 1; // Exit code 1 signals failure in CI/CD
        }

        // No issues found
        AnsiConsole.MarkupLine("[bold green]PASSED:[/] All localization keys and placeholders match cleanly!\n");
        return 0;
    }

    /// <summary>
    /// Reads JSON from <paramref name="filePath"/> and flattens nested objects into a dictionary
    /// using dotted paths (e.g., "footer.terms" => "Terms and conditions").
    /// </summary>
    private static Dictionary<string, string> LoadAndFlattenJson(string filePath)
    {
        string json = File.ReadAllText(filePath);
        using JsonDocument doc = JsonDocument.Parse(json);

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        FlattenElement(doc.RootElement, string.Empty, result);
        return result;
    }

    /// <summary>
    /// Recursively traverses a <see cref="JsonElement"/> and populates <paramref name="result"/>
    /// with dotted keys for nested objects.
    /// </summary>
    private static void FlattenElement(JsonElement element, string prefix, Dictionary<string, string> result)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                // Recurse into each property and build dotted key paths
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    string key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
                    FlattenElement(property.Value, key, result);
                }
                break;

            default:
                // For non-object values, store the string representation
                result[prefix] = element.ToString();
                break;
        }
    }

    /// <summary>
    /// Finds placeholder names inside curly braces in the provided text.
    /// Example: "Hello {username}" -> returns set containing "username".
    /// </summary>
    public static HashSet<string> ExtractPlaceholders(string text)
    {
        // Matches the innards of {...}, avoiding nested braces
        var matches = Regex.Matches(text ?? string.Empty, @"\{([^{}]+)\}");

        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match m in matches)
        {
            set.Add(m.Groups[1].Value);
        }

        return set;
    }
}
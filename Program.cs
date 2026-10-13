using Spectre.Console;
using System.Drawing;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LocaleGuard;

public class Program
{
    static int Main(string[] args)
    {
        // Default paths if running straight from Visual Studio (F5)
        string sourcePath = args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory, "locales", "en.json");
        string targetPath = args.Length > 1 ? args[1] : Path.Combine(AppContext.BaseDirectory, "locales", "cy.json");

        AnsiConsole.MarkupLine($"[cyan]Inspecting parity between:[/] [bold]{Path.GetFileName(sourcePath)}[/] and [bold]{Path.GetFileName(targetPath)}[/]\n");

        if (!File.Exists(sourcePath) || !File.Exists(targetPath))
        {
            AnsiConsole.MarkupLine("[bold red]Error:[/] One or both localization files could not be found.");
            return 1;
        }

        Dictionary<string, string> sourceData;
        Dictionary<string, string> targetData;

        try
        {
            sourceData = LoadAndFlattenJson(sourcePath);
            targetData = LoadAndFlattenJson(targetPath);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[bold red]JSON/Encoding Error:[/] {ex.Message}");
            return 1;
        }

        var errors = new List<(string Key, string Issue, string Details)>();

        foreach (var (key, sourceVal) in sourceData)
        {
            // Check 1: Missing Key
            if (!targetData.TryGetValue(key, out var targetVal))
            {
                errors.Add((key, "MISSING_KEY", "Key is present in source but missing in target"));
                continue;
            }

            targetVal = targetVal.Trim();

            // Check 2: Empty Value
            if (string.IsNullOrEmpty(targetVal))
            {
                errors.Add((key, "EMPTY_VALUE", "Target value is empty"));
            }
            // Check 3: Identical English copy for long strings
            else if (targetVal.Equals(sourceVal, StringComparison.OrdinalIgnoreCase) && targetVal.Split(' ').Length > 2)
            {
                errors.Add((key, "UNTRANSLATED", "Target value appears identical to English source"));
            }

            // Check 4: Placeholder Variable Mismatches (e.g. {username} vs {user_name})
            var sourceVars = ExtractPlaceholders(sourceVal);
            var targetVars = ExtractPlaceholders(targetVal);

            if (!sourceVars.SetEquals(targetVars))
            {
                errors.Add((key, "VARIABLE_MISMATCH", $"Expected {{{string.Join(", ", sourceVars)}}}, found {{{string.Join(", ", targetVars)}}}"));
            }
        }

        // Render Results Table
        if (errors.Count > 0)
        {
            var table = new Table().Border(TableBorder.Rounded).BorderColor(Spectre.Console.Color.Red);
            table.AddColumn(new TableColumn("[bold white]Key[/]"));
            table.AddColumn(new TableColumn("[bold yellow]Issue Type[/]"));
            table.AddColumn(new TableColumn("[bold dim]Details[/]"));

            foreach (var (key, issue, details) in errors)
            {
                table.AddRow($"[white]{key}[/]", $"[yellow]{issue}[/]", $"[grey]{details}[/]");
            }

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"\n[bold red]FAILED:[/] Found {errors.Count} localization issue(s).\n");
            return 1; // Exit code 1 flags a failed build in CI/CD pipelines
        }

        AnsiConsole.MarkupLine("[bold green]PASSED:[/] All localization keys and placeholders match cleanly!\n");
        return 0;
    }

    // Flattens nested JSON hierarchies (e.g., {"footer": {"terms": "..."}} -> "footer.terms")
    private static Dictionary<string, string> LoadAndFlattenJson(string filePath)
    {
        string json = File.ReadAllText(filePath);
        using JsonDocument doc = JsonDocument.Parse(json);
        var result = new Dictionary<string, string>();
        FlattenElement(doc.RootElement, "", result);
        return result;
    }

    private static void FlattenElement(JsonElement element, string prefix, Dictionary<string, string> result)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    string key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
                    FlattenElement(property.Value, key, result);
                }
                break;
            default:
                result[prefix] = element.ToString();
                break;
        }
    }

    // Finds anything inside curly braces: {username}, {count}, etc.
    public static HashSet<string> ExtractPlaceholders(string text)
    {
        var matches = Regex.Matches(text, @"\{([^{}]+)\}");
        var set = new HashSet<string>();
        foreach (Match m in matches)
        {
            set.Add(m.Groups[1].Value);
        }
        return set;
    }
}
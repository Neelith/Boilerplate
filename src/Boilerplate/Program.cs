using Boilerplate;
using System.CommandLine;
using System.Reflection;
using System.Text.Json;

/// <summary>
/// Entry point for the template-based file generator application.
/// Reads configuration, parses user input, loads templates, and generates files accordingly.
/// </summary>
internal class Program
{
    /// <summary>
    /// Main entry point. Reads settings, parses user input, loads templates, and generates files.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    private async static Task Main(string[] args)
    {
        Console.WriteLine("Starting template-based file generator...");

        // Define command line options
        var groupOption = new Option<string?>(
            aliases: ["--group", "-g"],
            description: "Template group (subfolder under templates)",
            getDefaultValue: () => string.Empty
        );
        var outputDirOption = new Option<string?>(
            aliases: ["--output", "-o"],
            description: "Base output directory for generated files"
        );
        var fileNamePrefixOption = new Option<string?>(
            aliases: ["--prefix", "-p"],
            description: "Prefix for generated file names"
        );
        var fileNameSuffixOption = new Option<string?>(
            aliases: ["--suffix", "-s"],
            description: "Suffix for generated file names"
        );
        var variablesOption = new Option<string?>(
            aliases: ["--vars", "-vs"],
            description: "Comma-separated key=value pairs for template variables"
        );

        var rootCommand = new RootCommand("Template-based file generator")
        {
            groupOption,
            outputDirOption,
            fileNamePrefixOption,
            fileNameSuffixOption,
            variablesOption
        };

        rootCommand.SetHandler(async (group, output, prefix, suffix, vars) =>
        {
            Console.WriteLine("Reading application settings...");
            AppSettings? appsettings = ReadApplicationSettings();

            Console.WriteLine("Parsing user input...");
            UserInput userInputSettings = ParseUserInput(group, output, prefix, suffix, vars);

            Console.WriteLine("Loading template settings...");
            List<TemplateSettings> templateSettings = await ReadTemplateSettings(appsettings, userInputSettings);

            Console.WriteLine($"Found {templateSettings.Count} template(s) to process.");

            foreach (var settings in templateSettings)
            {
                Console.WriteLine($"Generating file: {settings.FileName}.{settings.FileExtension} in {settings.OutputDirectory}");
                GenerateFileFromSettings(settings);
            }

            Console.WriteLine("File generation complete.");
        },
        groupOption, outputDirOption, fileNamePrefixOption, fileNameSuffixOption, variablesOption);

        await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// Loads template settings from template files based on application and user input settings.
    /// </summary>
    /// <param name="appsettings">Application settings (may be null).</param>
    /// <param name="userInput">User input parsed from command-line arguments.</param>
    /// <returns>List of TemplateSettings for file generation.</returns>
    private static async Task<List<TemplateSettings>> ReadTemplateSettings(AppSettings? appsettings, UserInput userInput)
    {
        string templatesBasePath = appsettings?.TemplatesFolderPath ?? Path.Combine(AppContext.BaseDirectory, "templates");
        string groupSubfolder = userInput.Group ?? string.Empty;
        string templatesPath = Path.Combine(templatesBasePath, groupSubfolder);

        Console.WriteLine($"Looking for templates in: {templatesPath}");

        if (!Directory.Exists(templatesPath))
            throw new DirectoryNotFoundException($"Templates directory not found: {templatesPath}");

        var templateFiles = Directory.GetFiles(templatesPath, "*.json", SearchOption.TopDirectoryOnly);

        Console.WriteLine($"Found {templateFiles.Length} template file(s).");

        var templateInputs = new List<TemplateInput>();
        foreach (var file in templateFiles)
        {
            Console.WriteLine($"Reading template file: {file}");
            string jsonContent = await File.ReadAllTextAsync(file);
            var templateInput = JsonSerializer.Deserialize<TemplateInput>(jsonContent);
            if (templateInput != null)
            {
                templateInputs.Add(templateInput);
            }
        }

        List<TemplateSettings> templateSettingsList = templateInputs.Select(templateInput =>
        {
            string fileName = templateInput.FileName;
            if (!string.IsNullOrEmpty(userInput.FileNamePrefix))
            {
                fileName = userInput.FileNamePrefix + fileName;
            }
            if (!string.IsNullOrEmpty(userInput.FileNameSuffix))
            {
                fileName = fileName + userInput.FileNameSuffix;
            }

            string outputDir = GetOutpurDir(userInput, templateInput);

            return new TemplateSettings
            {
                FileName = fileName,
                FileExtension = templateInput.FileExtension,
                OutputDirectory = outputDir,
                Template = templateInput.Template,
                Variables = userInput.Variables
            };
        }).ToList();

        return templateSettingsList;
    }

    /// <summary>
    /// Determines the output directory for a generated file based on user and template settings.
    /// </summary>
    /// <param name="userInput">User input settings.</param>
    /// <param name="templateInput">Template input definition.</param>
    /// <returns>Resolved output directory path.</returns>
    private static string GetOutpurDir(UserInput userInput, TemplateInput templateInput)
    {
        string? userSpecifiedOutputDir = userInput.OutputDirectoryBasePath;
        string templateOutputDir = templateInput.OutputDirectory;

        if (!string.IsNullOrEmpty(userSpecifiedOutputDir) && Path.IsPathRooted(userSpecifiedOutputDir))
        {
            return userSpecifiedOutputDir;
        }

        if (!string.IsNullOrEmpty(userSpecifiedOutputDir))
        {
            return Path.Combine(Environment.CurrentDirectory, userSpecifiedOutputDir);
        }

        if (Path.IsPathRooted(templateOutputDir))
        {
            return templateOutputDir;
        }

        return Path.Combine(Environment.CurrentDirectory, templateOutputDir);
    }

    /// <summary>
    /// Parses command-line arguments into a UserInput object.
    /// </summary>
    private static UserInput ParseUserInput(string? group, string? output, string? prefix, string? suffix, string? vars)
    {
        var userInputSettings = new UserInput
        {
            Group = group ?? string.Empty,
            OutputDirectoryBasePath = output,
            FileNamePrefix = prefix,
            FileNameSuffix = suffix,
            Variables = new Dictionary<string, string>()
        };

        // If prefix not specified, use last folder name of output directory as prefix
        if (userInputSettings.FileNamePrefix is null && !string.IsNullOrEmpty(userInputSettings.OutputDirectoryBasePath))
        {
            string outputDir = userInputSettings.OutputDirectoryBasePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            userInputSettings.FileNamePrefix = Path.GetFileName(outputDir);
        }

        if (!string.IsNullOrWhiteSpace(vars))
        {
            var variables = vars.Split(',', StringSplitOptions.RemoveEmptyEntries);
            userInputSettings.Variables = variables
                .Select(v => v.Split('=', 2))
                .Where(parts => parts.Length == 2)
                .ToDictionary(parts => parts[0], parts => parts[1]);
        }

        Console.WriteLine("User input parsed:");
        Console.WriteLine($"  Group: {userInputSettings.Group}");
        Console.WriteLine($"  OutputDirectoryBasePath: {userInputSettings.OutputDirectoryBasePath}");
        Console.WriteLine($"  FileNamePrefix: {userInputSettings.FileNamePrefix}");
        Console.WriteLine($"  FileNameSuffix: {userInputSettings.FileNameSuffix}");
        Console.WriteLine($"  Variables: {string.Join(", ", userInputSettings.Variables.Select(kv => $"{kv.Key}={kv.Value}"))}");

        return userInputSettings;
    }

    /// <summary>
    /// Reads application settings from appsettings.json if present.
    /// </summary>
    /// <returns>AppSettings object or null if not found.</returns>
    private static AppSettings? ReadApplicationSettings()
    {
        string configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        AppSettings? appSettings = null;

        if (!File.Exists(configPath))
        {
            Console.WriteLine("No appsettings.json found. Using defaults.");
            return appSettings;
        }

        string json = File.ReadAllText(configPath);

        ArgumentException.ThrowIfNullOrEmpty(nameof(json), "Configuration file is empty.");

        appSettings = JsonSerializer.Deserialize<AppSettings>(json!);

        Console.WriteLine("Application settings loaded.");

        return appSettings;
    }

    /// <summary>
    /// Generates a file from the provided template settings.
    /// </summary>
    /// <param name="settings">Template settings for file generation.</param>
    private static void GenerateFileFromSettings(TemplateSettings settings)
    {
        string template = GenerateFileContent(settings);

        GenerateFile(settings, template);
    }

    /// <summary>
    /// Replaces template variables in the template string with user-provided values.
    /// </summary>
    /// <param name="settings">Template settings containing variables and template.</param>
    /// <returns>Processed template string with variables replaced.</returns>
    private static string GenerateFileContent(TemplateSettings settings)
    {
        var template = settings.Template;

        foreach (var variable in settings.Variables)
        {
            template = template.Replace("{@" + variable.Key + "}", variable.Value);
        }

        return template;
    }

    /// <summary>
    /// Writes the generated template content to a file in the specified output directory.
    /// </summary>
    /// <param name="settings">Template settings including file name and output directory.</param>
    /// <param name="template">Content to write to the file.</param>
    private static void GenerateFile(TemplateSettings settings, string template)
    {
        string filename = $"{settings.FileName}.{settings.FileExtension}";

        string outputDirectory = settings.OutputDirectory.StartsWith('.')
            ? Path.Combine(AppContext.BaseDirectory, settings.OutputDirectory)
            : settings.OutputDirectory;

        if (!Directory.Exists(outputDirectory))
        {
            Console.WriteLine($"Creating output directory: {outputDirectory}");
            Directory.CreateDirectory(outputDirectory);
        }

        string filePath = Path.Combine(outputDirectory, filename);

        File.WriteAllText(filePath, template);
        Console.WriteLine($"File written: {filePath}");
    }
}
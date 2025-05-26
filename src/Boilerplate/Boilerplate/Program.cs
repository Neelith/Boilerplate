using Boilerplate;
using System.CommandLine;
using System.Reflection;
using System.Text.Json;

internal class Program
{
    private async static Task<int> Main(string[] args)
    {
        //I want a way to read config files defined by the user and generate files based on that config.
        //The user should define the templates for the files and the  files should be generated based on the templates.
        //The user then should be able to invoke this program, add the variables to the templates and generate the files.

        //case -g (group name) cqrs-query -fn (se non specificato è prefisso) GetUser -vs QueryName=GetUser
        //carica in memoria i template definiti in un file di configurazione, ad esempio un file json

        AppSettings? appsettings = ReadApplicationSettings();
        UserInput userInputSettings = ParseUserInput(args);
        List<TemplateSettings> templateSettings = ReadTemplateSettings(appsettings, userInputSettings);

        foreach (var settings in templateSettings)
        {
            GenerateFileFromSettings(settings);
        }

        return 0;
    }

    private static List<TemplateSettings> ReadTemplateSettings(AppSettings? appsettings, UserInput userInput)
    {
        // Step 1: Determine the templates directory
        string templatesBasePath = appsettings?.TemplatesFolderPath ?? Path.Combine(AppContext.BaseDirectory, "templates");
        string groupSubfolder = userInput.Group ?? string.Empty;
        string templatesPath = Path.Combine(templatesBasePath, groupSubfolder);

        // Step 2: Read all template files in the directory
        if (!Directory.Exists(templatesPath))
            throw new DirectoryNotFoundException($"Templates directory not found: {templatesPath}");

        var templateFiles = Directory.GetFiles(templatesPath, "*.json", SearchOption.TopDirectoryOnly);

        // Deserialize each template file into a list of TemplateInput
        var templateInputs = new List<TemplateInput>();
        foreach (var file in templateFiles)
        {
            string jsonContent = File.ReadAllText(file);
            var templateInput = JsonSerializer.Deserialize<TemplateInput>(jsonContent);
            if (templateInput != null)
            {
                templateInputs.Add(templateInput);
            }
        }

        // Step 3: Parse each template file into TemplateSettings
        List<TemplateSettings> templateSettingsList = templateInputs.Select(templateInput => {

            // Add prefix and/or suffix to the file name if specified
            string fileName = templateInput.FileName;
            if (!string.IsNullOrEmpty(userInput.FileNamePrefix))
            {
                fileName = userInput.FileNamePrefix + fileName;
            }
            if (!string.IsNullOrEmpty(userInput.FileNameSuffix))
            {
                fileName = fileName + userInput.FileNameSuffix;
            }

            return new TemplateSettings
            {
                FileName = fileName,
                FileExtension = templateInput.FileExtension,
                OutputDirectory = !string.IsNullOrEmpty(userInput.OutputDirectoryBasePath) ? userInput.OutputDirectoryBasePath : templateInput.OutputDirectory,
                Template = templateInput.Template,
                Variables = userInput.Variables
            };
        }).ToList();

        return templateSettingsList;
    }

    private static UserInput ParseUserInput(string[] args)
    {
        var userInputSettings = new UserInput();

        bool argsValorized = args is not null && args.Length > 0;

        if (!argsValorized)
        {
            return userInputSettings;
        }

        //Add -g option for group name
        var indexOfGroupOption = Array.IndexOf(args!, "-g");

        userInputSettings.Group = indexOfGroupOption != -1 && indexOfGroupOption + 1 < args!.Length ? args[indexOfGroupOption + 1] : string.Empty;

        //Add -fn option for file name prefix
        var indexOfFileNamePrefixOption = Array.IndexOf(args!, "-fn");
        userInputSettings.FileNamePrefix = indexOfFileNamePrefixOption != -1 && indexOfFileNamePrefixOption + 1 < args!.Length ? args[indexOfFileNamePrefixOption + 1] : null;

        //Add -vs option for variables
        var indexOfVariablesOption = Array.IndexOf(args!, "-vs");
        if (indexOfVariablesOption != -1 && indexOfVariablesOption + 1 < args!.Length)
        {
            var variables = args[indexOfVariablesOption + 1].Split(',');

            userInputSettings.Variables = variables.ToDictionary(
                v => v.Split('=')[0],
                v => v.Split('=')[1]);
        }

        return userInputSettings;
    }

    private static AppSettings? ReadApplicationSettings()
    {
        string configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        AppSettings? appSettings = null;

        if (!File.Exists(configPath))
        {
            return appSettings;
        }

        string json = File.ReadAllText(configPath);

        ArgumentException.ThrowIfNullOrEmpty(nameof(json), "Configuration file is empty.");

        appSettings = JsonSerializer.Deserialize<AppSettings>(json!);

        return appSettings;
    }

    private static void GenerateFileFromSettings(TemplateSettings settings)
    {
        string template = GenerateFileContent(settings);

        GenerateFile(settings, template);
    }

    private static string GenerateFileContent(TemplateSettings settings)
    {
        var template = settings.Template;

        foreach (var variable in settings.Variables)
        {
            template = template.Replace("{@" + variable.Key +"}", variable.Value);
        }

        return template;
    }

    private static void GenerateFile(TemplateSettings settings, string template)
    {
        string filename = $"{settings.FileName}.{settings.FileExtension}";

        string outputDirectory = settings.OutputDirectory.StartsWith('.') 
            ? Path.Combine(AppContext.BaseDirectory, settings.OutputDirectory) 
            : settings.OutputDirectory;

        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        string filePath = Path.Combine(outputDirectory, filename);

        File.WriteAllText(filePath, template);
    }
}
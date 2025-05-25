using Boilerplate;
using System.CommandLine;
using System.Reflection;

internal class Program
{
    private async static Task<int> Main(string[] args)
    {
        //I want a way to read config files defined by the user and generate files based on that config.
        //The user should define the templates for the files and the  files should be generated based on the templates.
        //The user then should be able to invoke this program, add the variables to the templates and generate the files.

        //case -g (group name) cqrs-query -fn (se non specificato è prefisso) GetUser -vs QueryName=GetUser
        //carica in memoria i template definiti in un file di configurazione, ad esempio un file json

        var rootCommand = new RootCommand("Boilerplate");

        var userInputSettings = new UserInputSettings();
        bool argsValorized = args is not null && args.Length > 0;

        //Add -g option for group name
        if (argsValorized)
        {
            var indexOfGroupOption = Array.IndexOf(args, "-g");

            userInputSettings.Group = indexOfGroupOption != -1 && indexOfGroupOption + 1 < args.Length ? args[indexOfGroupOption + 1] : string.Empty;

            var indexOfFileNamePrefixOption = Array.IndexOf(args, "-fn");
            userInputSettings.FileNamePrefix = indexOfFileNamePrefixOption != -1 && indexOfFileNamePrefixOption + 1 < args.Length ? args[indexOfFileNamePrefixOption + 1] : null;

            var indexOfVariablesOption = Array.IndexOf(args, "-vs");
            if (indexOfVariablesOption != -1 && indexOfVariablesOption + 1 < args.Length)
            {
                var variables = args[indexOfVariablesOption + 1].Split(',');
                userInputSettings.Variables = variables.ToDictionary(
                    v => v.Split('=')[0],
                    v => v.Split('=')[1]);
            }
        }

        var template1 = @"
public class {@QueryName}Query{}
";
        var template2 = @"
public class {@QueryName}QueryHandler{}
";
        var template3 = @"
public class {@QueryName}QueryValidator{}
";
        List<string> templates = [template1, template2, template3];

        var templateSettings1 = new TemplateSettings
        {
            Group = "cqrs-query",
            FileName = 
            (!string.IsNullOrEmpty(userInputSettings.FileNamePrefix) ? userInputSettings.FileNamePrefix : string.Empty) 
            + "Query" +
            (!string.IsNullOrEmpty(userInputSettings.FileNameSuffix) ? userInputSettings.FileNameSuffix : string.Empty),
            FileExtension = ".cs",
            OutputDirectory = "output",
            Template = templates[0],
            Variables = userInputSettings.Variables
        };

        List<TemplateSettings> templateSettings = [templateSettings1];

        foreach (var settings in templateSettings)
        {
            GenerateFileFromSettings(settings);
        }

        return 0;
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
        string filename = settings.FileName + settings.FileExtension;

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
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

        var settings = new TemplateSettings
        {
            FileName = "GetUserQuery",
            FileExtension = ".cs",
            OutputDirectory = "output",
            Template = templates[0],
            Variables = new Dictionary<string, string>
            {
                { "QueryName", "GetUser" }
            }
        };

        GenerateFileFromSettings(settings);

        return 0;
        //var fileOption = new Option<FileInfo?>(
        //   name: "--file",
        //   description: "The file to read and display on the console.");

        //var rootCommand = new RootCommand("Sample app for System.CommandLine");
        //rootCommand.AddOption(fileOption);

        //rootCommand.SetHandler((file) =>
        //{
        //    ReadFile(file!);
        //},
        //    fileOption);

        //return await rootCommand.InvokeAsync(args);
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
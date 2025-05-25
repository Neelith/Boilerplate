using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boilerplate
{
    internal class TemplateSettings
    {
        public string FileName { get; set; } = string.Empty;
        public string FileExtension { get; set; } = "txt";
        public string OutputDirectory { get; set; } = string.Empty;
        public string Template { get; set; } = string.Empty;
        public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();

    }
}

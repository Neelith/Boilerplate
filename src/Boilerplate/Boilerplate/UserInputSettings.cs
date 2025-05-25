namespace Boilerplate
{
    internal class UserInputSettings
    {
        public string Group { get; set; } = string.Empty;
        public string? FileNamePrefix { get; set; }
        public string? FileNameSuffix { get; set; }
        public Dictionary<string, string> Variables { get; set; } = [];
        public string? OutputDirectoryBasePath { get; set; }
    }
}

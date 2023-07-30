namespace Skynet.Services
{
    using Skynet.Entities;
    using System;
    using System.IO;
    using System.Text.Json;

    public class ConfigService
    {
        public Config GetConfig()
        {
            var file = $"{Directory.GetCurrentDirectory()}/Config.json";
            var data = File.ReadAllText(file);

            return JsonSerializer.Deserialize<Config>(data)
                ?? throw new InvalidOperationException("Failed to deserialize config");
        }
    }
}

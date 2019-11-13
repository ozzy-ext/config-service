﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ConfigService.Tools;

namespace ConfigService.Services
{
    public interface IConfigProvider
    {
        IEnumerable<string> GetConfigList();

        Task<string> LoadConfig(string id, bool hideSecrets, bool prettyJson);
    }

    class DefaultConfigProvider : IConfigProvider
    {
        string BasePath { get; }

        private string ClientsPath { get; }
        private string OverridesPath { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultConfigProvider"/>
        /// </summary>
        public DefaultConfigProvider(string basePath)
        {
            BasePath = basePath;
            ClientsPath = Path.Combine(basePath, "Clients");
            OverridesPath = Path.Combine(basePath, "Overrides");
        }

        public IEnumerable<string> GetConfigList()
        {
            return Directory
                .EnumerateFiles(ClientsPath, "*.json")
                .Select(Path.GetFileNameWithoutExtension);
        }

        public async Task<string> LoadConfig(string id, bool hideSecrets, bool prettyJson)
        {
            var originStr = await File.ReadAllTextAsync(Path.Combine(ClientsPath, id + ".json"));

            var overridePath = Path.Combine(OverridesPath, id + ".json");
            if (!File.Exists(overridePath))
            {
                var pretty = JsonPrettyFormatter.JsonPrettify(originStr);
                return pretty;
            }

            var overridingStr = await File.ReadAllTextAsync(overridePath);
            var merger = new ConfigMerger
            {
                HideSecrets = hideSecrets,
                PrettyJson = prettyJson
            };

            return merger.Merge(originStr, overridingStr);
        }
    }
}

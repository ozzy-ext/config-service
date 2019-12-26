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
        IEnumerable<string> GetOverrideList();
        IEnumerable<string> GetIncludeList();

        Task<string> LoadConfig(string id, bool prettyJson);
        Task<string> LoadConfigWithoutSecrets(string id, bool prettyJson);
        Task<string> LoadConfigBase(string id);
        Task<string> LoadOverride(string id);
        Task<string> LoadInclude(string id);
    }

    class DefaultConfigProvider : IConfigProvider
    {
        private string ConfigsPath { get; }
        private string OverridesPath { get; }
        private string IncludePath { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultConfigProvider"/>
        /// </summary>
        public DefaultConfigProvider(string basePath)
        {
            ConfigsPath = Path.Combine(basePath, "Configs");
            OverridesPath = Path.Combine(basePath, "Overrides");
            IncludePath = Path.Combine(basePath, "Includes");
        }

        public IEnumerable<string> GetConfigList()
        {
            return Directory
                .EnumerateFiles(ConfigsPath, "*.json")
                .Select(Path.GetFileNameWithoutExtension);
        }

        public IEnumerable<string> GetOverrideList()
        {
            return Directory
                .EnumerateFiles(OverridesPath, "*.json")
                .Select(Path.GetFileNameWithoutExtension);
        }

        public IEnumerable<string> GetIncludeList()
        {
            return Directory
                .EnumerateFiles(IncludePath, "*.json")
                .Select(Path.GetFileNameWithoutExtension);
        }

        public async Task<string> LoadConfig(string id, bool prettyJson)
        {
            return await LoadConfigCore(id, prettyJson, true);
        }

        public async Task<string> LoadConfigWithoutSecrets(string id, bool prettyJson)
        {
            return await LoadConfigCore(id, prettyJson, false);
        }

        public async Task<string> LoadConfigBase(string id)
        {
            var str = await File.ReadAllTextAsync(Path.Combine(ConfigsPath, id + ".json"));
            return JsonPrettyFormatter.JsonPrettify(str);
        }

        public async Task<string> LoadOverride(string id)
        {
            var str = await File.ReadAllTextAsync(Path.Combine(OverridesPath, id + ".json"));
            return JsonPrettyFormatter.JsonPrettify(str);
        }

        public async Task<string> LoadInclude(string id)
        {
            var str = await File.ReadAllTextAsync(Path.Combine(IncludePath, id + ".json"));
            return JsonPrettyFormatter.JsonPrettify(str);
        }

        public async Task<string> LoadConfigCore(string id, bool prettyJson, bool withSecrets)
        {
            var originStr = await File.ReadAllTextAsync(Path.Combine(ConfigsPath, id + ".json"));

            var overridePath = Path.Combine(OverridesPath, id + ".json");
            var overridingStr = File.Exists(overridePath) ? await File.ReadAllTextAsync(overridePath) : null;

            var configBuilder = new ConfigBuilder(new DefaultIncludeProvider(IncludePath))
            {
                PrettyJson = prettyJson
            };

            return await configBuilder.Build(originStr, overridingStr);
        }
    }
}

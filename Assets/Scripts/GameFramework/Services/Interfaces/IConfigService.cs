using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GrameFramework.Config;
using UCTools_ConfigVariables;

namespace GameFramework.Services.Interfaces
{
    /// <summary>
    /// Interface for configuration management service with ConfigVar integration and ScriptableObject queries
    /// </summary>
    public interface IConfigService : IGameService
    {
        Task LoadConfigAsync();
        Task SaveConfigAsync();
        T GetConfigValue<T>(string configName);
        void SetConfigValue<T>(string configName, T value);
        void ResetToDefaults();
        
        // ScriptableObject query methods
        T GetConfigCategory<T>() where T : ConfigCategory;
        ConfigCategory GetConfigCategory(Type categoryType);
        IReadOnlyList<ConfigCategory> GetAllConfigCategories();
        IReadOnlyList<T> GetConfigCategoriesOfType<T>() where T : ConfigCategory;
        bool HasConfigCategory<T>() where T : ConfigCategory;
        bool HasConfigCategory(Type categoryType);
    }
}
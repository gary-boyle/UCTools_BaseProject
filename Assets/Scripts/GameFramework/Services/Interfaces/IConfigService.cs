using System.Threading.Tasks;

namespace GameFramework.Services.Interfaces
{
    /// <summary>
    /// Interface for configuration management service with ConfigVar integration
    /// </summary>
    public interface IConfigService : IGameService
    {
        Task LoadConfigAsync();
        Task SaveConfigAsync();
        T GetConfigValue<T>(string configName);
        void SetConfigValue<T>(string configName, T value);
        void ResetToDefaults();
        void RegisterConfigVar(UCTools_ConfigVariables.ConfigVar configVar);
    }
}
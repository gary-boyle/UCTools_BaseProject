using System;
using GameFramework.Config.Enums;
using GameFramework.Core;
using GameFramework.Services.Interfaces;
using GameFramework.Config.ScriptableObjects;
using GameFramework.ConsoleTool.Enums;
using GameFramework.ConsoleTool.Interfaces;

namespace GameFramework.ConsoleTool.Commands
{
    /// <summary>
    /// Console command for controlling audio settings
    /// Uses the clean event-driven architecture via AudioSettings_SO
    /// 
    /// Usage Examples:
    /// > audio                    - Show current audio status
    /// > audio on/off             - Enable/disable all audio
    /// > audio master 80          - Set master volume to 80%
    /// > audio music 60           - Set music volume to 60%
    /// > audio sfx 90             - Set SFX volume to 90%
    /// > audio ui 100             - Set UI volume to 100%
    /// </summary>
    public class AudioCommand : ConsoleCommandBase
    {
        #region Command Properties
        
        public override string CommandName => "audio";
        public override string Description => "Control audio settings (enable/disable, volumes)";
        public override CategoryEnum Category => CategoryEnum.Audio;
        public override int Tag => 200;
        
        #endregion

        #region Services
        
        private AudioSettings_SO _audioSettings;
        
        #endregion

        #region Command Execution
        
        public override void Execute(string[] args, IConsoleContext context)
        {
            if (!TryGetServices(context))
                return;

            if (args.Length == 0)
            {
                ShowAudioStatus(context);
                return;
            }

            string command = args[0].ToLowerInvariant();
            
            switch (command)
            {
                case "on":
                case "enable":
                case "true":
                case "1":
                    SetAudioEnabled(true, context);
                    break;
                    
                case "off":
                case "disable":
                case "false":
                case "0":
                    SetAudioEnabled(false, context);
                    break;
                    
                case "master":
                    HandleVolumeCommand(args, "master", context);
                    break;
                    
                case "music":
                    HandleVolumeCommand(args, "music", context);
                    break;
                    
                case "sfx":
                case "effects":
                    HandleVolumeCommand(args, "sfx", context);
                    break;
                    
                case "ui":
                    HandleVolumeCommand(args, "ui", context);
                    break;
                    
                case "status":
                    ShowAudioStatus(context);
                    break;
                    
                default:
                    context.WriteError($"Unknown command: '{args[0]}'");
                    context.WriteLine(GetUsage());
                    break;
            }
        }
        
        #endregion

        #region Helper Methods
        
        private bool TryGetServices(IConsoleContext context)
        {
            try
            { 
                _audioSettings = SettingsRegistry.Get<AudioSettings_SO>();
                if (_audioSettings == null)
                {
                    context.WriteError("AudioSettings not found in ConfigService");
                    return false;
                }
                
                return true;
            }
            catch (Exception e)
            {
                context.WriteError($"Failed to get services: {e.Message}");
                return false;
            }
        }
        
        private void SetAudioEnabled(bool enabled, IConsoleContext context)
        {
            try
            {
                _audioSettings.SetAudioEnabled(enabled);
                context.WriteLine($"Audio {(enabled ? "enabled" : "disabled")}");
                SaveConfigAsync(context);
            }
            catch (Exception e)
            {
                context.WriteError($"Failed to set audio enabled state: {e.Message}");
            }
        }
        
        private void HandleVolumeCommand(string[] args, string volumeType, IConsoleContext context)
        {
            if (args.Length != 2)
            {
                context.WriteError($"Volume command requires a value. Usage: audio {volumeType} <0-100>");
                return;
            }

            if (!TryParseInt(args[1], out int volume, context, "volume"))
                return;

            if (volume < 0 || volume > 100)
            {
                context.WriteError("Volume must be between 0 and 100");
                return;
            }

            try
            {
                switch (volumeType)
                {
                    case "master":
                        _audioSettings.SetMasterVolumeFromPercentage(volume);
                        break;
                    case "music":
                        _audioSettings.SetMusicVolumeFromPercentage(volume);
                        break;
                    case "sfx":
                        _audioSettings.SetSfxVolumeFromPercentage(volume);
                        break;
                    case "ui":
                        _audioSettings.SetUIVolumeFromPercentage(volume);
                        break;
                }

                context.WriteLine($"{char.ToUpper(volumeType[0]) + volumeType.Substring(1)} volume set to {volume}%");
                SaveConfigAsync(context);
            }
            catch (Exception e)
            {
                context.WriteError($"Failed to set {volumeType} volume: {e.Message}");
            }
        }
        
        private void ShowAudioStatus(IConsoleContext context)
        {
            try
            {
                bool enabled = _audioSettings.AudioEnabled.Value;
                
                context.WriteLine("Audio Status:");
                context.WriteLine($"  Enabled: {(enabled ? "Yes" : "No")}");
                context.WriteLine($"  Master Volume: {_audioSettings.GetMasterVolumeAsPercentage()}%");
                context.WriteLine($"  Music Volume: {_audioSettings.GetMusicVolumeAsPercentage()}%");
                context.WriteLine($"  SFX Volume: {_audioSettings.GetSfxVolumeAsPercentage()}%");
                context.WriteLine($"  UI Volume: {_audioSettings.GetUIVolumeAsPercentage()}%");
            }
            catch (Exception e)
            {
                context.WriteError($"Failed to get audio status: {e.Message}");
            }
        }
        
        private async void SaveConfigAsync(IConsoleContext context)
        {
            try
            {
                await SettingsRegistry.SaveAllSettingsAsync();
            }
            catch (Exception e)
            {
                context.WriteWarning($"Settings updated but failed to save: {e.Message}");
            }
        }
        
        #endregion

        #region Documentation
        
        public override string GetUsage()
        {
            return "Usage: audio [command] [value]\n" +
                   "  audio                    - Show current audio status\n" +
                   "  audio on/off             - Enable/disable audio\n" +
                   "  audio master <0-100>     - Set master volume percentage\n" +
                   "  audio music <0-100>      - Set music volume percentage\n" +
                   "  audio sfx <0-100>        - Set SFX volume percentage\n" +
                   "  audio ui <0-100>         - Set UI volume percentage\n" +
                   "  audio status             - Show current audio status\n" +
                   "\n" +
                   "Examples:\n" +
                   "  audio master 75          - Set master volume to 75%\n" +
                   "  audio off                - Disable all audio\n" +
                   "\n" +
                   "Note: Changes are automatically applied and saved.";
        }
        
        public override bool ValidateArgs(string[] args)
        {
            return args.Length <= 2;
        }
        
        #endregion
    }
}

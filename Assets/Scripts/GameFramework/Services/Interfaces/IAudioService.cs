namespace GameFramework.Services.Interfaces
{
    /// <summary>
    /// Interface for audio management service
    /// </summary>
    public interface IAudioService : IGameService
    {
        void PlayMusic(string musicName);
        void PlaySound(string soundName);
        void StopMusic();
        void StopSound(string soundName);
        void SetMasterVolume(float volume);
        void SetMusicVolume(float volume);
        void SetSFXVolume(float volume);
        float GetMasterVolume();
        float GetMusicVolume();
        float GetSFXVolume();
    }
}
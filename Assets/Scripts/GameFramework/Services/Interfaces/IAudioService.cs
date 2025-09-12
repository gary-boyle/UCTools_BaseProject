using System.Threading.Tasks;
using GameFramework.Audio;
using UnityEngine.Audio;

namespace GameFramework.Services.Interfaces
{
    /// <summary>
    /// Interface for audio management service
    /// </summary>
    public interface IAudioService : IGameService
    {
        Task InitializeAsync(AudioManager audioManager);
        AudioDatabase_SO GetAudioDatabase();
        AudioMixerGroup GetSFXMixerGroup();
    }
}
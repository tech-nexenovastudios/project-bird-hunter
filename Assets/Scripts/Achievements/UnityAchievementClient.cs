using Cysharp.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace BirdHunter.Achievement
{
    public interface IUnityAchievementClient
    {
        UniTask SetProgressAsync(string unityId, float normalizedProgress);
        UniTask UnlockAsync(string unityId);
    }

    public sealed class UnityAchievementClient : IUnityAchievementClient
    {
        private bool _initialized;

        private async UniTask EnsureInitializedAsync()
        {
            if (_initialized)
                return;

            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            _initialized = true;
        }

        public async UniTask SetProgressAsync(string unityId, float normalizedProgress)
        {
            await EnsureInitializedAsync();

            normalizedProgress = Mathf.Clamp01(normalizedProgress);

            // await AchievementsService.Instance.SetAchievementProgressAsync(unityId, normalizedProgress);
        }

        public async UniTask UnlockAsync(string unityId)
        {
            await EnsureInitializedAsync();

            // await AchievementsService.Instance.SetAchievementProgressAsync(unityId, 1f);
        }
    }
}
namespace BirdHunter.Achievement
{
    using System.Collections.Generic;
    using System.IO;
    using Cysharp.Threading.Tasks;
    using UnityEngine;

    public sealed class JsonAchievementRepository : IAchievementRepository
    {
        private const string FileName = "achievements.json";

        [System.Serializable]
        private sealed class SaveData
        {
            public List<string> unlockedIds = new();
        }

        public UniTask<Dictionary<string, bool>> LoadStatesAsync(PlayerContext player)
        {
            var path = Path.Combine(Application.persistentDataPath, FileName);
            var result = new Dictionary<string, bool>();

            if (!File.Exists(path))
                return UniTask.FromResult(result);

            var json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();

            foreach (var id in data.unlockedIds)
                result[id] = true;

            return UniTask.FromResult(result);
        }

        public UniTask SaveStatesAsync(PlayerContext player, IReadOnlyDictionary<string, bool> states)
        {
            var data = new SaveData();

            foreach (var kvp in states)
            {
                if (kvp.Value)
                    data.unlockedIds.Add(kvp.Key);
            }

            var json = JsonUtility.ToJson(data);
            var path = Path.Combine(Application.persistentDataPath, FileName);
            File.WriteAllText(path, json);

            return UniTask.CompletedTask;
        }
    }
}
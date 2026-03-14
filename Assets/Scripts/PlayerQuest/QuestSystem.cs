using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.CloudSave;
using Unity.Services.RemoteConfig;
using Constants;
using Newtonsoft.Json;
using UnityUtils; // Your GameConstants

namespace PlayerQuest
{


    namespace PlayerQuest
    {
        public class QuestSystem : Singleton<QuestSystem>
        {
            public Quest[] activeQuests = new Quest[3];

            private const int DAILY_QUEST_COUNT = 3;

            async void Start()
            {
                await RefreshQuests();
            }

            public async Task RefreshQuests()
            {
                try
                {
                    // FIXED: Use HashSet<string> for LoadAsync keys [web:202]
                    var keys = new HashSet<string>
                    {
                        GameConstants.PLAYER_DAILY_QUEST_KEY,
                        GameConstants.PLAYER_QUEST_DATA_KEY
                    };

                    var savedData = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

                    // Load existing quests
                    if (savedData.ContainsKey(GameConstants.PLAYER_DAILY_QUEST_KEY))
                    {
                        var questsJson = savedData[GameConstants.PLAYER_DAILY_QUEST_KEY].Value.GetAs<string>();

                        // FIXED: Use JsonHelper instead of missing SerializableQuestArray
                        var questData = JsonHelper.FromJson<Quest>(questsJson);
                        activeQuests = questData ?? new Quest[0];

                        // Check if expired (CRON logic)
                        if (IsNewDay(activeQuests.FirstOrDefault()?.startTime ?? 0))
                        {
                            await GenerateNewDailyQuests();
                        }

                        return;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Quest load failed: {e.Message}. Generating fresh quests.");
                }

                await GenerateNewDailyQuests();
            }

            bool IsNewDay(long questStartTime)
            {
                var lastStart = DateTimeOffset.FromUnixTimeSeconds(questStartTime);
                return DateTimeOffset.UtcNow.Date > lastStart.Date;
            }

            async Task GenerateNewDailyQuests()
            {
                // FIXED: Correct RemoteConfigService usage [web:210][web:203]
                var configManager = RemoteConfigService.Instance;
                var templatesJson = configManager.appConfig.GetJson(GameConstants.REMOTE_QUEST_TEMPLATES_KEY, "[]");

                // FIXED: Use JsonHelper for QuestTemplateArray
                var templates = JsonHelper.FromJson<QuestTemplate>(templatesJson);
                if (templates == null || templates.Length == 0)
                {
                    Debug.LogWarning("No quest templates in Remote Config - using defaults");
                    templates = GenerateDefaultTemplates();
                }

                activeQuests = new Quest[DAILY_QUEST_COUNT];

                for (int i = 0; i < DAILY_QUEST_COUNT; i++)
                {
                    var selectedTemplate = templates[UnityEngine.Random.Range(0, templates.Length)];
                    activeQuests[i] = new Quest(selectedTemplate);
                }

                await SaveQuests();
                UIManager.Instance?.ShowQuestRefreshToast();
            }

            QuestTemplate[] GenerateDefaultTemplates()
            {
                return new QuestTemplate[]
                {
                    new QuestTemplate
                    {
                        id = "eggs_200", title = "Destroy 200 eggs", type = "DestroyEggs",
                        target = 200, rewards = new QuestReward { coins = 300, power = 5 }
                    },
                    new QuestTemplate
                    {
                        id = "combo_20", title = "Reach combo x20", type = "ReachCombo",
                        target = 20, rewards = new QuestReward { coins = 500, gems = 2 }
                    }
                };
            }

            async Task SaveQuests()
            {
                var questData = JsonUtility.ToJson(new SerializableQuestWrapper { quests = activeQuests });

                var data = new Dictionary<string, object>
                {
                    { GameConstants.PLAYER_DAILY_QUEST_KEY, questData },
                    { GameConstants.PLAYER_QUEST_DATA_KEY, questData }
                };

                await CloudSaveService.Instance.Data.Player.SaveAsync(data);
            }

            // Progress tracking methods
            public void UpdateEggProgress(int count = 1)
            {
                var quest = GetQuestByType(QuestType.DestroyEggs);
                if (quest != null)
                {
                    quest.progress += count;
                    CheckCompletion(quest);
                }
            }

            public void UpdateComboProgress(float combo)
            {
                var quest = GetQuestByType(QuestType.ReachCombo);
                if (quest != null)
                {
                    quest.progress = Mathf.Max(quest.progress, combo);
                    CheckCompletion(quest);
                }
            }

            public void UpdateCannonWin(int cannonIndex)
            {
                var quest = activeQuests.FirstOrDefault(q =>
                    q.type == QuestType.CannonWins && q.cannonIndex == cannonIndex);
                if (quest != null)
                {
                    quest.progress += 1f;
                    CheckCompletion(quest);
                }
            }

            void CheckCompletion(Quest quest)
            {
                if (quest.progress >= quest.target && !quest.isComplete)
                {
                    quest.isComplete = true;
                    UIManager.Instance?.ShowQuestReady(quest);
                }
            }

            Quest GetQuestByType(QuestType type) =>
                Array.Find(activeQuests, q => q.type == type);
        }
    }

}
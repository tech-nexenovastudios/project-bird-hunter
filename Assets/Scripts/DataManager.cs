using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityUtils;

public class PlayerData
{
    public bool isLoggedIn;
    public string userId;
    public string email;
    public string username;
    
    public int coins;
    public int diamonds;
    public int powers;
    public int currentCannon;
    public int currentChapter;
    
    public bool adblockStatus;
    public bool tutorialStatus;
    
    public bool isFirstTime;
}

public class DataManager : Singleton<DataManager>
{
    public PlayerData data;
    
    public async UniTask SavePlayerData(PlayerData playerData)
    {
        await CloudSaveManager.Instance.SaveAsync(new Dictionary<string, object>
        {
            {"isFirstTime", playerData.isFirstTime},
            {"tutorialStatus", playerData.tutorialStatus},
            {"isLoggedIn", playerData.isLoggedIn},
            {"userId", playerData.userId},
            {"email", playerData.email},
            {"username", playerData.username},
            {"coins", playerData.coins},
            {"diamonds", playerData.diamonds},
            {"powers", playerData.powers},
            {"currentCannon", playerData.currentCannon},
            {"currentChapter", playerData.currentChapter},
            {"adblockStatus", playerData.adblockStatus},
        });
    }

    public async UniTask LoadPlayerData()
    {
        this.data = new PlayerData();

        var res = await CloudSaveManager.Instance.LoadAsync(new HashSet<string>()
        {
            "isFirstTime", "tutorialStatus", "isLoggedIn", "userId", "email", "username",
            "coins", "diamonds", "powers", "currentCannon", "currentChapter", "adblockStatus"
        });
        
        data.isFirstTime = !res.ContainsKey("isFirstTime") || res["isFirstTime"].Value.GetAs<bool>();
        data.tutorialStatus = res.ContainsKey("tutorialStatus") && res["tutorialStatus"].Value.GetAs<bool>();
        data.isLoggedIn = res.ContainsKey("isLoggedIn") && res["isLoggedIn"].Value.GetAs<bool>();
        data.userId = res.ContainsKey("userId") ? res["userId"].Value.GetAs<string>() : "";
        data.email = res.ContainsKey("email") ? res["email"].Value.GetAs<string>() : "";
        data.username = res.ContainsKey("username") ? res["username"].Value.GetAs<string>() : "";
        data.coins = res.ContainsKey("coins") ? res["coins"].Value.GetAs<int>() : 0;
        data.diamonds = res.ContainsKey("diamonds") ? res["diamonds"].Value.GetAs<int>() : 0;
        data.powers = res.ContainsKey("powers") ? res["powers"].Value.GetAs<int>() : 0;
        data.currentCannon = res.ContainsKey("currentCannon") ? res["currentCannon"].Value.GetAs<int>() : 0;
    }
}
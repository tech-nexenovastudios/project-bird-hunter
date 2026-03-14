// JsonHelper (for arrays - Unity JsonUtility limitation)

using System;
using PlayerQuest;
using UnityEngine;

public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
        return wrapper.Items;
    }
    
    public static string ToJson<T>(T[] array)
    {
        Wrapper<T> wrapper = new Wrapper<T> { Items = array };
        return JsonUtility.ToJson(wrapper);
    }
    
    [Serializable]
    private class Wrapper<T>
    {
        public T[] Items;
    }
}

// Serializable wrapper for Quest[]
[System.Serializable]
public class SerializableQuestWrapper
{
    public Quest[] quests;
}

// QuestTemplate array wrapper
[System.Serializable]
public class QuestTemplateArray
{
    public QuestTemplate[] templates;
}
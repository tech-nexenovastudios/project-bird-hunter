using System;
using System.Collections.Generic;
using UnityEngine;

//storing all egg info 
public static class EggManager
{
    public static Action onEggsChanged;

    public static List<GameObject> eggs = new();

    public static IReadOnlyList<GameObject> eggsList => eggs;

    public static void EggsAdd(GameObject egg)
    {
        eggs.Add(egg);
    }

    public static void RemoveEggs(GameObject egg)
    {
        eggs.Remove(egg);
        onEggsChanged?.Invoke();
    }

    public static void ClearEgg()
    {
        eggs.Clear();
    }
}

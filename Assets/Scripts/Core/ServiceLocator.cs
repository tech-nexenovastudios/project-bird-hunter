using System;
using System.Collections.Generic;

public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> services = new Dictionary<Type, object>();

    public static void Register<T>(T service) where T : class
    {
        var type = typeof(T);
        if (services.ContainsKey(type))
        {
            UnityEngine.Debug.LogWarning($"[ServiceLocator] Service {type.Name} already registered. Overwriting.");
            services[type] = service;
            return;
        }
        services.Add(type, service);
    }

    public static T Get<T>() where T : class
    {
        var type = typeof(T);
        if (services.TryGetValue(type, out var service))
        {
            return service as T;
        }
        UnityEngine.Debug.LogError($"[ServiceLocator] Service {type.Name} not registered.");
        return null;
    }

    public static bool TryGet<T>(out T service) where T : class
    {
        var type = typeof(T);
        if (services.TryGetValue(type, out var obj))
        {
            service = obj as T;
            return service != null;
        }
        service = null;
        return false;
    }

    public static void Unregister<T>() where T : class
    {
        services.Remove(typeof(T));
    }

    public static void Clear()
    {
        services.Clear();
    }
}
using System;
using System.Collections.Generic;

public static class EventBus
{
    private static readonly Dictionary<Type, Delegate> subscribers = new Dictionary<Type, Delegate>();

    public static void Subscribe<T>(Action<T> handler) where T : struct
    {
        var type = typeof(T);
        if (subscribers.TryGetValue(type, out var existing))
        {
            subscribers[type] = Delegate.Combine(existing, handler);
        }
        else
        {
            subscribers[type] = handler;
        }
    }

    public static void Unsubscribe<T>(Action<T> handler) where T : struct
    {
        var type = typeof(T);
        if (subscribers.TryGetValue(type, out var existing))
        {
            var updated = Delegate.Remove(existing, handler);
            if (updated == null)
                subscribers.Remove(type);
            else
                subscribers[type] = updated;
        }
    }

    public static void Publish<T>(T eventData) where T : struct
    {
        var type = typeof(T);
        if (subscribers.TryGetValue(type, out var existing))
        {
            (existing as Action<T>)?.Invoke(eventData);
        }
    }

    public static void Clear()
    {
        subscribers.Clear();
    }
}
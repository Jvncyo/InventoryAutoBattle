using System;
using System.Collections.Generic;

public static class EventBus
{
    private static readonly Dictionary<Type, List<Delegate>> Subscribers = new();

    public static void Subscribe<T>(Action<T> handler)
    {
        var type = typeof(T);
        if (!Subscribers.ContainsKey(type)) Subscribers[type] = new List<Delegate>();
        Subscribers[type].Add(handler);
    }

    public static void Unsubscribe<T>(Action<T> handler)
    {
        var type = typeof(T);
        if (Subscribers.ContainsKey(type)) Subscribers[type].Remove(handler);
    }

    public static void Publish<T>(T eventData)
    {
        var type = typeof(T);
        if (Subscribers.TryGetValue(type, out var handlers))
        {
            foreach (var handler in handlers.ToArray())
            {
                ((Action<T>)handler)?.Invoke(eventData);
            }
        }
    }
}
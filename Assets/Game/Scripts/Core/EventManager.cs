using System;
using System.Collections.Generic;
using UnityEngine;

public static class EventManager
{
    private static Dictionary<Type, List<Delegate>> eventListeners = new Dictionary<Type, List<Delegate>>();

    // Subscribe to event
    public static void Subscribe<T>(Action<T> listener) where T : IGameEvent
    {
        Type eventType = typeof(T);
        
        if (!eventListeners.ContainsKey(eventType))
            eventListeners[eventType] = new List<Delegate>();
            
        eventListeners[eventType].Add(listener);
    }

    // Unsubscribe from event
    public static void Unsubscribe<T>(Action<T> listener) where T : IGameEvent
    {
        Type eventType = typeof(T);
        
        if (eventListeners.ContainsKey(eventType))
        {
            eventListeners[eventType].Remove(listener);
            
            if (eventListeners[eventType].Count == 0)
                eventListeners.Remove(eventType);
        }
    }

    // Trigger event
    public static void Trigger<T>(T gameEvent) where T : IGameEvent
    {
        Type eventType = typeof(T);
        
        if (eventListeners.ContainsKey(eventType))
        {
            foreach (Action<T> listener in eventListeners[eventType])
            {
                try
                {
                    listener?.Invoke(gameEvent);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error in event {eventType.Name}: {e.Message}");
                }
            }
        }
    }

    // Clear all events (call on scene change)
    public static void Clear()
    {
        eventListeners.Clear();
    }
}

// Base interface for all events
public interface IGameEvent { }
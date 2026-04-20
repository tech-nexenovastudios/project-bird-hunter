using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class SerializableDictionary<TKey, TValue>
    : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
{
    [SerializeField, HideInInspector] private List<TKey> _keys = new();
    [SerializeField, HideInInspector] private List<TValue> _values = new();

    public void OnBeforeSerialize()
    {
        _keys.Clear(); _values.Clear();
        foreach (var kvp in this) { _keys.Add(kvp.Key); _values.Add(kvp.Value); }
    }

    public void OnAfterDeserialize()
    {
        Clear();
        for (int i = 0; i < _keys.Count; i++) this[_keys[i]] = _values[i];
    }
}
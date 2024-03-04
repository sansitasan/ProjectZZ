using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ScriptableObjectExtension
{
    // scriptable object Ŭ��
    public static T Clone<T>(this T scriptableObject) where T : ScriptableObject
    {
        if (scriptableObject == null)
        {
            Debug.LogError($"ScriptableObject is null. Returning default {typeof(T)} object.");
            return (T)ScriptableObject.CreateInstance(typeof(T));
        }

        T instance = Object.Instantiate(scriptableObject);
        instance.name = scriptableObject.name; // �̸����� (Clone) ����
        return instance;
    }
}

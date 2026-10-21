using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

public static class Utils
{
    public static T GetAPIData<T>(string s) where T : new()
    {
        T data = new T();
        data = JsonUtility.FromJson<T>(s);

        return data;
    }

    public static T[] FromJson<T>(string json)
    {
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
        return wrapper.datas;
    }

    [System.Serializable]
    public class Wrapper<T>
    {
        public T[] datas;
    }
}
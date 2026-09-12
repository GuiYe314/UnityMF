using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameObjectExpansion 
{
    public static  T _GetComponent<T>(this GameObject obj) where T : Component
    {
        T component = obj.GetComponent<T>();
        if (component == null) { component = obj.AddComponent<T>(); }
        return component;
    }


    public static GameObject _Instantiate(this GameObject obj, Transform parent, bool worldPositionStays)
    {

        if (obj == null) { return null; }

        GameObject newObj = GameObject.Instantiate(obj, parent, worldPositionStays);
        newObj.name = obj.name;

        return newObj;
    }

    public static GameObject _Instantiate(this GameObject obj, Transform parent)
    {

        if (obj == null) { return null; }

        GameObject newObj = GameObject.Instantiate(obj, parent);
        newObj.name = obj.name;

        return newObj;
    }
    public static GameObject _Instantiate(this GameObject obj)
    {

        if (obj == null) { return null; }

        GameObject newObj = GameObject.Instantiate(obj);
        newObj.name = obj.name;

        return newObj;
    }
}

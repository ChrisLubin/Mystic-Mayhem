using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// A static class for general helpful methods
/// </summary>
public static class Helpers
{
    public static T[] ToArray<T>(IReadOnlyList<T> readOnlyList)
    {
        List<T> list = new();

        foreach (T element in readOnlyList)
        {
            list.Add(element);
        }

        return list.ToArray<T>();
    }
}
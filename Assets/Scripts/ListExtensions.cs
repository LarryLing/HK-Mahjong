using System.Collections.Generic;
using UnityEngine;

public static class ListExtensions
{
    /// <summary>
    /// Shuffles a generic list in place using the Fisher-Yates algorithm.
    /// </summary>
    public static void Shuffle<T>(this List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            // UnityEngine.Random.Range is max-exclusive for integers
            int k = Random.Range(0, n + 1);

            // Swap values
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}

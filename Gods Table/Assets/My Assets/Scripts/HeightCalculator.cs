using UnityEngine;
using System.Collections;

public class HeightCalculator : MonoBehaviour
{
    // returns 0..1 value
    public static float GetValue(Vector2 location)
    {
        return Mathf.PerlinNoise(location.x, location.y);
    }
}

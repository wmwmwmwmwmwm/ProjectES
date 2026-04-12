using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public static partial class Util
{
	public static T GetRandomItem<T>(List<T> itemList, Func<T, int> weightGetter)
	{
		int sum = itemList.Sum(x => weightGetter(x));
		int value = Random.Range(0, sum);
		foreach (T item in itemList)
		{
			int weight = weightGetter(item);
			if (value < weight)
			{
				return item;
			}
			else
			{
				value -= weight;
			}
		}
		return itemList.First();
	}

	public static int Mod(int a, int b)
	{
		return (a % b + b) % b;
	}

	public static float Mod(float a, float b)
	{
        return a - b * Mathf.Floor(a / b);
    }

    public static float DirectionToRotationZ(Vector2 v)
    {
        v.Normalize();
        return Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
    }

    public static Vector2 RotationZToDirection(float degree)
    {
        float radian = degree * Mathf.Deg2Rad;
        return new(Mathf.Cos(radian), Mathf.Sin(radian));
	}

	public static int MultiplyToInt(float a, float b) => (int)(a * b);

	public static float DivideWithFloat(int a, int b) => b == 0 ? 0f : (float)a / b;

#if UNITY_EDITOR
    public static void Ping(Object obj)
	{
		UnityEditor.EditorGUIUtility.PingObject(obj);
	}

	public static void Pause()
    {
        UnityEditor.EditorApplication.isPaused = true;
	}

	public static string ToAssetPath(string fullPath)
	{
        string path2 = Path.GetRelativePath(Application.dataPath, fullPath);
		return $"Assets/{path2}";
	}

	public static void SetDirty(Object obj)
	{
		UnityEditor.EditorUtility.SetDirty(obj);
	}
#endif
}
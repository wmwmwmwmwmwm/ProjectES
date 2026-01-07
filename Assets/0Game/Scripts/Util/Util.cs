using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public static partial class Util
{
	//public static void CalcChance<T>(List<T> itemList, Func<T, int> weightGetter, Action<T, float> minSetter, Action<T, float> maxSetter)
	//{
	//	itemList.Sort((a, b) => weightGetter(b) - weightGetter(a));
	//	int totalChance = itemList.Sum(weightGetter);
	//	float lastChance = 0f;
	//	foreach (T item in itemList)
	//	{
	//		float chance = weightGetter(item) / (float)totalChance;
	//		minSetter(item, lastChance);
	//		maxSetter(item, lastChance + chance);
	//		lastChance += chance;
	//	}
	//}

	//public static T GetRandomItem<T>(List<T> itemList, Func<T, float> weightMinGetter, Func<T, float> weightMaxGetter)
	//{
	//	float value = Random.value;
	//	foreach (T item in itemList)
	//	{
	//		float minValue = weightMinGetter(item);
	//		float maxValue = weightMaxGetter(item);
	//		if (value >= minValue && value <= maxValue)
	//		{
	//			return item;
	//		}
	//	}
	//	return itemList.First();
	//}

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

	public static void TryAndAddDictionaryList<TKey, T>(this Dictionary<TKey, List<T>> dict, TKey key, T item)
	{
		if (!dict.ContainsKey(key)) dict.Add(key, new List<T>());
		dict[key].Add(item);
	}

	public static Vector2Int[] AllDirections = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

	public static Vector3 GridToWorldPoint(Vector2Int Grid)
	{
		return new Vector3(Grid.x, 0f, Grid.y);
	}

	public static Vector2Int WorldToGridPoint(Vector3 WorldCoordinate)
	{
		return new Vector2Int(Mathf.RoundToInt(WorldCoordinate.x), Mathf.RoundToInt(WorldCoordinate.z));
	}

	public static Vector2Int WorldToGridDirection(Vector3 EulerAngles)
	{
		float RotationY = Mod(EulerAngles.y, 360f);
		if (RotationY > 45f && RotationY <= 135f) return Vector2Int.right;
		else if (RotationY > 135f && RotationY <= 225f) return Vector2Int.down;
		else if (RotationY > 225f && RotationY <= 315f) return Vector2Int.left;
		else return Vector2Int.up;
	}

	public static Vector2Int GetRandomDirection()
	{
		float random = Random.value;
		if (random < 0.25f)
			return Vector2Int.up;
		else if (random < 0.5f)
			return Vector2Int.down;
		else if (random < 0.75f)
			return Vector2Int.left;
		else
			return Vector2Int.right;
	}

	public static Vector2Int[] GetLocalForwardLeftRight(Vector2Int ForwardDirection)
	{
		return new Vector2Int[] { ForwardDirection, TurnLeft(ForwardDirection), TurnRight(ForwardDirection) };
	}

	public static Vector2Int TurnRight(Vector2Int Direction)
	{
		if (Direction == Vector2Int.up)
			return Vector2Int.right;
		else if (Direction == Vector2Int.right)
			return Vector2Int.down;
		else if (Direction == Vector2Int.down)
			return Vector2Int.left;
		else
			return Vector2Int.up;
	}

	public static Vector2Int TurnLeft(Vector2Int Direction)
	{
		if (Direction == Vector2Int.up)
			return Vector2Int.left;
		else if (Direction == Vector2Int.left)
			return Vector2Int.down;
		else if (Direction == Vector2Int.down)
			return Vector2Int.right;
		else
			return Vector2Int.up;
	}

	public static Vector2Int TurnBackward(Vector2Int Direction)
	{
		if (Direction == Vector2Int.up)
			return Vector2Int.down;
		else if (Direction == Vector2Int.down)
			return Vector2Int.up;
		else if (Direction == Vector2Int.left)
			return Vector2Int.right;
		else
			return Vector2Int.left;
	}

	public static int DirectionToIndex(Vector2Int Direction)
	{
		if (Direction == Vector2Int.up)
			return 0;
		else if (Direction == Vector2Int.right)
			return 1;
		else if (Direction == Vector2Int.down)
			return 2;
		else
			return 3;
	}

	public static Vector2Int LocalToWorldDirection(Vector2Int v, Transform target)
	{
		Vector2Int TargetForward = WorldToGridPoint(target.forward);
		if (TargetForward == Vector2Int.up)
			return v;
		else if (TargetForward == Vector2Int.right)
			return TurnRight(v);
		else if (TargetForward == Vector2Int.down)
			return TurnBackward(v);
		else
			return TurnLeft(v);
	}

	public static Vector2Int WorldToLocalDirection(Vector2Int v, Transform target)
	{
		Vector2Int TargetForward = WorldToGridPoint(target.forward);
		if (TargetForward == Vector2Int.up)
			return v;
		else if (TargetForward == Vector2Int.right)
			return TurnLeft(v);
		else if (TargetForward == Vector2Int.down)
			return TurnBackward(v);
		else
			return TurnRight(v);
	}

	public static int Mod(int a, int b)
	{
		return (a % b + b) % b;
	}

	public static float Mod(float a, float b)
	{
        return a - b * Mathf.Floor(a / b);
    }

	public static float ClampInnerAngle(float angle)
	{
		if (angle < -180)
		{
			angle += 360;
		}
		if (angle > 180)
		{
			angle -= 360;
		}
		return angle;
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
#endif
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public static partial class Util
{
	public static Vector3 WithX(this Vector3 v, float x)
	{
		v.x = x;
		return v;
	}

	public static Vector3 WithY(this Vector3 v, float y)
	{
		v.y = y;
		return v;
	}

	public static Vector3 WithZ(this Vector3 v, float z)
	{
		v.z = z;
		return v;
	}

	public static Color WithAlpha(this Color color, float alpha)
	{
		color.a = alpha;
		return color;
	}

	public static Vector2 WithX(this Vector2 v, float x)
	{
		v.x = x;
		return v;
	}

	public static Vector2 WithY(this Vector2 v, float y)
	{
		v.y = y;
		return v;
	}

	public static T PickOne<T>(this List<T> list)
	{
		return list[Random.Range(0, list.Count)];
	}

	public static void ShuffleList<T>(this List<T> list)
	{
		for (int i = 0; i < list.Count; i++)
		{
			int RandomIndex = Random.Range(0, list.Count);
			(list[RandomIndex], list[i]) = (list[i], list[RandomIndex]);
		}
	}

	public static List<T> PickList<T>(this List<T> list, int count)
	{
		List<T> duplicateList = new List<T>(list);
		ShuffleList(duplicateList);
		if (duplicateList.Count <= count)
			return duplicateList;
		duplicateList.RemoveRange(count, duplicateList.Count - count);
		return duplicateList;
	}

	public static void DestroyElement<T>(this ICollection<T> collection, T element) where T : MonoBehaviour
	{
		collection.Remove(element);
		Object.Destroy(element.gameObject);
	}

	public static void DestroyElements<T>(this ICollection<T> collection) where T : MonoBehaviour
	{
		foreach (T element in collection)
		{
			Object.Destroy(element.gameObject);
		}
		collection.Clear();
	}

	public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector)
	{
		return source.MinBy(selector, null);
	}

	public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey> comparer)
	{
		comparer ??= Comparer<TKey>.Default;
		using IEnumerator<TSource> sourceIterator = source.GetEnumerator();
		if (!sourceIterator.MoveNext())
		{
			return default;
		}
		TSource min = sourceIterator.Current;
		TKey minKey = selector(min);
		while (sourceIterator.MoveNext())
		{
			TSource candidate = sourceIterator.Current;
			TKey candidateProjected = selector(candidate);
			if (comparer.Compare(candidateProjected, minKey) < 0)
			{
				min = candidate;
				minKey = candidateProjected;
			}
		}
		return min;
	}
	public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector)
	{
		return source.MaxBy(selector, null);
	}

	public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey> comparer)
	{
		comparer ??= Comparer<TKey>.Default;
		using IEnumerator<TSource> sourceIterator = source.GetEnumerator();
		if (!sourceIterator.MoveNext())
		{
			return default;
		}
		TSource max = sourceIterator.Current;
		TKey maxKey = selector(max);
		while (sourceIterator.MoveNext())
		{
			TSource candidate = sourceIterator.Current;
			TKey candidateProjected = selector(candidate);
			if (comparer.Compare(candidateProjected, maxKey) > 0)
			{
				max = candidate;
				maxKey = candidateProjected;
			}
		}
		return max;
	}

	public static T ParseEnum<T>(this string str) where T : struct
	{
		return Enum.Parse<T>(str);
	}

	public static T TryParseEnum<T>(this string str) where T : struct
	{
		Enum.TryParse(str, out T t);
		return t;
	}

	public static float ParseFloat(this string str, bool allowNull = false)
	{
		if (!float.TryParse(str, out float f))
		{
			if (allowNull)
			{
				int.TryParse(str, out int i);
				f = i;
			}
			else
			{
				f = int.Parse(str);
			}
		}
		return f;
	}

	public static int ParseInt(this string str)
	{
		return int.Parse(str);
	}

	public static int TryParseInt(this string str)
	{
		int.TryParse(str, out int i);
		return i;
	}

	public static bool ToBool(this string str) => !string.IsNullOrWhiteSpace(str);

	public static string ToPercentString(this float f)
	{
		int Percent = (int)((f + 0.001f) * 100f);
		return string.Format("{0}%", Percent);
	}

	public static Vector3 Vector2ToXZ(this Vector2 v) => new(v.x, 0f, v.y);
	public static Vector3 Vector2ToXY(this Vector2 v) => new(v.x, v.y, 0f);
	public static Vector2 Vector2(this Vector3 v) => new(v.x, v.y);

	public static Vector2 WorldToCanvas(this Canvas canvas, Camera camera, Vector3 world_position)
	{
		Vector3 viewport_position = camera.WorldToViewportPoint(world_position);
		RectTransform canvas_rect = canvas.GetComponent<RectTransform>();

		return new Vector2((viewport_position.x - 0.5f) * canvas_rect.sizeDelta.x, 
			(viewport_position.y - 0.5f) * canvas_rect.sizeDelta.y);
	}

	public static Vector3 GetRandomPointInsideCollider(this BoxCollider2D boxCollider)
	{
		Vector2 extents = boxCollider.size / 2f;
		Vector2 point = boxCollider.offset + new Vector2(
			Random.Range(-extents.x, extents.x),
			Random.Range(-extents.y, extents.y));
		return boxCollider.transform.TransformPoint(point);
	}

	public static Component TryAddComponent(this GameObject obj, string typeStr) 
	{
		Type type = Type.GetType(typeStr);
		if (type == null) return null;
		Component component = obj.GetComponent(type);
		if (!component)
		{
			component = obj.AddComponent(type);
		}
		return component;
	}

	public static T TryAddComponent<T>(this GameObject obj) where T : Component
	{
		T component = obj.GetComponent<T>();
		if (!component)
		{
			component = obj.AddComponent<T>();
		}
		return component;
	}

	public static void RemoveComponents<T>(this Component c, string dontRemoveName) where T : Component
	{
		if (Application.IsPlaying(c)) return;
		List<T> comps = c.GetComponents<T>().ToList();
		foreach (T comp in comps)
		{
			if (comp.GetType().Name == dontRemoveName) continue;
			Object.DestroyImmediate(comp, true);
		}
	}

	//public static UniTask BP(this UniTask uniTask, MonoBehaviour behaviour, CancellationTokenSource source = null)
	//{
	//	if (source != null)
	//	{
	//		return uniTask.AttachExternalCancellation(source.Token)
	//			.AttachExternalCancellation(behaviour.destroyCancellationToken)
	//			.AttachExternalCancellation(Application.exitCancellationToken)
	//			.SuppressCancellationThrow();
	//	}
	//	else
	//	{
	//		return uniTask.AttachExternalCancellation(behaviour.destroyCancellationToken)
	//			.AttachExternalCancellation(Application.exitCancellationToken)
	//			.SuppressCancellationThrow();
	//	}
	//}

	//public static UniTask<(bool, T)> BP<T>(this UniTask<T> uniTask, MonoBehaviour behaviour, CancellationTokenSource source = null)
	//{
	//	if (source != null)
	//	{
	//		return uniTask.AttachExternalCancellation(source.Token)
	//			.AttachExternalCancellation(behaviour.destroyCancellationToken)
	//			.AttachExternalCancellation(Application.exitCancellationToken)
	//			.SuppressCancellationThrow();
	//	}
	//	else
	//	{
	//		return uniTask.AttachExternalCancellation(behaviour.destroyCancellationToken)
	//			.AttachExternalCancellation(Application.exitCancellationToken)
	//			.SuppressCancellationThrow();
	//	}
	//}

	public static bool IsName(this Animator animator, string stateName) => animator.GetCurrentAnimatorStateInfo(0).IsName(stateName);

	public static bool IsComplete(this Animator animator) => animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.99f;
}

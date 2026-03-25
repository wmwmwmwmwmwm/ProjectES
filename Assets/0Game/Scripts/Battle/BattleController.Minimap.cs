using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using static SingletonManager;

namespace Battle
{
	public partial class BattleController 
	{
		[Header("미니맵")]
		public Transform _MinimapCameraHandle;
		public Camera _MinimapCamera;
		public RawImage _MinimapTraceImage;
		public RawImage _MinimapImage;
		public RenderTexture _MinimapRT;
		public MinimapMarker _MinimapMarker_Player, _MinimapMarker_Enemy;
		public Transform _MinimapMarkerParent;

		int _SightRange;
		List<MinimapMarker> _MinimapMarkers;
		Vector2Int _TraceTextureSize;
		Texture2D _MinimapTraceTexture;
		Color32[] _TraceColorArray;
		Vector3[] _WorldBoundCorners;

		void InitMinimap()
		{
			_MinimapMarkers = new();
			RenderTexture minimapRT = new(_MinimapRT);
			_WorldBoundCorners = new Vector3[4];
			_WorldBound.GetWorldCorners(_WorldBoundCorners);
			Vector2 padding = _MinimapImage.GetComponent<RectTransform>().sizeDelta;
			_TraceTextureSize = _WorldBound.sizeDelta.ToVector2Int() * 3;
			_TraceTextureSize += padding.ToVector2Int();
			_SightRange = 30;
			_MinimapTraceTexture = new(
				width: _TraceTextureSize.x,
				height: _TraceTextureSize.y,
				textureFormat: TextureFormat.RGBA32,
				mipChain: false);
			Color32[] initColors = _MinimapTraceTexture.GetPixels32();
			Color32 black = new(0, 0, 0, 255);
			for (int i = 0; i < initColors.Length; i++)
			{
				initColors[i] = black;
			}
			_MinimapTraceTexture.SetPixels32(initColors);
			_TraceColorArray = new Color32[_SightRange * _SightRange];
			Color32 clear = new(0, 0, 0, 0);
			for (int x = 0; x < _SightRange; x++)
			{
				for (int y = 0; y < _SightRange; y++)
				{
					_TraceColorArray[x * _SightRange + y] = clear;
				}
			}
			_MinimapTraceImage.texture = _MinimapTraceTexture;
			Vector2 worldBoundSizeRate = _WorldBound.sizeDelta / (_MinimapCamera.orthographicSize * 2f);
			_MinimapTraceImage.transform.localScale = new(worldBoundSizeRate.x, worldBoundSizeRate.y, 1f);
			_MinimapCamera.targetTexture = minimapRT;
			_MinimapImage.texture = minimapRT;
			_MinimapMarker_Player.gameObject.SetActive(false);
			_MinimapMarker_Enemy.gameObject.SetActive(false);
		}

		void AddMinimapMarker(Character character, bool isPlayer)
		{
			MinimapMarker prefab = isPlayer ? _MinimapMarker_Player : _MinimapMarker_Enemy;
			MinimapMarker marker = Instantiate(prefab, _MinimapMarkerParent);
			marker._Character = character;
			marker.gameObject.SetActive(true);
			_MinimapMarkers.Add(marker);
		}

		public void RemoveMinimapMarker(Character character)
		{
			MinimapMarker marker = _MinimapMarkers.Find(x => x._Character == character);
			Destroy(marker.gameObject);
			_MinimapMarkers.Remove(marker);
		}

		void UpdateMinimap()
		{
			_MinimapCameraHandle.position = _ActivePlayer.transform.position;

			// 미니맵 좌표
			Rect minimapRect = _MinimapImage.GetComponent<RectTransform>().rect;
			RectTransform traceImageRT = _MinimapTraceImage.GetComponent<RectTransform>();
			Vector3 viewportPos2 = _MinimapCamera.WorldToViewportPoint(_WorldBound.transform.position);
			traceImageRT.anchoredPosition = new Vector2(
				(viewportPos2.x - 0.5f) * minimapRect.width,
				(viewportPos2.y - 0.5f) * minimapRect.height);
			foreach (MinimapMarker marker in _MinimapMarkers)
			{
				marker.gameObject.SetActive(marker._Character.isActiveAndEnabled);
				Vector3 viewportPos = _MinimapCamera.WorldToViewportPoint(marker._Character.transform.position);
				marker.GetComponent<RectTransform>().anchoredPosition = new Vector2(
					(viewportPos.x - 0.5f) * minimapRect.width,
					(viewportPos.y - 0.5f) * minimapRect.height);
			}

			// 미니맵 밝히기
			Vector2 coord = (_ActivePlayer.transform.position - _WorldBoundCorners[0]).XZToVector2();
			coord /= _WorldBound.sizeDelta;
			coord *= _TraceTextureSize;
			Vector2Int coordInt = coord.ToVector2Int();
			int texX = coordInt.x - _SightRange / 2;
			int texY = coordInt.y - _SightRange / 2;
			_MinimapTraceTexture.SetPixels32(texX, texY, _SightRange, _SightRange, _TraceColorArray);
			_MinimapTraceTexture.Apply();
		}
	}
}

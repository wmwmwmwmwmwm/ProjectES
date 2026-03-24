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

		List<MinimapMarker> _MinimapMarkers;
		Texture2D _MinimapTraceTexture;

		void InitMinimap()
		{
			_MinimapMarkers = new();
			RenderTexture minimapRT = new(_MinimapRT);
			_MinimapTraceTexture = new(
				width: 2048,
				height: 2048,
				textureFormat: TextureFormat.RGBA32,
				mipChain: false);
			Color32[] initColors = _MinimapTraceTexture.GetPixels32();
			Color32 black = new(0, 0, 0, 255);
			for (int i = 0; i < initColors.Length; i++)
			{
				initColors[i] = black;
			}
			_MinimapTraceTexture.SetPixels32(initColors);
			_MinimapTraceImage.texture = _MinimapTraceTexture;
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
			foreach (MinimapMarker marker in _MinimapMarkers)
			{
				marker.gameObject.SetActive(marker._Character.isActiveAndEnabled);
				Vector3 viewportPos = _MinimapCamera.WorldToViewportPoint(marker._Character.transform.position);
				marker.GetComponent<RectTransform>().anchoredPosition = new Vector2(
					(viewportPos.x - 0.5f) * minimapRect.width,
					(viewportPos.y - 0.5f) * minimapRect.height);
			}

			// 흔적 남기기
			Vector2 coord = _ActivePlayer.transform.position.XZToVector2();
			Vector2Int coordInt = new((int)coord.x, (int)coord.y);
			int range = 30;
			Color32[] traceColors = new Color32[range * range];
			Color32 clear = new(0, 0, 0, 0);
			for (int x = 0; x < range; x++)
			{
				for (int y = 0; y < range; y++)
				{
					traceColors[x * range + y] = clear;
				}
			}
			int texX = coordInt.x - range / 2;
			texX = Mathf.Clamp(texX, 0, 2048 - 1);
			int texY = coordInt.y - range / 2;
			texY = Mathf.Clamp(texY, 0, 2048 - 1);
			_MinimapTraceTexture.SetPixels32(texX, texY, range, range, traceColors);
		}
	}
}

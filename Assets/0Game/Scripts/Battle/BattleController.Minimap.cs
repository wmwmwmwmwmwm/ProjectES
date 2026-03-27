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
		public AnimationCurve _MinimapSightCurve;

		int _SightRange;
		List<MinimapMarker> _MinimapMarkers;
		Vector2Int _TraceTextureSize;
		Texture2D _MinimapTraceTexture;
		Color32[] _BlurColorArray;
		Color32[] _TraceColorArray;
		Vector3[] _WorldBoundCorners;

		void InitMinimap()
		{
			_MinimapMarkers = new();
			RenderTexture minimapRT = new(_MinimapRT);
			_WorldBoundCorners = new Vector3[4];
			_WorldBound.GetWorldCorners(_WorldBoundCorners);
			_TraceTextureSize = _WorldBound.sizeDelta.ToVector2Int() * 3;
			_SightRange = 200;

			// 흔적 픽셀배열 설정
			_BlurColorArray = new Color32[_SightRange * _SightRange];
			for (int y = 0; y < _SightRange; y++)
			{
				float yNormalized = (float)y / _SightRange;
				for (int x = 0; x < _SightRange; x++)
				{
					float xNormalized = (float)x / _SightRange;
					float alphaF = _MinimapSightCurve.Evaluate(xNormalized) * _MinimapSightCurve.Evaluate(yNormalized);
					alphaF *= alphaF;
					byte alpha = (byte)(255f - alphaF * 255f);
					Color32 color = new(0, 0, 0, alpha);
					_BlurColorArray[y * _SightRange + x] = color;
				}
			}
			_TraceColorArray = new Color32[_SightRange * _SightRange];

			// 흔적 텍스처 설정
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
				bool active = marker._Character.isActiveAndEnabled;
				Vector2Int traceTextureCoord = GetTraceTextureCoord(marker._Character.transform.position);
				active &= _MinimapTraceTexture.GetPixel(traceTextureCoord.x, traceTextureCoord.y).a < 1f;
				marker.gameObject.SetActive(active);
				Vector3 viewportPos = _MinimapCamera.WorldToViewportPoint(marker._Character.transform.position);
				marker.GetComponent<RectTransform>().anchoredPosition = new Vector2(
					(viewportPos.x - 0.5f) * minimapRect.width,
					(viewportPos.y - 0.5f) * minimapRect.height);
			}

			// 미니맵 밝히기
			Vector2Int coord = GetTraceTextureCoord(_ActivePlayer.transform.position);
			int texX = coord.x - _SightRange / 2;
			int texY = coord.y - _SightRange / 2;
			for (int y = 0; y < _SightRange; y++)
			{
				for (int x = 0; x < _SightRange; x++)
				{
					int index = y * _SightRange + x;
					Color32 color = _BlurColorArray[index];
					byte textureAlpha = (byte)(_MinimapTraceTexture.GetPixel(texX + x, texY + y).a * 255f);
					color.a = color.a < textureAlpha ? color.a : textureAlpha;
					_TraceColorArray[index] = color;
				}
			}
			_MinimapTraceTexture.SetPixels32(texX, texY, _SightRange, _SightRange, _TraceColorArray);
			_MinimapTraceTexture.Apply();

			Vector2Int GetTraceTextureCoord(Vector3 worldPos)
			{
				Vector2 coord = (worldPos - _WorldBoundCorners[0]).XZToVector2();
				coord /= _WorldBound.sizeDelta;
				coord *= _TraceTextureSize;
				return coord.ToVector2Int();
			}
		}

		//float[,] CreateGaussianKernel(int size, float sigma)
		//{
		//	float[,] kernel = new float[size, size];
		//	int radius = size / 2;
		//	float sum = 0;
		//	float constant = 1.0f / (2.0f * Mathf.PI * Mathf.Pow(sigma, 2));

		//	for (int y = -radius; y < radius; y++)
		//	{
		//		for (int x = -radius; x < radius; x++)
		//		{
		//			// 가우시안 공식: G(x,y) = (1 / 2πσ²) * e^(-(x²+y²)/2σ²)
		//			float distance = Mathf.Pow(x, 2) + Mathf.Pow(y, 2);
		//			kernel[y + radius, x + radius] = constant * Mathf.Exp(-distance / (2.0f * Mathf.Pow(sigma, 2)));
		//			sum += kernel[y + radius, x + radius];
		//		}
		//	}

		//	// 정규화: 커널 값의 합이 1이 되도록 함
		//	for (int y = 0; y < size; y++)
		//	{
		//		for (int x = 0; x < size; x++)
		//		{
		//			print(kernel[y, x]);
		//			kernel[y, x] /= sum;
		//		}
		//	}

		//	return kernel;
		//}
	}
}

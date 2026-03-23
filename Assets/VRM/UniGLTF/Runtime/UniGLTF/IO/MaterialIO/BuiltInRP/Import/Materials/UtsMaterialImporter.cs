//using System;
//using System.Collections.Generic;
//using UnityEngine;

//namespace UniGLTF
//{
//	public class UtsUnlitMaterialImporter
//	{
//		private static readonly int Cutoff = Shader.PropertyToID("_Cutoff");

//		public Shader Shader { get; set; }

//		public UtsUnlitMaterialImporter(Shader shader = null)
//		{
//			Shader = shader != null ? shader : Shader.Find(UniUnlitUtil.ShaderName);
//		}

//		public bool TryCreateParam(GltfData data, int i, out MaterialDescriptor matDesc)
//		{
//			if (i < 0 || i >= data.GLTF.materials.Count)
//			{
//				matDesc = default;
//				return false;
//			}

//			var src = data.GLTF.materials[i];
//			if (!glTF_KHR_materials_unlit.IsEnable(src))
//			{
//				matDesc = default;
//				return false;
//			}

//			var colors = new Dictionary<string, Color>();
//			var textureSlots = new Dictionary<string, TextureDescriptor>();

//			// color
//			var baseColorFactor = GltfMaterialImportUtils.ImportLinearBaseColorFactor(data, src);
//			if (baseColorFactor.HasValue)
//			{
//				colors.Add("_Color", baseColorFactor.Value.gamma);
//			}

//			// texture
//			if (src.pbrMetallicRoughness.baseColorTexture != null)
//			{
//				var (offset, scale) = GltfTextureImporter.GetTextureOffsetAndScale(src.pbrMetallicRoughness.baseColorTexture);
//				if (GltfTextureImporter.TryCreateSrgb(data, src.pbrMetallicRoughness.baseColorTexture.index, offset, scale, out var key, out var desc))
//				{
//					textureSlots.Add("_MainTex", desc);
//				}
//			}

//			matDesc = new MaterialDescriptor(
//				GltfMaterialImportUtils.ImportMaterialName(i, src),
//				Shader,
//				null,
//				textureSlots,
//				new Dictionary<string, float>(),
//				colors,
//				new Dictionary<string, Vector4>(),
//				new Action<Material>[]
//				{
//					//renderMode
//					material =>
//					{
//						switch (src.alphaMode)
//						{
//							case "OPAQUE":
//								UniUnlitUtil.SetRenderMode(material, UniUnlitRenderMode.Opaque);
//								break;
//							case "BLEND":
//								UniUnlitUtil.SetRenderMode(material, UniUnlitRenderMode.Transparent);
//								break;
//							case "MASK":
//								UniUnlitUtil.SetRenderMode(material, UniUnlitRenderMode.Cutout);
//								material.SetFloat(Cutoff, src.alphaCutoff);
//								break;
//							default:
//								// default OPAQUE
//								UniUnlitUtil.SetRenderMode(material, UniUnlitRenderMode.Opaque);
//								break;
//						}

//						// culling
//						if (src.doubleSided)
//						{
//							UniUnlitUtil.SetCullMode(material, UniUnlitCullMode.Off);
//						}
//						else
//						{
//							UniUnlitUtil.SetCullMode(material, UniUnlitCullMode.Back);
//						}

//						// VColor
//						var hasVertexColor = data.MaterialHasVertexColor(i);
//						if (hasVertexColor)
//						{
//							UniUnlitUtil.SetVColBlendMode(material, UniUnlitVertexColorBlendOp.Multiply);
//						}

//						UniUnlitUtil.ValidateProperties(material, true);
//					}
//				}
//			);

//			return true;
//		}

//		public MaterialDescriptor CreateParam(string materialName = null)
//		{
//			// FIXME
//			return new MaterialDescriptor(
//				string.IsNullOrEmpty(materialName) ? "__default__" : materialName,
//				Shader,
//				default,
//				new Dictionary<string, TextureDescriptor>(),
//				new Dictionary<string, float>(),
//				new Dictionary<string, Color>(),
//				new Dictionary<string, Vector4>(),
//				new List<Action<Material>>()
//			);
//		}
//	}
//	public enum UniUnlitRenderMode
//	{
//		Opaque = 0,
//		Cutout = 1,
//		Transparent = 2,
//	}

//	public enum UniUnlitCullMode
//	{
//		Off = 0,
//		// Front = 1,
//		Back = 2,
//	}

//	public enum UniUnlitVertexColorBlendOp
//	{
//		None = 0,
//		Multiply = 1,
//	}

//	public static class UniUnlitUtil
//	{
//		public const string ShaderName = "UniGLTF/UniUnlit";
//		public const string PropNameMainTex = "_MainTex";
//		public const string PropNameColor = "_Color";
//		public const string PropNameCutoff = "_Cutoff";
//		public const string PropNameBlendMode = "_BlendMode";
//		public const string PropNameCullMode = "_CullMode";
//		[Obsolete("Use PropNameVColBlendMode")]
//		public const string PropeNameVColBlendMode = PropNameVColBlendMode;
//		public const string PropNameVColBlendMode = "_VColBlendMode";
//		public const string PropNameSrcBlend = "_SrcBlend";
//		public const string PropNameDstBlend = "_DstBlend";
//		public const string PropNameZWrite = "_ZWrite";

//		public const string PropNameStandardShadersRenderMode = "_Mode";

//		public const string KeywordAlphaTestOn = "_ALPHATEST_ON";
//		public const string KeywordAlphaBlendOn = "_ALPHABLEND_ON";
//		public const string KeywordVertexColMul = "_VERTEXCOL_MUL";

//		public const string TagRenderTypeKey = "RenderType";
//		public const string TagRenderTypeValueOpaque = "Opaque";
//		public const string TagRenderTypeValueTransparentCutout = "TransparentCutout";
//		public const string TagRenderTypeValueTransparent = "Transparent";

//		public static void SetRenderMode(Material material, UniUnlitRenderMode mode)
//		{
//			material.SetInt(PropNameBlendMode, (int)mode);
//		}

//		public static void SetCullMode(Material material, UniUnlitCullMode mode)
//		{
//			material.SetInt(PropNameCullMode, (int)mode);
//		}

//		public static void SetVColBlendMode(Material material, UniUnlitVertexColorBlendOp mode)
//		{
//			material.SetInt(PropNameVColBlendMode, (int)mode);
//		}

//		public static UniUnlitRenderMode GetRenderMode(Material material)
//		{
//			return (UniUnlitRenderMode)material.GetInt(PropNameBlendMode);
//		}

//		public static UniUnlitCullMode GetCullMode(Material material)
//		{
//			return (UniUnlitCullMode)material.GetInt(PropNameCullMode);
//		}

//		public static UniUnlitVertexColorBlendOp GetVColBlendMode(Material material)
//		{
//			return (UniUnlitVertexColorBlendOp)material.GetInt(PropNameVColBlendMode);
//		}

//		/// <summary>
//		/// Validate target material's UniUnlitRenderMode, UniUnlitVertexColorBlendOp.
//		/// Set appropriate hidden properties & keywords.
//		/// This will change RenderQueue independent to UniUnlitRenderMode if isRenderModeChangedByUser is true.
//		/// </summary>
//		/// <param name="material">Target material</param>
//		/// <param name="isRenderModeChangedByUser">Is changed by user</param>
//		public static void ValidateProperties(Material material, bool isRenderModeChangedByUser = false)
//		{
//			SetupBlendMode(material, (UniUnlitRenderMode)material.GetFloat(PropNameBlendMode),
//				isRenderModeChangedByUser);
//			SetupVertexColorBlendOp(material, (UniUnlitVertexColorBlendOp)material.GetFloat(PropNameVColBlendMode));
//		}

//		private static void SetupBlendMode(Material material, UniUnlitRenderMode renderMode,
//			bool isRenderModeChangedByUser = false)
//		{
//			switch (renderMode)
//			{
//				case UniUnlitRenderMode.Opaque:
//					material.SetOverrideTag(TagRenderTypeKey, TagRenderTypeValueOpaque);
//					material.SetInt(PropNameSrcBlend, (int)BlendMode.One);
//					material.SetInt(PropNameDstBlend, (int)BlendMode.Zero);
//					material.SetInt(PropNameZWrite, 1);
//					SetKeyword(material, KeywordAlphaTestOn, false);
//					SetKeyword(material, KeywordAlphaBlendOn, false);
//					if (isRenderModeChangedByUser) material.renderQueue = -1;
//					break;
//				case UniUnlitRenderMode.Cutout:
//					material.SetOverrideTag(TagRenderTypeKey, TagRenderTypeValueTransparentCutout);
//					material.SetInt(PropNameSrcBlend, (int)BlendMode.One);
//					material.SetInt(PropNameDstBlend, (int)BlendMode.Zero);
//					material.SetInt(PropNameZWrite, 1);
//					SetKeyword(material, KeywordAlphaTestOn, true);
//					SetKeyword(material, KeywordAlphaBlendOn, false);
//					if (isRenderModeChangedByUser) material.renderQueue = (int)RenderQueue.AlphaTest;
//					break;
//				case UniUnlitRenderMode.Transparent:
//					material.SetOverrideTag(TagRenderTypeKey, TagRenderTypeValueTransparent);
//					material.SetInt(PropNameSrcBlend, (int)BlendMode.SrcAlpha);
//					material.SetInt(PropNameDstBlend, (int)BlendMode.OneMinusSrcAlpha);
//					material.SetInt(PropNameZWrite, 0);
//					SetKeyword(material, KeywordAlphaTestOn, false);
//					SetKeyword(material, KeywordAlphaBlendOn, true);
//					if (isRenderModeChangedByUser) material.renderQueue = (int)RenderQueue.Transparent;
//					break;
//			}
//		}

//		private static void SetupVertexColorBlendOp(Material material, UniUnlitVertexColorBlendOp vColBlendOp)
//		{
//			switch (vColBlendOp)
//			{
//				case UniUnlitVertexColorBlendOp.None:
//					SetKeyword(material, KeywordVertexColMul, false);
//					break;
//				case UniUnlitVertexColorBlendOp.Multiply:
//					SetKeyword(material, KeywordVertexColMul, true);
//					break;
//			}
//		}

//		private static void SetKeyword(Material mat, string keyword, bool required)
//		{
//			if (required)
//				mat.EnableKeyword(keyword);
//			else
//				mat.DisableKeyword(keyword);
//		}
//	}
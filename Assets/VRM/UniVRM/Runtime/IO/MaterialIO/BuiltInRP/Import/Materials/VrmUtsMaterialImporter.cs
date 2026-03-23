using System;
using System.Collections.Generic;
using System.Linq;
using UniGLTF;
using UnityEngine;

namespace VRM
{
	public class VrmUtsMaterialImporter
	{
		private static readonly string[] UtsTextureSlots = new string[]
		{
			//"_MainTex",
			//"_ShadeTexture",
			//"_BumpMap",
			//"_EmissionMap",
			//"_OutlineWidthTexture",
			//"_ReceiveShadowTexture",
			//"_RimTexture",
			//"_ShadingGradeTexture",
			//"_SphereAdd",
			//"_UvAnimMaskTexture",
			"_MainTex",
		};

		public Shader UTS { get; set; }

		public VrmUtsMaterialImporter()
		{
			UTS = Shader.Find("Universal Render Pipeline/Toon");
		}

		public bool TryCreateParam(GltfData data, glTF_VRM_extensions vrm, int materialIdx, out MaterialDescriptor matDesc)
		{
			if (vrm?.materialProperties == null || vrm.materialProperties.Count == 0)
			{
				matDesc = default;
				return false;
			}

			if (materialIdx < 0 || materialIdx >= vrm.materialProperties.Count)
			{
				matDesc = default;
				return false;
			}

			glTF_VRM_Material vrmMaterial = vrm.materialProperties[materialIdx];
			if (vrmMaterial.shader == glTF_VRM_Material.VRM_USE_GLTFSHADER)
			{
				// fallback to gltf
				matDesc = default;
				return false;
			}

			string name = data.GLTF.materials[materialIdx].name;
			string shaderName = vrmMaterial.shader;

			//Shader shader = shaderName switch
			//{
			//	"VRM/MToon" => UTS,
			//	"Unlit/Texture" => UnlitTextureShader,
			//	"Unlit/Transparent" => UnlitTransparentShader,
			//	"Unlit/Transparent Cutout" => UnlitTransparentCutoutShader,
			//	UniGLTF.UniUnlit.UniUnlitUtil.ShaderName => UniUnlitShader,
			//	_ => Shader.Find(shaderName),
			//};
			Shader shader = UTS;

			Dictionary<string, TextureDescriptor> textureSlots = new();
			Dictionary<string, float> floatValues = new();
			Dictionary<string, Color> colors = new();
			Dictionary<string, Vector4> vectors = new();
			List<Action<Material>> actions = new();
			matDesc = new MaterialDescriptor(
				name,
				shader,
				vrmMaterial.renderQueue,
				textureSlots,
				floatValues,
				colors,
				vectors,
				actions);

			foreach (KeyValuePair<string, float> kv in vrmMaterial.floatProperties)
			{
				floatValues.Add(kv.Key, kv.Value);
			}

			foreach (KeyValuePair<string, float[]> kv in vrmMaterial.vectorProperties)
			{
				// vector4 exclude TextureOffsetScale
				if (UtsTextureSlots.Contains(kv.Key)) continue;
				Vector4 v = new(kv.Value[0], kv.Value[1], kv.Value[2], kv.Value[3]);
				vectors.Add(kv.Key, v);
			}

			foreach (KeyValuePair<string, int> kv in vrmMaterial.textureProperties)
			{
				if (VRMMToonTextureImporter.TryGetTextureFromMaterialProperty(data, vrmMaterial, kv.Key,
					out SubAssetKey key, out TextureDescriptor desc))
				{
					textureSlots.Add(kv.Key, desc);
				}
			}

			foreach (KeyValuePair<string, bool> kv in vrmMaterial.keywordMap)
			{
				if (kv.Value)
				{
					actions.Add(material => material.EnableKeyword(kv.Key));
				}
				else
				{
					actions.Add(material => material.DisableKeyword(kv.Key));
				}
			}

			foreach (KeyValuePair<string, string> kv in vrmMaterial.tagMap)
			{
				actions.Add(material => material.SetOverrideTag(kv.Key, kv.Value));
			}

			//if (vrmMaterial.shader == MToon.Utils.ShaderName)
			//{
			//	floatValues[MToon.Utils.PropVersion] = MToon.Utils.VersionNumber;
			//}

			return true;
		}
	}
}
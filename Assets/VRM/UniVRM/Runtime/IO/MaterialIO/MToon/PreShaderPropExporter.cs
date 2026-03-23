namespace VRM
{
	/// <summary>
	/// TODO: MToon にひきとってもらう
	/// </summary>
	public static class PreShaderPropExporter
	{
		//public static ShaderProps GetPropsForMToon() => VRM_MToon;

		//static ShaderProps VRM_MToon = new()
		//{
		//	Properties = new ShaderProperty[]{ new("_Cutoff", ShaderPropertyType.Range)
		//		, new("_Color", ShaderPropertyType.Color)
		//		, new("_ShadeColor", ShaderPropertyType.Color)
		//		, new("_MainTex", ShaderPropertyType.TexEnv)
		//		, new("_ShadeTexture", ShaderPropertyType.TexEnv)
		//		, new("_BumpScale", ShaderPropertyType.Float)
		//		, new("_BumpMap", ShaderPropertyType.TexEnv)
		//		, new("_ReceiveShadowRate", ShaderPropertyType.Range)
		//		, new("_ReceiveShadowTexture", ShaderPropertyType.TexEnv)
		//		, new("_ShadingGradeRate", ShaderPropertyType.Range)
		//		, new("_ShadingGradeTexture", ShaderPropertyType.TexEnv)
		//		, new("_ShadeShift", ShaderPropertyType.Range)
		//		, new("_ShadeToony", ShaderPropertyType.Range)
		//		, new("_LightColorAttenuation", ShaderPropertyType.Range)
		//		, new("_IndirectLightIntensity", ShaderPropertyType.Range)
		//		, new("_RimColor", ShaderPropertyType.Color)
		//		, new("_RimTexture", ShaderPropertyType.TexEnv)
		//		, new("_RimLightingMix", ShaderPropertyType.Range)
		//		, new("_RimFresnelPower", ShaderPropertyType.Range)
		//		, new("_RimLift", ShaderPropertyType.Range)
		//		, new("_SphereAdd", ShaderPropertyType.TexEnv)
		//		, new("_EmissionColor", ShaderPropertyType.Color)
		//		, new("_EmissionMap", ShaderPropertyType.TexEnv)
		//		, new("_OutlineWidthTexture", ShaderPropertyType.TexEnv)
		//		, new("_OutlineWidth", ShaderPropertyType.Range)
		//		, new("_OutlineScaledMaxDistance", ShaderPropertyType.Range)
		//		, new("_OutlineColor", ShaderPropertyType.Color)
		//		, new("_OutlineLightingMix", ShaderPropertyType.Range)
		//		, new("_UvAnimMaskTexture", ShaderPropertyType.TexEnv)
		//		, new("_UvAnimScrollX", ShaderPropertyType.Float)
		//		, new("_UvAnimScrollY", ShaderPropertyType.Float)
		//		, new("_UvAnimRotation", ShaderPropertyType.Float)
		//		, new("_MToonVersion", ShaderPropertyType.Float)
		//		, new("_DebugMode", ShaderPropertyType.Float)
		//		, new("_BlendMode", ShaderPropertyType.Float)
		//		, new("_OutlineWidthMode", ShaderPropertyType.Float)
		//		, new("_OutlineColorMode", ShaderPropertyType.Float)
		//		, new("_CullMode", ShaderPropertyType.Float)
		//		, new("_OutlineCullMode", ShaderPropertyType.Float)
		//		, new("_SrcBlend", ShaderPropertyType.Float)
		//		, new("_DstBlend", ShaderPropertyType.Float)
		//		, new("_ZWrite", ShaderPropertyType.Float)
		//	}
		//};

		public static ShaderProps GetPropsForUTS() => VRM_UTS;

		static ShaderProps VRM_UTS = new()
		{
			Properties = new ShaderProperty[] { 
				new("_Color", ShaderPropertyType.Color)
				, new("_MainTex", ShaderPropertyType.TexEnv)
				, new("_CullMode", ShaderPropertyType.Float)
				, new("_SrcBlend", ShaderPropertyType.Float)
				, new("_DstBlend", ShaderPropertyType.Float)
				, new("_ZWrite", ShaderPropertyType.Float)
			}
		};
	}
}

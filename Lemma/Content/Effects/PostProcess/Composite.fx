#include "EffectCommon.fxh"
#include "EffectSamplers.fxh"

const float SSAOIntensity = 0.25f;

float4 CompositePS(in PostProcessPSInput input)	: COLOR0
{
	float4 color = tex2D(SourceSampler0, input.texCoord);
	float3 result;
	if (color.a == 0.0f)
		result = color.xyz;
	else
	{
		float4 lighting = tex2D(SourceSampler1, input.texCoord);
		float4 specular = tex2D(SourceSampler2, input.texCoord);
		result = DecodeColor(color.xyz * lighting.xyz) + specular.xyz;
	}
	
	return float4(result, 1.0f);
}

float4 CompositeSSAOPS(in PostProcessPSInput input)	: COLOR0
{
	float4 color = tex2D(SourceSampler0, input.texCoord);
	float3 result;
	if (color.a == 0.0f)
		result = color.xyz;
	else
	{
		/*float4 lighting = tex2D(SourceSampler1, input.texCoord);
		float4 specular = tex2D(SourceSampler2, input.texCoord);
		float4 ssao = tex2D(SourceSampler3, input.texCoord);
		result = DecodeColor(color.xyz * (lighting.xyz - ((1.0f - ssao.x) * SSAOIntensity))) + specular.xyz;
		*/
		result = tex2D(SourceSampler3, input.texCoord);
	}
	
	return float4(result, 1.0f);
}

technique Composite
{
	pass p0
	{
		VertexShader = compile vs_3_0 PostProcessVS();
		PixelShader = compile ps_3_0 CompositePS();
		
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = false;
	}
}

technique CompositeSSAO
{
	pass p0
	{
		VertexShader = compile vs_3_0 PostProcessVS();
		PixelShader = compile ps_3_0 CompositeSSAOPS();
		
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = false;
	}
}
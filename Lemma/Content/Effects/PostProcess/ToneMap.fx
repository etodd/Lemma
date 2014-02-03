#include "EffectCommon.fxh"

float3 Tint = float3(1, 1, 1);

float Gamma = 1.0f;
float Brightness = 0.0f;

texture1D RampTexture;
sampler1D RampSampler = sampler_state
{
	Texture = <RampTexture>;
	MinFilter = linear;
	MagFilter = linear;
	MipFilter = linear;
	MaxAnisotropy = 1;
	AddressU = CLAMP;
	AddressV = CLAMP;
};

float3 toneMap(float3 color)
{
	color.x = tex1D(RampSampler, color.x).x;
	color.y = tex1D(RampSampler, color.y).y;
	color.z = tex1D(RampSampler, color.z).z;
	return Brightness + color * Tint * Gamma;
}

float4 ToneMapPS(in PostProcessPSInput input)	: COLOR0
{
	float3 color = DecodeColor(tex2D(SourceSampler0, input.texCoord).xyz);

	return float4(EncodeColor(toneMap(color)), 1.0f);
}

float4 ToneMapDecodePS(in PostProcessPSInput input)	: COLOR0
{
	float3 color = DecodeColor(tex2D(SourceSampler0, input.texCoord).xyz);

	return float4(toneMap(color), 1.0f);
}

technique ToneMap
{
	pass p0
	{
		VertexShader = compile vs_3_0 PostProcessVS();
		PixelShader = compile ps_3_0 ToneMapPS();
		
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = false;
	}
}

technique ToneMapDecode
{
	pass p0
	{
		VertexShader = compile vs_3_0 PostProcessVS();
		PixelShader = compile ps_3_0 ToneMapDecodePS();
		
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = false;
	}
}


#include "EffectCommon.fxh"

float3 AmbientLightColor;

float2 SourceDimensions0;
texture2D SourceTexture0;
sampler2D SourceSampler0 = sampler_state
{
	Texture = <SourceTexture0>;
	MinFilter = point;
	MagFilter = point;
	MipFilter = point;
	MaxAnisotropy = 1;
	AddressU = CLAMP;
	AddressV = CLAMP;
};

float2 SourceDimensions1;
texture2D SourceTexture1;
sampler2D SourceSampler1 = sampler_state
{
	Texture = <SourceTexture1>;
	MinFilter = point;
	MagFilter = point;
	MipFilter = point;
	MaxAnisotropy = 1;
	AddressU = CLAMP;
	AddressV = CLAMP;
};

float2 SourceDimensions2;
texture2D SourceTexture2;
sampler2D SourceSampler2 = sampler_state
{
	Texture = <SourceTexture2>;
	MinFilter = point;
	MagFilter = point;
	MipFilter = point;
	MaxAnisotropy = 1;
	AddressU = CLAMP;
	AddressV = CLAMP;
};

float2 SourceDimensions3;
texture2D SourceTexture3;
sampler2D SourceSampler3 = sampler_state
{
	Texture = <SourceTexture3>;
	MinFilter = linear;
	MagFilter = linear;
	MipFilter = linear;
	MaxAnisotropy = 1;
	AddressU = CLAMP;
	AddressV = CLAMP;
};

const float SSAOIntensity = 0.6f;

float2 Materials[16];

float4 CompositePS(in PostProcessPSInput input, uniform bool ssao)	: COLOR0
{
	float4 color = tex2D(SourceSampler0, input.texCoord);
	float2 specularData = Materials[DecodeMaterial(color.a)];
	float3 result;
	if (specularData.x == 0.0f)
		result = color.rgb;
	else
	{
		float4 lighting = tex2D(SourceSampler1, input.texCoord);
		float4 specular = tex2D(SourceSampler2, input.texCoord);
		float3 ambient = AmbientLightColor;
		if (ssao)
		{
			float ao = tex2D(SourceSampler3, input.texCoord).x;
			ambient -= (1.0f - ao) * SSAOIntensity;
		}
		
		result = color.rgb * (ambient + DecodeColor(lighting.rgb)) + DecodeColor(specular.rgb);
	}
	
	return float4(result, 1.0f);
}

float4 CompositeSSAOPS(in PostProcessPSInput input) : COLOR0
{
	return CompositePS(input, true);
}

float4 CompositeNormalPS(in PostProcessPSInput input) : COLOR0
{
	return CompositePS(input, false);
}

technique Composite
{
	pass p0
	{
		VertexShader = compile vs_3_0 PostProcessVS();
		PixelShader = compile ps_3_0 CompositeNormalPS();
		
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
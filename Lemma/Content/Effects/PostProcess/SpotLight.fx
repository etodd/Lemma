#include "LightingCommon.fxh"
#include "Shadow2D.fxh"

float4x4 WorldMatrix;
float3 SpotLightPosition;
float3 SpotLightDirection;
float SpotLightRadius;
float3 SpotLightColor;
float4x4 SpotLightViewProjectionMatrix;

float2 Materials[16];

texture2D CookieTexture;
sampler2D CookieSampler = sampler_state
{
	Texture = <CookieTexture>;
	MinFilter = linear;
	MagFilter = linear;
	MipFilter = linear;
	AddressU = CLAMP;
	AddressV = CLAMP;
};

// Calculate the contribution of a spot light
LightingOutput CalcSpotLighting(float3 lightColor,
						float lightAttenuation,
						float3 normal,
						float3 lightPos,
						float3 lightDirection,
						float3 pixelPos,
						float3 cameraToPoint,
						float materialParam,
						float3 cookieColor)
{
	LightingOutput output = (LightingOutput)0;
	// Calculate the raw lighting terms
	float3 direction = lightPos - pixelPos;
	float distance = length(direction);
	direction /= distance;
	if (dot(direction, lightDirection) < 0)
		return output;
	
	// Modulate the lighting terms based on the material colors, and the attenuation factor
	float attenuation = saturate(1.0f - max(0.01f, distance) / lightAttenuation);
	float3 totalLightColor = lightColor * cookieColor * attenuation;
	float2 specularData = Materials[DecodeMaterial(materialParam)];
	if (dot(normal, normal) < 0.01f)
		output.lighting = totalLightColor;
	else
		output.lighting = totalLightColor * saturate(dot(normal, direction));
	output.specular = totalLightColor * pow(saturate(dot(normal, normalize(direction - cameraToPoint))), specularData.x) * specularData.y;
	return output;
}

struct SpotLightVSInput
{
	float4 position : POSITION0;
};

struct SpotLightPSInput
{
	float4 projectedPosition : TEXCOORD0;
	float4 worldPosition : TEXCOORD1;
};

void SpotLightVS(	in SpotLightVSInput input,
					out float4 outputPosition : POSITION0,
					out SpotLightPSInput output)
{
	// Calculate the clip-space vertex position
	float4 worldPosition = mul(input.position, WorldMatrix);
	outputPosition = mul(worldPosition, ViewProjectionMatrix);

	output.projectedPosition = outputPosition;

	output.worldPosition = worldPosition;
}

void SpotLightPS(	in SpotLightPSInput input,
					out float4 lighting : COLOR0,
					out float4 specular : COLOR1,
					uniform bool shadow)
{
	float2 texCoord = (0.5f * input.projectedPosition.xy / input.projectedPosition.w) + float2(0.5f, 0.5f);
	texCoord.y = 1.0f - texCoord.y;
	texCoord = (round(texCoord * DestinationDimensions) + float2(0.5f, 0.5f)) / DestinationDimensions;
	float4 normalValue = tex2D(SourceSampler1, texCoord);
	float2 depthValue = tex2D(SourceSampler0, texCoord).xy;
	float3 normal = float3(DecodeNormal(normalValue.xy), depthValue.y);
	float3 viewRay = normalize(input.worldPosition);
	float3 position = PositionFromDepth(depthValue.x, viewRay);

	float4 spotProjectedPosition = mul(float4(position + normal * NormalShadowBias, 1.0f), SpotLightViewProjectionMatrix);
	float2 spotClipPosition = 0.5f * spotProjectedPosition.xy / spotProjectedPosition.w + float2(0.5f, 0.5f);
	spotClipPosition.y = 1.0f - spotClipPosition.y;
	float3 cookieColor = tex2D(CookieSampler, spotClipPosition).xyz;
	
	LightingOutput data = CalcSpotLighting(SpotLightColor, SpotLightRadius, normal, SpotLightPosition, SpotLightDirection, position, viewRay, tex2D(SourceSampler2, texCoord).a, cookieColor);
	
	if (shadow)
	{
		float shadowValue = GetShadowValueFromClip(spotClipPosition, 1.0f - (spotProjectedPosition.z / spotProjectedPosition.w));
		data.lighting *= shadowValue;
		data.specular *= shadowValue;
	}

	lighting.rgb = EncodeColor(data.lighting);
	lighting.a = 1.0f;
	specular.rgb = EncodeColor(data.specular);
	specular.a = 1.0f;
}

technique SpotLight
{
	pass p0
	{
		VertexShader = compile vs_3_0 SpotLightVS();
		PixelShader = compile ps_3_0 SpotLightPS(false);
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = true;
		SrcBlend = One;
		DestBlend = One;
	}
}

technique SpotLightShadowed
{
	pass p0
	{
		VertexShader = compile vs_3_0 SpotLightVS();
		PixelShader = compile ps_3_0 SpotLightPS(true);
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = true;
		SrcBlend = One;
		DestBlend = One;
	}
}
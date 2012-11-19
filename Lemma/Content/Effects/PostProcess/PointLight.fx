#include "LightingCommon.fxh"

TextureCube ShadowMapTexture;
samplerCUBE ShadowMapSampler = sampler_state
{
	Texture = <ShadowMapTexture>;
	MinFilter = point;
	MagFilter = point;
	MipFilter = point;
	MaxAnisotropy = 1;
	AddressU = CLAMP;
	AddressV = CLAMP;
};
float3 ShadowMapSize;

const float SHADOW_DEPTH_BIAS = 0.002f; // Depth bias to prevent shadow artifacts

float4x4 WorldMatrix;
float3 PointLightPosition;
float PointLightRadius;
float3 PointLightColor;

// Calculate the contribution of a point light
LightingOutput CalcPointLighting(float3 lightColor,
						float lightAttenuation,
						float3 normal,
						float3 lightPos,
						float3 pixelPos,
						float3 cameraToPoint,
						float specularPower,
						float specularIntensity)
{
	LightingOutput output;
	// Calculate the raw lighting terms
	float3 direction = lightPos - pixelPos;
	float distance = length(direction);
	direction /= distance;
	
	// Modulate the lighting terms based on the material colors, and the attenuation factor
	float attenuation = saturate(1.0f - max(0.01f, distance) / lightAttenuation);
	float3 totalLightColor = lightColor * attenuation;
	if (length(normal) < 0.01f)
		output.lighting = totalLightColor;
	else
	{
		output.lighting = totalLightColor * saturate(dot(normal, direction));
		output.specular = totalLightColor * pow(saturate(dot(normal, normalize(direction - cameraToPoint))), specularPower) * specularIntensity;
	}
	return output;
}

// Cube map shadow filtering
float GetShadowValue(float3 uvw, float depth)
{
	// Multiply coordinates by the texture size
	float3 texPos = uvw * ShadowMapSize;

	// Compute first integer coordinates
	float3 texPos0 = floor(texPos + 0.5f);

	// Compute second integer coordinates
	float3 texPos1 = texPos0 + 1.0f;

	// Perform division on integer coordinates
	texPos0 = texPos0 / ShadowMapSize;
	texPos1 = texPos1 / ShadowMapSize;

	// Compute contributions for each coordinate
	float3 blend = frac(texPos + 0.5f);

	// Construct 8 new coordinates
	float3 texPos000 = texPos0;
	float3 texPos001 = float3(texPos0.x, texPos0.y, texPos1.z);
	float3 texPos010 = float3(texPos0.x, texPos1.y, texPos0.z);
	float3 texPos011 = float3(texPos0.x, texPos1.y, texPos1.z);
	float3 texPos100 = float3(texPos1.x, texPos0.y, texPos0.z);
	float3 texPos101 = float3(texPos1.x, texPos0.y, texPos1.z);
	float3 texPos110 = float3(texPos1.x, texPos1.y, texPos0.z);
	float3 texPos111 = texPos1;

	// Sample cube map
	float C000 = texCUBE(ShadowMapSampler, texPos000).r - SHADOW_DEPTH_BIAS < depth;
	float C001 = texCUBE(ShadowMapSampler, texPos001).r - SHADOW_DEPTH_BIAS < depth;
	float C010 = texCUBE(ShadowMapSampler, texPos010).r - SHADOW_DEPTH_BIAS < depth;
	float C011 = texCUBE(ShadowMapSampler, texPos011).r - SHADOW_DEPTH_BIAS < depth;
	float C100 = texCUBE(ShadowMapSampler, texPos100).r - SHADOW_DEPTH_BIAS < depth;
	float C101 = texCUBE(ShadowMapSampler, texPos101).r - SHADOW_DEPTH_BIAS < depth;
	float C110 = texCUBE(ShadowMapSampler, texPos110).r - SHADOW_DEPTH_BIAS < depth;
	float C111 = texCUBE(ShadowMapSampler, texPos111).r - SHADOW_DEPTH_BIAS < depth;

	// Compute final value by lerping everything
	return lerp(lerp(lerp(C000, C010, blend.y), lerp(C100, C110, blend.y), blend.x), lerp( lerp(C001, C011, blend.y), lerp(C101, C111, blend.y), blend.x), blend.z);
}

struct PointLightVSInput
{
	float4 position : POSITION0;
};

struct PointLightPSInput
{
	float4 projectedPosition : TEXCOORD0;
	float3 worldPosition : TEXCOORD1;
};

void PointLightVS(	in PointLightVSInput input,
					out float4 outputPosition : POSITION0,
					out PointLightPSInput output)
{
	// Calculate the clip-space vertex position
	float4 worldPosition = mul(input.position, WorldMatrix);
	outputPosition = mul(worldPosition, ViewProjectionMatrix);

	output.projectedPosition = outputPosition;

	output.worldPosition = worldPosition;
}

void PointLightPS(	in PointLightPSInput input,
					out float4 lighting : COLOR0,
					out float4 specular : COLOR1)
{
	float2 texCoord = (0.5f * input.projectedPosition.xy / input.projectedPosition.w) + float2(0.5f, 0.5f);
	texCoord.y = 1.0f - texCoord.y;
	texCoord = (round(texCoord * DestinationDimensions) + float2(0.5f, 0.5f)) / DestinationDimensions;
	float4 normalValue = tex2D(SourceSampler1, texCoord);
	float3 normal = DecodeNormal(normalValue);
	float3 viewRay = normalize(input.worldPosition - CameraPosition);
	float3 position = PositionFromDepthSampler(SourceSampler0, texCoord, viewRay);
	LightingOutput data = CalcPointLighting(PointLightColor, PointLightRadius, normal, PointLightPosition, position, viewRay, tex2D(SourceSampler1, texCoord).w * 255.0f, normalValue.w);
	lighting.xyz = EncodeColor(data.lighting);
	lighting.w = 1.0f;
	specular.xyz = EncodeColor(data.specular);
	specular.w = 1.0f;
}

void PointLightShadowedPS(	in PointLightPSInput input,
							out float4 lighting : COLOR0,
							out float4 specular : COLOR1)
{
	float2 texCoord = (0.5f * input.projectedPosition.xy / input.projectedPosition.w) + float2(0.5f, 0.5f);
	texCoord.y = 1.0f - texCoord.y;
	texCoord = (round(texCoord * DestinationDimensions) + float2(0.5f, 0.5f)) / DestinationDimensions;
	float4 normalValue = tex2D(SourceSampler1, texCoord);
	float3 normal = DecodeNormal(normalValue);
	float3 viewRay = normalize(input.worldPosition - CameraPosition);
	float3 position = PositionFromDepthSampler(SourceSampler0, texCoord, viewRay);
	LightingOutput data = CalcPointLighting(PointLightColor, PointLightRadius, normal, PointLightPosition, position, viewRay, tex2D(SourceSampler1, texCoord).w * 255.0f, normalValue.w);

	float3 fromLight = position - PointLightPosition;
	float depth = length(fromLight);
	fromLight.z *= -1.0f;
	float shadow = GetShadowValue(fromLight / depth, 1.0f - (depth / PointLightRadius));
	lighting.xyz = EncodeColor(data.lighting * shadow);
	lighting.w = 1.0f;
	specular.xyz = EncodeColor(data.specular * shadow);
	specular.w = 1.0f;
}

technique PointLight
{
	pass p0
	{
		VertexShader = compile vs_3_0 PointLightVS();
		PixelShader = compile ps_3_0 PointLightPS();
		
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = true;
		SrcBlend = One;
		DestBlend = One;
	}
}

technique PointLightShadowed
{
	pass p0
	{
		VertexShader = compile vs_3_0 PointLightVS();
		PixelShader = compile ps_3_0 PointLightShadowedPS();
		
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = true;
		SrcBlend = One;
		DestBlend = One;
	}
}
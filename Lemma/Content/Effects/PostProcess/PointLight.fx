#include "LightingCommon.fxh"

float4x4 WorldMatrix;
float3 PointLightPosition;
float PointLightRadius;
float3 PointLightColor;

float2 Materials[16];

// Calculate the contribution of a point light
LightingOutput CalcPointLighting(float3 lightColor,
						float lightAttenuation,
						float3 normal,
						float3 lightPos,
						float3 pixelPos,
						float3 cameraToPoint,
						float materialParam)
{
	LightingOutput output;
	// Calculate the raw lighting terms
	float3 direction = lightPos - pixelPos;
	float distance = length(direction);
	direction /= distance;
	
	float attenuation = saturate(1.0f - max(0.01f, distance) / lightAttenuation);
	float3 totalLightColor = lightColor * attenuation;
	float2 specularData = Materials[DecodeMaterial(materialParam)];
	if (dot(normal, normal) < 0.01f)
		output.lighting = totalLightColor;
	else
		output.lighting = totalLightColor * saturate(dot(normal, direction));
	output.specular = totalLightColor * pow(saturate(dot(normal, normalize(direction - cameraToPoint))), specularData.x) * specularData.y;
	return output;
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
	float2 depthValue = tex2D(SourceSampler0, texCoord).xy;
	float3 normal = float3(DecodeNormal(normalValue.xy), depthValue.y);
	float3 viewRay = normalize(input.worldPosition);
	float3 position = PositionFromDepth(depthValue.x, viewRay);
	LightingOutput data = CalcPointLighting(PointLightColor, PointLightRadius, normal, PointLightPosition, position, viewRay, tex2D(SourceSampler2, texCoord).a);
	lighting.rgb = EncodeColor(data.lighting);
	lighting.a = 1.0f;
	specular.rgb = EncodeColor(data.specular);
	specular.a = 1.0f;
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
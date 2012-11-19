#include "LightingCommon.fxh"
#include "Shadow2D.fxh"

// Lighting parameters
static const int NUM_DIRECTIONAL_LIGHTS = 3;
float3 DirectionalLightDirections[NUM_DIRECTIONAL_LIGHTS];
float3 DirectionalLightColors[NUM_DIRECTIONAL_LIGHTS];
float3 AmbientLightColor;

float4x4 ShadowViewProjectionMatrix;

// Calculate the contribution of a directional light
LightingOutput CalcDirectionalLighting(
						float3 lightColor,
						float3 normal,
						float specularPower,
						float specularIntensity,
						float3 cameraToPoint,
						float3 lightDir)
{
	LightingOutput output;
	// Modulate the lighting terms based on the material colors, and the attenuation factor
	if (length(normal) < 0.01f)
		output.lighting = lightColor;
	else
	{
		output.lighting = lightColor * saturate(dot(normal, lightDir));
		output.specular = lightColor * pow(saturate(dot(normal, normalize(lightDir - cameraToPoint))), specularPower) * specularIntensity;
	}
	return output;
}

LightingOutput GetGlobalLighting(uniform int start, float3 normal, float specularPower, float specularIntensity, float3 cameraToPoint)
{
	LightingOutput output;
	output.lighting = AmbientLightColor;
	output.specular = float3(0, 0, 0);

	// Directional lights
	[unroll]
	for(int i = start; i < NUM_DIRECTIONAL_LIGHTS; i++)
	{
		LightingOutput light = CalcDirectionalLighting(DirectionalLightColors[i],
								normal,
								specularPower,
								specularIntensity,
								cameraToPoint,
								normalize(-DirectionalLightDirections[i]));
		output.lighting += light.lighting;
		output.specular += light.specular;
	}

	return output;
}

void GlobalLightShadowPS(	in PostProcessPSInput input,
					out float4 lighting : COLOR0,
					out float4 specular : COLOR1)
{
	float4 normalValue = tex2D(SourceSampler1, input.texCoord);
	float3 normal = DecodeNormal(normalValue);
	if (normal.x * normal.y * normal.z == 0.0f)
	{
		lighting = (float4)0;
		specular = (float4)0;
	}
	else
	{
		float3 viewRay = normalize(input.viewRay);
		float3 worldPos = PositionFromDepthSampler(SourceSampler0, input.texCoord, viewRay);
		float specularPower = tex2D(SourceSampler2, input.texCoord).w * 255.0f;
		float specularIntensity = normalValue.w;
		LightingOutput data = GetGlobalLighting(1, normal, specularPower, specularIntensity, viewRay);

		// Shadowed light
		LightingOutput shadowLight = CalcDirectionalLighting(DirectionalLightColors[0],
								normal,
								specularPower,
								specularIntensity,
								viewRay,
								normalize(-DirectionalLightDirections[0]));
		float4 shadowPos = mul(float4(worldPos, 1.0f), ShadowViewProjectionMatrix);
		float shadowValue = GetShadowValue(shadowPos);
		data.lighting += shadowLight.lighting * shadowValue;
		data.specular += shadowLight.specular * shadowValue;

		lighting.xyz = EncodeColor(data.lighting);
		lighting.w = 1.0f;
		specular.xyz = EncodeColor(data.specular);
		specular.w = 1.0f;
	}
}

void GlobalLightPS(	in PostProcessPSInput input,
					out float4 lighting : COLOR0,
					out float4 specular : COLOR1)
{
	float4 normalValue = tex2D(SourceSampler1, input.texCoord);
	float3 normal = DecodeNormal(normalValue);
	if (normal.x * normal.y * normal.z == 0.0f)
	{
		lighting = (float4)0;
		specular = (float4)0;
	}
	else
	{
		float3 viewRay = normalize(input.viewRay);
		float3 worldPos = PositionFromDepthSampler(SourceSampler0, input.texCoord, viewRay);
		LightingOutput data = GetGlobalLighting(0, normal, tex2D(SourceSampler2, input.texCoord).w * 255.0f, normalValue.w, viewRay);
		lighting.xyz = EncodeColor(data.lighting);
		lighting.w = 1.0f;
		specular.xyz = EncodeColor(data.specular);
		specular.w = 1.0f;
	}
}

technique GlobalLightShadow
{
	pass p0
	{
		VertexShader = compile vs_3_0 PostProcessVS();
		PixelShader = compile ps_3_0 GlobalLightShadowPS();
		
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = false;
	}
}

technique GlobalLight
{
	pass p0
	{
		VertexShader = compile vs_3_0 PostProcessVS();
		PixelShader = compile ps_3_0 GlobalLightPS();
		
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = false;
	}
}
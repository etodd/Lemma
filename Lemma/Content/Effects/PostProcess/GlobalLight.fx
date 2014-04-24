#include "LightingCommon.fxh"
#include "Shadow2D.fxh"

// Lighting parameters
static const int NUM_DIRECTIONAL_LIGHTS = 3;
float3 DirectionalLightDirections[NUM_DIRECTIONAL_LIGHTS];
float3 DirectionalLightColors[NUM_DIRECTIONAL_LIGHTS];
float3 AmbientLightColor;

float4x4 ShadowViewProjectionMatrix;
float4x4 DetailShadowViewProjectionMatrix;

float3 EnvironmentColor = float3(1, 1, 1);

float2 Materials[16];

textureCUBE EnvironmentTexture;
samplerCUBE EnvironmentSampler = sampler_state
{
	Texture = <EnvironmentTexture>;
	MinFilter = linear;
	MagFilter = linear;
	MipFilter = linear;
	AddressU = WRAP;
	AddressV = WRAP;
};

// Calculate the contribution of a directional light
LightingOutput CalcDirectionalLighting(
						float3 lightColor,
						float3 normal,
						float2 specularData,
						float3 cameraToPoint,
						float3 lightDir,
						float3 reflectedViewRay,
						bool ignoreNormal)
{
	LightingOutput output;
	if (ignoreNormal)
		output.lighting = lightColor;
	else
		output.lighting = lightColor * saturate(dot(normal, lightDir));
	output.specular = lightColor * pow(saturate(dot(reflectedViewRay, lightDir)), specularData.x) * specularData.y;
	return output;
}

const float ShadowBias = 0.0013f;

void GlobalLightPS(	in PostProcessPSInput input,
					out float4 lighting : COLOR0,
					out float4 specular : COLOR1,
					uniform bool shadow)
{
	float2 depthValue = tex2D(SourceSampler0, input.texCoord).xy;
	float4 normalValue = tex2D(SourceSampler1, input.texCoord);
	float3 normal = float3(DecodeNormal(normalValue.xy), depthValue.y);

	float3 viewRay = normalize(input.viewRay);
	float3 worldPos = PositionFromDepth(depthValue.x, viewRay);
	float materialParam = tex2D(SourceSampler2, input.texCoord).a;
	float2 specularData = Materials[DecodeMaterial(materialParam)];

	float3 reflectedViewRay = reflect(viewRay, normal);

	LightingOutput output;
	output.lighting = AmbientLightColor;
	output.specular = float3(0, 0, 0);

	// Directional lights
	bool ignoreNormal = dot(normal, normal) < 0.01f;

	if (shadow)
	{
		// Shadowed light
		LightingOutput shadowLight = CalcDirectionalLighting(DirectionalLightColors[0],
								normal,
								specularData,
								viewRay,
								-DirectionalLightDirections[0],
								reflectedViewRay,
								ignoreNormal);
		float4 detailShadowPos = mul(float4(worldPos, 1.0f), DetailShadowViewProjectionMatrix);
		float4 shadowPos = mul(float4(worldPos, 1.0f), ShadowViewProjectionMatrix);
		float shadowValue = GetShadowValueDetail(detailShadowPos, shadowPos, ShadowBias);
		output.lighting += shadowLight.lighting * shadowValue;
		output.specular += shadowLight.specular * shadowValue;
	}

	[unroll]
	for(int i = shadow ? 1 : 0; i < NUM_DIRECTIONAL_LIGHTS; i++)
	{
		LightingOutput light = CalcDirectionalLighting(DirectionalLightColors[i],
								normal,
								specularData,
								viewRay,
								-DirectionalLightDirections[i],
								reflectedViewRay,
								ignoreNormal);
		output.lighting += light.lighting;
		output.specular += light.specular;
	}

	output.specular += texCUBE(EnvironmentSampler, reflectedViewRay).xyz * EnvironmentColor * specularData.y * specularData.x * (0.3f / 255.0f);

	lighting.rgb = output.lighting;
	lighting.a = 1.0f;
	specular.rgb = output.specular;
	specular.a = 1.0f;
}

technique GlobalLightShadow
{
	pass p0
	{
		VertexShader = compile vs_3_0 PostProcessVS();
		PixelShader = compile ps_3_0 GlobalLightPS(true);
		
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
		PixelShader = compile ps_3_0 GlobalLightPS(false);
		
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = false;
	}
}
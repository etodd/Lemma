#include "EffectCommon.fxh"
#include "EffectSamplers.fxh"

// Adapted from http://jcoluna.wordpress.com/2011/10/28/xna-light-pre-pass-ambient-light-ssao-and-more/
// Jorge Adriano Luna 2011
// http://jcoluna.wordpress.com

const float TotalStrength = 1.75f;
const float Radius = 0.06f;
const float RandomTile = 18.0f;
const float Bias = 0.1f;

texture2D RandomTexture;
sampler2D RandomSampler = sampler_state
{
	Texture = <RandomTexture>;
	MinFilter = point;
	MagFilter = point;
	MipFilter = point;
	AddressU = CLAMP;
	AddressV = CLAMP;
};

#define SAMPLE_COUNT 12
float3 SAMPLES[SAMPLE_COUNT] =
{
	float3(0.355512, 	-0.709318, 	-0.102371),
	float3(0.534186, 	0.71511, 	-0.115167),
	float3(-0.87866, 	0.157139,	-0.115167),
	float3(0.140679, 	-0.475516, 	-0.0639818),
	float3(-0.0796121, 	0.158842, 	-0.677075),
	float3(-0.0759516, 	-0.101676, 	-0.483625),
	float3(0.12493, 	-0.0223423,	0.483625),
	float3(-0.0720074, 	0.243395, 	0.967251),
	float3(-0.207641, 	0.414286, 	0.187755),
	float3(-0.277332, 	-0.371262, 	0.187755),
	float3(0.63864, 	-0.114214, 	0.262857),
	float3(-0.184051, 	0.622119, 	0.262857),
	//float3(0.110007, 	-0.219486, 	-0.435574),
	//float3(0.235085, 	0.314707, 	-0.696918),
	//float3(-0.290012, 	0.0518654, 	0.522688),
	//float3(0.0975089, 	-0.329594, 	0.609803),
};

void SSAOPS(in PostProcessPSInput input, out float4 output : COLOR0)
{
	float depth = tex2D(SourceSampler0, input.texCoord).r;
	
	float3 normal = mul(DecodeNormal(tex2D(SourceSampler1, input.texCoord).xyz), (float3x3)ViewMatrix);
	float3 normalScaled = normal * 0.25f;

	float3 randomNormal = DecodeNormal(tex2D(RandomSampler, input.texCoord * RandomTile).xyz);

	float occlusion = 0.0f;
	[unroll]
	for (int i = 0; i < SAMPLE_COUNT; i++)
	{
		float3 randomDirection = reflect(SAMPLES[i], randomNormal);
		
		// Prevent it pointing inside the geometry
		randomDirection *= sign(dot(normal, randomDirection));

		// add that scaled normal
		randomDirection += normalScaled;

		randomDirection *= Radius / depth;
		
		float sampleDepth = tex2D(SourceSampler0, input.texCoord + randomDirection.xy).x;
		
		// we only care about samples in front of our original-modifies 
		float deltaDepth = saturate(depth - sampleDepth);
		
		// ignore negative deltas
		occlusion += (1.0f - deltaDepth) * (deltaDepth > Bias);
	}
 
	// output the result
	float ao = 1.0f - (TotalStrength * occlusion / SAMPLE_COUNT);
	output = float4(ao, ao, ao, 1.0f);
}

technique SSAO
{
	pass p0
	{
		VertexShader = compile vs_3_0 PostProcessVS();
		PixelShader = compile ps_3_0 SSAOPS();
		
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = false;
	}
}
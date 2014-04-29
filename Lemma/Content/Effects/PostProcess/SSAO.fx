#include "EffectCommon.fxh"
#include "EffectSamplers.fxh"

const float GaussianKernel[16] = { 0.003829872f, 0.0088129551f, 0.0181463396f, 0.03343381f, 0.0551230286f, 0.0813255467f, 0.1073650667f, 0.1268369298f, 0.1340827751f, 0.1268369298f, 0.1073650667f, 0.0813255467f, 0.0551230286f, 0.03343381f, 0.0181463396f, 0.0088129551 };

// Adapted from http://jcoluna.wordpress.com/2011/10/28/xna-light-pre-pass-ambient-light-ssao-and-more/
// Jorge Adriano Luna 2011
// http://jcoluna.wordpress.com

const float Radius = 0.7f;
const float RandomTile = 50.0f;
const float Bias = 0.2f;

texture2D RandomTexture;
sampler2D RandomSampler = sampler_state
{
	Texture = <RandomTexture>;
	MinFilter = point;
	MagFilter = point;
	MipFilter = point;
	AddressU = WRAP;
	AddressV = WRAP;
};

#define SAMPLE_COUNT 12
float3 SAMPLES[SAMPLE_COUNT] =
{
	float3( 0.5381, 0.1856,-0.4319),
	float3( 0.1379, 0.2486, 0.4430),
	float3( 0.3371, 0.5679,-0.0057),
	float3(-0.6999,-0.0451,-0.0019),
	float3( 0.0689,-0.1598,-0.8547),
	float3( 0.0560, 0.0069,-0.1843),
	float3(-0.0146, 0.1402, 0.0762),
	float3( 0.0100,-0.1924,-0.0344),
	float3(-0.3577,-0.5301,-0.4358),
	float3(-0.3169, 0.1063, 0.0158),
	float3( 0.0103,-0.5869, 0.0046),
	float3(-0.0897,-0.4940, 0.3287),
};

void SSAOPS(in PostProcessPSInput input, out float4 output : COLOR0)
{
	float depth = tex2D(SourceSampler0, input.texCoord).x;
	
	float3 normal = mul(normalize(DecodeNormalBuffer(tex2D(SourceSampler1, input.texCoord).xyz)), (float3x3)ViewMatrix);
	normal.y *= -1.0f;
	float3 normalScaled = normal * 0.5f;

	float3 randomNormal = DecodeNormalMap(tex2D(RandomSampler, input.texCoord * RandomTile).xyz);

	float ao = SAMPLE_COUNT;
	[unroll]
	for (int i = 0; i < SAMPLE_COUNT; i++)
	{
		float3 randomDirection = reflect(SAMPLES[i], randomNormal);
		
		// Prevent it pointing inside the geometry
		randomDirection *= sign(dot(normal, randomDirection));

		// add that scaled normal
		randomDirection += normalScaled;

		float sampleDepth = tex2D(SourceSampler0, input.texCoord + randomDirection.xy * Radius / depth).x;
		
		// we only care about samples in front of our original-modifies 
		float deltaDepth = saturate((depth - randomDirection.z * Radius) - sampleDepth);
		
		// ignore negative deltas
		ao -= (1 - deltaDepth) * (deltaDepth > Bias);
	}
 
	// output the result
	output = float4(float3(ao, ao, ao) / SAMPLE_COUNT, 1.0f);
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

const float BlurAmount = 1.0f;
const float BlurDiscardThreshold = 0.05f;

void BlurHorizontalPS(	in PostProcessPSInput input,
						out float4 out_Color		: COLOR0)
{
	float xInterval = (1.0f / SourceDimensions0.x) * BlurAmount;

	float depth = tex2D(SourceSampler1, input.texCoord).x;

	float3 sum = 0;
	float count = 0;
	[unroll]
	for (int x = -8; x < 8; x++)
	{
		float2 tap = float2(input.texCoord.x + (x * xInterval), input.texCoord.y);
		if (abs(depth - tex2D(SourceSampler1, tap).x) < BlurDiscardThreshold * depth)
		{
			sum += tex2D(SourceSampler0, tap).xyz * GaussianKernel[x + 8];
			count += GaussianKernel[x + 8];
		}
	}
	
	// Return the average color of all the samples
	out_Color.xyz = sum / count;
	out_Color.w = 1.0f;
}

void CompositePS(	in PostProcessPSInput input,
					out float4 out_Color		: COLOR0)
{
	float yInterval = (1.0f / SourceDimensions0.y) * BlurAmount;

	float depth = tex2D(SourceSampler1, input.texCoord).x;

	float3 sum = 0;
	float count = 0;
	[unroll]
	for (int y = -8; y < 8; y++)
	{
		float2 tap = float2(input.texCoord.x, input.texCoord.y + (y * yInterval));
		if (abs(depth - tex2D(SourceSampler1, tap).x) < BlurDiscardThreshold * depth)
		{
			sum += tex2D(SourceSampler0, tap).xyz * GaussianKernel[y + 8];
			count += GaussianKernel[y + 8];
		}
	}
	
	out_Color.xyz = sum / count;
	out_Color.w = 1.0f;
}

technique BlurHorizontal
{
	pass p0
	{
		VertexShader = compile vs_3_0 PostProcessVS();
		PixelShader = compile ps_3_0 BlurHorizontalPS();
		
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = false;
	}
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
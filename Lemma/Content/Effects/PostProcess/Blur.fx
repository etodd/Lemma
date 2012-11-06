#include "EffectCommon.fxh"

const float GaussianKernel[16] = { 0.003829872f, 0.0088129551f, 0.0181463396f, 0.03343381f, 0.0551230286f, 0.0813255467f, 0.1073650667f, 0.1268369298f, 0.1340827751f, 0.1268369298f, 0.1073650667f, 0.0813255467f, 0.0551230286f, 0.03343381f, 0.0181463396f, 0.0088129551 };

float BlurAmount = 1.0f;

void BlurHorizontalPS(	in PostProcessPSInput input,
						out float4 out_Color		: COLOR0)
{
	float xInterval = (1.0f / SourceDimensions0.x) * BlurAmount;

	float3 sum = 0;
	[unroll]
	for (int x = -8; x < 8; x++)
		sum += tex2D(PointSampler0, float2(input.texCoord.x + (x * xInterval), input.texCoord.y)).xyz * GaussianKernel[x + 8];
	
	// Return the average color of all the samples
	out_Color.xyz = sum;
	out_Color.w = 1.0f;
}

void CompositePS(	in PostProcessPSInput input,
					out float4 out_Color		: COLOR0)
{
	float yInterval = (1.0f / SourceDimensions0.y) * BlurAmount;

	float3 sum = 0;
	[unroll]
	for (int y = -8; y < 8; y++)
		sum += tex2D(PointSampler0, float2(input.texCoord.x, input.texCoord.y + (y * yInterval))).xyz * GaussianKernel[y + 8];
	
	out_Color.xyz = sum;
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
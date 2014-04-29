#include "EffectCommon.fxh"
#include "EffectSamplers.fxh"
#include "BloomCommon.fxh"

void DownsampleDepthPS(
	in PostProcessPSInput input,
	out float4 depth : COLOR0,
	out float4 normal : COLOR1)
{
	float2 pixelSize = 1.0f / SourceDimensions0;
	float2 sample0 = input.texCoord;
	float2 sample1 = input.texCoord + float2(pixelSize.x, 0);
	float2 sample2 = input.texCoord + float2(0, pixelSize.y);
	float2 sample3 = input.texCoord + pixelSize;
	float2 depth0 = tex2D(SourceSampler0, sample0).xy;
	float2 depth1 = tex2D(SourceSampler0, sample1).xy;
	float2 depth2 = tex2D(SourceSampler0, sample2).xy;
	float2 depth3 = tex2D(SourceSampler0, sample3).xy;
	depth = float4(max(max(depth0.x, depth1.x), max(depth2.x, depth3.x)), 0, 0, 1);

	float3 normal0 = float3(DecodeNormal(tex2D(SourceSampler1, sample0).xy), depth0.y);
	float3 normal1 = float3(DecodeNormal(tex2D(SourceSampler1, sample1).xy), depth1.y);
	float3 normal2 = float3(DecodeNormal(tex2D(SourceSampler1, sample2).xy), depth2.y);
	float3 normal3 = float3(DecodeNormal(tex2D(SourceSampler1, sample3).xy), depth3.y);
	normal = float4(EncodeNormalBuffer(normal0 * 0.25f + normal1 * 0.25f + normal2 * 0.25f + normal3 * 0.25f), 1.0f);
}

technique DownsampleDepth
{
	pass p0
	{
		VertexShader = compile vs_3_0 PostProcessVS();
		PixelShader = compile ps_3_0 DownsampleDepthPS();
		
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = false;
	}
}
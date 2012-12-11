#include "EffectCommon.fxh"

void DownsamplePS(in PostProcessPSInput input, out float4 output : COLOR0)
{
	float2 pixelSize = 1.0f / SourceDimensions0;
	float depth0 = tex2D(SourceSampler0, input.texCoord).r;
	float depth1 = tex2D(SourceSampler0, input.texCoord + float2(pixelSize.x, 0)).r;
	float depth2 = tex2D(SourceSampler0, input.texCoord + float2(0, pixelSize.y)).r;
	float depth3 = tex2D(SourceSampler0, input.texCoord + pixelSize).r;
	output = float4(max(max(depth0, depth1), max(depth2, depth3)), 0, 0, 1);
}

technique Downsample
{
	pass p0
	{
		VertexShader = compile vs_3_0 PostProcessVS();
		PixelShader = compile ps_3_0 DownsamplePS();
		
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = false;
	}
}
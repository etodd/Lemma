#include "EffectCommon.fxh"

float4 Color;
float StartDistance;
float EndDistance;
float VerticalSize;
float VerticalCenter;

texture2D DepthBuffer;
sampler2D DepthSampler = sampler_state
{
	Texture = <DepthBuffer>;
	MinFilter = point;
	MagFilter = point;
	MipFilter = point;
	AddressU = CLAMP;
	AddressV = CLAMP;
};

void FogPS(	in PostProcessPSInput input,
					out float4 color : COLOR0)
{
	// Convert from clip space to UV coordinate space
	float depth = tex2D(DepthSampler, input.texCoord).r;

	float blend = clamp(lerp(0, 1, (depth - StartDistance) / (EndDistance - StartDistance)), 0, 1);

	float3 worldPosition = PositionFromDepth(depth, input.texCoord, normalize(input.viewRay));

	float verticalBlend = 1.0f - clamp(abs((worldPosition.y - VerticalCenter) / VerticalSize), 0, 1);

	color = float4(EncodeColor(Color.xyz), Color.a * blend * verticalBlend);
}

technique Fog
{
	pass p0
	{
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = true;
		SrcBlend = SrcAlpha;
		DestBlend = InvSrcAlpha;
		
		VertexShader = compile vs_3_0 PostProcessVS();
		PixelShader = compile ps_3_0 FogPS();
	}
}
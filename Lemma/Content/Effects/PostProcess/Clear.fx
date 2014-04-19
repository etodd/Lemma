#include "EffectCommon.fxh"
float4x4 LastFrameViewProjectionMatrixRotationOnly;
float4x4 InverseViewMatrixRotationOnly;

float3 BackgroundColor;

struct ClearMotionBlurPSInput
{
	float2 velocity : TEXCOORD3;
};

void ClearVS (	in float3 position : POSITION,
						in float3 texCoord : TEXCOORD0,
						out float4 outputPosition : POSITION,
						out PostProcessPSInput output,
						out ClearMotionBlurPSInput motionBlur)
{
	// Offset the position by half a pixel to correctly align texels to pixels
	outputPosition.x = position.x - (0.5f / DestinationDimensions.x);
	outputPosition.y = position.y + (0.5f / DestinationDimensions.y);
	outputPosition.z = position.z;
	outputPosition.w = 1.0f;
	
	// Pass along the texture coordinate and the world-space view ray
	output.texCoord = texCoord.xy;
	float4 v = mul(outputPosition, InverseViewProjectionMatrix);
	output.viewRay = v.xyz / v.w;

	output.viewSpacePosition = mul(outputPosition, InverseProjectionMatrix);

	float4 currentPos = outputPosition;

	float4 worldRay = mul(float4(output.viewSpacePosition, 1), InverseViewMatrixRotationOnly);
	float4 previousPos = mul(worldRay, LastFrameViewProjectionMatrixRotationOnly);

	motionBlur.velocity = (currentPos.xy / currentPos.w) - (previousPos.xy / previousPos.w);
	motionBlur.velocity.y *= -1.0f;
}

void ClearPS(
	in PostProcessPSInput input,
	in ClearMotionBlurPSInput motionBlur,
	out MotionBlurPSOutput motionBlurOutput,
	out RenderPSOutput output
	)
{
	output.color = float4(BackgroundColor, 0.0f);
	output.depth = float4(FarPlaneDistance, 0.0f, 0.0f, 0.0f);
	output.normal = (float4)0;
	motionBlurOutput.velocity = float4(EncodeVelocity(motionBlur.velocity), 1.0f, 1.0f);
}

technique Clear
{
	pass p0
	{
		VertexShader = compile vs_3_0 ClearVS();
		PixelShader = compile ps_3_0 ClearPS();
		
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = false;
	}
}
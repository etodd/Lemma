#include "EffectCommon.fxh"
float4x4 ViewProjectionMatrixRotationOnly;
float4x4 LastFrameViewProjectionMatrixRotationOnly;

float3 BackgroundColor;

void ClearPS(in PostProcessPSInput input, out RenderPSOutput output)
{
	output.color = float4(BackgroundColor, 0.0f);
	output.depth = float4(FarPlaneDistance, 0.0f, 0.0f, 0.0f);
	output.normal = (float4)0;
}

void ClearMotionBlurPS(in PostProcessPSInput input,
				out RenderPSOutput output,
				out MotionBlurPSOutput motionBlurOutput)
{
	ClearPS(input, output);

	float3 viewRay = normalize(input.viewSpacePosition);

	float4 currentPos = mul(float4(viewRay, 1), ViewProjectionMatrixRotationOnly);
	float4 previousPos = mul(float4(viewRay, 1), LastFrameViewProjectionMatrixRotationOnly);

	float2 velocity = (currentPos.xy / currentPos.w) - (previousPos.xy / previousPos.w);
	velocity.y *= -1.0f;
	motionBlurOutput.velocity = float4(EncodeVelocity(velocity), 1.0f, 1.0f);
}

technique Clear
{
	pass p0
	{
		VertexShader = compile vs_3_0 PostProcessVS();
		PixelShader = compile ps_3_0 ClearPS();
		
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = false;
	}
}

technique ClearMotionBlur
{
	pass p0
	{
		VertexShader = compile vs_3_0 PostProcessVS();
		PixelShader = compile ps_3_0 ClearMotionBlurPS();
		
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = false;
	}
}
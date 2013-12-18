#include "RenderCommon.fxh"

float Tiling = 1.0f;

float3 Offset;

struct RenderVSInput
{
	float3 position : POSITION0;
	float3 normal : NORMAL0;
	float3 binormal : BINORMAL0;
	float3 tangent : TANGENT0;
};

void RenderVS(	in RenderVSInput input,
				out RenderVSOutput vs,
				out RenderPSInput output,
				out TexturePSInput tex,
				out NormalMapPSInput normalMap)
{
	// Calculate the clip-space vertex position
	float4 worldPosition = mul(float4(input.position - Offset, 1), WorldMatrix);
	output.position = worldPosition;
	float4 viewSpacePosition = mul(worldPosition, ViewMatrix);
	vs.position = mul(viewSpacePosition, ProjectionMatrix);
	output.viewSpacePosition = viewSpacePosition;

	normalMap.tangentToWorld[0] = normalize(mul(input.tangent, WorldMatrix));
	normalMap.tangentToWorld[1] = normalize(mul(input.binormal, WorldMatrix));
	normalMap.tangentToWorld[2] = normalize(mul(input.normal, WorldMatrix));

	float diff = length(input.position * input.normal) * 2;

	tex.uvCoordinates = float2(diff + input.position.x + (input.position.z * input.normal.x), diff - input.position.y + (input.position.z * input.normal.y)) * 0.075f * Tiling;
}

void ClipVS(	in RenderVSInput input,
				out RenderVSOutput vs,
				out RenderPSInput output,
				out TexturePSInput tex,
				out NormalMapPSInput normalMap,
				out ClipPSInput clipData)
{
	RenderVS(input, vs, output, tex, normalMap);
	clipData = GetClipData(output.position);
}

// Motion blur vertex shader
void MotionBlurVS (	in RenderVSInput input,
					out RenderVSOutput vs,
					out RenderPSInput output,
					out TexturePSInput tex,
					out NormalMapPSInput normalMap,
					out MotionBlurPSInput motionBlur)
{
	RenderVS(input, vs, output, tex, normalMap);
	
	// Pass along the current vertex position in clip-space,
	// as well as the previous vertex position in clip-space
	motionBlur.currentPosition = vs.position;
	motionBlur.previousPosition = mul(float4(input.position - Offset, 1), LastFrameWorldViewProjectionMatrix);
}

void ShadowVS(	in float3 inPosition : POSITION0,
				out ShadowVSOutput vs,
				out ShadowPSInput output)
{
	// Calculate the clip-space vertex position
	float4 worldPosition = mul(float4(inPosition - Offset, 1), WorldMatrix);
	output.worldPosition = worldPosition.xyz;
	vs.position = mul(worldPosition, ViewProjectionMatrix);
	output.clipSpacePosition = vs.position;
}

void ShadowAlphaVS(	in float3 inPosition : POSITION0,
				in float3 inNormal : NORMAL0,
				out ShadowVSOutput vs,
				out TexturePSInput tex,
				out ShadowPSInput output)
{
	// Calculate the clip-space vertex position
	float3 localPosition = inPosition - Offset;
	float4 worldPosition = mul(float4(localPosition, 1), WorldMatrix);
	output.worldPosition = worldPosition.xyz;
	vs.position = mul(worldPosition, ViewProjectionMatrix);
	output.clipSpacePosition = vs.position;

	tex.uvCoordinates = float2(localPosition.x + (localPosition.z * inNormal.x), localPosition.y + (localPosition.z * inNormal.y)) * 0.075f * Tiling;
}

// Regular techniques

technique Shadow
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = true;
		AlphaBlendEnable = false;
	
		VertexShader = compile vs_3_0 ShadowVS();
		PixelShader = compile ps_3_0 ShadowPS();
	}
}

technique PointLightShadow
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = true;
		AlphaBlendEnable = false;
	
		VertexShader = compile vs_3_0 ShadowVS();
		PixelShader = compile ps_3_0 PointLightShadowPS();
	}
}

technique Render
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = true;
		AlphaBlendEnable = false;
	
		VertexShader = compile vs_3_0 RenderVS();
		PixelShader = compile ps_3_0 RenderTextureNormalMapPlainPS();
	}
}

technique Clip
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = true;
		AlphaBlendEnable = false;

		VertexShader = compile vs_3_0 ClipVS();
		PixelShader = compile ps_3_0 ClipTextureNormalMapPlainPS();
	}
}

technique MotionBlur
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = true;
		AlphaBlendEnable = false;
	
		VertexShader = compile vs_3_0 MotionBlurVS();
		PixelShader = compile ps_3_0 MotionBlurTextureNormalMapPlainPS();
	}
}

// Glow techniques

technique ShadowGlow
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = true;
		AlphaBlendEnable = false;
	
		VertexShader = compile vs_3_0 ShadowVS();
		PixelShader = compile ps_3_0 ShadowPS();
	}
}

technique PointLightShadowGlow
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = true;
		AlphaBlendEnable = false;
	
		VertexShader = compile vs_3_0 ShadowVS();
		PixelShader = compile ps_3_0 PointLightShadowPS();
	}
}

technique RenderGlow
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = true;
		AlphaBlendEnable = false;
	
		VertexShader = compile vs_3_0 RenderVS();
		PixelShader = compile ps_3_0 RenderTextureNormalMapGlowPS();
	}
}

technique ClipGlow
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = true;
		AlphaBlendEnable = false;

		VertexShader = compile vs_3_0 ClipVS();
		PixelShader = compile ps_3_0 ClipTextureNormalMapGlowPS();
	}
}

technique MotionBlurGlow
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = true;
		AlphaBlendEnable = false;
	
		VertexShader = compile vs_3_0 MotionBlurVS();
		PixelShader = compile ps_3_0 MotionBlurTextureNormalMapGlowPS();
	}
}

// Alpha techniques

technique ShadowAlpha
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = true;
		AlphaBlendEnable = false;
	
		VertexShader = compile vs_3_0 ShadowAlphaVS();
		PixelShader = compile ps_3_0 ShadowAlphaPS();
	}
}

technique PointLightShadowAlpha
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = true;
		AlphaBlendEnable = false;
	
		VertexShader = compile vs_3_0 ShadowAlphaVS();
		PixelShader = compile ps_3_0 PointLightShadowAlphaPS();
	}
}

technique RenderAlpha
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = true;
		AlphaBlendEnable = false;
	
		VertexShader = compile vs_3_0 RenderVS();
		PixelShader = compile ps_3_0 RenderTextureNormalMapClipAlphaPS();
	}
}

technique ClipAlpha
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = true;
		AlphaBlendEnable = false;

		VertexShader = compile vs_3_0 ClipVS();
		PixelShader = compile ps_3_0 ClipTextureNormalMapClipAlphaPS();
	}
}

technique MotionBlurAlpha
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = true;
		AlphaBlendEnable = false;
	
		VertexShader = compile vs_3_0 MotionBlurVS();
		PixelShader = compile ps_3_0 MotionBlurTextureNormalMapClipAlphaPS();
	}
}

// Alpha/glow techniques

technique ShadowAlphaGlow
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = true;
		AlphaBlendEnable = false;
	
		VertexShader = compile vs_3_0 ShadowAlphaVS();
		PixelShader = compile ps_3_0 ShadowAlphaPS();
	}
}

technique PointLightShadowAlphaGlow
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = true;
		AlphaBlendEnable = false;
	
		VertexShader = compile vs_3_0 ShadowAlphaVS();
		PixelShader = compile ps_3_0 PointLightShadowAlphaPS();
	}
}
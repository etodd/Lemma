#include "EnvironmentCommon.fxh"

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
				out MotionBlurPSInput motionBlur,
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

	tex.uvCoordinates = CalculateUVs(input.position, input.normal);
	
	// Pass along the current vertex position in clip-space,
	// as well as the previous vertex position in clip-space
	motionBlur.currentPosition = vs.position;
	motionBlur.previousPosition = mul(float4(input.position - Offset, 1), LastFrameWorldViewProjectionMatrix);
}

void RenderOverlayVS(	in RenderVSInput input,
				out RenderVSOutput vs,
				out RenderPSInput output,
				out TexturePSInput tex,
				out MotionBlurPSInput motionBlur,
				out NormalMapPSInput normalMap,
				out OverlayPSInput overlay)
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

	CalculateOverlayUVs(input.position, input.normal, tex, overlay);
	
	// Pass along the current vertex position in clip-space,
	// as well as the previous vertex position in clip-space
	motionBlur.currentPosition = vs.position;
	motionBlur.previousPosition = mul(float4(input.position - Offset, 1), LastFrameWorldViewProjectionMatrix);
}

void ClipVS(	in RenderVSInput input,
				out RenderVSOutput vs,
				out RenderPSInput output,
				out TexturePSInput tex,
				out MotionBlurPSInput motionBlur,
				out NormalMapPSInput normalMap,
				out ClipPSInput clipData)
{
	RenderVS(input, vs, output, tex, motionBlur, normalMap);
	clipData = GetClipData(output.position);
}

void ClipOverlayVS(	in RenderVSInput input,
				out RenderVSOutput vs,
				out RenderPSInput output,
				out TexturePSInput tex,
				out MotionBlurPSInput motionBlur,
				out NormalMapPSInput normalMap,
				out OverlayPSInput overlay,
				out ClipPSInput clipData)
{
	RenderOverlayVS(input, vs, output, tex, motionBlur, normalMap, overlay);
	clipData = GetClipData(output.position);
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

	tex.uvCoordinates = CalculateUVs(localPosition, inNormal);
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

technique ShadowOverlay
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

technique ShadowShadowMask
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

technique ShadowOverlayShadowMask
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

technique RenderShadowMask
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

technique RenderOverlay
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = true;
		AlphaBlendEnable = false;
	
		VertexShader = compile vs_3_0 RenderOverlayVS();
		PixelShader = compile ps_3_0 RenderTextureNormalMapPlainOverlayPS();
	}
}

technique RenderOverlayShadowMask
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = true;
		AlphaBlendEnable = false;
	
		VertexShader = compile vs_3_0 RenderOverlayVS();
		PixelShader = compile ps_3_0 RenderTextureNormalMapPlainOverlayPS();
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

technique ClipShadowMask
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

technique ClipOverlay
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = true;
		AlphaBlendEnable = false;

		VertexShader = compile vs_3_0 ClipOverlayVS();
		PixelShader = compile ps_3_0 ClipTextureNormalMapPlainOverlayPS();
	}
}

technique ClipOverlayShadowMask
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = true;
		AlphaBlendEnable = false;

		VertexShader = compile vs_3_0 ClipOverlayVS();
		PixelShader = compile ps_3_0 ClipTextureNormalMapPlainOverlayPS();
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

technique ShadowOverlayAlpha
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

technique RenderOverlayAlpha
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = true;
		AlphaBlendEnable = false;
	
		VertexShader = compile vs_3_0 RenderOverlayVS();
		PixelShader = compile ps_3_0 RenderTextureNormalMapClipOverlayAlphaPS();
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

technique ClipOverlayAlpha
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = true;
		AlphaBlendEnable = false;

		VertexShader = compile vs_3_0 ClipOverlayVS();
		PixelShader = compile ps_3_0 ClipTextureNormalMapClipOverlayAlphaPS();
	}
}
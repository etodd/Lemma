#include "RenderCommon.fxh"

float Tiling = 1.0f;

float3 Offset[200];

struct RenderVSInput
{
	float4 position : POSITION0;
	float3 normal : NORMAL0;
	float3 binormal : BINORMAL0;
	float3 tangent : TANGENT0;
	int index : BLENDINDICES0;
	float4x4 instanceTransform : BLENDWEIGHT0;
};

void RenderVS(	in RenderVSInput input,
				out RenderVSOutput vs,
				out RenderPSInput output,
				out TexturePSInput tex,
				out NormalMapPSInput normalMap,
				in float4x4 lastInstanceTransform : BLENDWEIGHT4,
				out MotionBlurPSInput motionBlur)
{
	// Calculate the clip-space vertex position
	float4x4 world = mul(WorldMatrix, transpose(input.instanceTransform));
	float4 worldPosition = mul(input.position, world);
	output.position = worldPosition;
	float4 viewSpacePosition = mul(worldPosition, ViewMatrix);
	vs.position = mul(viewSpacePosition, ProjectionMatrix);
	output.viewSpacePosition = viewSpacePosition;

	normalMap.tangentToWorld[0] = normalize(mul(input.tangent, world));
	normalMap.tangentToWorld[1] = normalize(mul(input.binormal, world));
	normalMap.tangentToWorld[2] = normalize(mul(input.normal, world));

	float3 pos = input.position + Offset[input.index];

	float diff = length(pos * input.normal) * 2;

	tex.uvCoordinates = float2(diff + pos.x + (pos.z * input.normal.x), diff - pos.y + (pos.z * input.normal.y)) * 0.075f * Tiling;
	
	// Pass along the current vertex position in clip-space,
	// as well as the previous vertex position in clip-space
	motionBlur.currentPosition = vs.position;
	motionBlur.previousPosition = mul(input.position, mul(transpose(lastInstanceTransform), LastFrameWorldViewProjectionMatrix));
}

void ClipVS(	in RenderVSInput input,
				out RenderVSOutput vs,
				out RenderPSInput output,
				out TexturePSInput tex,
				out NormalMapPSInput normalMap,
				in float4x4 lastInstanceTransform : BLENDWEIGHT4,
				out MotionBlurPSInput motionBlur,
				out ClipPSInput clipData)
{
	RenderVS(input, vs, output, tex, normalMap, lastInstanceTransform, motionBlur);
	clipData = GetClipData(output.position);
}

// Shadow vertex shader
void ShadowVS (	in float4 in_Position			: POSITION0,
				in float3 in_Normal				: NORMAL0,
				in float4x4 instanceTransform	: BLENDWEIGHT0,
				in int in_Index					: BLENDINDICES0,
				out ShadowVSOutput vs,
				out ShadowPSInput output)
{
	// Calculate shadow-space position
	float4x4 world = mul(WorldMatrix, transpose(instanceTransform));
	float4 worldPosition = mul(in_Position, world);
	output.worldPosition = worldPosition.xyz;
	vs.position = mul(worldPosition, ViewProjectionMatrix);
	output.clipSpacePosition = vs.position;
}

void ShadowAlphaVS(	in float4 in_Position			: POSITION0,
				in float3 in_Normal				: NORMAL0,
				in float4x4 instanceTransform	: BLENDWEIGHT0,
				in int in_Index					: BLENDINDICES0,
				out ShadowVSOutput vs,
				out ShadowPSInput output,
				out TexturePSInput tex)
{
	ShadowVS(in_Position, in_Normal, instanceTransform, in_Index, vs, output);
	float3 pos = in_Position + Offset[in_Index];
	tex.uvCoordinates = float2(pos.x + (pos.z * in_Normal.x), pos.y + (pos.z * in_Normal.y)) * 0.075f * Tiling;
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
#include "ParticleCommon.fxh"

const float Thickness = 0.01f;
const float RainSpecularPower = 50.0f;
const float RainSpecularIntensity = 0.5f;

float3 PointLightPosition;
float PointLightRadius;

// Custom vertex shader animates particles entirely on the GPU.
void RainVS(in VertexShaderInput input, out VertexShaderOutput output)
{	
	// Compute the age of the particle.
	float age = CurrentTime - input.Time;
	
	// Apply a random factor to make different particles age at different rates.
	age *= 1 + input.Random.x * DurationRandomness;
	
	// Normalize the age into the range zero to one.
	float normalizedAge = saturate(age / input.Lifetime);

	// Compute the particle position, size, color, and rotation.
	float3 worldPosition = ComputeParticlePosition(input.Position, input.Velocity, age, normalizedAge, input.Lifetime);
	worldPosition -= input.Corner.x * Thickness * InverseView._m00_m01_m02;
	worldPosition.y += input.Corner.y;
	output.WorldSpacePosition = worldPosition;
	float4 viewSpacePosition = mul(float4(worldPosition, 1), View);
	output.ViewSpacePosition = viewSpacePosition;
	float4 pos = mul(viewSpacePosition, Projection);
	
	output.Color = ComputeParticleColor(output.Position, input.Random.z, normalizedAge);
	output.TextureCoordinate = (input.Corner + 1) / 2;
	output.Position = output.ClipSpacePosition = pos;
}

void ClipRainVS(in VertexShaderInput input, out VertexShaderOutput output, out ClipPSInput clipData)
{
	RainVS(input, output);
	clipData = GetClipData(float4(output.WorldSpacePosition, 1));
}

float4 AlphaPS(VertexShaderOutput input) : COLOR0
{
	// Convert from clip space to UV coordinate space
	float2 uv = 0.5f * input.ClipSpacePosition.xy / input.ClipSpacePosition.w + float2(0.5f, 0.5f);
	uv.y = 1.0f - uv.y;

	clip(tex2D(DepthSampler, uv).r - length(input.ViewSpacePosition));

	float4 color = tex2D(Sampler, input.TextureCoordinate) * float4(input.Color.xyz, 1.0f);
	return float4(EncodeColor(color.rgb), color.a);
}

void OpaquePS(VertexShaderOutput input, out RenderPSOutput output)
{
	clip(input.Color.a - 0.01f);
	float4 color = tex2D(Sampler, input.TextureCoordinate) * float4(input.Color.xyz, 1.0f);
	clip(color.a - 0.5f);
	
	output.color.xyz = EncodeColor(color.xyz);
	output.color.w = RainSpecularPower / 255.0f;
	output.depth = float4(length(input.ViewSpacePosition), 1.0f, 1.0f, 1.0f);
	output.normal.xyz = float3(0.0f, 0.0f, 0.0f);
	output.normal.w = RainSpecularIntensity;
}

float4 ClipAlphaPS(VertexShaderOutput input, ClipPSInput clipData) : COLOR0
{
	HandleClipPlanes(clipData.clipPlaneDistances);
	return AlphaPS(input);
}

void ClipOpaquePS(VertexShaderOutput input, ClipPSInput clipData, out RenderPSOutput output)
{
	HandleClipPlanes(clipData.clipPlaneDistances);
	OpaquePS(input, output);
}

void PointLightShadowPS(in VertexShaderOutput input, out float4 out_Depth : COLOR0)
{
	out_Depth = float4(1.0f - (length(input.WorldSpacePosition - PointLightPosition) / PointLightRadius), 1.0f, 1.0f, 1.0f);
}

void ShadowPS(in VertexShaderOutput input, out float4 out_Depth : COLOR0)
{
	out_Depth = float4(1.0f - (input.ClipSpacePosition.z / input.ClipSpacePosition.w), 1.0f, 1.0f, 1.0f);
}

technique RenderOpaqueParticles
{
	pass P0
	{
		VertexShader = compile vs_3_0 RainVS();
		PixelShader = compile ps_3_0 OpaquePS();
		AlphaBlendEnable = false;
		ZEnable = true;
		ZWriteEnable = true;
	}
}

// Disable shadowing
/*technique ShadowOpaqueParticles
{
	pass P0
	{
		VertexShader = compile vs_3_0 RainVS();
		PixelShader = compile ps_3_0 ShadowPS();
		AlphaBlendEnable = false;
		ZEnable = true;
		ZWriteEnable = true;
	}
}

technique PointLightShadowOpaqueParticles
{
	pass P0
	{
		VertexShader = compile vs_3_0 RainVS();
		PixelShader = compile ps_3_0 ShadowPS();
		AlphaBlendEnable = false;
		ZEnable = true;
		ZWriteEnable = true;
	}
}*/

technique ClipOpaqueParticles
{
	pass P0
	{
		VertexShader = compile vs_3_0 ClipRainVS();
		PixelShader = compile ps_3_0 ClipOpaquePS();
		AlphaBlendEnable = false;
		ZEnable = true;
		ZWriteEnable = true;
	}
}

technique RenderAlphaParticles
{
	pass P0
	{
		VertexShader = compile vs_3_0 RainVS();
		PixelShader = compile ps_3_0 AlphaPS();
		AlphaBlendEnable = true;
		SrcBlend = SrcAlpha;
		DestBlend = InvSrcAlpha;
		ZEnable = true;
		ZWriteEnable = false;
	}
}

technique ClipAlphaParticles
{
	pass P0
	{
		VertexShader = compile vs_3_0 ClipRainVS();
		PixelShader = compile ps_3_0 ClipAlphaPS();
		AlphaBlendEnable = true;
		SrcBlend = One;
		DestBlend = One;
		ZEnable = true;
		ZWriteEnable = false;
	}
}
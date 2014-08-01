#include "ParticleCommon.fxh"

int MaterialID;

// Custom vertex shader animates particles entirely on the GPU.
void OpaqueVS(in VertexShaderInput input, out VertexShaderOutput output, uniform bool vertical, uniform float thickness)
{	
	// Compute the age of the particle.
	float age = CurrentTime - input.Time;
	
	// Apply a random factor to make different particles age at different rates.
	age *= 1 + input.Random.x * DurationRandomness;
	
	// Normalize the age into the range zero to one.
	float normalizedAge = saturate(age / input.Lifetime);

	// Compute the particle position, size, color, and rotation.
	float3 worldPosition = ComputeParticlePosition(input.Position, input.Velocity, age, normalizedAge, input.Lifetime);

	if (vertical)
	{
		worldPosition -= input.Corner.x * thickness * InverseView._m00_m01_m02;
		worldPosition.y += input.Corner.y;
	}

	output.WorldSpacePosition = worldPosition;
	float4 viewSpacePosition = mul(float4(worldPosition, 1), View);
	output.ViewSpacePosition = viewSpacePosition;
	float4 pos = mul(viewSpacePosition, Projection);

	if (!vertical)
	{
		float size = ComputeParticleSize(input.StartSize, input.Random.y, normalizedAge);
		float2x2 rotation = ComputeParticleRotation(input.Random.w, age);
		pos.xy += mul(input.Corner, rotation) * size * ViewportScale;
	}
	
	output.Color = ComputeParticleColor(output.Position, input.Random.z, normalizedAge, false);
	output.TextureCoordinate = (input.Corner + 1) / 2;
	output.Position = output.ClipSpacePosition = pos;
}

void ClipOpaqueVS(in VertexShaderInput input, out VertexShaderOutput output, out ClipPSInput clipData, uniform bool vertical, uniform float thickness)
{
	OpaqueVS(input, output, vertical, thickness);
	clipData = GetClipData(float4(output.WorldSpacePosition, 1));
}

void OpaquePS(VertexShaderOutput input, out RenderPSOutput output)
{
	clip(input.Color.a - 0.01f);
	float4 color = tex2D(Sampler, input.TextureCoordinate) * float4(input.Color.xyz, 1.0f);
	clip(color.a - 0.5f);
	
	output.color.rgb = color.rgb;
	output.color.a = EncodeMaterial(MaterialID);
	output.depth.x = length(input.ViewSpacePosition);
	output.normal.xy = EncodeNormal(float2(0, 0));
	output.depth.y = 0.0f;
	output.depth.zw = (float2)0;
	output.normal.zw = EncodeVelocity(float2(0, 0), float2(0, 0));
}

void ClipOpaquePS(VertexShaderOutput input, ClipPSInput clipData, out RenderPSOutput output)
{
	HandleClipPlanes(clipData.clipPlaneDistances);
	OpaquePS(input, output);
}
#include "Common.fxh"
#include "ClipCommon.fxh"

// Camera parameters.
float4x4 View;
float4x4 InverseView;
float4x4 Projection;
float2 ViewportScale;

// The current time, in seconds.
float CurrentTime;

// Parameters describing how the particles animate.
float DurationRandomness;
float3 Gravity;
float EndVelocity;
float4 MinColor;
float4 MaxColor;

// These float2 parameters describe the min and max of a range.
// The actual value is chosen differently for each particle,
// interpolating between x and y by some random amount.
float2 RotateSpeed;
float2 EndSize;

// Particle texture and sampler.
texture2D Texture;

sampler2D Sampler = sampler_state
{
	Texture = (Texture);
	
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Point;
	
	AddressU = Clamp;
	AddressV = Clamp;
};

// Depth texture sampler
texture2D DepthTexture;
sampler2D DepthSampler = sampler_state
{
	Texture = <DepthTexture>;
	MinFilter = point;
	MagFilter = point;
	MipFilter = point;
	AddressU = CLAMP;
	AddressV = CLAMP;
};

// Vertex shader input structure describes the start position and
// velocity of the particle, and the time at which it was created,
// along with some random values that affect its size and rotation.
struct VertexShaderInput
{
	float2 Corner : POSITION0;
	float3 Position : POSITION1;
	float3 Velocity : NORMAL0;
	float4 Random : COLOR0;
	float Time : TEXCOORD0;
	float Lifetime : TEXCOORD1;
	float StartSize : TEXCOORD2;
};

// Vertex shader output structure specifies the position and color of the particle.
struct VertexShaderOutput
{
	float4 Position : POSITION0;
	float4 Color : COLOR0;
	float2 TextureCoordinate : COLOR1;
	float4 ViewSpacePosition : TEXCOORD0;
	float4 ClipSpacePosition : TEXCOORD1;
	float3 WorldSpacePosition : TEXCOORD2;
};

// Vertex shader helper for computing the position of a particle.
float3 ComputeParticlePosition(float3 position, float3 velocity, float age, float normalizedAge, float lifetime)
{
	float startVelocity = length(velocity);

	// Work out how fast the particle should be moving at the end of its life,
	// by applying a constant scaling factor to its starting velocity.
	float endVelocity = startVelocity * EndVelocity;
	
	// Our particles have constant acceleration, so given a starting velocity
	// S and ending velocity E, at time T their velocity should be S + (E-S)*T.
	// The particle position is the sum of this velocity over the range 0 to T.
	// To compute the position directly, we must integrate the velocity
	// equation. Integrating S + (E-S)*T for T produces S*T + (E-S)*T*T/2.

	float velocityIntegral = startVelocity * normalizedAge +
							 (endVelocity - startVelocity) * normalizedAge *
															 normalizedAge / 2;
	 
	position += normalize(velocity) * velocityIntegral * lifetime;
	
	// Apply the gravitational force.
	position += Gravity * age * normalizedAge;
	
	// Apply the camera view and projection transforms.
	return position;
}

// Vertex shader helper for computing the size of a particle.
float ComputeParticleSize(float startSize, float randomValue, float normalizedAge)
{
	// Apply a random factor to make each particle a slightly different size.
	float endSize = lerp(EndSize.x, EndSize.y, randomValue);
	
	// Compute the actual size based on the age of the particle.
	float size = lerp(startSize, endSize, normalizedAge);
	
	// Project the size into screen coordinates.
	return size * Projection._m11;
}

// Vertex shader helper for computing the color of a particle.
float4 ComputeParticleColor(float4 projectedPosition,
							float randomValue, float normalizedAge)
{
	// Apply a random factor to make each particle a slightly different color.
	float4 color = lerp(MinColor, MaxColor, randomValue);
	
	color.a *= normalizedAge < 0.1f ? normalizedAge / 0.1f : 1.0f - normalizedAge;
   
	return color;
}

// Vertex shader helper for computing the rotation of a particle.
float2x2 ComputeParticleRotation(float randomValue, float age)
{    
	// Apply a random factor to make each particle rotate at a different speed.
	float rotateSpeed = lerp(RotateSpeed.x, RotateSpeed.y, randomValue);
	
	float rotation = randomValue * 3.1415 * 2.0 + (rotateSpeed * age);

	// Compute a 2x2 rotation matrix.
	float c = cos(rotation);
	float s = sin(rotation);
	
	return float2x2(c, -s, s, c);
}

// Custom vertex shader animates particles entirely on the GPU.
void ParticleVS(in VertexShaderInput input, out VertexShaderOutput output)
{	
	// Compute the age of the particle.
	float age = CurrentTime - input.Time;
	
	// Apply a random factor to make different particles age at different rates.
	age *= 1 + input.Random.x * DurationRandomness;
	
	// Normalize the age into the range zero to one.
	float normalizedAge = saturate(age / input.Lifetime);

	// Compute the particle position, size, color, and rotation.
	float3 worldPosition = ComputeParticlePosition(input.Position, input.Velocity, age, normalizedAge, input.Lifetime);
	output.WorldSpacePosition = worldPosition;
	float4 viewSpacePosition = mul(float4(worldPosition, 1), View);
	output.ViewSpacePosition = viewSpacePosition;
	float4 pos = mul(viewSpacePosition, Projection);

	float size = ComputeParticleSize(input.StartSize, input.Random.y, normalizedAge);
	float2x2 rotation = ComputeParticleRotation(input.Random.w, age);

	pos.xy += mul(input.Corner, rotation) * size * ViewportScale;
	
	output.Color = ComputeParticleColor(output.Position, input.Random.z, normalizedAge);
	output.TextureCoordinate = (input.Corner + 1) / 2;
	output.Position = output.ClipSpacePosition = pos;
}

void ClipParticleVS(in VertexShaderInput input, out VertexShaderOutput output, out ClipPSInput clipData)
{
	ParticleVS(input, output);
	clipData = GetClipData(float4(output.WorldSpacePosition, 1));
}
// Camera matrices
float4x4 ViewMatrix;
float4x4 ProjectionMatrix;
float4x4 ViewProjectionMatrix;

// Constants
static const float PI = 3.14159265f;

// Normal encoding Function
float3 EncodeNormal(float3 n)
{
	return n * 0.5f + 0.5f;
}

// Normal decoding Function
float3 DecodeNormal(float3 enc)
{
	return (enc - 0.5f) * 2.0f;
}

float3 DecodeNormalMap(float3 enc)
{
	return normalize((2.0f * enc) - 1.0f);
}

float3 EncodeColor(float3 c)
{
	return c;
}

float3 DecodeColor(float3 c)
{
	return c;
}

float2 EncodeVelocity(float2 v)
{
	return v;
}

float2 DecodeVelocity(float2 v)
{
	return v;
};

// Input and output structures
struct RenderPSInput
{
	float4 position : TEXCOORD0;
	float3 viewSpacePosition : TEXCOORD1;
};

struct RenderVSOutput
{
	float4 position : POSITION0;
};

struct ShadowVSOutput
{
	float4 position : POSITION0;
};

struct ShadowPSInput
{
	float4 clipSpacePosition : TEXCOORD0;
	float3 worldPosition : TEXCOORD1;
};

struct TexturePSInput
{
	float2 uvCoordinates : TEXCOORD2;
};

struct ClipPSInput
{
	float4 clipPlaneDistances : TEXCOORD3;
};

struct NormalMapPSInput
{
	float3x3 tangentToWorld : TEXCOORD4;
};

struct FlatPSInput
{
	float3 normal : TEXCOORD4;
};

struct AlphaPSInput
{
	float4 clipSpacePosition : TEXCOORD4;
};

struct VertexColorPSInput
{
	float4 color : TEXCOORD5;
};

struct RenderPSOutput
{
	float4 color : COLOR0;
	float4 depth : COLOR1;
	float4 normal : COLOR2;
};

struct MotionBlurPSInput
{
	float4 previousPosition : TEXCOORD7;
	float4 currentPosition : TEXCOORD8;
};

struct MotionBlurPSOutput
{
	float4 velocity : COLOR3;
};
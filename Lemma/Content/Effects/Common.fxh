// Camera matrices
float4x4 ViewMatrix;
float4x4 ProjectionMatrix;
float4x4 ViewProjectionMatrix;

// Constants
static const float PI = 3.14159265f;

float3 DecodeNormalMap(float3 enc)
{
	return (2.0f * enc) - 1.0f;
}

float2 EncodeNormal(float2 v)
{
	return v * 0.5f + 0.5f;
}

float2 DecodeNormal(float2 v)
{
	return (2.0f * v) - 1.0f;
}

const float MotionBlurMax = 32.0f;

float2 EncodeVelocity(float2 v, float2 p)
{
	// p = screen dimensions
	return (v * p * (1.0f / MotionBlurMax)) * 0.5f + 0.5f;
}

float2 DecodeVelocity(float2 v, float2 p)
{
	return ((2.0f * v) - 1.0f) * (MotionBlurMax / p);
}

float EncodeMaterial(int id)
{
	return (float)id * (1.0f / 255.0f);
}

int DecodeMaterial(float x)
{
	return (int)(x * 255.0f);
}

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
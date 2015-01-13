#include "RenderCommonAlpha.fxh"

float StartDistance;

float Time;
float2 Velocity;
float Height;
float3 CameraPosition;

struct RenderVSInput
{
	float4 position : POSITION0;
	float2 uvCoordinates : TEXCOORD0;
};

void RenderVS(	in RenderVSInput input,
				uniform bool infinite,
				out RenderVSOutput vs,
				out RenderPSInput output,
				out AlphaPSInput alpha,
				out TexturePSInput tex)
{
	float3 worldPosition;
	if (infinite)
	{
		worldPosition = input.position * 4.5f;
		worldPosition.y += Height;
		tex.uvCoordinates = input.uvCoordinates;
	}
	else
	{
		worldPosition = input.position * FarPlaneDistance;
		worldPosition.y += Height - CameraPosition.y;
		tex.uvCoordinates = input.uvCoordinates + (CameraPosition.zx / FarPlaneDistance) * 0.5f;
	}
	output.position = float4(worldPosition, 1);
	float3 viewSpacePosition = mul(worldPosition, (float3x3)ViewMatrix);
	vs.position = mul(float4(viewSpacePosition, 1), ProjectionMatrix);
	output.viewSpacePosition = viewSpacePosition;
	alpha.clipSpacePosition = vs.position;

}

void CloudPS(in RenderPSInput input,
						uniform bool infinite,
						in TexturePSInput tex,
						in AlphaPSInput alpha,
						out float4 output : COLOR0)
{
	float2 uv = 0.5f * alpha.clipSpacePosition.xy / alpha.clipSpacePosition.w + float2(0.5f, 0.5f);
	uv.y = 1.0f - uv.y;
	uv = (round(uv * DestinationDimensions) + float2(0.5f, 0.5f)) / DestinationDimensions;
	float depth = tex2D(DepthSampler, uv).r;
	float4 texColor = tex2D(DiffuseSampler, tex.uvCoordinates + Velocity * Time);
	output.rgb = DiffuseColor.rgb * texColor.rgb;

	float blend = (depth - StartDistance) / (FarPlaneDistance - StartDistance);
	if (infinite)
		blend *= max(1.0f - 2.0f * length(tex.uvCoordinates - float2(0.5f, 0.5f)), 0);
	else
		blend *= max(1.0f - (length(input.position.xyz) - StartDistance) / (FarPlaneDistance - StartDistance), 0);

	output.a = Alpha * texColor.a * clamp(blend, 0, 1);
}

void ClipCloudPS(in RenderPSInput input,
						uniform bool infinite,
						in TexturePSInput tex,
						in AlphaPSInput alpha,
						in ClipPSInput clipData,
						out float4 output : COLOR0)
{
	HandleClipPlanes(clipData.clipPlaneDistances);
	CloudPS(input, infinite, tex, alpha, output);
}
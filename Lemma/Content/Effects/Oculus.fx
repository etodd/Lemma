float2 EyeToSourceUVScale, EyeToSourceUVOffset;

float4x4 EyeRotationStart, EyeRotationEnd;

float2 TimewarpTexCoord(float2 TexCoord, float4x4 rotMat)
{
	// Vertex inputs are in TanEyeAngle space for the R,G,B channels (i.e. after chromatic
	// aberration and distortion). These are now "real world" vectors in direction (x,y,1)
	// relative to the eye of the HMD. Apply the 3x3 timewarp rotation to these vectors.
	float3 transformed = float3( mul ( rotMat, float4(TexCoord.xy, 1, 1) ).xyz);
	// Project them back onto the Z=1 plane of the rendered images.
	float2 flattened = (transformed.xy / transformed.z);
	// Scale them into ([0,0.5],[0,1]) or ([0.5,0],[0,1]) UV lookup space (depending on eye)
	return(EyeToSourceUVScale * flattened + EyeToSourceUVOffset);
}

void OculusVS
(
	in float2 Position : POSITION,
	in float timewarpLerpFactor : POSITION1,
	in float Vignette : POSITION2,
	in float2 TexCoord0 : TEXCOORD0,
	in float2 TexCoord1 : TEXCOORD1,
	in float2 TexCoord2 : TEXCOORD2,
	out float4 oPosition : POSITION,
	out float2 oTexCoord0 : TEXCOORD0,
	out float2 oTexCoord1 : TEXCOORD1,
	out float2 oTexCoord2 : TEXCOORD2,
	out float oVignette : TEXCOORD3
)
{
	float4x4 lerpedEyeRot = lerp(EyeRotationStart, EyeRotationEnd, timewarpLerpFactor);
	oTexCoord0 = TimewarpTexCoord(TexCoord0, lerpedEyeRot);
	oTexCoord1 = TimewarpTexCoord(TexCoord1, lerpedEyeRot);
	oTexCoord2 = TimewarpTexCoord(TexCoord2, lerpedEyeRot);
	oPosition = float4(Position.xy, 0.5, 1.0);
	oVignette = Vignette; /* For vignette fade */
}

texture2D FrameBufferTexture;
sampler2D FrameBufferSampler = sampler_state
{
	Texture = <FrameBufferTexture>;
	MinFilter = linear;
	MagFilter = linear;
	MipFilter = linear;
	MaxAnisotropy = 1;
	AddressU = CLAMP;
	AddressV = CLAMP;
};

float4 OculusPS
(
	in float4 oPosition : SV_Position,
	in float2 oTexCoord0 : TEXCOORD0,
	in float2 oTexCoord1 : TEXCOORD1,
	in float2 oTexCoord2 : TEXCOORD2,
	in float oVignette : TEXCOORD3
) : COLOR0
{
	// 3 samples for fixing chromatic aberrations
	float R = tex2D(FrameBufferSampler, oTexCoord0.xy).r;
	float G = tex2D(FrameBufferSampler, oTexCoord1.xy).g;
	float B = tex2D(FrameBufferSampler, oTexCoord2.xy).b;
	return oVignette * float4(R, G, B, 1);
}

technique Oculus
{
	pass p0
	{
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = false;
	
		VertexShader = compile vs_3_0 OculusVS();
		PixelShader = compile ps_3_0 OculusPS();
	}
}
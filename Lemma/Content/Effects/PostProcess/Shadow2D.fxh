// Shadow map
texture2D ShadowMapTexture;
sampler2D ShadowMapSampler = sampler_state
{
	Texture = <ShadowMapTexture>;
	MinFilter = point;
	MagFilter = point;
	MipFilter = point;
	MaxAnisotropy = 1;
	AddressU = CLAMP;
	AddressV = CLAMP;
};
float ShadowMapSize;
const float SHADOW_DEPTH_BIAS = 0.0f; // Depth bias to prevent shadow artifacts

// Shadow map sampling with simple filtering
float GetShadowValueFromClip(float2 clipPos, float depth)
{
	if (clipPos.x > 1.0f || clipPos.x < -1.0f || clipPos.y > 1.0f || clipPos.y < -1.0f)
		return 1.0f;

	// UV coordinates of the uppermost, leftmost pixel
	float2 pos = floor(clipPos * ShadowMapSize) / ShadowMapSize;
	
	// Collect samples from the surrounding four shadow map pixels
	// These will all evaluate to 1.0 or 0.0
	float bl = tex2D(ShadowMapSampler, pos).r + SHADOW_DEPTH_BIAS < depth; // Bottom left sample
	float br = tex2D(ShadowMapSampler, pos + float2(1 / ShadowMapSize, 0)).r + SHADOW_DEPTH_BIAS < depth; // Bottom right sample
	float tl = tex2D(ShadowMapSampler, pos + float2(0, 1 / ShadowMapSize)).r + SHADOW_DEPTH_BIAS < depth; // Top left sample
	float tr = tex2D(ShadowMapSampler, pos + (1 / ShadowMapSize)).r + SHADOW_DEPTH_BIAS < depth; // Top right sample
	
	// Blend between the four samples
	float horizontalBlend = (clipPos.x - pos.x) * ShadowMapSize;
	float verticalBlend = (clipPos.y - pos.y) * ShadowMapSize;
	float bottom = (bl * (1.0f - horizontalBlend)) + (br * horizontalBlend);
	float top = (tl * (1.0f - horizontalBlend)) + (tr * horizontalBlend);
	return (bottom * (1.0f - verticalBlend)) + (top * verticalBlend);
}

float GetShadowValue(float4 position)
{
	// Get the shadow map depth value for this pixel
	float depth = 1.0f - (position.z / position.w);

	// Convert from clip space to UV coordinate space
	float2 ShadowTexClipPosition = (0.5f * (position.xy / position.w)) + float2(0.5f, 0.5f);
	ShadowTexClipPosition.y = 1.0f - ShadowTexClipPosition.y;

	return GetShadowValueFromClip(ShadowTexClipPosition, depth);
}
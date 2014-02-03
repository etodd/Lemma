#include "EffectCommon.fxh"

float MotionBlurAmount = 1.0f;
float SpeedBlurAmount = 1.0f;

float4 SampleMotionBlur(float2 texCoord, float2 pixelVelocity)
{
	const int numSamples = 16;

	// Clamp to a max velocity.  The max we can go without artifacts os
	// is 1.4f * iNumSamples...but we can fudge things a little.
	float2 maxVelocity = (MotionBlurAmount * 1.4f * numSamples) / SourceDimensions0;
	pixelVelocity = clamp(pixelVelocity, -maxVelocity, maxVelocity);

	// For each sample, sum up each sample's color in "vSum" and then divide
	// to average the color after all the samples are added.
	float4 sum = 0;
	[unroll]
	for (int i = 0; i < numSamples; i++)
		sum += tex2D(SourceSampler0, texCoord + (pixelVelocity * ((float)i  / (float)numSamples)));
	
	// Return the average color of all the samples
	return sum / (float)numSamples;
}

float4 MotionBlurPS(in PostProcessPSInput input)	: COLOR0
{
	const float threshold = 1.0f / 127.0f;
	const float thresholdSquared = threshold * threshold;
	// Sample velocity from our velocity buffers
	float2 currentFramePixelVelocity = tex2D(SourceSampler1, input.texCoord).xy - float2(0.5f, 0.5f);

	float2 lastFramePixelVelocity = tex2D(SourceSampler2, input.texCoord).xy - float2(0.5f, 0.5f);

	// We'll compare the magnitude of the velocity from the current frame and from
	// the previous frame, and then use whichever is larger
	float2 pixelVelocity = 0;

	float currentVelocitySquared = currentFramePixelVelocity.x * currentFramePixelVelocity.x +
						   currentFramePixelVelocity.y * currentFramePixelVelocity.y;

	// Speed blurring
	float2 speedOffset = (input.texCoord + float2(-0.5f, -0.5f)) * SpeedBlurAmount * 0.08f;
	float speedOffsetSquared = speedOffset.x * speedOffset.x + speedOffset.y * speedOffset.y;
	if (speedOffsetSquared > thresholdSquared)
	{
		currentFramePixelVelocity = speedOffset * (1.0f - threshold / sqrt(speedOffsetSquared));
		currentVelocitySquared = speedOffsetSquared;
	}

	float lastVelocitySquared = lastFramePixelVelocity.x * lastFramePixelVelocity.x +
							  lastFramePixelVelocity.y * lastFramePixelVelocity.y;

	float velocitySquared;
	if (lastVelocitySquared > currentVelocitySquared)
	{
		pixelVelocity = lastFramePixelVelocity;
		velocitySquared = lastVelocitySquared;
	}
	else
	{
		pixelVelocity = currentFramePixelVelocity;
		velocitySquared = currentVelocitySquared;
	}
	if (velocitySquared < thresholdSquared)
		return tex2D(SourceSampler0, input.texCoord);
	return SampleMotionBlur(input.texCoord, pixelVelocity);
}

technique MotionBlur
{
	pass p0
	{
		VertexShader = compile vs_3_0 PostProcessVS();
		PixelShader = compile ps_3_0 MotionBlurPS();
		
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = false;
	}
}
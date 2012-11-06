float4 ClipPlanes[4] = { (float4)0, (float4)0, (float4)0, (float4)0 };

ClipPSInput GetClipData(float4 position)
{
	ClipPSInput result;
	result.clipPlaneDistances.x = dot(position, ClipPlanes[0]);
	result.clipPlaneDistances.y = dot(position, ClipPlanes[1]);
	result.clipPlaneDistances.z = dot(position, ClipPlanes[2]);
	result.clipPlaneDistances.w = dot(position, ClipPlanes[3]);
	return result;
}

void HandleClipPlanes(float4 clipPlanes)
{
	clip(clipPlanes.xyz);
	clip(clipPlanes.w);
}
namespace BEPUphysics.Paths
{
    /// <summary>
    /// Defines how a curve behaves beyond an endpoint.
    /// </summary>
    public enum CurveEndpointBehavior
    {
        /// <summary>
        /// When the time exceeds the endpoint, it wraps around to the other end of the curve.
        /// </summary>
        Wrap,
        /// <summary>
        /// Times exceeding the endpoint are clamped to the endpoint's value.
        /// </summary>
        Clamp,
        /// <summary>
        /// Times exceeding the endpoint will reverse direction and sample backwards.
        /// </summary>
        Mirror
    }
}
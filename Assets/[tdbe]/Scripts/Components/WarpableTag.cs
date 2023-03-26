using Unity.Entities;

namespace GameWorld
{
    // can be placed on anything, but aimed for things that hit the edge of hte screen and should warp to the other side.
    public struct WarpableTag : IComponentData
    {
        public double lastWarpTime;
        public double warpImmunityPeriod;// this is obviously not reliable if warpables move fast or if physics system lags
    }
}

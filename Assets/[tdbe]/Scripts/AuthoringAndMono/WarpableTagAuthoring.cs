using UnityEngine;
using Unity.Entities;

namespace GameWorld
{
    // can be placed on anything, but aimed for things that hit the edge of hte screen and should warp to the other side.
    public class WarpableAuthoring : MonoBehaviour
    {
        public double warpImmunityPeriod = 0.5;// this is obviously not reliable if warpables move fast or if physics system lags

        public class WarpableBaker : Baker<WarpableAuthoring>
        {
            public override void Bake(WarpableAuthoring authoring)
            {
                AddComponent<WarpableTag>(new WarpableTag{
                   warpImmunityPeriod = authoring.warpImmunityPeriod
                });
            }
        }
    }

    
}
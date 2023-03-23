using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace World.Asteroid
{
    public class AsteroidSpawnerStateAuthoring : MonoBehaviour
    {
       
        public AsteroidSpawnerStateComponent.State state = AsteroidSpawnerStateComponent.State.Inactive;


        public class AsteroidSpawnerStateBaker : Baker<AsteroidSpawnerStateAuthoring>
        {
            public override void Bake(AsteroidSpawnerStateAuthoring authoring)
            {
                AddComponent<AsteroidSpawnerStateComponent>(new AsteroidSpawnerStateComponent{
                    state = authoring.state
                });
            }
        }
    }

    
}
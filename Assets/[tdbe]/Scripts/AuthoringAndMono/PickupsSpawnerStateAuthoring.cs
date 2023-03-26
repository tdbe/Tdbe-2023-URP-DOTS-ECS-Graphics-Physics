using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace GameWorld.Pickups
{
    public class PickupsSpawnerStateAuthoring : MonoBehaviour
    {
       
        public PickupsSpawnerStateComponent.State state = PickupsSpawnerStateComponent.State.Inactive;


        public class PickupsSpawnerStateBaker : Baker<PickupsSpawnerStateAuthoring>
        {
            public override void Bake(PickupsSpawnerStateAuthoring authoring)
            {
                AddComponent<PickupsSpawnerStateComponent>(new PickupsSpawnerStateComponent{
                    state = authoring.state
                });
            }
        }
    }

    
}
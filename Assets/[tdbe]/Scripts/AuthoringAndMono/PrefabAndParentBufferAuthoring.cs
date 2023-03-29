using UnityEngine;
using Unity.Entities;
using Unity.Collections;

namespace GameWorld
{
    public class PrefabAndParentAuthoring : MonoBehaviour
    {
        [Header("One or more.You can leave the parent empty.")]
        public GameObject[] prefabs;
        public GameObject[] parents;

        public class AsteroidPrefabBaker : Baker<PrefabAndParentAuthoring>
        {
            public override void Bake(PrefabAndParentAuthoring authoring)
            {
                var buffer = AddBuffer<PrefabAndParentBufferComponent>();
                for (int i = 0; i < authoring.prefabs.Length; i++)
                {
                    DependsOn(authoring.prefabs[i]);
    
                    buffer.Add(new PrefabAndParentBufferComponent
                    {
                        prefab = GetEntity(authoring.prefabs[i]),
                        parent = GetEntity(authoring.parents[i])
                    });
                }
            }
        }
    }

    
}
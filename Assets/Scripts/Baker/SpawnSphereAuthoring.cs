using Components;
using Unity.Entities;
using UnityEngine;

namespace Baker
{
    public class SpawnSphereAuthoring : MonoBehaviour
    {
        private class SpawnSphereAuthoringBaker : Baker<SpawnSphereAuthoring>
        {
            public override void Bake(SpawnSphereAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                
                AddComponent<SpawnSphereComponent>(entity);
            }
        }
    }
}
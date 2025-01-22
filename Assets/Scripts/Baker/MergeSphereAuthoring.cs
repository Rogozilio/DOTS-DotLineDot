using Components;
using Unity.Entities;
using UnityEngine;

namespace Baker
{
    public class MergeSphereAuthoring : MonoBehaviour
    {
        private class MergeSphereAuthoringBaker : Baker<MergeSphereAuthoring>
        {
            public override void Bake(MergeSphereAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                
                AddComponent<MergeSphereComponent>(entity);
            }
        }
    }
}
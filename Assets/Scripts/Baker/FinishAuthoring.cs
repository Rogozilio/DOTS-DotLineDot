using Components;
using Unity.Entities;
using UnityEngine;

namespace Baker
{
    public class FinishAuthoring : MonoBehaviour
    {
        private class FinishAuthoringBaker : Baker<FinishAuthoring>
        {
            public override void Bake(FinishAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.WorldSpace);
                
                AddComponent<FinishComponent>(entity);
                SetComponentEnabled<FinishComponent>(entity, false);
            }
        }
    }
}
using Components;
using Components.Shared;
using Tags;
using Unity.Entities;
using UnityEngine;

namespace Baker
{
    public class ElementAuthoring : MonoBehaviour
    {
        private class ElementAuthoringBaker : Baker<ElementAuthoring>
        {
            public override void Bake(ElementAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent<IndexConnectComponent>(entity);
                AddComponent<IsElementNotConnected>(entity);
                AddComponent<TargetGravityComponent>(entity);
                SetComponentEnabled<TargetGravityComponent>(entity, true);
                AddSharedComponent(entity, new IndexSharedComponent { value = -1 });
            }
        }
    }
}
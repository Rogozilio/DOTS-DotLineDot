using Components;
using Unity.Entities;
using UnityEngine;

namespace Baker
{
    public class RaycastAuthoring : MonoBehaviour
    {
        private class RaycastAuthoringBaker : Baker<RaycastAuthoring>
        {
            public override void Bake(RaycastAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent<RaycastHitComponent>(entity);
            }
        }
    }
}
using Components;
using Unity.Entities;
using UnityEngine;

namespace Baker
{
    public class InputAuthoring : MonoBehaviour
    {
        private class InputAuthoringBaker : Baker<InputAuthoring>
        {
            public override void Bake(InputAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent<InputDataComponent>(entity);
            }
        }
    }
}
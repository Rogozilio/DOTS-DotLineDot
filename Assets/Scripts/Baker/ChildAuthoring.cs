using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Baker
{
    public class ChildAuthoring : MonoBehaviour
    {
        public GameObject parent;
    }

    public class ChildAuthoringBaker : Baker<ChildAuthoring>
    {
        public override void Bake(ChildAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            var parent = GetEntity(authoring.parent, TransformUsageFlags.Dynamic);
            AddComponent(entity, new Parent(){ Value = parent});
        }
    }
}
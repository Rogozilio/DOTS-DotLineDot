using Unity.Entities;
using Unity.Entities.Serialization;

namespace Components
{
    public struct CurrentSceneComponent : IComponentData
    {
        public SceneDataComponent data;
        public Entity entity;
    }
}
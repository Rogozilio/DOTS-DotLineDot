using Unity.Entities;
using Unity.Entities.Serialization;

namespace Components
{
    public struct SceneDataComponent : IComponentData
    {
        public int indexBuffer;
        public EntitySceneReference sceneReference;
    }
}
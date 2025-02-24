using Unity.Entities;
using Unity.Entities.Serialization;

namespace Components
{
    public struct LoadLevelComponent : IComponentData
    {
        public SceneDataComponent data;
    }
}
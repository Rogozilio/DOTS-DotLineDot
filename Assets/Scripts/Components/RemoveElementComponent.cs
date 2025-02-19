using Unity.Entities;

namespace Components
{
    public struct RemoveElementComponent : IComponentData
    {
        public Entity element;
        public Entity joint1;
        public Entity joint2;
        public Entity connect1;
        public Entity connect2;
    }
}
using Unity.Entities;

namespace Tags
{
    public struct IsMouseMove : IComponentData, IEnableableComponent
    {
        public bool isLastMove;
    }
}
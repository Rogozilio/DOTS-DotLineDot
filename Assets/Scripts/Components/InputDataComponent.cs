using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Components
{
    public struct InputDataComponent : IComponentData
    {
        public bool isLeftMouseClicked;
        public bool isLeftMouseUp;
        public bool isLeftMouseDown;
        public bool isRightMouseClicked;
        public bool isRightMouseUp;
        public bool isRightMouseDown;
        public Ray ray;
    }
}
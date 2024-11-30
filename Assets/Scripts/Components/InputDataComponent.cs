using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Components
{
    public struct InputDataComponent : IComponentData
    {
        public bool isMouseClicked;
        public bool isMouseUp;
        public bool isMouseDown;
        public float3 mousePosition;
        public Ray ray;
    }
}
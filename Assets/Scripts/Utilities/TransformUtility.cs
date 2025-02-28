using Unity.Mathematics;

namespace Utilities
{
    public static class TransformUtility
    {
        public static float3 DefaultPositionSphere(int offsetX = 0)
        {
            return new float3(0f + offsetX, -10, 0f);
        }
        
        public static float3 DefaultPositionElement(int offsetX = 0)
        {
            return new float3(0f + offsetX, -15, 0f);
        }
    }
}
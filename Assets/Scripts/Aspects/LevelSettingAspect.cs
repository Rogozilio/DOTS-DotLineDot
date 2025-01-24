using Components.DynamicBuffers;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Aspects
{
    public readonly partial struct LevelSettingAspect : IAspect
    {
        private readonly Entity _entity;
        private readonly DynamicBuffer<NotActiveSphereBuffer> _buffer;

        public DynamicBuffer<NotActiveSphereBuffer> buffer => _buffer;

        public void AddSphereInBuffer(EntityCommandBuffer ecb, Entity entity)
        {
            AddSphereInBuffer(ecb, entity, _buffer.Length);
        }
        
        public void AddSphereInBuffer(EntityCommandBuffer ecb, Entity entity, int indexBuffer)
        {
            ecb.SetName(entity, "Sphere" + indexBuffer);
            ecb.SetComponent(entity, new LocalTransform()
            {
                Position = new float3(1 + indexBuffer * 1.1f, -10, 1),
                Rotation = quaternion.identity,
                Scale = 1f
            });
            
            ecb.AppendToBuffer(_entity, new NotActiveSphereBuffer { value = entity });
        }

        public Entity GetSphereInBuffer => buffer[^1].value;
    }
}
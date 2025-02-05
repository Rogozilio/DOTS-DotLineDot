using Components.DynamicBuffers;
using Components.Shared;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Aspects
{
    public readonly partial struct LevelSettingAspect : IAspect
    {
        private readonly Entity _entity;
        private readonly RefRO<LevelSettingComponent> _levelSettingComponent;
        private readonly DynamicBuffer<NotActiveSphereBuffer> _buffer;
        public DynamicBuffer<NotActiveSphereBuffer> buffer => _buffer;
        public LevelSettingComponent level => _levelSettingComponent.ValueRO;

        public void AddSphereInBuffer(EntityCommandBuffer ecb, Entity entity, bool isChangeName = false)
        {
            AddSphereInBuffer(ecb, entity, _buffer.Length, isChangeName);
        }

        public int LengthBuffer => _buffer.Length;

        public void AddSphereInBuffer(EntityCommandBuffer ecb, Entity entity, int indexBuffer, bool isChangeName = false)
        {
            if(isChangeName)
                ecb.SetName(entity, "Sphere" + indexBuffer);
            
            ecb.SetComponent(entity, new LocalTransform()
            {
                Position = new float3(1 + indexBuffer * 1.1f, -10, 1),
                Rotation = quaternion.identity,
                Scale = 1f
            });
            ecb.SetSharedComponent(entity, new IndexSharedComponent { value = -1 });
            ecb.SetBuffer<IndexConnectionBuffer>(entity); //Clear buffer

            ecb.AppendToBuffer(_entity, new NotActiveSphereBuffer { value = entity });
        }

        public Entity GetSphereFromBuffer()
        {
            var entity = _buffer[^1].value;
            buffer.RemoveAt(_buffer.Length - 1);
            return entity;
        }
    }
}
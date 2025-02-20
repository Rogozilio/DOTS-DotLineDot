using Components;
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
        private readonly RefRW<LevelSettingComponent> _levelSettingComponent;
        private readonly DynamicBuffer<PullSphereBuffer> _pullSphere;
        private readonly DynamicBuffer<PullElementBuffer> _pullElement;
        private readonly DynamicBuffer<PullJointBuffer> _pullJoint;
        public DynamicBuffer<PullSphereBuffer> buffer => _pullSphere;
        public LevelSettingComponent level => _levelSettingComponent.ValueRO;

        public void IncrementIndexConnect()
        {
            _levelSettingComponent.ValueRW.indexConnection++;
        }
        
        public void IncrementIndexConnect(EntityCommandBuffer ecb, Entity entity)
        {
            _levelSettingComponent.ValueRW.indexConnection++;
            ecb.SetComponent(entity, _levelSettingComponent.ValueRO);
        }
        
        public void AddSphereInPull(EntityCommandBuffer ecb, Entity entity, bool isChangeName = false)
        {
            AddSphereInPull(ecb, entity, _pullSphere.Length, isChangeName);
        }

        public void AddSphereInPull(EntityCommandBuffer ecb, Entity entity, int index, bool isChangeName = false)
        {
            if(isChangeName)
                ecb.SetName(entity, "Sphere" + index);
            
            ecb.SetComponent(entity, new LocalTransform()
            {
                Position = new float3(1 + index * 1.1f, -10, 1),
                Rotation = quaternion.identity,
                Scale = 1f
            });
            ecb.SetSharedComponent(entity, new IndexSharedComponent { value = -1 });
            ecb.SetBuffer<IndexConnectionBuffer>(entity); //Clear buffer

            ecb.AppendToBuffer(_entity, new PullSphereBuffer { value = entity });
        }
        
        public void AddElementInPull(EntityCommandBuffer ecb, Entity entity, bool isChangeName = false)
        {
            if(isChangeName)
                ecb.SetName(entity, "Element");
            
            ecb.SetSharedComponent(entity, new IndexSharedComponent { value = -1 });
            ecb.SetComponent(entity, new IndexConnectComponent{value = -1}); //Clear buffer
            
            ecb.AppendToBuffer(_entity, new PullElementBuffer { value = entity });
        }
        
        public void AddJointInPull(EntityCommandBuffer ecb, Entity entity, bool isChangeName = false)
        {
            if(isChangeName)
                ecb.SetName(entity, "Joint");
            
            ecb.AppendToBuffer(_entity, new PullJointBuffer { value = entity });
        }

        public Entity GetSphereFromBuffer()
        {
            var entity = _pullSphere[^1].value;
            buffer.RemoveAt(_pullSphere.Length - 1);
            return entity;
        }
    }
}
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
        private readonly RefRW<LevelSettingComponent> _levelSettingComponent;
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
    }
}
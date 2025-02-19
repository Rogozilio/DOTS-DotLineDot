using Components;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace Systems
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial struct SkipFrameSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            state.Dependency = new SkipFrameJob
            {
                ecbParallel = ecb.AsParallelWriter()
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        public partial struct SkipFrameJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ecbParallel;
            public void Execute(Entity entity, [EntityIndexInQuery]int sortKey, ref SkipFrameComponent skipFrame)
            {
                skipFrame.count--;
                
                if (skipFrame.count == 0)
                {
                    ecbParallel.RemoveComponent<SkipFrameComponent>(sortKey, entity);
                }
            }
        }
    }
}
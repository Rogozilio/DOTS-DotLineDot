using Components;
using Components.DynamicBuffers;
using Static;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace Systems
{
    [UpdateInGroup(typeof(BeforePhysicsSystemGroup))]
    [UpdateBefore(typeof(GravityInSphereSystem))]
    [UpdateAfter(typeof(MoveMouseSphereSystem))]
    public partial struct BlockElementsInSphere : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.Enabled = false;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var buffer = SystemAPI.GetSingletonBuffer<BlockElementBufferComponent>();

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var count = SystemAPI.QueryBuilder().WithDisabled<TargetGravityComponent>().Build().CalculateEntityCount();
            Debug.Log(count);
            if (count >= 5 && buffer.Length == 0)
            {
                state.Dependency = new CreateJointForDisabledTargetGravity
                {
                    ecb = ecb.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                    buffer = buffer
                }.Schedule(state.Dependency);
            }
        }

        [BurstCompile]
        private partial struct CreateJointForDisabledTargetGravity : IJobEntity
        {
            internal EntityCommandBuffer.ParallelWriter ecb;
            public DynamicBuffer<BlockElementBufferComponent> buffer;

            private void Execute(Entity entity, [ChunkIndexInQuery] int sortKey,
                in TargetGravityComponent targetGravityComponent, in LocalToWorld local)
            {
                var distance = math.distance(local.Position, targetGravityComponent.position);
                var e = StaticMethod.CreateJoint(ecb, sortKey, entity, targetGravityComponent.target, distance, "asd");
                buffer.Add(new BlockElementBufferComponent { element = e });
            }
        }
    }
}
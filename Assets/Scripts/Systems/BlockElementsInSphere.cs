using Aspects;
using Components;
using Components.DynamicBuffers;
using Components.Shared;
using Static;
using Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace Systems
{
    [UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
    //[UpdateAfter(typeof(MoveMouseSphereSystem))]
    [UpdateBefore(typeof(GravityInSphereSystem))]
    public partial struct BlockElementsInSphere : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BlockElementBuffer>();
            state.RequireForUpdate<SphereComponent>();
            state.RequireForUpdate<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            var buffer = SystemAPI.GetSingletonEntity<BlockElementBuffer>();

            state.EntityManager.GetAllUniqueSharedComponents(out NativeList<IndexSharedComponent> indexSharedComponents,
                AllocatorManager.TempJob);
            
            var entityQueryElements = SystemAPI.QueryBuilder()
                .WithAll<IndexSharedComponent>()
                .WithDisabled<TargetGravityComponent>()
                .Build();
            var entityQuerySpheres = SystemAPI.QueryBuilder()
                .WithAll<IndexSharedComponent, SphereComponent>()
                .Build();
            var entityQueryBlockedSpheres = SystemAPI.QueryBuilder()
                .WithAll<IndexSharedComponent, SphereComponent, IsBlockedSphere>()
                .Build();

            
            foreach (var index in indexSharedComponents)
            {
                if (index.value == -1) continue;
                
                var countMax = GetLimitElements(entityQuerySpheres, index);
                var count = GetCountElements(entityQueryElements, index);
                var isBlocked = GetCountElements(entityQueryBlockedSpheres, index) > 0;

                Debug.Log("Index " + index.value + " Count " + count);

                if (count >= countMax - 4 && !isBlocked)
                {
                    Debug.Log("Block Index " + index.value + "; Count " + count + " " + SystemAPI.Time.ElapsedTime);
                    state.Dependency = new CreateJointForDisabledTargetGravity
                    {
                        ecb = ecb,
                        bufferEntity = buffer,
                        index = index.value
                    }.Schedule(state.Dependency);
                    state.Dependency = new SwitchIsBlockedSphereJob
                    {
                        value = true,
                        index = index.value
                    }.Schedule(state.Dependency);
                    break;
                }
                
                if (count < countMax - 6 && isBlocked)
                {
                    Debug.Log("Unblock " + count + " " + SystemAPI.Time.ElapsedTime);
          
                    state.Dependency = new RemoveFromBlockElementBufferByIndexJob
                    {
                        ecb = ecb, index = index.value
                    }.Schedule(state.Dependency);
                    state.Dependency = new SwitchIsBlockedSphereJob
                    {
                        value = false,
                        index = index.value
                    }.Schedule(state.Dependency);
                    break;
                }
            }

            indexSharedComponents.Dispose();
        }

        private int GetLimitElements(EntityQuery entityQuery, IndexSharedComponent index)
        {
            entityQuery.SetSharedComponentFilter(index);
            var spheres = entityQuery.ToComponentDataArray<SphereComponent>(Allocator.Temp);

            if (spheres.Length == 0)
            {
                spheres.Dispose();
                return 0;
            }

            var count = spheres[0].countElements;
            spheres.Dispose();
            return count;
        }
        
        private int GetCountElements(EntityQuery entityQuery, IndexSharedComponent index)
        {
            entityQuery.SetSharedComponentFilter(index);
            return entityQuery.CalculateEntityCount();
        }

        [BurstCompile]
        private partial struct CreateJointForDisabledTargetGravity : IJobEntity
        {
            internal EntityCommandBuffer.ParallelWriter ecb;
            public Entity bufferEntity;
            public int index;

            private void Execute(Entity entity, [ChunkIndexInQuery] int sortKey, ElementAspect element,
                in IndexSharedComponent index)
            {
                if(index.value != this.index) return;
                
                var e = StaticMethod.CreateJoint(ecb, sortKey, entity, element.TargetGravity.target,
                    element.DistanceToTargetGravity, -1, "JointForBlock", true);
                ecb.AppendToBuffer(sortKey, bufferEntity, new BlockElementBuffer { element = e, index = index.value });
            }
        }

        [BurstCompile]
        public partial struct RemoveFromBlockElementBufferByIndexJob : IJobEntity
        {
            internal EntityCommandBuffer.ParallelWriter ecb;
            public int index;

            public void Execute([ChunkIndexInQuery] int sortKey, DynamicBuffer<BlockElementBuffer> buffer)
            {
                for (var i = 0; i < buffer.Length; i++)
                {
                    if (buffer[i].index == index)
                        ecb.DestroyEntity(sortKey, buffer[i].element);
                }
            }
        }

        [BurstCompile]
        [WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
        public partial struct SwitchIsBlockedSphereJob : IJobEntity
        {
            [ReadOnly] public bool value;
            [ReadOnly] public int index;

            public void Execute(EnabledRefRW<IsBlockedSphere> isBlockedSphere, in IndexSharedComponent index)
            {
                if(index.value != this.index) return;
                
                isBlockedSphere.ValueRW = value;
            }
        }
    }
}
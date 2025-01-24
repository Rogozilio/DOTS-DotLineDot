using Aspects;
using Components;
using Components.DynamicBuffers;
using Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace Systems
{
    [UpdateInGroup(typeof(BeforePhysicsSystemGroup))]
    [UpdateAfter(typeof(MoveMouseSphereSystem))]
    public partial struct MergeSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MergeSphereComponent>();
            state.RequireForUpdate<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var merge = SystemAPI.GetSingletonRW<MergeSphereComponent>();

            var ecbSingleton = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            state.Dependency = new SetMergeSphereFromJob
            {
                merge = merge
            }.Schedule(state.Dependency);
            state.Dependency = new SetMergeSphereToJob
            {
                merge = merge
            }.Schedule(state.Dependency);
            state.Dependency = new SphereMoveIntoSphereJob
            {
                merge = merge,
                deltaTime = SystemAPI.Time.fixedDeltaTime,
                localTransforms = SystemAPI.GetComponentLookup<LocalTransform>(),
                isCollisionWithSpheres = SystemAPI.GetComponentLookup<IsCollisionWithSphere>(),
                isMouseMoves = SystemAPI.GetComponentLookup<IsMouseMove>(),
            }.Schedule(state.Dependency);
            if(!merge.ValueRO.launchLastStageMerge) return;
            state.Dependency = new SetIndexBetweenFromAndToForMergeSphereJob
            {
                merge = merge,
                buffer = SystemAPI.GetBufferLookup<IndexConnectionBuffer>(true)
            }.Schedule(state.Dependency);
            state.Dependency = new RemoveConnectBetweenSphereJob
            {
                merge = merge,
                ecb = ecb.AsParallelWriter()
            }.Schedule(state.Dependency);
            state.Dependency = new RemoveIndexConnectInSphereJob
            {
                merge = merge
            }.ScheduleParallel(state.Dependency);
            state.Dependency = new RemoveSphereJob()
            {
                merge = merge,
                ecb = ecb
            }.Schedule(state.Dependency);
            Debug.Log("13");
            merge.ValueRW.launchLastStageMerge = false;
        }

        [BurstCompile]
        [WithAll(typeof(IsMouseMove))]
        public partial struct SetMergeSphereFromJob : IJobEntity
        {
            [NativeDisableUnsafePtrRestriction] public RefRW<MergeSphereComponent> merge;

            public void Execute(Entity entity)
            {
                merge.ValueRW.from = entity;
            }
        }

        [BurstCompile]
        [WithAll(typeof(IsCollisionWithSphere))]
        [WithNone(typeof(IsMouseMove))]
        public partial struct SetMergeSphereToJob : IJobEntity
        {
            [NativeDisableUnsafePtrRestriction] public RefRW<MergeSphereComponent> merge;

            public void Execute(Entity entity)
            {
                if (merge.ValueRW.from == entity) return;

                merge.ValueRW.to = entity;
            }
        }

        [BurstCompile]
        public struct SphereMoveIntoSphereJob : IJob
        {
            public ComponentLookup<LocalTransform> localTransforms;

            [ReadOnly] public float deltaTime;
            [ReadOnly] public ComponentLookup<IsCollisionWithSphere> isCollisionWithSpheres;
            [ReadOnly] public ComponentLookup<IsMouseMove> isMouseMoves;

            [NativeDisableUnsafePtrRestriction] public RefRW<MergeSphereComponent> merge;

            public void Execute()
            {
                if (merge.ValueRW.from == Entity.Null || merge.ValueRW.to == Entity.Null) return;

                if (!isCollisionWithSpheres.IsComponentEnabled(merge.ValueRW.from) ||
                    !isCollisionWithSpheres.IsComponentEnabled(merge.ValueRW.to))
                    return;

                if (isMouseMoves.IsComponentEnabled(merge.ValueRW.from) ||
                    isMouseMoves.IsComponentEnabled(merge.ValueRW.to))
                    return;

                float3 direction =
                    math.normalize(localTransforms[merge.ValueRW.to].Position -
                                   localTransforms[merge.ValueRW.from].Position);
                float distance = math.distance(localTransforms[merge.ValueRW.to].Position,
                    localTransforms[merge.ValueRW.from].Position);

                var transform = localTransforms[merge.ValueRW.from];

                if (distance > 5f * deltaTime)
                    transform.Position += direction * 5f * deltaTime;
                else
                {
                    transform.Position = localTransforms[merge.ValueRW.to].Position;
                    merge.ValueRW.launchLastStageMerge = true;
                }

                localTransforms[merge.ValueRW.from] = transform;
            }
        }

        [BurstCompile]
        public struct SetIndexBetweenFromAndToForMergeSphereJob : IJob
        {
            [NativeDisableUnsafePtrRestriction] public RefRW<MergeSphereComponent> merge;
            [ReadOnly] public BufferLookup<IndexConnectionBuffer> buffer;

            public void Execute()
            {
                if (!buffer.EntityExists(merge.ValueRW.from) || !buffer.EntityExists(merge.ValueRW.to)) return;

                foreach (var indexFrom in buffer[merge.ValueRW.from])
                {
                    foreach (var indexTo in buffer[merge.ValueRW.to])
                    {
                        if (indexFrom.value != indexTo.value) continue;

                        merge.ValueRW.indexBetweenFromAndTo = indexFrom.value;
                        return;
                    }
                }

                merge.ValueRW.indexBetweenFromAndTo = -1;
            }
        }

        [BurstCompile]
        public partial struct RemoveConnectBetweenSphereJob : IJobEntity
        {
            [NativeDisableUnsafePtrRestriction] public RefRW<MergeSphereComponent> merge;
            public EntityCommandBuffer.ParallelWriter ecb;

            public void Execute(Entity entity,[EntityIndexInQuery] int sortKey, in IndexConnectComponent indexConnect)
            {
                if (indexConnect.value == merge.ValueRW.indexBetweenFromAndTo)
                    ecb.DestroyEntity(sortKey, entity);
            }
        }
        
        [BurstCompile]
        public partial struct RemoveIndexConnectInSphereJob : IJobEntity
        {
            [NativeDisableUnsafePtrRestriction] public RefRW<MergeSphereComponent> merge;

            public void Execute(ref DynamicBuffer<IndexConnectionBuffer> buffer)
            {
                for (var i = 0; i < buffer.Length; i++)
                {
                    if (buffer[i].value != merge.ValueRW.indexBetweenFromAndTo) continue;
                    
                    buffer.RemoveAt(i);
                    return;
                }
            }
        }

        [BurstCompile]
        public partial struct RemoveSphereJob : IJobEntity
        {
            [NativeDisableUnsafePtrRestriction] public RefRW<MergeSphereComponent> merge;

            public EntityCommandBuffer ecb;
            
            public void Execute(LevelSettingAspect levelSettingAspect)
            {
                levelSettingAspect.AddSphereInBuffer(ecb, merge.ValueRW.from);
                merge.ValueRW.from = Entity.Null;
            }
        }
    }
}
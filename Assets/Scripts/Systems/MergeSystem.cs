﻿using Aspects;
using Components;
using Components.DynamicBuffers;
using Static;
using Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

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
                isCollisionWithSpheres = SystemAPI.GetComponentLookup<IsCollisionWithSphere>(true),
                isMouseMoves = SystemAPI.GetComponentLookup<IsMouseMove>(true),
            }.Schedule(state.Dependency);
            if (!merge.ValueRO.launchLastStageMerge) return;
            state.Dependency = new SetIndexBetweenFromAndToForMergeSphereJob
            {
                merge = merge,
                buffers = SystemAPI.GetBufferLookup<IndexConnectionBuffer>(true)
            }.Schedule(state.Dependency);
            state.Dependency = new RemoveConnectBetweenSphereJob
            {
                merge = merge,
                ecb = ecb.AsParallelWriter()
            }.Schedule(state.Dependency);
            state.Dependency = new RemoveIndexConnectInSphereJob
            {
                merge = merge
            }.Schedule(state.Dependency);
            state.Dependency = new ReconnectJointJob()
            {
                merge = merge,
                ecb = ecb,
                localTransforms = SystemAPI.GetComponentLookup<LocalTransform>(true),
                buffers = SystemAPI.GetBufferLookup<IndexConnectionBuffer>()
            }.Schedule(state.Dependency);
            state.Dependency = new RemoveSphereJob()
            {
                merge = merge,
                ecb = ecb
            }.Schedule(state.Dependency);
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
                if (merge.ValueRO.from == entity) return;

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
                if (merge.ValueRO.from == Entity.Null || merge.ValueRO.to == Entity.Null) return;

                if (!isCollisionWithSpheres.IsComponentEnabled(merge.ValueRO.from) ||
                    !isCollisionWithSpheres.IsComponentEnabled(merge.ValueRO.to))
                    return;

                if (isMouseMoves.IsComponentEnabled(merge.ValueRO.from) ||
                    isMouseMoves.IsComponentEnabled(merge.ValueRO.to))
                    return;

                float3 direction =
                    math.normalizesafe(localTransforms[merge.ValueRO.to].Position -
                                       localTransforms[merge.ValueRO.from].Position);
                float distance = math.distance(localTransforms[merge.ValueRO.to].Position,
                    localTransforms[merge.ValueRO.from].Position);

                var transform = localTransforms[merge.ValueRO.from];

                if (distance > 5f * deltaTime)
                    transform.Position += direction * 5f * deltaTime;
                else
                {
                    transform.Position = localTransforms[merge.ValueRO.to].Position;
                    merge.ValueRW.launchLastStageMerge = true;
                }

                localTransforms[merge.ValueRO.from] = transform;
            }
        }

        [BurstCompile]
        public struct SetIndexBetweenFromAndToForMergeSphereJob : IJob
        {
            [NativeDisableUnsafePtrRestriction] public RefRW<MergeSphereComponent> merge;
            [ReadOnly] public BufferLookup<IndexConnectionBuffer> buffers;

            public void Execute()
            {
                if (!buffers.EntityExists(merge.ValueRO.from) || !buffers.EntityExists(merge.ValueRO.to)) return;

                foreach (var indexFrom in buffers[merge.ValueRO.from])
                {
                    foreach (var indexTo in buffers[merge.ValueRO.to])
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

            public void Execute(Entity entity, [EntityIndexInQuery] int sortKey, in IndexConnectComponent indexConnect)
            {
                if (indexConnect.value == merge.ValueRO.indexBetweenFromAndTo)
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
                    if (buffer[i].value != merge.ValueRO.indexBetweenFromAndTo) continue;

                    buffer.RemoveAt(i);
                }
            }
        }

        [BurstCompile]
        public partial struct ReconnectJointJob : IJobEntity
        {
            [NativeDisableUnsafePtrRestriction] public RefRW<MergeSphereComponent> merge;

            public EntityCommandBuffer ecb;
            [ReadOnly] public ComponentLookup<LocalTransform> localTransforms;
            public BufferLookup<IndexConnectionBuffer> buffers;

            public void Execute(Entity entity, in PhysicsConstrainedBodyPair bodyPair,
                in IndexConnectComponent indexConnect)
            {
                if(merge.ValueRO.indexBetweenFromAndTo == indexConnect.value) return;
                
                if (merge.ValueRO.from != bodyPair.EntityA && merge.ValueRO.from != bodyPair.EntityB) return;

                var element = merge.ValueRO.from != bodyPair.EntityA ? bodyPair.EntityA : bodyPair.EntityB;

                ecb.DestroyEntity(entity);

                StaticMethod.CreateJoint(ecb, merge.ValueRO.to, element, localTransforms[element].Scale / 1.5f,
                    indexConnect.value);

                buffers[merge.ValueRO.to].Add(new IndexConnectionBuffer{value = indexConnect.value});
            }
        }

        [BurstCompile]
        public partial struct RemoveSphereJob : IJobEntity
        {
            [NativeDisableUnsafePtrRestriction] public RefRW<MergeSphereComponent> merge;

            public EntityCommandBuffer ecb;

            public void Execute(LevelSettingAspect levelSettingAspect)
            {
                levelSettingAspect.AddSphereInBuffer(ecb, merge.ValueRO.from);
                merge.ValueRW.from = Entity.Null;

                merge.ValueRW.launchLastStageMerge = false;
            }
        }
    }
}
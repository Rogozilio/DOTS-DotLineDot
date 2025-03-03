using Aspects;
using Components;
using Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace Systems
{
    [UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
    public partial struct GravityInSphereSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LevelSettingComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var data = SystemAPI.GetSingleton<LevelSettingComponent>();
            var sphereForTargetGravity = SystemAPI.GetSingletonRW<SphereForTargetGravityComponent>();

            state.Dependency = new SetSphereForTargetGravityJob
            {
                sphereForTargetGravity = sphereForTargetGravity
            }.Schedule(state.Dependency);
            state.Dependency = new DisableTargetGravityComponentJob().Schedule(state.Dependency);
            state.Dependency = new SetTargetGravityJob
            {
                sphereForTargetGravity = sphereForTargetGravity,
                localToWorlds = SystemAPI.GetComponentLookup<LocalToWorld>(true),
                targetGravity = SystemAPI.GetComponentLookup<TargetGravityComponent>()
            }.Schedule(state.Dependency);
            state.Dependency = new GravityInSphereJob
            {
                speed = data.speedGravityInSphere,
                sphereForTargetGravity = sphereForTargetGravity
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(IsMouseMove))]
        public partial struct SetSphereForTargetGravityJob : IJobEntity
        {
            [NativeDisableUnsafePtrRestriction] public RefRW<SphereForTargetGravityComponent> sphereForTargetGravity;

            public void Execute(Entity entity)
            {
                sphereForTargetGravity.ValueRW.sphere = entity;
            }
        }

        [BurstCompile]
        public partial struct DisableTargetGravityComponentJob : IJobEntity
        {
            public void Execute(EnabledRefRW<TargetGravityComponent> enableTargetGravity)
            {
                enableTargetGravity.ValueRW = false;
            }
        }

        [BurstCompile]
        public partial struct SetTargetGravityJob : IJobEntity
        {
            [NativeDisableUnsafePtrRestriction] public RefRW<SphereForTargetGravityComponent> sphereForTargetGravity;
            [ReadOnly] public ComponentLookup<LocalToWorld> localToWorlds;
            public ComponentLookup<TargetGravityComponent> targetGravity;

            public void Execute(PhysicsConstrainedBodyPair bodyPair)
            {
                if (bodyPair.EntityA == Entity.Null || bodyPair.EntityB == Entity.Null) return;

                if (bodyPair.EntityA == sphereForTargetGravity.ValueRO.sphere 
                    && targetGravity.HasComponent(bodyPair.EntityB))
                {
                    var targetGravityComponent = new TargetGravityComponent()
                    {
                        target = bodyPair.EntityA,
                        position = localToWorlds[bodyPair.EntityA].Position,
                        distance = math.distance(localToWorlds[bodyPair.EntityA].Position,
                            localToWorlds[bodyPair.EntityB].Position)
                    };
                    targetGravity[bodyPair.EntityB] = targetGravityComponent;
                    targetGravity.SetComponentEnabled(bodyPair.EntityB, true);
                }
                else if (bodyPair.EntityB == sphereForTargetGravity.ValueRO.sphere
                         && targetGravity.HasComponent(bodyPair.EntityA))
                {
                    var targetGravityComponent = new TargetGravityComponent
                    {
                        target = bodyPair.EntityB,
                        position = localToWorlds[bodyPair.EntityB].Position,
                        distance = math.distance(localToWorlds[bodyPair.EntityB].Position,
                            localToWorlds[bodyPair.EntityA].Position)
                    };
                    targetGravity[bodyPair.EntityA] = targetGravityComponent;
                    targetGravity.SetComponentEnabled(bodyPair.EntityA, true);
                }
            }
        }

        [BurstCompile]
        [WithAll(typeof(TargetGravityComponent))]
        private partial struct GravityInSphereJob : IJobEntity
        {
            public EntityCommandBuffer ecb;
            public float speed;
            [NativeDisableUnsafePtrRestriction] public RefRW<SphereForTargetGravityComponent> sphereForTargetGravity;

            private void Execute(ElementAspect element)
            {
                if (sphereForTargetGravity.ValueRO.sphere == Entity.Null) return;
                if (element.TargetGravity.target != sphereForTargetGravity.ValueRO.sphere) return;
                
                element.LinearVelocity = element.ToTargetGravity * speed;
                //element.EnableTargetGravity = false;
            }
        }
    }
}
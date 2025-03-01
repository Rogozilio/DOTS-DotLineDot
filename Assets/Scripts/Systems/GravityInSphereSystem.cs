using Aspects;
using Components;
using Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Physics.Systems;

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
        [WithAll(typeof(TargetGravityComponent))]
        private partial struct GravityInSphereJob : IJobEntity
        {
            public EntityCommandBuffer ecb;
            public float speed;
            [NativeDisableUnsafePtrRestriction] public RefRW<SphereForTargetGravityComponent> sphereForTargetGravity;

            private void Execute(ElementAspect element)
            {
                if(sphereForTargetGravity.ValueRO.sphere == Entity.Null) return;
                if (element.TargetGravity.target != sphereForTargetGravity.ValueRO.sphere) return;

                element.LinearVelocity = element.ToTargetGravity * speed;
                element.EnableTargetGravity = false;
            }
        }
    }
}
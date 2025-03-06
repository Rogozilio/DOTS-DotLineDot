using Components;
using Components.DynamicBuffers;
using Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace Systems
{
    [UpdateInGroup(typeof(BeforePhysicsSystemGroup))]
    [UpdateBefore(typeof(MoveMouseSphereSystem))]
    public partial struct SwitchMassSphereSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PullElementBuffer>();
            state.RequireForUpdate<LevelSettingComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var countElementsInPull = SystemAPI.GetSingletonBuffer<PullElementBuffer>(true).Length;
            var countAllElements = SystemAPI.GetSingleton<LevelSettingComponent>().countElement;
            
            state.Dependency = new DynamicMassSphereJob().Schedule(state.Dependency);
            state.Dependency = new KinematicMassSphereJob().Schedule(state.Dependency);
            state.Dependency = new ChangeInertiaElementJob
            {
                countActiveElement = countAllElements - countElementsInPull
            }.Schedule(state.Dependency);
        }
    }

    [BurstCompile]
    [WithAll(typeof(IsMouseMove))]
    [WithOptions(EntityQueryOptions.FilterWriteGroup)]
    partial struct DynamicMassSphereJob : IJobEntity
    {
        private void Execute(ref PhysicsMass mass, in LocalTransform transform)
        {
            mass.InverseMass = transform.Scale;
            mass.InverseInertia = new float3(1000f, 1000f, 1000f);
        }
    }
    
    [BurstCompile]
    [WithDisabled(typeof(IsMouseMove))]
    [WithChangeFilter(typeof(IsMouseMove))]
    partial struct KinematicMassSphereJob : IJobEntity
    {
        private void Execute(ref PhysicsMass mass)
        {
            mass.InverseMass = 0f;
            mass.InverseInertia = float3.zero;
        }
    }

    [BurstCompile]
    [WithPresent(typeof(TargetGravityComponent))]
    public partial struct ChangeInertiaElementJob : IJobEntity
    {
        [ReadOnly] public int countActiveElement;
        public void Execute(ref PhysicsMass mass)
        {
            mass.InverseMass = 0.2f / countActiveElement;//0.01f;
            mass.InverseInertia = new float3(1000f, 1000f, 1000f);
        }
    }
}
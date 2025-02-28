using Tags;
using Unity.Burst;
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
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new DynamicMassSphereJob().Schedule(state.Dependency);
            state.Dependency = new KinematicMassSphereJob().Schedule(state.Dependency);
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
        }
    }
    
    [BurstCompile]
    [WithDisabled(typeof(IsMouseMove))]
    partial struct KinematicMassSphereJob : IJobEntity
    {
        private void Execute(Entity e, ref PhysicsMass mass, ref PhysicsVelocity velocity, in IsMouseMove isMouseMove)
        {
            velocity.Linear = float3.zero;
            velocity.Angular = float3.zero;
            mass.InverseMass = 0;
        }
    }
}
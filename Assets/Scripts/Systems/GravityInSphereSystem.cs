using Aspects;
using Components;
using Tags;
using Unity.Burst;
using Unity.Collections;
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

            state.Dependency = new GravityInSphereJob
            {
                speed = data.speedGravityInSphere,
                isMoveMouse = SystemAPI.GetComponentLookup<IsMouseMove>(true)
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(TargetGravityComponent))]
        private partial struct GravityInSphereJob : IJobEntity
        {
            public EntityCommandBuffer ecb;
            public float speed;
            [ReadOnly] public ComponentLookup<IsMouseMove> isMoveMouse;

            private void Execute(ElementAspect element)
            {
                if(element.TargetGravity.target == Entity.Null) return;
                
                if(!isMoveMouse.IsComponentEnabled(element.TargetGravity.target)) return;
                
                element.LinearVelocity = element.ToTargetGravity * speed;
                element.EnableTargetGravity = false;
            }
        }
    }
}
using Aspects;
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
                isMouseMoves = SystemAPI.GetComponentLookup<IsMouseMove>(true)
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        private partial struct GravityInSphereJob : IJobEntity
        {
            public float speed;
            [ReadOnly] public ComponentLookup<IsMouseMove> isMouseMoves;

            private void Execute(ElementAspect element)
            {
                //TODO: Изменить притяжку к сфере, но позже     
                if(element.TargetGravity.target == Entity.Null) return;
                if(isMouseMoves.GetEnabledRefRO<IsMouseMove>(element.TargetGravity.target).ValueRO) return;
                
                element.LinearVelocity = element.ToTargetGravity * speed;
                element.EnableTargetGravity = false;
            }
        }
    }
}
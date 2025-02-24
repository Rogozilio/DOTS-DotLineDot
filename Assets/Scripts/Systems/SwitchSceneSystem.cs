using Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;

namespace Systems
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial struct SwitchSceneSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LoadLevelComponent>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            var loadLevel = SystemAPI.GetSingleton<LoadLevelComponent>();
            var entityLoadLevel = SystemAPI.GetSingletonEntity<LoadLevelComponent>();

            var currentScene = SystemAPI.GetSingleton<CurrentSceneComponent>();
            var entityCurrentScene = SystemAPI.GetSingletonEntity<CurrentSceneComponent>();

            var entity = SceneSystem.LoadSceneAsync(state.WorldUnmanaged, loadLevel.data.sceneReference);
            if (currentScene.entity != Entity.Null)
                SceneSystem.UnloadScene(state.WorldUnmanaged, currentScene.entity);
            currentScene.entity = entity;
            currentScene.data = loadLevel.data;

            ecb.SetComponent(entityCurrentScene, currentScene);
            ecb.RemoveComponent<LoadLevelComponent>(entityLoadLevel);
        }
    }
}
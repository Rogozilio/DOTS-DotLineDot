using Unity.Entities;
using UnityEngine;

public struct LevelSettingComponent : IComponentData
{
    public byte indexConnection;
    public byte indexShared;
    
    public int countElement;
    public int countSphere;
    public Entity prefabSphere;
    public Entity prefabElement;
    public float speedMoveSphere;
    public float speedGravityInSphere;
    public float distanceRemove;
    public float distanceSpawn;
    public float distanceBetweenElements;
}
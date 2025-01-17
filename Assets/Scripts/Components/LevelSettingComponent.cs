using Unity.Entities;
using UnityEngine;

public struct LevelSettingComponent : IComponentData
{
    public byte indexConnection;
    public byte countElements;
    public int countSphere;
    public Entity prefabSphere;
    public Entity prefabElement;
    public float speedMoveSphere;
    public float speedGravityInSphere;
}
using Unity.Entities;
using UnityEngine;

public struct MultiSphereComponent : IComponentData
{
    [HideInInspector]public byte indexConnection;
    public byte countElements;
    public Entity prefabSphere;
    public Entity prefabElement;
    public float speedMoveSphere;
    public float speedGravityInSphere;
}
using Unity.Entities;

public struct MultiSphereComponent : IComponentData
{
    public byte countElements;
    public Entity prefabSphere;
    public Entity prefabElement;
    public float speedMoveSphere;
    public float speedGravityInSphere;
}
using Unity.Entities;

public struct MultiSphereComponent : IComponentData
{
    public byte countElements;
    public Entity prefabElement;
    public Entity startSphere;
    public Entity endSphere;
    public float speedMoveSphere;
    public float speedGravityInSphere;
}
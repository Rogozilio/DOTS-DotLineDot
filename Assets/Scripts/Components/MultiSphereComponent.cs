using Unity.Entities;

public struct MultiSphereComponent : IComponentData
{
    public byte countElements;
    public Entity prefabElement;
    public Entity endSphere;
}
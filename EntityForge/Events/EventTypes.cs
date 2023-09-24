namespace EntityForge.Events
{
    public delegate void EntityEvent(EntityId entityId);
    public delegate void ComponentEvent(EntityId entityId, int componentId);
    public delegate void TagEvent(EntityId entityId, int tagId);
}

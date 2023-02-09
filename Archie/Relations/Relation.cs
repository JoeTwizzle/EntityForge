namespace Archie.Relations
{
    internal struct Relation<T> : IComponent<T> where T : struct, IComponent<T>
    {
        public T RelationType;
        public int TargetEntity;
    }
}

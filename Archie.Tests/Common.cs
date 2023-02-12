namespace Archie.Tests
{
    struct Rel1 : IRelation<Rel1>
    {
        public int dataVal;

        public Rel1(int dataVal)
        {
            this.dataVal = dataVal;
        }

        public static RelationKind RelationKind => RelationKind.SingleSingle;
    }

    struct Component1 : IComponent<Component1>
    {
        public int Value;
    }

    public struct Component2 : IComponent<Component2>
    {
        public int Value;
    }

    public struct Component3 : IComponent<Component3>
    {
        public int Value;
    }

    struct ExampleComponent : IComponent<ExampleComponent>
    {
        public int Number;
    }

    struct ExampleTransform : IComponent<ExampleTransform>
    {
        public float X, Y, Z;

        public ExampleTransform(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}

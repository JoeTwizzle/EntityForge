namespace EntityForge.Tests
{
    //struct RelSS : ITreeRelation<RelSS>
    //{
    //    public int dataVal;

    //    public RelSS(int dataVal)
    //    {
    //        this.dataVal = dataVal;
    //    }

    //    public static RelationKind RelationKind => RelationKind.SingleSingle;
    //}

    //struct RelDSS : ITreeRelation<RelDSS>
    //{
    //    public int dataVal;

    //    public RelDSS(int dataVal)
    //    {
    //        this.dataVal = dataVal;
    //    }

    //    public static RelationKind RelationKind => RelationKind.Discriminated;
    //}

    //struct RelSM : ITreeRelation<RelSM>
    //{
    //    public int dataVal;

    //    public RelSM(int dataVal)
    //    {
    //        this.dataVal = dataVal;
    //    }

    //    public static RelationKind RelationKind => RelationKind.SingleMulti;
    //}

    //struct RelMM : ITreeRelation<RelMM>
    //{
    //    public int dataVal;

    //    public RelMM(int dataVal)
    //    {
    //        this.dataVal = dataVal;
    //    }

    //    public static RelationKind RelationKind => RelationKind.MultiMulti;
    //}

    struct Component1 : IComponent
    {
        public int Value;
    }

    public struct Component2 : IComponent
    {
        public int Value;
    }

    public struct Component3 : IComponent
    {
        public int Value;
    }

    struct ExampleComponent : IComponent
    {
        public int Number;
    }

    struct ExampleTransform : IComponent
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

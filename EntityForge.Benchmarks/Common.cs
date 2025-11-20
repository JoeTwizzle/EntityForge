namespace EntityForge.Benchmarks
{
    struct Component0 : IComponent;
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

    public struct Velocity2 : IComponent
    {
        public float X, Y;
    }

    public struct Velocity3 : IComponent
    {
        public float X, Y, Z;
    }

    public struct Rotation : IComponent
    {
        public float X, Y, Z, W;
    }

    public struct Position2 : IComponent
    {
        public float X, Y;
    }

    public struct Position3 : IComponent
    {
        public float X, Y, Z;
    }

    public struct Health : IComponent
    {
        public int Amount;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Archie.Benchmarks
{
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

    public struct Velocity2 : IComponent<Velocity2>
    {
        public float X, Y;
    }

    public struct Velocity3 : IComponent<Velocity3>
    {
        public float X, Y, Z;
    }

    public struct Rotation : IComponent<Rotation>
    {
        public float X, Y, Z, W;
    }

    public struct Position2 : IComponent<Position2>
    {
        public float X, Y;
    }

    public struct Position3 : IComponent<Position3>
    {
        public float X, Y, Z;
    }

    public struct Health : IComponent<Health>
    {
        public int Amount;
    }
}

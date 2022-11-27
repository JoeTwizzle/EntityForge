using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Archie.Tests
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

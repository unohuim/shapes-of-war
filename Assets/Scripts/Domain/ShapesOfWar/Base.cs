using System;

namespace ShapesOfWar.Domain
{
    public sealed class Base
    {
        public Base(BaseType type, int points)
        {
            if (points < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(points), "Base points cannot be negative.");
            }

            Type = type;
            Points = points;
        }

        public BaseType Type { get; }

        public int Points { get; }
    }
}


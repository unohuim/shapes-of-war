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

        public BaseType Type { get; private set; }

        public int Points { get; private set; }

        internal void UpgradeTo(BaseType type, int points)
        {
            if (points < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(points), "Base points cannot be negative.");
            }

            Type = type;
            Points = points;
        }
    }
}

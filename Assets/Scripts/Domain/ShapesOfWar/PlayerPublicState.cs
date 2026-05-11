using System.Collections.Generic;

namespace ShapesOfWar.Domain
{
    public sealed class PlayerPublicState
    {
        public PlayerPublicState(
            int index,
            string name,
            BaseType baseType,
            int basePoints,
            IReadOnlyDictionary<UnitShape, int> unitCounts,
            IReadOnlyDictionary<ResourceType, int> resourceCounts,
            int actionCardCount,
            bool isEliminated)
        {
            Index = index;
            Name = name;
            BaseType = baseType;
            BasePoints = basePoints;
            UnitCounts = unitCounts;
            ResourceCounts = resourceCounts;
            ActionCardCount = actionCardCount;
            IsEliminated = isEliminated;
        }

        public int Index { get; }

        public string Name { get; }

        public BaseType BaseType { get; }

        public int BasePoints { get; }

        public IReadOnlyDictionary<UnitShape, int> UnitCounts { get; }

        public IReadOnlyDictionary<ResourceType, int> ResourceCounts { get; }

        public int ActionCardCount { get; }

        public bool IsEliminated { get; }
    }
}


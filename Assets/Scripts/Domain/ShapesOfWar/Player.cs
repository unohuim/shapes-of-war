#nullable enable

using System;
using System.Collections.Generic;

namespace ShapesOfWar.Domain
{
    public sealed class Player
    {
        public Player(
            int index,
            string name,
            Base playerBase,
            EnumCountSet<UnitShape>? unitCounts = null,
            EnumCountSet<ResourceType>? resourceCounts = null,
            ActionCardHand? actionCards = null,
            bool isEliminated = false)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Player index cannot be negative.");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Player name is required.", nameof(name));
            }

            Index = index;
            Name = name;
            Base = playerBase ?? throw new ArgumentNullException(nameof(playerBase));
            UnitCounts = unitCounts ?? new EnumCountSet<UnitShape>();
            ResourceCounts = resourceCounts ?? new EnumCountSet<ResourceType>();
            ActionCards = actionCards ?? new ActionCardHand();
            IsEliminated = isEliminated;
        }

        public int Index { get; }

        public string Name { get; }

        public Base Base { get; }

        public EnumCountSet<UnitShape> UnitCounts { get; }

        public EnumCountSet<ResourceType> ResourceCounts { get; }

        public int ActionCardCount => ActionCards.Count;

        public bool IsEliminated { get; private set; }

        internal ActionCardHand ActionCards { get; }

        internal void AddUnit(UnitShape unitShape, int count)
        {
            UnitCounts.Add(unitShape, count);
        }

        internal bool TrySpendUnit(UnitShape unitShape, int count)
        {
            return UnitCounts.TrySpend(unitShape, count);
        }

        internal void AddResource(ResourceType resourceType, int count)
        {
            ResourceCounts.Add(resourceType, count);
        }

        internal bool TrySpendResource(ResourceType resourceType, int count)
        {
            return ResourceCounts.TrySpend(resourceType, count);
        }

        internal void AddActionCard(ActionCardType actionCard)
        {
            ActionCards.Add(actionCard);
        }

        internal IReadOnlyList<ActionCardType> EliminateAndDiscardHoldings()
        {
            IsEliminated = true;
            UnitCounts.Clear();
            ResourceCounts.Clear();
            return ActionCards.DiscardAll();
        }

        public PlayerPublicState ToPublicState()
        {
            return new PlayerPublicState(
                Index,
                Name,
                Base.Type,
                Base.Points,
                UnitCounts.AsReadOnlyDictionary(),
                ResourceCounts.AsReadOnlyDictionary(),
                ActionCardCount,
                IsEliminated);
        }
    }
}

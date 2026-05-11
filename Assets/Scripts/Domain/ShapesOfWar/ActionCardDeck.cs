using System.Collections.Generic;
using System.Linq;

namespace ShapesOfWar.Domain
{
    public sealed class ActionCardDeck
    {
        private readonly List<ActionCardType> _cards;

        public ActionCardDeck(IEnumerable<ActionCardType> cards)
        {
            _cards = cards?.ToList() ?? new List<ActionCardType>();
        }

        public int Count => _cards.Count;

        internal IReadOnlyList<ActionCardType> Cards => _cards;

        public static ActionCardDeck CreateStandard()
        {
            List<ActionCardType> cards = new List<ActionCardType>();
            cards.AddRange(Enumerable.Repeat(ActionCardType.RaidBase, 10));
            cards.AddRange(Enumerable.Repeat(ActionCardType.ResourceTheft, 10));
            cards.AddRange(Enumerable.Repeat(ActionCardType.UnitKill, 10));
            cards.AddRange(Enumerable.Repeat(ActionCardType.Counter, 20));
            return new ActionCardDeck(cards);
        }

        public int CountOf(ActionCardType cardType)
        {
            return _cards.Count(card => card == cardType);
        }
    }
}


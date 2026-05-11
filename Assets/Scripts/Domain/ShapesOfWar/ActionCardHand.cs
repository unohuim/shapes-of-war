#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace ShapesOfWar.Domain
{
    public sealed class ActionCardHand
    {
        private readonly List<ActionCardType> _cards;

        public ActionCardHand(IEnumerable<ActionCardType>? cards = null)
        {
            _cards = cards?.ToList() ?? new List<ActionCardType>();
        }

        public int Count => _cards.Count;

        internal IReadOnlyList<ActionCardType> Cards => _cards;

        internal void Add(ActionCardType card)
        {
            _cards.Add(card);
        }

        internal bool TryRemove(ActionCardType card)
        {
            return _cards.Remove(card);
        }

        internal IReadOnlyList<ActionCardType> DiscardAll()
        {
            List<ActionCardType> discardedCards = _cards.ToList();
            _cards.Clear();
            return discardedCards;
        }
    }
}

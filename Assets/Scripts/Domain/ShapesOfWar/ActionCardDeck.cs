#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace ShapesOfWar.Domain
{
    public sealed class ActionCardDeck
    {
        private readonly List<ActionCardType> _cards;
        private readonly List<ActionCardType> _discardPile;
        private readonly System.Random _random;

        public ActionCardDeck(IEnumerable<ActionCardType> cards, IEnumerable<ActionCardType>? discardPile = null)
        {
            _cards = cards?.ToList() ?? new List<ActionCardType>();
            _discardPile = discardPile?.ToList() ?? new List<ActionCardType>();
            _random = new System.Random();
        }

        public int Count => _cards.Count;

        public int DiscardPileCount => _discardPile.Count;

        public bool CanDraw => _cards.Count > 0 || _discardPile.Count > 0;

        internal IReadOnlyList<ActionCardType> Cards => _cards;

        internal IReadOnlyList<ActionCardType> DiscardPile => _discardPile;

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

        public int CountOfDiscard(ActionCardType cardType)
        {
            return _discardPile.Count(card => card == cardType);
        }

        public void DiscardUsedCard(ActionCardType card)
        {
            _discardPile.Add(card);
        }

        internal bool TryDraw(out ActionCardType card)
        {
            if (_cards.Count == 0)
            {
                RebuildDeckFromDiscardPile();
            }

            if (_cards.Count == 0)
            {
                card = default;
                return false;
            }

            card = _cards[0];
            _cards.RemoveAt(0);
            return true;
        }

        private void RebuildDeckFromDiscardPile()
        {
            if (_discardPile.Count == 0)
            {
                return;
            }

            _cards.AddRange(_discardPile);
            _discardPile.Clear();
            Shuffle(_cards);
        }

        private void Shuffle(IList<ActionCardType> cards)
        {
            for (int index = cards.Count - 1; index > 0; index--)
            {
                int swapIndex = _random.Next(index + 1);
                ActionCardType current = cards[index];
                cards[index] = cards[swapIndex];
                cards[swapIndex] = current;
            }
        }
    }
}

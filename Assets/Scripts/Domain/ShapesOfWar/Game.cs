#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ShapesOfWar.Domain
{
    public sealed class Game
    {
        public const int MinimumPlayerCount = 2;
        public const int MaximumPlayerCount = 4;

        public Game(IEnumerable<Player> players, ActionCardDeck? actionDeck = null)
        {
            if (players == null)
            {
                throw new ArgumentNullException(nameof(players));
            }

            List<Player> playerList = players.ToList();
            ValidatePlayers(playerList);

            Players = new ReadOnlyCollection<Player>(playerList);
            ActionDeck = actionDeck ?? ActionCardDeck.CreateStandard();
        }

        public IReadOnlyList<Player> Players { get; }

        public ActionCardDeck ActionDeck { get; }

        public IReadOnlyList<PlayerPublicState> GetPublicPlayerStates()
        {
            return Players.Select(player => player.ToPublicState()).ToList();
        }

        private static void ValidatePlayers(IReadOnlyCollection<Player> players)
        {
            if (players.Count < MinimumPlayerCount || players.Count > MaximumPlayerCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(players),
                    $"Shapes of War supports {MinimumPlayerCount}-{MaximumPlayerCount} players.");
            }

            if (players.Select(player => player.Index).Distinct().Count() != players.Count)
            {
                throw new ArgumentException("Player indexes must be unique.", nameof(players));
            }

            if (players.Select(player => player.Name).Distinct().Count() != players.Count)
            {
                throw new ArgumentException("Player names must be unique.", nameof(players));
            }
        }
    }
}

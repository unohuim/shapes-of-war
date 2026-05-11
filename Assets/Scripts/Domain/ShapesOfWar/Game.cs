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
        public const int StartingBasePoints = 3;
        public const int StartingSquareCount = 3;

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

        public static Game CreateNew(IEnumerable<string> playerNames)
        {
            if (playerNames == null)
            {
                throw new ArgumentNullException(nameof(playerNames));
            }

            List<Player> players = playerNames
                .Select((name, index) => CreateStartingPlayer(index, name))
                .ToList();

            return new Game(players);
        }

        public IReadOnlyList<PlayerPublicState> GetPublicPlayerStates()
        {
            return Players.Select(player => player.ToPublicState()).ToList();
        }

        public IReadOnlyDictionary<UnitShape, int> CountUnits(int playerIndex)
        {
            return GetPlayer(playerIndex).UnitCounts.AsReadOnlyDictionary();
        }

        public void CollectResources(int playerIndex)
        {
            Player player = GetPlayer(playerIndex);
            player.AddResource(ResourceType.Wood, player.UnitCounts.Get(UnitShape.Triangle));
            player.AddResource(ResourceType.Stone, player.UnitCounts.Get(UnitShape.Square));
            player.AddResource(ResourceType.Metal, player.UnitCounts.Get(UnitShape.Circle));
        }

        public bool TryBuyUnit(int playerIndex, UnitShape unitShape)
        {
            Player player = GetPlayer(playerIndex);
            ResourceType costResource = GetUnitCostResource(unitShape);

            if (!player.TrySpendResource(costResource, 1))
            {
                return false;
            }

            player.AddUnit(unitShape, 1);
            return true;
        }

        public bool TryUpgradeBase(int playerIndex, BaseType targetBaseType)
        {
            Player player = GetPlayer(playerIndex);

            if (player.Base.Type == BaseType.Wood && targetBaseType == BaseType.Stone)
            {
                if (!player.TrySpendResource(ResourceType.Stone, 2))
                {
                    return false;
                }

                player.Base.UpgradeTo(BaseType.Stone, 5);
                return true;
            }

            if (player.Base.Type == BaseType.Stone && targetBaseType == BaseType.Metal)
            {
                if (!player.TrySpendResource(ResourceType.Metal, 2))
                {
                    return false;
                }

                player.Base.UpgradeTo(BaseType.Metal, 7);
                return true;
            }

            return false;
        }

        public bool TrySacrificeUnitForActionCard(int playerIndex, UnitShape unitShape)
        {
            return TrySacrificeUnitToDrawActionCard(playerIndex, unitShape);
        }

        public bool TrySacrificeUnitToDrawActionCard(int playerIndex, UnitShape unitShape)
        {
            Player player = GetPlayer(playerIndex);

            if (player.UnitCounts.Get(unitShape) < 1)
            {
                return false;
            }

            if (!TryDrawActionCard(playerIndex))
            {
                return false;
            }

            return player.TrySpendUnit(unitShape, 1);
        }

        public bool TryDrawActionCard(int playerIndex)
        {
            Player player = GetPlayer(playerIndex);

            if (!ActionDeck.TryDraw(out ActionCardType actionCard))
            {
                return false;
            }

            player.AddActionCard(actionCard);
            return true;
        }

        public void DiscardUsedActionCard(ActionCardType actionCard)
        {
            ActionDeck.DiscardUsedCard(actionCard);
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

        private static Player CreateStartingPlayer(int index, string name)
        {
            return new Player(
                index,
                name,
                new Base(BaseType.Wood, StartingBasePoints),
                new EnumCountSet<UnitShape>(
                    new Dictionary<UnitShape, int>
                    {
                        [UnitShape.Square] = StartingSquareCount
                    }),
                new EnumCountSet<ResourceType>(),
                new ActionCardHand());
        }

        private Player GetPlayer(int playerIndex)
        {
            Player? player = Players.FirstOrDefault(candidate => candidate.Index == playerIndex);
            if (player == null)
            {
                throw new ArgumentOutOfRangeException(nameof(playerIndex), "Unknown player index.");
            }

            return player;
        }

        private static ResourceType GetUnitCostResource(UnitShape unitShape)
        {
            switch (unitShape)
            {
                case UnitShape.Triangle:
                    return ResourceType.Metal;
                case UnitShape.Square:
                    return ResourceType.Stone;
                case UnitShape.Circle:
                    return ResourceType.Wood;
                default:
                    throw new ArgumentOutOfRangeException(nameof(unitShape), unitShape, "Unknown unit shape.");
            }
        }
    }
}

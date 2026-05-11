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

        private readonly Dictionary<int, ActionPhaseChoice> _actionPhaseChoices;
        private PendingAction? _pendingAction;

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
            _actionPhaseChoices = playerList.ToDictionary(player => player.Index, _ => ActionPhaseChoice.None);
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

        public ActionPhaseChoice GetActionPhaseChoice(int playerIndex)
        {
            GetPlayer(playerIndex);
            return _actionPhaseChoices[playerIndex];
        }

        public bool HasPendingAction => _pendingAction != null;

        public bool TryPassActionPhase(int playerIndex)
        {
            GetPlayer(playerIndex);
            return TryChooseActionPhaseOption(playerIndex, ActionPhaseChoice.Pass);
        }

        public bool TryStartBattleRoyaleActionPhase(int playerIndex)
        {
            GetPlayer(playerIndex);
            return TryChooseActionPhaseOption(playerIndex, ActionPhaseChoice.BattleRoyale);
        }

        public bool TryPlayResourceTheft(int playerIndex, int targetPlayerIndex, ResourceType resourceType)
        {
            Player player = GetPlayer(playerIndex);
            Player target = GetTargetPlayer(playerIndex, targetPlayerIndex);

            if (_pendingAction != null ||
                target.ResourceCounts.Get(resourceType) < 1 ||
                !CanChooseActionPhaseOption(playerIndex) ||
                !player.ActionCards.TryRemove(ActionCardType.ResourceTheft))
            {
                return false;
            }

            _actionPhaseChoices[playerIndex] = ActionPhaseChoice.ActionCard;
            _pendingAction = PendingAction.CreateResourceTheft(playerIndex, targetPlayerIndex, resourceType);
            return true;
        }

        public bool TryPlayUnitKill(int playerIndex, int targetPlayerIndex, UnitShape unitShape)
        {
            Player player = GetPlayer(playerIndex);
            Player target = GetTargetPlayer(playerIndex, targetPlayerIndex);

            if (_pendingAction != null ||
                target.UnitCounts.Get(unitShape) < 1 ||
                !CanChooseActionPhaseOption(playerIndex) ||
                !player.ActionCards.TryRemove(ActionCardType.UnitKill))
            {
                return false;
            }

            _actionPhaseChoices[playerIndex] = ActionPhaseChoice.ActionCard;
            _pendingAction = PendingAction.CreateUnitKill(playerIndex, targetPlayerIndex, unitShape);
            return true;
        }

        public bool TryPlayRaidBase(int playerIndex, int targetPlayerIndex, UnitShape raidingUnitShape)
        {
            GetPlayer(playerIndex);
            GetTargetPlayer(playerIndex, targetPlayerIndex);
            _ = raidingUnitShape;
            return false;
        }

        public bool TryPlayCounterAsActionPhaseCard(int playerIndex)
        {
            GetPlayer(playerIndex);
            return false;
        }

        public bool TryRespondWithCounter(int playerIndex)
        {
            Player player = GetPlayer(playerIndex);

            if (_pendingAction == null || !player.ActionCards.TryRemove(ActionCardType.Counter))
            {
                return false;
            }

            _pendingAction.AddCounter();
            return true;
        }

        public bool TryDefendPendingActionWithUnits(int defenderPlayerIndex, UnitShape unitShape, int count)
        {
            GetPlayer(defenderPlayerIndex);
            _ = unitShape;
            _ = count;
            return false;
        }

        public bool ResolvePendingAction()
        {
            if (_pendingAction == null)
            {
                return false;
            }

            PendingAction action = _pendingAction;
            bool actionResolved = false;

            if (action.CounterCount % 2 == 0)
            {
                actionResolved = ResolveUncounteredAction(action);
            }

            DiscardUsedActionCard(action.ActionCard);
            for (int counterIndex = 0; counterIndex < action.CounterCount; counterIndex++)
            {
                DiscardUsedActionCard(ActionCardType.Counter);
            }

            _pendingAction = null;
            return actionResolved;
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

        private Player GetTargetPlayer(int playerIndex, int targetPlayerIndex)
        {
            if (playerIndex == targetPlayerIndex)
            {
                throw new ArgumentException("Target player must be another player.", nameof(targetPlayerIndex));
            }

            return GetPlayer(targetPlayerIndex);
        }

        private bool TryChooseActionPhaseOption(int playerIndex, ActionPhaseChoice choice)
        {
            if (!CanChooseActionPhaseOption(playerIndex))
            {
                return false;
            }

            _actionPhaseChoices[playerIndex] = choice;
            return true;
        }

        private bool CanChooseActionPhaseOption(int playerIndex)
        {
            return _actionPhaseChoices[playerIndex] == ActionPhaseChoice.None && _pendingAction == null;
        }

        private bool ResolveUncounteredAction(PendingAction action)
        {
            Player activePlayer = GetPlayer(action.ActivePlayerIndex);
            Player targetPlayer = GetPlayer(action.TargetPlayerIndex);

            if (action.ActionCard == ActionCardType.ResourceTheft &&
                action.ResourceType.HasValue &&
                targetPlayer.TrySpendResource(action.ResourceType.Value, 1))
            {
                activePlayer.AddResource(action.ResourceType.Value, 1);
                return true;
            }

            if (action.ActionCard == ActionCardType.UnitKill &&
                action.UnitShape.HasValue)
            {
                return targetPlayer.TrySpendUnit(action.UnitShape.Value, 1);
            }

            return false;
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

        private sealed class PendingAction
        {
            private PendingAction(
                ActionCardType actionCard,
                int activePlayerIndex,
                int targetPlayerIndex,
                ResourceType? resourceType,
                UnitShape? unitShape)
            {
                ActionCard = actionCard;
                ActivePlayerIndex = activePlayerIndex;
                TargetPlayerIndex = targetPlayerIndex;
                ResourceType = resourceType;
                UnitShape = unitShape;
            }

            public ActionCardType ActionCard { get; }

            public int ActivePlayerIndex { get; }

            public int TargetPlayerIndex { get; }

            public ResourceType? ResourceType { get; }

            public UnitShape? UnitShape { get; }

            public int CounterCount { get; private set; }

            public static PendingAction CreateResourceTheft(int activePlayerIndex, int targetPlayerIndex, ResourceType resourceType)
            {
                return new PendingAction(
                    ActionCardType.ResourceTheft,
                    activePlayerIndex,
                    targetPlayerIndex,
                    resourceType,
                    null);
            }

            public static PendingAction CreateUnitKill(int activePlayerIndex, int targetPlayerIndex, UnitShape unitShape)
            {
                return new PendingAction(
                    ActionCardType.UnitKill,
                    activePlayerIndex,
                    targetPlayerIndex,
                    null,
                    unitShape);
            }

            public void AddCounter()
            {
                CounterCount++;
            }
        }
    }
}

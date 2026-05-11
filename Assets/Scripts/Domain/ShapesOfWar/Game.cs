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
        private BattleRoyaleState? _pendingBattleRoyale;

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

        public bool IsGameOver => ActivePlayers.Count() == 1;

        public int? WinningPlayerIndex => IsGameOver ? ActivePlayers.Single().Index : (int?)null;

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
            Player player = GetActivePlayer(playerIndex);
            player.AddResource(ResourceType.Wood, player.UnitCounts.Get(UnitShape.Triangle));
            player.AddResource(ResourceType.Stone, player.UnitCounts.Get(UnitShape.Square));
            player.AddResource(ResourceType.Metal, player.UnitCounts.Get(UnitShape.Circle));
        }

        public bool TryBuyUnit(int playerIndex, UnitShape unitShape)
        {
            Player player = GetActivePlayer(playerIndex);
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
            Player player = GetActivePlayer(playerIndex);

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
            Player player = GetActivePlayer(playerIndex);

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
            Player player = GetActivePlayer(playerIndex);
            return TryDrawActionCardForPlayer(player);
        }

        private bool TryDrawActionCardForPlayer(Player player)
        {
            if (player.IsEliminated)
            {
                return false;
            }

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

        public bool HasPendingBattleRoyale => _pendingBattleRoyale != null;

        public int? BattleRoyaleCurrentWinningPlayerIndex => _pendingBattleRoyale?.CurrentWinningPlayerIndex;

        public UnitShape? BattleRoyaleCurrentWinningShape => _pendingBattleRoyale?.CurrentWinningShape;

        public int? BattleRoyaleCurrentWinningCount => _pendingBattleRoyale?.CurrentWinningCount;

        public bool TryPassActionPhase(int playerIndex)
        {
            GetActivePlayer(playerIndex);
            return TryChooseActionPhaseOption(playerIndex, ActionPhaseChoice.Pass);
        }

        public bool TryStartBattleRoyaleActionPhase(int playerIndex)
        {
            GetActivePlayer(playerIndex);
            return false;
        }

        public bool TryStartBattleRoyale(int playerIndex, UnitShape unitShape)
        {
            Player player = GetActivePlayer(playerIndex);

            if (!CanChooseActionPhaseOption(playerIndex) ||
                player.UnitCounts.Get(unitShape) < 1 ||
                !player.TrySpendUnit(unitShape, 1))
            {
                return false;
            }

            _actionPhaseChoices[playerIndex] = ActionPhaseChoice.BattleRoyale;
            _pendingBattleRoyale = BattleRoyaleState.Start(ActivePlayers.Select(player => player.Index), playerIndex, unitShape);
            return true;
        }

        public bool TryPlayBattleRoyaleUnits(int playerIndex, UnitShape unitShape, int count)
        {
            Player player = GetActivePlayer(playerIndex);

            if (_pendingBattleRoyale == null ||
                !_pendingBattleRoyale.CanPlayerAct(playerIndex) ||
                count < 1 ||
                !DoesPlayBeatCurrent(unitShape, count, _pendingBattleRoyale.CurrentWinningShape, _pendingBattleRoyale.CurrentWinningCount) ||
                player.UnitCounts.Get(unitShape) < count ||
                !player.TrySpendUnit(unitShape, count))
            {
                return false;
            }

            _pendingBattleRoyale.CommitWinningPlay(playerIndex, unitShape, count);
            ResolveBattleRoyaleIfComplete();
            return true;
        }

        public bool TryPlayBattleRoyaleUnits(int playerIndex, IReadOnlyDictionary<UnitShape, int> unitCounts)
        {
            if (unitCounts == null)
            {
                throw new ArgumentNullException(nameof(unitCounts));
            }

            List<KeyValuePair<UnitShape, int>> committedShapes = unitCounts
                .Where(unitCount => unitCount.Value > 0)
                .ToList();

            if (committedShapes.Count != 1)
            {
                return false;
            }

            return TryPlayBattleRoyaleUnits(playerIndex, committedShapes[0].Key, committedShapes[0].Value);
        }

        public bool TryPassBattleRoyale(int playerIndex)
        {
            GetActivePlayer(playerIndex);

            if (_pendingBattleRoyale == null || !_pendingBattleRoyale.CanPlayerAct(playerIndex))
            {
                return false;
            }

            _pendingBattleRoyale.Pass(playerIndex);
            ResolveBattleRoyaleIfComplete();
            return true;
        }

        public bool TryPlayResourceTheft(int playerIndex, int targetPlayerIndex, ResourceType resourceType)
        {
            Player player = GetActivePlayer(playerIndex);
            Player target = GetTargetActivePlayer(playerIndex, targetPlayerIndex);

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
            Player player = GetActivePlayer(playerIndex);
            Player target = GetTargetActivePlayer(playerIndex, targetPlayerIndex);

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
            Player player = GetActivePlayer(playerIndex);
            GetTargetActivePlayer(playerIndex, targetPlayerIndex);

            if (_pendingAction != null ||
                player.UnitCounts.Get(raidingUnitShape) < 1 ||
                !CanChooseActionPhaseOption(playerIndex) ||
                !player.ActionCards.TryRemove(ActionCardType.RaidBase))
            {
                return false;
            }

            _actionPhaseChoices[playerIndex] = ActionPhaseChoice.ActionCard;
            _pendingAction = PendingAction.CreateRaidBase(playerIndex, targetPlayerIndex, raidingUnitShape);
            return true;
        }

        public bool TryPlayCounterAsActionPhaseCard(int playerIndex)
        {
            GetActivePlayer(playerIndex);
            return false;
        }

        public bool TryRespondWithCounter(int playerIndex)
        {
            Player player = GetActivePlayer(playerIndex);

            if (_pendingAction == null || !player.ActionCards.TryRemove(ActionCardType.Counter))
            {
                return false;
            }

            _pendingAction.AddCounter();
            return true;
        }

        public bool TryDefendPendingActionWithUnits(int defenderPlayerIndex, UnitShape unitShape, int count)
        {
            Player defender = GetActivePlayer(defenderPlayerIndex);

            if (_pendingAction == null ||
                _pendingAction.ActionCard != ActionCardType.RaidBase ||
                _pendingAction.TargetPlayerIndex != defenderPlayerIndex ||
                !_pendingAction.RaidingUnitShape.HasValue ||
                _pendingAction.HasDefense ||
                _pendingAction.CounterCount % 2 != 0 ||
                !TryGetMinimumDefenderCount(_pendingAction.RaidingUnitShape.Value, unitShape, out int requiredCount) ||
                count != requiredCount ||
                defender.UnitCounts.Get(unitShape) < count)
            {
                return false;
            }

            _pendingAction.AddDefense(unitShape, count);
            return true;
        }

        public bool ResolvePendingAction()
        {
            if (_pendingAction == null)
            {
                return false;
            }

            PendingAction action = _pendingAction;
            bool actionResolved = false;

            if (action.ActionCard == ActionCardType.RaidBase)
            {
                actionResolved = ResolveRaidBase(action, action.CounterCount % 2 == 0);
            }
            else if (action.CounterCount % 2 == 0)
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

        private IEnumerable<Player> ActivePlayers => Players.Where(player => !player.IsEliminated);

        private Player GetActivePlayer(int playerIndex)
        {
            Player player = GetPlayer(playerIndex);
            if (player.IsEliminated)
            {
                throw new InvalidOperationException("Eliminated players cannot act.");
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

        private Player GetTargetActivePlayer(int playerIndex, int targetPlayerIndex)
        {
            Player targetPlayer = GetTargetPlayer(playerIndex, targetPlayerIndex);
            if (targetPlayer.IsEliminated)
            {
                throw new InvalidOperationException("Eliminated players cannot be targeted.");
            }

            return targetPlayer;
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
            return _actionPhaseChoices[playerIndex] == ActionPhaseChoice.None &&
                _pendingAction == null &&
                _pendingBattleRoyale == null;
        }

        private void ResolveBattleRoyaleIfComplete()
        {
            if (_pendingBattleRoyale == null || !_pendingBattleRoyale.IsComplete)
            {
                return;
            }

            Player winner = GetPlayer(_pendingBattleRoyale.CurrentWinningPlayerIndex);
            winner.AddUnit(_pendingBattleRoyale.CurrentWinningShape, 1);
            TryDrawActionCard(winner.Index);
            _pendingBattleRoyale = null;
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

        private bool ResolveRaidBase(PendingAction action, bool countersAllowRaid)
        {
            Player activePlayer = GetPlayer(action.ActivePlayerIndex);
            Player targetPlayer = GetPlayer(action.TargetPlayerIndex);

            if (!action.RaidingUnitShape.HasValue ||
                !activePlayer.TrySpendUnit(action.RaidingUnitShape.Value, 1))
            {
                return false;
            }

            if (!countersAllowRaid)
            {
                return false;
            }

            if (action.DefendingUnitShape.HasValue && action.DefendingUnitCount.HasValue)
            {
                targetPlayer.TrySpendUnit(action.DefendingUnitShape.Value, action.DefendingUnitCount.Value);
                return false;
            }

            targetPlayer.Base.LosePoints(1);
            if (targetPlayer.Base.Points == 0)
            {
                EliminatePlayer(targetPlayer, activePlayer);
            }

            return true;
        }

        private void EliminatePlayer(Player eliminatedPlayer, Player eliminatingPlayer)
        {
            if (eliminatedPlayer.IsEliminated)
            {
                return;
            }

            IReadOnlyList<ActionCardType> discardedCards = eliminatedPlayer.EliminateAndDiscardHoldings();
            foreach (ActionCardType actionCard in discardedCards)
            {
                DiscardUsedActionCard(actionCard);
            }

            TryDrawActionCardForPlayer(eliminatingPlayer);
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

        private static bool TryGetMinimumDefenderCount(UnitShape raidingUnitShape, UnitShape defendingUnitShape, out int count)
        {
            count = 0;

            if (raidingUnitShape == UnitShape.Triangle)
            {
                if (defendingUnitShape == UnitShape.Square)
                {
                    count = 2;
                    return true;
                }

                if (defendingUnitShape == UnitShape.Circle)
                {
                    count = 3;
                    return true;
                }
            }

            if (raidingUnitShape == UnitShape.Square)
            {
                if (defendingUnitShape == UnitShape.Triangle)
                {
                    count = 1;
                    return true;
                }

                if (defendingUnitShape == UnitShape.Circle)
                {
                    count = 2;
                    return true;
                }
            }

            if (raidingUnitShape == UnitShape.Circle)
            {
                if (defendingUnitShape == UnitShape.Triangle || defendingUnitShape == UnitShape.Square)
                {
                    count = 1;
                    return true;
                }
            }

            return false;
        }

        private static bool DoesPlayBeatCurrent(
            UnitShape playShape,
            int playCount,
            UnitShape currentShape,
            int currentCount)
        {
            if (playShape == currentShape)
            {
                return false;
            }

            if (playShape == UnitShape.Triangle && playCount == 1)
            {
                return currentShape == UnitShape.Square && currentCount == 1 ||
                    currentShape == UnitShape.Circle && currentCount == 1;
            }

            if (playShape == UnitShape.Square)
            {
                return playCount == 2 && currentShape == UnitShape.Triangle && currentCount == 1 ||
                    playCount == 1 && currentShape == UnitShape.Circle && currentCount == 1;
            }

            if (playShape == UnitShape.Circle)
            {
                return playCount == 2 && currentShape == UnitShape.Square && currentCount == 1 ||
                    playCount == 3 && currentShape == UnitShape.Triangle && currentCount == 1;
            }

            return false;
        }

        private sealed class PendingAction
        {
            private PendingAction(
                ActionCardType actionCard,
                int activePlayerIndex,
                int targetPlayerIndex,
                ResourceType? resourceType,
                UnitShape? unitShape,
                UnitShape? raidingUnitShape)
            {
                ActionCard = actionCard;
                ActivePlayerIndex = activePlayerIndex;
                TargetPlayerIndex = targetPlayerIndex;
                ResourceType = resourceType;
                UnitShape = unitShape;
                RaidingUnitShape = raidingUnitShape;
            }

            public ActionCardType ActionCard { get; }

            public int ActivePlayerIndex { get; }

            public int TargetPlayerIndex { get; }

            public ResourceType? ResourceType { get; }

            public UnitShape? UnitShape { get; }

            public UnitShape? RaidingUnitShape { get; }

            public UnitShape? DefendingUnitShape { get; private set; }

            public int? DefendingUnitCount { get; private set; }

            public bool HasDefense => DefendingUnitShape.HasValue && DefendingUnitCount.HasValue;

            public int CounterCount { get; private set; }

            public static PendingAction CreateResourceTheft(int activePlayerIndex, int targetPlayerIndex, ResourceType resourceType)
            {
                return new PendingAction(
                    ActionCardType.ResourceTheft,
                    activePlayerIndex,
                    targetPlayerIndex,
                    resourceType,
                    null,
                    null);
            }

            public static PendingAction CreateUnitKill(int activePlayerIndex, int targetPlayerIndex, UnitShape unitShape)
            {
                return new PendingAction(
                    ActionCardType.UnitKill,
                    activePlayerIndex,
                    targetPlayerIndex,
                    null,
                    unitShape,
                    null);
            }

            public static PendingAction CreateRaidBase(int activePlayerIndex, int targetPlayerIndex, UnitShape raidingUnitShape)
            {
                return new PendingAction(
                    ActionCardType.RaidBase,
                    activePlayerIndex,
                    targetPlayerIndex,
                    null,
                    null,
                    raidingUnitShape);
            }

            public void AddCounter()
            {
                CounterCount++;
            }

            public void AddDefense(UnitShape unitShape, int count)
            {
                DefendingUnitShape = unitShape;
                DefendingUnitCount = count;
            }
        }

        private sealed class BattleRoyaleState
        {
            private readonly List<int> _playerOrder;
            private readonly HashSet<int> _activePlayerIndexes;

            private BattleRoyaleState(IEnumerable<int> playerOrder, int starterPlayerIndex, UnitShape startingShape)
            {
                _playerOrder = playerOrder.ToList();
                CurrentWinningPlayerIndex = starterPlayerIndex;
                CurrentWinningShape = startingShape;
                CurrentWinningCount = 1;
                _activePlayerIndexes = _playerOrder
                    .Where(playerIndex => playerIndex != starterPlayerIndex)
                    .ToHashSet();
                CurrentActingPlayerIndex = GetNextActivePlayerAfter(starterPlayerIndex);
            }

            public int CurrentWinningPlayerIndex { get; private set; }

            public UnitShape CurrentWinningShape { get; private set; }

            public int CurrentWinningCount { get; private set; }

            public int? CurrentActingPlayerIndex { get; private set; }

            public bool IsComplete => _activePlayerIndexes.Count == 0;

            public static BattleRoyaleState Start(IEnumerable<int> playerOrder, int starterPlayerIndex, UnitShape startingShape)
            {
                return new BattleRoyaleState(playerOrder, starterPlayerIndex, startingShape);
            }

            public bool CanPlayerAct(int playerIndex)
            {
                return CurrentActingPlayerIndex == playerIndex &&
                    _activePlayerIndexes.Contains(playerIndex);
            }

            public void CommitWinningPlay(int playerIndex, UnitShape unitShape, int count)
            {
                int previousWinner = CurrentWinningPlayerIndex;
                CurrentWinningPlayerIndex = playerIndex;
                CurrentWinningShape = unitShape;
                CurrentWinningCount = count;
                _activePlayerIndexes.Add(previousWinner);
                _activePlayerIndexes.Remove(playerIndex);
                CurrentActingPlayerIndex = GetNextActivePlayerAfter(playerIndex);
            }

            public void Pass(int playerIndex)
            {
                _activePlayerIndexes.Remove(playerIndex);
                CurrentActingPlayerIndex = GetNextActivePlayerAfter(playerIndex);
            }

            private int? GetNextActivePlayerAfter(int playerIndex)
            {
                if (_activePlayerIndexes.Count == 0)
                {
                    return null;
                }

                int currentIndex = _playerOrder.IndexOf(playerIndex);
                for (int offset = 1; offset <= _playerOrder.Count; offset++)
                {
                    int candidate = _playerOrder[(currentIndex + offset) % _playerOrder.Count];
                    if (_activePlayerIndexes.Contains(candidate))
                    {
                        return candidate;
                    }
                }

                return null;
            }
        }
    }
}

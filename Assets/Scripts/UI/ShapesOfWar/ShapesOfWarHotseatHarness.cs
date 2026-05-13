#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using ShapesOfWar.Domain;
using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ShapesOfWar.UI
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class ShapesOfWarHotseatHarness : MonoBehaviour
    {
        private const int DefaultPlayerCount = 2;

        private const string LayoutAssetPath = "Assets/UI/ShapesOfWar/ShapesOfWarHotseatHarness.uxml";
        private const string StyleSheetAssetPath = "Assets/UI/ShapesOfWar/ShapesOfWarHotseatHarness.uss";
        private const string PanelSettingsAssetPath = "Assets/UI/ShapesOfWar/ShapesOfWarPanelSettings.asset";
        private const string ThemeStyleSheetAssetPath = "Assets/UI/ShapesOfWar/ShapesOfWarRuntimeTheme.tss";

        [SerializeField] private UIDocument? uiDocument;
        [SerializeField] private VisualTreeAsset? layoutAsset;
        [SerializeField] private StyleSheet? styleSheet;
        [SerializeField] private PanelSettings? panelSettings;
        [SerializeField] private ThemeStyleSheet? themeStyleSheet;

        private UIDocument? _document;
        private Game? _game;
        private int _activePlayerIndex;
        private bool _activePlayerRevealed;
        private bool _resourcesCollectedThisTurn;
        private string _status = "Create a game to begin.";
        private PendingUiAction _pendingUiAction = PendingUiAction.None;
        private int? _pendingTargetPlayerIndex;
        private UnitShape? _pendingRaidingUnitShape;

        private VisualElement? _header;
        private VisualElement? _setupPanel;
        private VisualElement? _passScreen;
        private VisualElement? _turnPanel;
        private VisualElement? _publicState;
        private VisualElement? _privateHand;
        private VisualElement? _economyControls;
        private VisualElement? _actionPhaseControls;
        private VisualElement? _pendingAction;
        private VisualElement? _battleRoyale;
        private VisualElement? _gameOver;
        private Label? _statusLabel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindAnyObjectByType<ShapesOfWarHotseatHarness>() != null)
            {
                return;
            }

            GameObject harness = new GameObject("Shapes of War Hotseat Harness");
            harness.AddComponent<ShapesOfWarHotseatHarness>();
        }

        private void OnEnable()
        {
            EnsureDocument();
            CacheContainers();
            Render();
        }

        private void EnsureDocument()
        {
            LoadAssetReferencesIfNeeded();

            _document = uiDocument != null ? uiDocument : GetComponent<UIDocument>();
            if (_document == null)
            {
                _document = gameObject.AddComponent<UIDocument>();
            }

            EnsurePanelSettingsTheme();

            if (panelSettings != null)
            {
                if (panelSettings.themeStyleSheet != null)
                {
                    _document.panelSettings = panelSettings;
                }
            }

            if (layoutAsset != null)
            {
                _document.visualTreeAsset = layoutAsset;
            }

            if (styleSheet != null && !_document.rootVisualElement.styleSheets.Contains(styleSheet))
            {
                _document.rootVisualElement.styleSheets.Add(styleSheet);
            }
        }

        private void LoadAssetReferencesIfNeeded()
        {
#if UNITY_EDITOR
            layoutAsset ??= AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LayoutAssetPath);
            styleSheet ??= AssetDatabase.LoadAssetAtPath<StyleSheet>(StyleSheetAssetPath);
            panelSettings ??= AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsAssetPath);
            themeStyleSheet ??= AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemeStyleSheetAssetPath);
#endif
        }

        private void EnsurePanelSettingsTheme()
        {
            if (panelSettings == null)
            {
                return;
            }

            if (panelSettings.themeStyleSheet != null)
            {
                return;
            }

#if UNITY_EDITOR
            if (themeStyleSheet != null)
            {
                panelSettings.themeStyleSheet = themeStyleSheet;
                EditorUtility.SetDirty(panelSettings);
                AssetDatabase.SaveAssets();
                return;
            }
#endif

            Debug.LogError(
                $"{nameof(ShapesOfWarHotseatHarness)} could not assign a Theme Style Sheet to {panelSettings.name}. " +
                $"Verify that {ThemeStyleSheetAssetPath} is a valid UI Toolkit Theme Style Sheet and that {PanelSettingsAssetPath} references it.");
        }

        private void CacheContainers()
        {
            if (_document == null)
            {
                return;
            }

            VisualElement root = _document.rootVisualElement;
            _header = root.Q<VisualElement>("header");
            _setupPanel = root.Q<VisualElement>("setup-panel");
            _passScreen = root.Q<VisualElement>("pass-screen");
            _turnPanel = root.Q<VisualElement>("turn-panel");
            _publicState = root.Q<VisualElement>("public-state");
            _privateHand = root.Q<VisualElement>("private-hand");
            _economyControls = root.Q<VisualElement>("economy-controls");
            _actionPhaseControls = root.Q<VisualElement>("action-phase-controls");
            _pendingAction = root.Q<VisualElement>("pending-action");
            _battleRoyale = root.Q<VisualElement>("battle-royale");
            _gameOver = root.Q<VisualElement>("game-over");
            _statusLabel = root.Q<Label>("status-label");
        }

        private void Render()
        {
            if (_document == null)
            {
                return;
            }

            ClearDynamicContainers();
            RenderHeader();

            if (_game == null)
            {
                ShowOnly(_setupPanel, _publicState);
                RenderSetupPanel();
                SetStatus();
                return;
            }

            RenderPublicState();

            if (_game.IsGameOver)
            {
                ShowOnly(_publicState, _gameOver);
                RenderGameOver();
                SetStatus();
                return;
            }

            if (!_activePlayerRevealed)
            {
                ShowOnly(_passScreen, _publicState);
                RenderPassScreen();
                SetStatus();
                return;
            }

            ShowOnly(_turnPanel, _publicState, _privateHand, _economyControls, _actionPhaseControls);
            RenderTurnPanel();
            RenderPrivateHand();
            RenderEconomyPanel();
            RenderActionPhasePanel();
            SetStatus();
        }

        private void ClearDynamicContainers()
        {
            foreach (VisualElement? container in DynamicContainers())
            {
                container?.Clear();
            }
        }

        private void RenderHeader()
        {
            if (_header == null)
            {
                return;
            }

            _header.Add(SectionTitle("Shapes of War", "app-title"));
            _header.Add(Button("New 2P Game", () => StartNewGame(DefaultPlayerCount)));
        }

        private void RenderSetupPanel()
        {
            if (_setupPanel == null)
            {
                return;
            }

            _setupPanel.Add(SectionTitle("New Hotseat Game"));
            _setupPanel.Add(new Label("Choose a local player count. Players pass the shared screen between turns."));

            VisualElement row = Row();
            for (int playerCount = Game.MinimumPlayerCount; playerCount <= Game.MaximumPlayerCount; playerCount++)
            {
                int capturedCount = playerCount;
                row.Add(Button($"{capturedCount} Players", () => StartNewGame(capturedCount)));
            }

            _setupPanel.Add(row);
        }

        private void RenderPassScreen()
        {
            if (_passScreen == null)
            {
                return;
            }

            PlayerPublicState activePlayer = ActivePlayerState();
            _passScreen.Add(SectionTitle($"Pass to {activePlayer.Name}"));
            _passScreen.Add(new Label("Private action-card identities are hidden until this player reveals their turn."));
            _passScreen.Add(Button($"Reveal {activePlayer.Name}'s Turn", () =>
            {
                _activePlayerRevealed = true;
                _status = $"{activePlayer.Name} revealed their hand.";
                Render();
            }));
        }

        private void RenderTurnPanel()
        {
            if (_turnPanel == null)
            {
                return;
            }

            PlayerPublicState activePlayer = ActivePlayerState();
            _turnPanel.Add(SectionTitle($"Active Player: {activePlayer.Name}"));

            VisualElement row = Row();
            row.Add(new Label($"Action choice: {_game!.GetActionPhaseChoice(_activePlayerIndex)}"));
            row.Add(Button("End Turn", EndTurn));
            _turnPanel.Add(row);
        }

        private void RenderPublicState()
        {
            if (_publicState == null || _game == null)
            {
                return;
            }

            _publicState.Add(SectionTitle("Public State"));

            VisualElement grid = new VisualElement();
            grid.AddToClassList("card-grid");

            foreach (PlayerPublicState player in _game.GetPublicPlayerStates())
            {
                VisualElement card = Card();
                card.Add(SectionTitle(player.Name, "card-title"));
                card.Add(new Label(player.IsEliminated ? "Eliminated" : "Active"));
                card.Add(new Label($"Base: {player.BaseType} ({player.BasePoints})"));
                card.Add(new Label($"Units: T {Count(player.UnitCounts, UnitShape.Triangle)} | S {Count(player.UnitCounts, UnitShape.Square)} | C {Count(player.UnitCounts, UnitShape.Circle)}"));
                card.Add(new Label($"Resources: W {Count(player.ResourceCounts, ResourceType.Wood)} | S {Count(player.ResourceCounts, ResourceType.Stone)} | M {Count(player.ResourceCounts, ResourceType.Metal)}"));
                card.Add(new Label($"Action cards: {player.ActionCardCount}"));
                grid.Add(card);
            }

            _publicState.Add(grid);
        }

        private void RenderPrivateHand()
        {
            if (_privateHand == null)
            {
                return;
            }

            _privateHand.Add(SectionTitle($"{ActivePlayerState().Name}'s Private Hand"));

            IReadOnlyList<ActionCardType> hand = _game!.GetPrivateActionCardHand(_activePlayerIndex);
            if (hand.Count == 0)
            {
                _privateHand.Add(new Label("No action cards."));
                return;
            }

            VisualElement row = Row();
            foreach (ActionCardType card in hand)
            {
                row.Add(Pill(Format(card)));
            }

            _privateHand.Add(row);
        }

        private void RenderEconomyPanel()
        {
            if (_economyControls == null)
            {
                return;
            }

            _economyControls.Add(SectionTitle("Economy"));

            Button collect = Button("Count / Collect Resources", () =>
            {
                if (_resourcesCollectedThisTurn)
                {
                    _status = "Resources were already collected this turn.";
                    Render();
                    return;
                }

                RunAction(
                    () =>
                    {
                        _game!.CollectResources(_activePlayerIndex);
                        return true;
                    },
                    "Resources collected.",
                    "Could not collect resources.");
                _resourcesCollectedThisTurn = true;
            });
            collect.SetEnabled(!_resourcesCollectedThisTurn);
            _economyControls.Add(collect);

            VisualElement buyRow = Row();
            buyRow.Add(new Label("Buy:"));
            foreach (UnitShape shape in EnumValues<UnitShape>())
            {
                UnitShape capturedShape = shape;
                buyRow.Add(Button(Format(shape), () => RunAction(
                    () => _game!.TryBuyUnit(_activePlayerIndex, capturedShape),
                    $"Bought {Format(capturedShape)}.",
                    $"Could not buy {Format(capturedShape)}.")));
            }

            _economyControls.Add(buyRow);

            VisualElement exchangeRow = Row();
            exchangeRow.Add(new Label("Exchange:"));
            exchangeRow.Add(Button("1 Stone -> 2 Wood", () => RunAction(
                () => _game!.TryExchangeStoneForWood(_activePlayerIndex),
                "Exchanged 1 Stone for 2 Wood.",
                "Could not exchange 1 Stone for 2 Wood.")));
            exchangeRow.Add(Button("1 Metal -> 3 Wood", () => RunAction(
                () => _game!.TryExchangeMetalForWood(_activePlayerIndex),
                "Exchanged 1 Metal for 3 Wood.",
                "Could not exchange 1 Metal for 3 Wood.")));
            exchangeRow.Add(Button("1 Metal -> 1 Stone + 1 Wood", () => RunAction(
                () => _game!.TryExchangeMetalForStoneAndWood(_activePlayerIndex),
                "Exchanged 1 Metal for 1 Stone and 1 Wood.",
                "Could not exchange 1 Metal for 1 Stone and 1 Wood.")));
            _economyControls.Add(exchangeRow);

            VisualElement upgradeRow = Row();
            upgradeRow.Add(new Label("Upgrade:"));
            upgradeRow.Add(Button("Stone Base", () => RunAction(
                () => _game!.TryUpgradeBase(_activePlayerIndex, BaseType.Stone),
                "Upgraded to Stone Base.",
                "Could not upgrade to Stone Base.")));
            upgradeRow.Add(Button("Metal Base", () => RunAction(
                () => _game!.TryUpgradeBase(_activePlayerIndex, BaseType.Metal),
                "Upgraded to Metal Base.",
                "Could not upgrade to Metal Base.")));
            _economyControls.Add(upgradeRow);

            VisualElement sacrificeRow = Row();
            sacrificeRow.Add(new Label("Sacrifice for card (Triangle only):"));
            sacrificeRow.Add(Button(Format(UnitShape.Triangle), () => RunAction(
                () => _game!.TrySacrificeUnitToDrawActionCard(_activePlayerIndex, UnitShape.Triangle),
                "Sacrificed Triangle and drew 1 card.",
                "Could not sacrifice Triangle for a card.")));

            _economyControls.Add(sacrificeRow);
        }

        private void RenderActionPhasePanel()
        {
            if (_actionPhaseControls == null)
            {
                return;
            }

            _actionPhaseControls.Add(SectionTitle("Action Phase"));

            if (_game!.HasPendingAction)
            {
                Show(_pendingAction);
                RenderPendingActionPanel();
                return;
            }

            if (_game.HasPendingBattleRoyale)
            {
                Show(_battleRoyale);
                RenderPendingBattleRoyalePanel();
                return;
            }

            _actionPhaseControls.Add(Button("Pass Action Phase", () => RunAction(
                () => _game.TryPassActionPhase(_activePlayerIndex),
                "Action phase passed.",
                "Could not pass action phase.")));
            _actionPhaseControls.Add(BuildActionCardButtons());
            _actionPhaseControls.Add(BuildBattleRoyaleStartButtons());
        }

        private VisualElement BuildActionCardButtons()
        {
            VisualElement panel = Card();
            panel.Add(SectionTitle("Play Action Card", "card-title"));

            foreach (PlayerPublicState target in TargetPlayers())
            {
                VisualElement targetPanel = Card();
                targetPanel.Add(new Label($"Target {target.Name}"));

                VisualElement theftRow = Row();
                theftRow.Add(new Label("Resource Theft:"));
                foreach (ResourceType resource in EnumValues<ResourceType>())
                {
                    ResourceType capturedResource = resource;
                    int capturedTarget = target.Index;
                    theftRow.Add(Button(Format(resource), () =>
                    {
                        bool played = _game!.TryPlayResourceTheft(_activePlayerIndex, capturedTarget, capturedResource);
                        OnActionCardPlayAttempt(played, PendingUiAction.ResourceTheft, capturedTarget, null, $"Resource Theft targeting {target.Name}'s {Format(capturedResource)}.");
                    }));
                }

                targetPanel.Add(theftRow);

                VisualElement killRow = Row();
                killRow.Add(new Label("Unit Kill:"));
                foreach (UnitShape shape in EnumValues<UnitShape>())
                {
                    UnitShape capturedShape = shape;
                    int capturedTarget = target.Index;
                    killRow.Add(Button(Format(shape), () =>
                    {
                        bool played = _game!.TryPlayUnitKill(_activePlayerIndex, capturedTarget, capturedShape);
                        OnActionCardPlayAttempt(played, PendingUiAction.UnitKill, capturedTarget, null, $"Unit Kill targeting {target.Name}'s {Format(capturedShape)}.");
                    }));
                }

                targetPanel.Add(killRow);

                VisualElement raidRow = Row();
                raidRow.Add(new Label("Raid Base:"));
                foreach (UnitShape shape in EnumValues<UnitShape>())
                {
                    UnitShape capturedShape = shape;
                    int capturedTarget = target.Index;
                    raidRow.Add(Button(Format(shape), () =>
                    {
                        bool played = _game!.TryPlayRaidBase(_activePlayerIndex, capturedTarget, capturedShape);
                        OnActionCardPlayAttempt(played, PendingUiAction.RaidBase, capturedTarget, capturedShape, $"Raid Base targeting {target.Name} with {Format(capturedShape)}.");
                    }));
                }

                targetPanel.Add(raidRow);
                panel.Add(targetPanel);
            }

            return panel;
        }

        private VisualElement BuildBattleRoyaleStartButtons()
        {
            VisualElement panel = Card();
            panel.Add(SectionTitle("Start Battle Royale", "card-title"));

            VisualElement row = Row();
            foreach (UnitShape shape in EnumValues<UnitShape>())
            {
                UnitShape capturedShape = shape;
                row.Add(Button(Format(shape), () => RunAction(
                    () => _game!.TryStartBattleRoyale(_activePlayerIndex, capturedShape),
                    $"Battle Royale started with {Format(capturedShape)}.",
                    $"Could not start Battle Royale with {Format(capturedShape)}.")));
            }

            panel.Add(row);
            return panel;
        }

        private void RenderPendingActionPanel()
        {
            if (_pendingAction == null)
            {
                return;
            }

            _pendingAction.Add(SectionTitle("Pending Action"));
            _pendingAction.Add(new Label(_pendingUiAction == PendingUiAction.None ? "Action pending." : $"{Format(_pendingUiAction)} pending."));

            VisualElement counterRow = Row();
            counterRow.Add(new Label("Counter response:"));
            foreach (PlayerPublicState player in _game!.GetPublicPlayerStates().Where(player => !player.IsEliminated))
            {
                int capturedPlayer = player.Index;
                counterRow.Add(Button($"{player.Name} Counter", () => RunAction(
                    () => _game.TryRespondWithCounter(capturedPlayer),
                    $"{player.Name} responded with Counter.",
                    $"{player.Name} could not respond with Counter.")));
            }

            _pendingAction.Add(counterRow);

            if (_pendingUiAction == PendingUiAction.RaidBase && _pendingTargetPlayerIndex.HasValue && _pendingRaidingUnitShape.HasValue)
            {
                PlayerPublicState? defender = _game.GetPublicPlayerStates().FirstOrDefault(player => player.Index == _pendingTargetPlayerIndex.Value);
                if (defender != null && !defender.IsEliminated)
                {
                    VisualElement defenseRow = Row();
                    defenseRow.Add(new Label($"{defender.Name} defend:"));
                    foreach (UnitShape defenseShape in EnumValues<UnitShape>())
                    {
                        if (TryGetMinimumDefense(_pendingRaidingUnitShape.Value, defenseShape, out int count))
                        {
                            UnitShape capturedShape = defenseShape;
                            int capturedCount = count;
                            defenseRow.Add(Button($"{capturedCount} {Format(capturedShape)}", () => RunAction(
                                () => _game.TryDefendPendingActionWithUnits(defender.Index, capturedShape, capturedCount),
                                $"{defender.Name} committed {capturedCount} {Format(capturedShape)} to defend.",
                                $"{defender.Name} could not defend with {capturedCount} {Format(capturedShape)}.")));
                        }
                    }

                    _pendingAction.Add(defenseRow);
                }
            }

            _pendingAction.Add(Button("Resolve Pending Action", () =>
            {
                bool resolved = _game!.ResolvePendingAction();
                _pendingUiAction = PendingUiAction.None;
                _pendingTargetPlayerIndex = null;
                _pendingRaidingUnitShape = null;
                _status = resolved ? "Pending action resolved successfully." : "Pending action was stopped or failed.";
                Render();
            }));
        }

        private void RenderPendingBattleRoyalePanel()
        {
            if (_battleRoyale == null)
            {
                return;
            }

            _battleRoyale.Add(SectionTitle("Pending Battle Royale"));
            _battleRoyale.Add(new Label($"Winning: {PlayerName(_game!.BattleRoyaleCurrentWinningPlayerIndex)} with {_game.BattleRoyaleCurrentWinningCount} {Format(_game.BattleRoyaleCurrentWinningShape)}"));
            _battleRoyale.Add(new Label($"Acting: {PlayerName(_game.BattleRoyaleCurrentActingPlayerIndex)}"));

            int? actingPlayer = _game.BattleRoyaleCurrentActingPlayerIndex;
            if (!actingPlayer.HasValue)
            {
                _battleRoyale.Add(new Label("Battle Royale is ready to resolve."));
                return;
            }

            _battleRoyale.Add(Button($"{PlayerName(actingPlayer)} Pass", () =>
            {
                bool passed = _game.TryPassBattleRoyale(actingPlayer.Value);
                _status = passed ? $"{PlayerName(actingPlayer)} passed Battle Royale." : "Could not pass Battle Royale.";
                if (!_game.HasPendingBattleRoyale)
                {
                    _status = "Battle Royale resolved.";
                }

                Render();
            }));

            VisualElement playRow = Row();
            playRow.Add(new Label("Play to beat:"));
            foreach (UnitShape shape in EnumValues<UnitShape>())
            {
                for (int count = 1; count <= 3; count++)
                {
                    UnitShape capturedShape = shape;
                    int capturedCount = count;
                    playRow.Add(Button($"{capturedCount} {Format(capturedShape)}", () =>
                    {
                        bool played = _game.TryPlayBattleRoyaleUnits(actingPlayer.Value, capturedShape, capturedCount);
                        _status = played ? $"{PlayerName(actingPlayer)} played {capturedCount} {Format(capturedShape)}." : "That Battle Royale play is not valid.";
                        if (!_game.HasPendingBattleRoyale)
                        {
                            _status = "Battle Royale resolved.";
                        }

                        Render();
                    }));
                }
            }

            _battleRoyale.Add(playRow);
        }

        private void RenderGameOver()
        {
            if (_gameOver == null)
            {
                return;
            }

            _gameOver.Add(SectionTitle("Game Over"));
            _gameOver.Add(new Label($"Winner: {PlayerName(_game!.WinningPlayerIndex)}"));
            _gameOver.Add(Button("Start New 2P Game", () => StartNewGame(DefaultPlayerCount)));
        }

        private void StartNewGame(int playerCount)
        {
            _game = Game.CreateNew(Enumerable.Range(1, playerCount).Select(index => $"Player {index}"));
            _activePlayerIndex = 0;
            _activePlayerRevealed = false;
            _resourcesCollectedThisTurn = false;
            _pendingUiAction = PendingUiAction.None;
            _pendingTargetPlayerIndex = null;
            _pendingRaidingUnitShape = null;
            _status = $"Started a {playerCount}-player hotseat game.";
            Render();
        }

        private void EndTurn()
        {
            if (_game == null)
            {
                return;
            }

            if (_game.HasPendingAction || _game.HasPendingBattleRoyale)
            {
                _status = "Resolve the pending action or Battle Royale before ending the turn.";
                Render();
                return;
            }

            int nextPlayerIndex = NextActivePlayerIndex();
            if (!_game.TryResetActionPhaseChoiceForNextTurn(nextPlayerIndex))
            {
                _status = "Could not prepare the next player's action phase.";
                Render();
                return;
            }

            _activePlayerIndex = nextPlayerIndex;
            _activePlayerRevealed = false;
            _resourcesCollectedThisTurn = false;
            _status = $"Pass control to {PlayerName(_activePlayerIndex)}.";
            Render();
        }

        private void OnActionCardPlayAttempt(
            bool played,
            PendingUiAction pendingAction,
            int targetPlayerIndex,
            UnitShape? raidingUnitShape,
            string successMessage)
        {
            if (played)
            {
                _pendingUiAction = pendingAction;
                _pendingTargetPlayerIndex = targetPlayerIndex;
                _pendingRaidingUnitShape = raidingUnitShape;
                _status = successMessage;
            }
            else
            {
                _status = "Could not play that action card.";
            }

            Render();
        }

        private void RunAction(Func<bool> action, string successMessage, string failureMessage)
        {
            try
            {
                _status = action() ? successMessage : failureMessage;
            }
            catch (Exception exception)
            {
                _status = exception.Message;
            }

            Render();
        }

        private void SetStatus()
        {
            if (_statusLabel != null)
            {
                _statusLabel.text = _status;
            }
        }

        private int NextActivePlayerIndex()
        {
            IReadOnlyList<PlayerPublicState> players = _game!.GetPublicPlayerStates();
            for (int offset = 1; offset <= players.Count; offset++)
            {
                int candidateIndex = (_activePlayerIndex + offset) % players.Count;
                PlayerPublicState candidate = players[candidateIndex];
                if (!candidate.IsEliminated)
                {
                    return candidate.Index;
                }
            }

            return _activePlayerIndex;
        }

        private PlayerPublicState ActivePlayerState()
        {
            return _game!.GetPublicPlayerStates().First(player => player.Index == _activePlayerIndex);
        }

        private IEnumerable<PlayerPublicState> TargetPlayers()
        {
            return _game!.GetPublicPlayerStates()
                .Where(player => player.Index != _activePlayerIndex && !player.IsEliminated);
        }

        private string PlayerName(int? playerIndex)
        {
            if (_game == null || !playerIndex.HasValue)
            {
                return "None";
            }

            PlayerPublicState? player = _game.GetPublicPlayerStates().FirstOrDefault(candidate => candidate.Index == playerIndex.Value);
            return player?.Name ?? "Unknown";
        }

        private IEnumerable<VisualElement?> DynamicContainers()
        {
            yield return _header;
            yield return _setupPanel;
            yield return _passScreen;
            yield return _turnPanel;
            yield return _publicState;
            yield return _privateHand;
            yield return _economyControls;
            yield return _actionPhaseControls;
            yield return _pendingAction;
            yield return _battleRoyale;
            yield return _gameOver;
        }

        private void ShowOnly(params VisualElement?[] visibleContainers)
        {
            HideAllSections();

            HashSet<VisualElement> visible = visibleContainers
                .Where(container => container != null)
                .Cast<VisualElement>()
                .ToHashSet();

            foreach (VisualElement container in visible)
            {
                Show(container);
            }
        }

        private void HideAllSections()
        {
            Hide(_setupPanel);
            Hide(_passScreen);
            Hide(_turnPanel);
            Hide(_publicState);
            Hide(_privateHand);
            Hide(_economyControls);
            Hide(_actionPhaseControls);
            Hide(_pendingAction);
            Hide(_battleRoyale);
            Hide(_gameOver);
        }

        private static void Show(VisualElement? element)
        {
            if (element != null)
            {
                element.style.display = DisplayStyle.Flex;
            }
        }

        private static void Hide(VisualElement? element)
        {
            if (element != null)
            {
                element.style.display = DisplayStyle.None;
            }
        }

        private static bool TryGetMinimumDefense(UnitShape raidingUnitShape, UnitShape defendingUnitShape, out int count)
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

            if (raidingUnitShape == UnitShape.Circle && (defendingUnitShape == UnitShape.Triangle || defendingUnitShape == UnitShape.Square))
            {
                count = 1;
                return true;
            }

            return false;
        }

        private static IReadOnlyList<T> EnumValues<T>() where T : Enum
        {
            return Enum.GetValues(typeof(T)).Cast<T>().ToList();
        }

        private static int Count<T>(IReadOnlyDictionary<T, int> counts, T key)
        {
            return counts.TryGetValue(key, out int count) ? count : 0;
        }

        private static string Format<T>(T? value) where T : struct
        {
            return value.HasValue ? Format(value.Value) : "None";
        }

        private static string Format(object value)
        {
            string text = value.ToString() ?? string.Empty;
            return string.Concat(text.SelectMany((character, index) =>
                index > 0 && char.IsUpper(character) ? new[] { ' ', character } : new[] { character }));
        }

        private static VisualElement Row()
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("row");
            return row;
        }

        private static VisualElement Card()
        {
            VisualElement card = new VisualElement();
            card.AddToClassList("card");
            return card;
        }

        private static Label SectionTitle(string text, string className = "section-title")
        {
            Label label = new Label(text);
            label.AddToClassList(className);
            return label;
        }

        private static Label Pill(string text)
        {
            Label label = new Label(text);
            label.AddToClassList("pill");
            return label;
        }

        private static Button Button(string text, Action action)
        {
            Button button = new Button(action)
            {
                text = text
            };
            button.AddToClassList("command-button");
            return button;
        }

        private enum PendingUiAction
        {
            None,
            ResourceTheft,
            UnitKill,
            RaidBase
        }
    }
}

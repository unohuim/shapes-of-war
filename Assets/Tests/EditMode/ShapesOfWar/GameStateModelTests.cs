#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ShapesOfWar.Domain;

namespace ShapesOfWar.Domain.Tests
{
    public sealed class GameStateModelTests
    {
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void GameCanBeCreatedWithSupportedPlayerCounts(int playerCount)
        {
            Game game = CreateGame(playerCount);

            Assert.That(game.Players, Has.Count.EqualTo(playerCount));
        }

        [Test]
        public void GameCreationRejectsFewerThanTwoPlayers()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateGame(1));
        }

        [Test]
        public void GameCreationRejectsMoreThanFourPlayers()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateGame(5));
        }

        [Test]
        public void PlayersHaveUniqueIdentityIndexAndName()
        {
            Game game = CreateGame(4);

            Assert.That(game.Players.Select(player => player.Index), Is.Unique);
            Assert.That(game.Players.Select(player => player.Name), Is.Unique);
        }

        [Test]
        public void GameCreationRejectsDuplicatePlayerIndexes()
        {
            Player first = CreatePlayer(0, "Player 1");
            Player second = CreatePlayer(0, "Player 2");

            Assert.Throws<ArgumentException>(() => new Game(new[] { first, second }));
        }

        [Test]
        public void GameCreationRejectsDuplicatePlayerNames()
        {
            Player first = CreatePlayer(0, "Player");
            Player second = CreatePlayer(1, "Player");

            Assert.Throws<ArgumentException>(() => new Game(new[] { first, second }));
        }

        [Test]
        public void PublicPlayerStateExposesBaseTypeAndPoints()
        {
            PlayerPublicState state = CreatePlayer(0, "Player 1").ToPublicState();

            Assert.That(state.BaseType, Is.EqualTo(BaseType.Wood));
            Assert.That(state.BasePoints, Is.EqualTo(3));
        }

        [Test]
        public void PublicPlayerStateExposesUnitCounts()
        {
            EnumCountSet<UnitShape> unitCounts = new EnumCountSet<UnitShape>(
                new Dictionary<UnitShape, int>
                {
                    [UnitShape.Triangle] = 1,
                    [UnitShape.Square] = 2,
                    [UnitShape.Circle] = 3
                });

            PlayerPublicState state = CreatePlayer(0, "Player 1", unitCounts: unitCounts).ToPublicState();

            Assert.That(state.UnitCounts[UnitShape.Triangle], Is.EqualTo(1));
            Assert.That(state.UnitCounts[UnitShape.Square], Is.EqualTo(2));
            Assert.That(state.UnitCounts[UnitShape.Circle], Is.EqualTo(3));
        }

        [Test]
        public void PublicPlayerStateExposesResourceCounts()
        {
            EnumCountSet<ResourceType> resourceCounts = new EnumCountSet<ResourceType>(
                new Dictionary<ResourceType, int>
                {
                    [ResourceType.Wood] = 4,
                    [ResourceType.Stone] = 5,
                    [ResourceType.Metal] = 6
                });

            PlayerPublicState state = CreatePlayer(0, "Player 1", resourceCounts: resourceCounts).ToPublicState();

            Assert.That(state.ResourceCounts[ResourceType.Wood], Is.EqualTo(4));
            Assert.That(state.ResourceCounts[ResourceType.Stone], Is.EqualTo(5));
            Assert.That(state.ResourceCounts[ResourceType.Metal], Is.EqualTo(6));
        }

        [Test]
        public void ActionCardIdentitiesRemainInternalToPlayer()
        {
            Player player = CreatePlayer(
                0,
                "Player 1",
                actionCards: new ActionCardHand(new[] { ActionCardType.Counter, ActionCardType.RaidBase }));

            Assert.That(player.ActionCards.Cards, Is.EqualTo(new[] { ActionCardType.Counter, ActionCardType.RaidBase }));
            Assert.That(HasPublicActionCardIdentityProperty(typeof(Player)), Is.False);
            Assert.That(HasPublicActionCardIdentityProperty(typeof(PlayerPublicState)), Is.False);
        }

        [Test]
        public void PublicPlayerStateExposesOnlyActionCardCount()
        {
            PlayerPublicState state = CreatePlayer(
                0,
                "Player 1",
                actionCards: new ActionCardHand(new[] { ActionCardType.Counter, ActionCardType.RaidBase }))
                .ToPublicState();

            Assert.That(state.ActionCardCount, Is.EqualTo(2));
        }

        [Test]
        public void StandardActionDeckContainsDocumentedComposition()
        {
            ActionCardDeck deck = ActionCardDeck.CreateStandard();

            Assert.That(deck.Count, Is.EqualTo(50));
            Assert.That(deck.CountOf(ActionCardType.RaidBase), Is.EqualTo(10));
            Assert.That(deck.CountOf(ActionCardType.ResourceTheft), Is.EqualTo(10));
            Assert.That(deck.CountOf(ActionCardType.UnitKill), Is.EqualTo(10));
            Assert.That(deck.CountOf(ActionCardType.Counter), Is.EqualTo(20));
        }

        [Test]
        public void DrawingActionCardDecreasesDeckCountAndAddsToPlayerHand()
        {
            Game game = CreateSetupGame();

            bool drew = game.TryDrawActionCard(0);

            PlayerPublicState state = game.Players[0].ToPublicState();
            Assert.That(drew, Is.True);
            Assert.That(game.ActionDeck.Count, Is.EqualTo(49));
            Assert.That(state.ActionCardCount, Is.EqualTo(1));
        }

        [Test]
        public void DrawnActionCardIdentityRemainsInternal()
        {
            Game game = CreateSetupGame();

            bool drew = game.TryDrawActionCard(0);

            Assert.That(drew, Is.True);
            Assert.That(game.Players[0].ActionCards.Cards, Has.Count.EqualTo(1));
            Assert.That(HasPublicActionCardIdentityProperty(typeof(Player)), Is.False);
            Assert.That(HasPublicActionCardIdentityProperty(typeof(PlayerPublicState)), Is.False);
        }

        [Test]
        public void DiscardingUsedActionCardMovesCardToDiscardPile()
        {
            Game game = CreateSetupGame();

            game.DiscardUsedActionCard(ActionCardType.Counter);

            Assert.That(game.ActionDeck.DiscardPileCount, Is.EqualTo(1));
            Assert.That(game.ActionDeck.CountOfDiscard(ActionCardType.Counter), Is.EqualTo(1));
        }

        [Test]
        public void DrawingFromEmptyDeckShufflesDiscardPileIntoNewDeck()
        {
            Game game = new Game(
                new[] { CreatePlayer(0, "Player 1"), CreatePlayer(1, "Player 2") },
                new ActionCardDeck(
                    Array.Empty<ActionCardType>(),
                    new[] { ActionCardType.Counter }));

            bool drew = game.TryDrawActionCard(0);

            PlayerPublicState state = game.Players[0].ToPublicState();
            Assert.That(drew, Is.True);
            Assert.That(game.ActionDeck.Count, Is.EqualTo(0));
            Assert.That(game.ActionDeck.DiscardPileCount, Is.EqualTo(0));
            Assert.That(state.ActionCardCount, Is.EqualTo(1));
            Assert.That(game.Players[0].ActionCards.Cards[0], Is.EqualTo(ActionCardType.Counter));
        }

        [Test]
        public void DrawingFromEmptyDeckAndEmptyDiscardPileDrawsNoCard()
        {
            Game game = new Game(
                new[]
                {
                    CreatePlayer(
                        0,
                        "Player 1",
                        unitCounts: new EnumCountSet<UnitShape>(
                            new Dictionary<UnitShape, int>
                            {
                                [UnitShape.Square] = 1
                            })),
                    CreatePlayer(1, "Player 2")
                },
                new ActionCardDeck(Array.Empty<ActionCardType>()));

            bool drew = game.TryDrawActionCard(0);

            PlayerPublicState state = game.Players[0].ToPublicState();
            Assert.That(drew, Is.False);
            Assert.That(game.ActionDeck.Count, Is.EqualTo(0));
            Assert.That(game.ActionDeck.DiscardPileCount, Is.EqualTo(0));
            Assert.That(state.ActionCardCount, Is.EqualTo(0));
        }

        [Test]
        public void SacrificeToDrawNoOpsWhenDeckAndDiscardPileAreEmpty()
        {
            Game game = new Game(
                new[]
                {
                    CreatePlayer(
                        0,
                        "Player 1",
                        unitCounts: new EnumCountSet<UnitShape>(
                            new Dictionary<UnitShape, int>
                            {
                                [UnitShape.Square] = 1
                            })),
                    CreatePlayer(1, "Player 2")
                },
                new ActionCardDeck(Array.Empty<ActionCardType>()));

            bool sacrificed = game.TrySacrificeUnitToDrawActionCard(0, UnitShape.Square);

            PlayerPublicState state = game.Players[0].ToPublicState();
            Assert.That(sacrificed, Is.False);
            Assert.That(state.UnitCounts[UnitShape.Square], Is.EqualTo(1));
            Assert.That(state.ActionCardCount, Is.EqualTo(0));
        }

        [Test]
        public void SacrificeWrapperNoOpsWhenDeckAndDiscardPileAreEmpty()
        {
            Game game = new Game(
                new[]
                {
                    CreatePlayer(
                        0,
                        "Player 1",
                        unitCounts: new EnumCountSet<UnitShape>(
                            new Dictionary<UnitShape, int>
                            {
                                [UnitShape.Square] = 1
                            })),
                    CreatePlayer(1, "Player 2")
                },
                new ActionCardDeck(Array.Empty<ActionCardType>()));

            bool sacrificed = game.TrySacrificeUnitForActionCard(0, UnitShape.Square);

            PlayerPublicState state = game.Players[0].ToPublicState();
            Assert.That(sacrificed, Is.False);
            Assert.That(state.UnitCounts[UnitShape.Square], Is.EqualTo(1));
            Assert.That(state.ActionCardCount, Is.EqualTo(0));
        }

        [Test]
        public void PlayerEliminationStateDefaultsToNotEliminated()
        {
            Player player = CreatePlayer(0, "Player 1");

            Assert.That(player.IsEliminated, Is.False);
            Assert.That(player.ToPublicState().IsEliminated, Is.False);
        }

        [Test]
        public void NewGameSetupUsesDocumentedStartingState()
        {
            Game game = CreateSetupGame();

            foreach (PlayerPublicState state in game.GetPublicPlayerStates())
            {
                Assert.That(state.BaseType, Is.EqualTo(BaseType.Wood));
                Assert.That(state.BasePoints, Is.EqualTo(3));
                Assert.That(state.UnitCounts[UnitShape.Triangle], Is.EqualTo(0));
                Assert.That(state.UnitCounts[UnitShape.Square], Is.EqualTo(3));
                Assert.That(state.UnitCounts[UnitShape.Circle], Is.EqualTo(0));
                Assert.That(state.ResourceCounts[ResourceType.Wood], Is.EqualTo(0));
                Assert.That(state.ResourceCounts[ResourceType.Stone], Is.EqualTo(0));
                Assert.That(state.ResourceCounts[ResourceType.Metal], Is.EqualTo(0));
                Assert.That(state.ActionCardCount, Is.EqualTo(0));
                Assert.That(state.IsEliminated, Is.False);
            }
        }

        [Test]
        public void CountUnitsReturnsCurrentPublicUnitCounts()
        {
            Game game = CreateSetupGame();

            IReadOnlyDictionary<UnitShape, int> unitCounts = game.CountUnits(0);

            Assert.That(unitCounts[UnitShape.Triangle], Is.EqualTo(0));
            Assert.That(unitCounts[UnitShape.Square], Is.EqualTo(3));
            Assert.That(unitCounts[UnitShape.Circle], Is.EqualTo(0));
        }

        [Test]
        public void CollectResourcesAddsResourcesFromCurrentUnitCounts()
        {
            Game game = new Game(new[]
            {
                CreatePlayer(
                    0,
                    "Player 1",
                    unitCounts: new EnumCountSet<UnitShape>(
                        new Dictionary<UnitShape, int>
                        {
                            [UnitShape.Triangle] = 1,
                            [UnitShape.Square] = 2,
                            [UnitShape.Circle] = 3
                        })),
                CreatePlayer(1, "Player 2")
            });

            game.CollectResources(0);

            PlayerPublicState state = game.Players[0].ToPublicState();
            Assert.That(state.ResourceCounts[ResourceType.Wood], Is.EqualTo(1));
            Assert.That(state.ResourceCounts[ResourceType.Stone], Is.EqualTo(2));
            Assert.That(state.ResourceCounts[ResourceType.Metal], Is.EqualTo(3));
        }

        [TestCase(UnitShape.Triangle, ResourceType.Metal)]
        [TestCase(UnitShape.Square, ResourceType.Stone)]
        [TestCase(UnitShape.Circle, ResourceType.Wood)]
        public void BuyingUnitSpendsMatchingResourceAndAddsUnitImmediately(UnitShape unitShape, ResourceType resourceType)
        {
            Game game = new Game(new[]
            {
                CreatePlayer(
                    0,
                    "Player 1",
                    resourceCounts: new EnumCountSet<ResourceType>(
                        new Dictionary<ResourceType, int>
                        {
                            [resourceType] = 1
                        })),
                CreatePlayer(1, "Player 2")
            });

            bool bought = game.TryBuyUnit(0, unitShape);

            PlayerPublicState state = game.Players[0].ToPublicState();
            Assert.That(bought, Is.True);
            Assert.That(state.UnitCounts[unitShape], Is.EqualTo(1));
            Assert.That(state.ResourceCounts[resourceType], Is.EqualTo(0));
        }

        [Test]
        public void BuyingUnitFailsWithInsufficientResources()
        {
            Game game = CreateSetupGame();

            bool bought = game.TryBuyUnit(0, UnitShape.Triangle);

            PlayerPublicState state = game.Players[0].ToPublicState();
            Assert.That(bought, Is.False);
            Assert.That(state.UnitCounts[UnitShape.Triangle], Is.EqualTo(0));
            Assert.That(state.ResourceCounts[ResourceType.Metal], Is.EqualTo(0));
        }

        [Test]
        public void BoughtUnitsAreAvailableForLaterSameTurnEconomyChoices()
        {
            Game game = new Game(new[]
            {
                CreatePlayer(
                    0,
                    "Player 1",
                    resourceCounts: new EnumCountSet<ResourceType>(
                        new Dictionary<ResourceType, int>
                        {
                            [ResourceType.Wood] = 1
                        })),
                CreatePlayer(1, "Player 2")
            });

            bool bought = game.TryBuyUnit(0, UnitShape.Circle);
            bool sacrificed = game.TrySacrificeUnitToDrawActionCard(0, UnitShape.Circle);

            PlayerPublicState state = game.Players[0].ToPublicState();
            Assert.That(bought, Is.True);
            Assert.That(sacrificed, Is.True);
            Assert.That(state.UnitCounts[UnitShape.Circle], Is.EqualTo(0));
            Assert.That(state.ActionCardCount, Is.EqualTo(1));
        }

        [Test]
        public void WoodBaseUpgradesToStoneBaseForTwoStone()
        {
            Game game = new Game(new[]
            {
                CreatePlayer(
                    0,
                    "Player 1",
                    resourceCounts: new EnumCountSet<ResourceType>(
                        new Dictionary<ResourceType, int>
                        {
                            [ResourceType.Stone] = 2
                        })),
                CreatePlayer(1, "Player 2")
            });

            bool upgraded = game.TryUpgradeBase(0, BaseType.Stone);

            PlayerPublicState state = game.Players[0].ToPublicState();
            Assert.That(upgraded, Is.True);
            Assert.That(state.BaseType, Is.EqualTo(BaseType.Stone));
            Assert.That(state.BasePoints, Is.EqualTo(5));
            Assert.That(state.ResourceCounts[ResourceType.Stone], Is.EqualTo(0));
        }

        [Test]
        public void StoneBaseUpgradesToMetalBaseForTwoMetal()
        {
            Game game = new Game(new[]
            {
                CreatePlayer(
                    0,
                    "Player 1",
                    playerBase: new Base(BaseType.Stone, 5),
                    resourceCounts: new EnumCountSet<ResourceType>(
                        new Dictionary<ResourceType, int>
                        {
                            [ResourceType.Metal] = 2
                        })),
                CreatePlayer(1, "Player 2")
            });

            bool upgraded = game.TryUpgradeBase(0, BaseType.Metal);

            PlayerPublicState state = game.Players[0].ToPublicState();
            Assert.That(upgraded, Is.True);
            Assert.That(state.BaseType, Is.EqualTo(BaseType.Metal));
            Assert.That(state.BasePoints, Is.EqualTo(7));
            Assert.That(state.ResourceCounts[ResourceType.Metal], Is.EqualTo(0));
        }

        [Test]
        public void BaseUpgradeFailsWithInsufficientResources()
        {
            Game game = CreateSetupGame();

            bool upgraded = game.TryUpgradeBase(0, BaseType.Stone);

            PlayerPublicState state = game.Players[0].ToPublicState();
            Assert.That(upgraded, Is.False);
            Assert.That(state.BaseType, Is.EqualTo(BaseType.Wood));
            Assert.That(state.BasePoints, Is.EqualTo(3));
        }

        [TestCase(BaseType.Wood, BaseType.Metal)]
        [TestCase(BaseType.Stone, BaseType.Wood)]
        [TestCase(BaseType.Metal, BaseType.Stone)]
        public void NonlinearBaseUpgradeAttemptsFail(BaseType currentBaseType, BaseType targetBaseType)
        {
            int points = currentBaseType == BaseType.Wood ? 3 : currentBaseType == BaseType.Stone ? 5 : 7;
            Game game = new Game(new[]
            {
                CreatePlayer(
                    0,
                    "Player 1",
                    playerBase: new Base(currentBaseType, points),
                    resourceCounts: new EnumCountSet<ResourceType>(
                        new Dictionary<ResourceType, int>
                        {
                            [ResourceType.Stone] = 2,
                            [ResourceType.Metal] = 2
                        })),
                CreatePlayer(1, "Player 2")
            });

            bool upgraded = game.TryUpgradeBase(0, targetBaseType);

            PlayerPublicState state = game.Players[0].ToPublicState();
            Assert.That(upgraded, Is.False);
            Assert.That(state.BaseType, Is.EqualTo(currentBaseType));
            Assert.That(state.BasePoints, Is.EqualTo(points));
        }

        [Test]
        public void SacrificingOneUnitDrawsOneActionCard()
        {
            Game game = CreateSetupGame();

            bool sacrificed = game.TrySacrificeUnitToDrawActionCard(0, UnitShape.Square);

            PlayerPublicState state = game.Players[0].ToPublicState();
            Assert.That(sacrificed, Is.True);
            Assert.That(state.UnitCounts[UnitShape.Square], Is.EqualTo(2));
            Assert.That(state.ActionCardCount, Is.EqualTo(1));
            Assert.That(game.ActionDeck.Count, Is.EqualTo(49));
        }

        [Test]
        public void SacrificingUnitFailsWhenPlayerHasNoUnitOfThatShape()
        {
            Game game = CreateSetupGame();

            bool sacrificed = game.TrySacrificeUnitToDrawActionCard(0, UnitShape.Triangle);

            PlayerPublicState state = game.Players[0].ToPublicState();
            Assert.That(sacrificed, Is.False);
            Assert.That(state.UnitCounts[UnitShape.Triangle], Is.EqualTo(0));
            Assert.That(state.ActionCardCount, Is.EqualTo(0));
            Assert.That(game.ActionDeck.Count, Is.EqualTo(50));
        }

        [Test]
        public void ActivePlayerCanPassActionPhase()
        {
            Game game = CreateSetupGame();

            bool passed = game.TryPassActionPhase(0);

            Assert.That(passed, Is.True);
            Assert.That(game.GetActionPhaseChoice(0), Is.EqualTo(ActionPhaseChoice.Pass));
        }

        [Test]
        public void ActivePlayerCanPlayExactlyOneNonRaidActionCard()
        {
            Game game = CreateActionCardGame(
                CreatePlayer(
                    0,
                    "Player 1",
                    actionCards: new ActionCardHand(new[] { ActionCardType.ResourceTheft })),
                CreatePlayer(
                    1,
                    "Player 2",
                    resourceCounts: new EnumCountSet<ResourceType>(
                        new Dictionary<ResourceType, int>
                        {
                            [ResourceType.Wood] = 1
                        })));

            bool played = game.TryPlayResourceTheft(0, 1, ResourceType.Wood);
            bool passed = game.TryPassActionPhase(0);
            bool startedBattleRoyale = game.TryStartBattleRoyaleActionPhase(0);

            Assert.That(played, Is.True);
            Assert.That(passed, Is.False);
            Assert.That(startedBattleRoyale, Is.False);
            Assert.That(game.GetActionPhaseChoice(0), Is.EqualTo(ActionPhaseChoice.ActionCard));
        }

        [Test]
        public void BattleRoyaleActionPhaseChoiceDoesNotResolveBattleRoyaleInPr004()
        {
            Game game = CreateSetupGame();

            bool started = game.TryStartBattleRoyaleActionPhase(0);
            bool playedActionCard = game.TryPlayResourceTheft(0, 1, ResourceType.Stone);

            PlayerPublicState state = game.Players[0].ToPublicState();
            Assert.That(started, Is.True);
            Assert.That(playedActionCard, Is.False);
            Assert.That(game.GetActionPhaseChoice(0), Is.EqualTo(ActionPhaseChoice.BattleRoyale));
            Assert.That(state.UnitCounts[UnitShape.Square], Is.EqualTo(3));
        }

        [Test]
        public void RaidBaseIsNotResolvedInPr004()
        {
            Game game = CreateActionCardGame(
                CreatePlayer(
                    0,
                    "Player 1",
                    unitCounts: new EnumCountSet<UnitShape>(
                        new Dictionary<UnitShape, int>
                        {
                            [UnitShape.Square] = 1
                        }),
                    actionCards: new ActionCardHand(new[] { ActionCardType.RaidBase })),
                CreatePlayer(1, "Player 2"));

            bool played = game.TryPlayRaidBase(0, 1, UnitShape.Square);

            Assert.That(played, Is.False);
            Assert.That(game.HasPendingAction, Is.False);
            Assert.That(game.Players[0].ToPublicState().ActionCardCount, Is.EqualTo(1));
            Assert.That(game.Players[0].ToPublicState().UnitCounts[UnitShape.Square], Is.EqualTo(1));
            Assert.That(game.ActionDeck.DiscardPileCount, Is.EqualTo(0));
        }

        [Test]
        public void ResourceTheftStealsExactlyOneChosenResource()
        {
            Game game = CreateActionCardGame(
                CreatePlayer(
                    0,
                    "Player 1",
                    actionCards: new ActionCardHand(new[] { ActionCardType.ResourceTheft })),
                CreatePlayer(
                    1,
                    "Player 2",
                    resourceCounts: new EnumCountSet<ResourceType>(
                        new Dictionary<ResourceType, int>
                        {
                            [ResourceType.Stone] = 2
                        })));

            bool played = game.TryPlayResourceTheft(0, 1, ResourceType.Stone);
            bool resolved = game.ResolvePendingAction();

            Assert.That(played, Is.True);
            Assert.That(resolved, Is.True);
            Assert.That(game.Players[0].ToPublicState().ResourceCounts[ResourceType.Stone], Is.EqualTo(1));
            Assert.That(game.Players[1].ToPublicState().ResourceCounts[ResourceType.Stone], Is.EqualTo(1));
        }

        [Test]
        public void ResourceTheftNoOpsWhenTargetLacksChosenResource()
        {
            Game game = CreateActionCardGame(
                CreatePlayer(
                    0,
                    "Player 1",
                    actionCards: new ActionCardHand(new[] { ActionCardType.ResourceTheft })),
                CreatePlayer(1, "Player 2"));

            bool played = game.TryPlayResourceTheft(0, 1, ResourceType.Wood);

            Assert.That(played, Is.False);
            Assert.That(game.HasPendingAction, Is.False);
            Assert.That(game.Players[0].ToPublicState().ActionCardCount, Is.EqualTo(1));
            Assert.That(game.ActionDeck.DiscardPileCount, Is.EqualTo(0));
        }

        [Test]
        public void ResourceTheftCardLeavesHandAndGoesToDiscardAfterResolution()
        {
            Game game = CreateActionCardGame(
                CreatePlayer(
                    0,
                    "Player 1",
                    actionCards: new ActionCardHand(new[] { ActionCardType.ResourceTheft })),
                CreatePlayer(
                    1,
                    "Player 2",
                    resourceCounts: new EnumCountSet<ResourceType>(
                        new Dictionary<ResourceType, int>
                        {
                            [ResourceType.Metal] = 1
                        })));

            bool played = game.TryPlayResourceTheft(0, 1, ResourceType.Metal);
            int handCountAfterPlay = game.Players[0].ToPublicState().ActionCardCount;
            bool resolved = game.ResolvePendingAction();

            Assert.That(played, Is.True);
            Assert.That(handCountAfterPlay, Is.EqualTo(0));
            Assert.That(resolved, Is.True);
            Assert.That(game.ActionDeck.DiscardPileCount, Is.EqualTo(1));
            Assert.That(game.ActionDeck.CountOfDiscard(ActionCardType.ResourceTheft), Is.EqualTo(1));
        }

        [Test]
        public void ResourceTheftCannotBeDefendedWithUnits()
        {
            Game game = CreateActionCardGame(
                CreatePlayer(
                    0,
                    "Player 1",
                    actionCards: new ActionCardHand(new[] { ActionCardType.ResourceTheft })),
                CreatePlayer(
                    1,
                    "Player 2",
                    unitCounts: new EnumCountSet<UnitShape>(
                        new Dictionary<UnitShape, int>
                        {
                            [UnitShape.Square] = 3
                        }),
                    resourceCounts: new EnumCountSet<ResourceType>(
                        new Dictionary<ResourceType, int>
                        {
                            [ResourceType.Wood] = 1
                        })));

            bool played = game.TryPlayResourceTheft(0, 1, ResourceType.Wood);
            bool defended = game.TryDefendPendingActionWithUnits(1, UnitShape.Square, 1);
            bool resolved = game.ResolvePendingAction();

            Assert.That(played, Is.True);
            Assert.That(defended, Is.False);
            Assert.That(resolved, Is.True);
            Assert.That(game.Players[1].ToPublicState().UnitCounts[UnitShape.Square], Is.EqualTo(3));
            Assert.That(game.Players[1].ToPublicState().ResourceCounts[ResourceType.Wood], Is.EqualTo(0));
        }

        [Test]
        public void UnitKillDestroysExactlyOneChosenTargetUnit()
        {
            Game game = CreateActionCardGame(
                CreatePlayer(
                    0,
                    "Player 1",
                    actionCards: new ActionCardHand(new[] { ActionCardType.UnitKill })),
                CreatePlayer(
                    1,
                    "Player 2",
                    unitCounts: new EnumCountSet<UnitShape>(
                        new Dictionary<UnitShape, int>
                        {
                            [UnitShape.Circle] = 2
                        })));

            bool played = game.TryPlayUnitKill(0, 1, UnitShape.Circle);
            bool resolved = game.ResolvePendingAction();

            Assert.That(played, Is.True);
            Assert.That(resolved, Is.True);
            Assert.That(game.Players[1].ToPublicState().UnitCounts[UnitShape.Circle], Is.EqualTo(1));
        }

        [Test]
        public void UnitKillNoOpsWhenTargetLacksChosenUnit()
        {
            Game game = CreateActionCardGame(
                CreatePlayer(
                    0,
                    "Player 1",
                    actionCards: new ActionCardHand(new[] { ActionCardType.UnitKill })),
                CreatePlayer(1, "Player 2"));

            bool played = game.TryPlayUnitKill(0, 1, UnitShape.Triangle);

            Assert.That(played, Is.False);
            Assert.That(game.HasPendingAction, Is.False);
            Assert.That(game.Players[0].ToPublicState().ActionCardCount, Is.EqualTo(1));
            Assert.That(game.ActionDeck.DiscardPileCount, Is.EqualTo(0));
        }

        [Test]
        public void UnitKillCardLeavesHandAndGoesToDiscardAfterResolution()
        {
            Game game = CreateActionCardGame(
                CreatePlayer(
                    0,
                    "Player 1",
                    actionCards: new ActionCardHand(new[] { ActionCardType.UnitKill })),
                CreatePlayer(
                    1,
                    "Player 2",
                    unitCounts: new EnumCountSet<UnitShape>(
                        new Dictionary<UnitShape, int>
                        {
                            [UnitShape.Triangle] = 1
                        })));

            bool played = game.TryPlayUnitKill(0, 1, UnitShape.Triangle);
            int handCountAfterPlay = game.Players[0].ToPublicState().ActionCardCount;
            bool resolved = game.ResolvePendingAction();

            Assert.That(played, Is.True);
            Assert.That(handCountAfterPlay, Is.EqualTo(0));
            Assert.That(resolved, Is.True);
            Assert.That(game.ActionDeck.DiscardPileCount, Is.EqualTo(1));
            Assert.That(game.ActionDeck.CountOfDiscard(ActionCardType.UnitKill), Is.EqualTo(1));
        }

        [Test]
        public void UnitKillCannotBeDefendedWithUnits()
        {
            Game game = CreateActionCardGame(
                CreatePlayer(
                    0,
                    "Player 1",
                    actionCards: new ActionCardHand(new[] { ActionCardType.UnitKill })),
                CreatePlayer(
                    1,
                    "Player 2",
                    unitCounts: new EnumCountSet<UnitShape>(
                        new Dictionary<UnitShape, int>
                        {
                            [UnitShape.Square] = 2
                        })));

            bool played = game.TryPlayUnitKill(0, 1, UnitShape.Square);
            bool defended = game.TryDefendPendingActionWithUnits(1, UnitShape.Square, 1);
            bool resolved = game.ResolvePendingAction();

            Assert.That(played, Is.True);
            Assert.That(defended, Is.False);
            Assert.That(resolved, Is.True);
            Assert.That(game.Players[1].ToPublicState().UnitCounts[UnitShape.Square], Is.EqualTo(1));
        }

        [Test]
        public void CounterCanStopResourceTheft()
        {
            Game game = CreateActionCardGame(
                CreatePlayer(
                    0,
                    "Player 1",
                    actionCards: new ActionCardHand(new[] { ActionCardType.ResourceTheft })),
                CreatePlayer(
                    1,
                    "Player 2",
                    resourceCounts: new EnumCountSet<ResourceType>(
                        new Dictionary<ResourceType, int>
                        {
                            [ResourceType.Wood] = 1
                        }),
                    actionCards: new ActionCardHand(new[] { ActionCardType.Counter })));

            bool played = game.TryPlayResourceTheft(0, 1, ResourceType.Wood);
            bool countered = game.TryRespondWithCounter(1);
            bool resolved = game.ResolvePendingAction();

            Assert.That(played, Is.True);
            Assert.That(countered, Is.True);
            Assert.That(resolved, Is.False);
            Assert.That(game.Players[0].ToPublicState().ResourceCounts[ResourceType.Wood], Is.EqualTo(0));
            Assert.That(game.Players[1].ToPublicState().ResourceCounts[ResourceType.Wood], Is.EqualTo(1));
            Assert.That(game.ActionDeck.CountOfDiscard(ActionCardType.ResourceTheft), Is.EqualTo(1));
            Assert.That(game.ActionDeck.CountOfDiscard(ActionCardType.Counter), Is.EqualTo(1));
        }

        [Test]
        public void CounterCanStopUnitKill()
        {
            Game game = CreateActionCardGame(
                CreatePlayer(
                    0,
                    "Player 1",
                    actionCards: new ActionCardHand(new[] { ActionCardType.UnitKill })),
                CreatePlayer(
                    1,
                    "Player 2",
                    unitCounts: new EnumCountSet<UnitShape>(
                        new Dictionary<UnitShape, int>
                        {
                            [UnitShape.Circle] = 1
                        }),
                    actionCards: new ActionCardHand(new[] { ActionCardType.Counter })));

            bool played = game.TryPlayUnitKill(0, 1, UnitShape.Circle);
            bool countered = game.TryRespondWithCounter(1);
            bool resolved = game.ResolvePendingAction();

            Assert.That(played, Is.True);
            Assert.That(countered, Is.True);
            Assert.That(resolved, Is.False);
            Assert.That(game.Players[1].ToPublicState().UnitCounts[UnitShape.Circle], Is.EqualTo(1));
        }

        [Test]
        public void CounterCannotBePlayedAsActiveActionPhaseCard()
        {
            Game game = CreateActionCardGame(
                CreatePlayer(
                    0,
                    "Player 1",
                    actionCards: new ActionCardHand(new[] { ActionCardType.Counter })),
                CreatePlayer(1, "Player 2"));

            bool played = game.TryPlayCounterAsActionPhaseCard(0);

            Assert.That(played, Is.False);
            Assert.That(game.Players[0].ToPublicState().ActionCardCount, Is.EqualTo(1));
            Assert.That(game.ActionDeck.DiscardPileCount, Is.EqualTo(0));
        }

        [Test]
        public void CounterCanRespondToAnotherCounter()
        {
            Game game = CreateActionCardGame(
                CreatePlayer(
                    0,
                    "Player 1",
                    actionCards: new ActionCardHand(new[] { ActionCardType.ResourceTheft, ActionCardType.Counter })),
                CreatePlayer(
                    1,
                    "Player 2",
                    resourceCounts: new EnumCountSet<ResourceType>(
                        new Dictionary<ResourceType, int>
                        {
                            [ResourceType.Stone] = 1
                        }),
                    actionCards: new ActionCardHand(new[] { ActionCardType.Counter })));

            bool played = game.TryPlayResourceTheft(0, 1, ResourceType.Stone);
            bool firstCounter = game.TryRespondWithCounter(1);
            bool secondCounter = game.TryRespondWithCounter(0);
            bool resolved = game.ResolvePendingAction();

            Assert.That(played, Is.True);
            Assert.That(firstCounter, Is.True);
            Assert.That(secondCounter, Is.True);
            Assert.That(resolved, Is.True);
            Assert.That(game.Players[0].ToPublicState().ResourceCounts[ResourceType.Stone], Is.EqualTo(1));
            Assert.That(game.Players[1].ToPublicState().ResourceCounts[ResourceType.Stone], Is.EqualTo(0));
            Assert.That(game.ActionDeck.CountOfDiscard(ActionCardType.Counter), Is.EqualTo(2));
        }

        [Test]
        public void OddNumberOfCountersStopsOriginalAction()
        {
            Game game = CreateActionCardGame(
                CreatePlayer(
                    0,
                    "Player 1",
                    actionCards: new ActionCardHand(new[] { ActionCardType.ResourceTheft })),
                CreatePlayer(
                    1,
                    "Player 2",
                    resourceCounts: new EnumCountSet<ResourceType>(
                        new Dictionary<ResourceType, int>
                        {
                            [ResourceType.Metal] = 1
                        }),
                    actionCards: new ActionCardHand(new[] { ActionCardType.Counter })));

            game.TryPlayResourceTheft(0, 1, ResourceType.Metal);
            game.TryRespondWithCounter(1);
            bool resolved = game.ResolvePendingAction();

            Assert.That(resolved, Is.False);
            Assert.That(game.Players[0].ToPublicState().ResourceCounts[ResourceType.Metal], Is.EqualTo(0));
            Assert.That(game.Players[1].ToPublicState().ResourceCounts[ResourceType.Metal], Is.EqualTo(1));
        }

        [Test]
        public void EvenNumberOfCountersAllowsOriginalAction()
        {
            Game game = CreateActionCardGame(
                CreatePlayer(
                    0,
                    "Player 1",
                    actionCards: new ActionCardHand(new[] { ActionCardType.UnitKill, ActionCardType.Counter })),
                CreatePlayer(
                    1,
                    "Player 2",
                    unitCounts: new EnumCountSet<UnitShape>(
                        new Dictionary<UnitShape, int>
                        {
                            [UnitShape.Square] = 1
                        }),
                    actionCards: new ActionCardHand(new[] { ActionCardType.Counter })));

            game.TryPlayUnitKill(0, 1, UnitShape.Square);
            game.TryRespondWithCounter(1);
            game.TryRespondWithCounter(0);
            bool resolved = game.ResolvePendingAction();

            Assert.That(resolved, Is.True);
            Assert.That(game.Players[1].ToPublicState().UnitCounts[UnitShape.Square], Is.EqualTo(0));
            Assert.That(game.ActionDeck.CountOfDiscard(ActionCardType.Counter), Is.EqualTo(2));
        }

        [Test]
        public void PublicActionCardCountsUpdateAndIdentitiesRemainPrivateAfterActionAndCounters()
        {
            Game game = CreateActionCardGame(
                CreatePlayer(
                    0,
                    "Player 1",
                    actionCards: new ActionCardHand(new[] { ActionCardType.ResourceTheft })),
                CreatePlayer(
                    1,
                    "Player 2",
                    resourceCounts: new EnumCountSet<ResourceType>(
                        new Dictionary<ResourceType, int>
                        {
                            [ResourceType.Wood] = 1
                        }),
                    actionCards: new ActionCardHand(new[] { ActionCardType.Counter })));

            game.TryPlayResourceTheft(0, 1, ResourceType.Wood);
            game.TryRespondWithCounter(1);

            Assert.That(game.Players[0].ToPublicState().ActionCardCount, Is.EqualTo(0));
            Assert.That(game.Players[1].ToPublicState().ActionCardCount, Is.EqualTo(0));
            Assert.That(HasPublicActionCardIdentityProperty(typeof(PlayerPublicState)), Is.False);
        }

        private static Game CreateGame(int playerCount)
        {
            return new Game(Enumerable.Range(0, playerCount).Select(index => CreatePlayer(index, $"Player {index + 1}")));
        }

        private static Game CreateActionCardGame(Player playerOne, Player playerTwo)
        {
            return new Game(new[] { playerOne, playerTwo }, new ActionCardDeck(Array.Empty<ActionCardType>()));
        }

        private static Player CreatePlayer(
            int index,
            string name,
            Base? playerBase = null,
            EnumCountSet<UnitShape>? unitCounts = null,
            EnumCountSet<ResourceType>? resourceCounts = null,
            ActionCardHand? actionCards = null)
        {
            return new Player(
                index,
                name,
                playerBase ?? new Base(BaseType.Wood, 3),
                unitCounts,
                resourceCounts,
                actionCards);
        }

        private static Game CreateSetupGame()
        {
            return Game.CreateNew(new[] { "Player 1", "Player 2" });
        }

        private static bool HasPublicActionCardIdentityProperty(Type type)
        {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Any(property =>
                    property.PropertyType == typeof(ActionCardType) ||
                    property.PropertyType != typeof(string) &&
                    typeof(IEnumerable<ActionCardType>).IsAssignableFrom(property.PropertyType));
        }
    }
}

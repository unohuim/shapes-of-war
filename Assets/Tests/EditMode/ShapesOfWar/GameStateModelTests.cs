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
        public void PrivateActionCardHandCanBeRequestedForAPlayer()
        {
            Game game = new Game(new[]
            {
                CreatePlayer(
                    0,
                    "Player 1",
                    actionCards: new ActionCardHand(new[] { ActionCardType.Counter, ActionCardType.RaidBase })),
                CreatePlayer(
                    1,
                    "Player 2",
                    actionCards: new ActionCardHand(new[] { ActionCardType.UnitKill }))
            });

            IReadOnlyList<ActionCardType> hand = game.GetPrivateActionCardHand(0);

            Assert.That(hand, Is.EqualTo(new[] { ActionCardType.Counter, ActionCardType.RaidBase }));
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
                                [UnitShape.Triangle] = 1
                            })),
                    CreatePlayer(1, "Player 2")
                },
                new ActionCardDeck(Array.Empty<ActionCardType>()));

            bool sacrificed = game.TrySacrificeUnitToDrawActionCard(0, UnitShape.Triangle);

            PlayerPublicState state = game.Players[0].ToPublicState();
            Assert.That(sacrificed, Is.False);
            Assert.That(state.UnitCounts[UnitShape.Triangle], Is.EqualTo(1));
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
                                [UnitShape.Triangle] = 1
                            })),
                    CreatePlayer(1, "Player 2")
                },
                new ActionCardDeck(Array.Empty<ActionCardType>()));

            bool sacrificed = game.TrySacrificeUnitForActionCard(0, UnitShape.Triangle);

            PlayerPublicState state = game.Players[0].ToPublicState();
            Assert.That(sacrificed, Is.False);
            Assert.That(state.UnitCounts[UnitShape.Triangle], Is.EqualTo(1));
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
                            [ResourceType.Metal] = 1
                        })),
                CreatePlayer(1, "Player 2")
            });

            bool bought = game.TryBuyUnit(0, UnitShape.Triangle);
            bool sacrificed = game.TrySacrificeUnitToDrawActionCard(0, UnitShape.Triangle);

            PlayerPublicState state = game.Players[0].ToPublicState();
            Assert.That(bought, Is.True);
            Assert.That(sacrificed, Is.True);
            Assert.That(state.UnitCounts[UnitShape.Triangle], Is.EqualTo(0));
            Assert.That(state.ActionCardCount, Is.EqualTo(1));
        }

        [Test]
        public void StoneCanBeExchangedForTwoWood()
        {
            Game game = new Game(new[]
            {
                CreatePlayer(0, "Player 1", resourceCounts: Resources(ResourceType.Stone, 1)),
                CreatePlayer(1, "Player 2")
            });

            bool exchanged = game.TryExchangeStoneForWood(0);

            PlayerPublicState state = game.Players[0].ToPublicState();
            Assert.That(exchanged, Is.True);
            Assert.That(state.ResourceCounts[ResourceType.Stone], Is.EqualTo(0));
            Assert.That(state.ResourceCounts[ResourceType.Wood], Is.EqualTo(2));
        }

        [Test]
        public void StoneExchangeFailsWithoutEnoughStone()
        {
            Game game = CreateSetupGame();

            bool exchanged = game.TryExchangeStoneForWood(0);

            PlayerPublicState state = game.Players[0].ToPublicState();
            Assert.That(exchanged, Is.False);
            Assert.That(state.ResourceCounts[ResourceType.Stone], Is.EqualTo(0));
            Assert.That(state.ResourceCounts[ResourceType.Wood], Is.EqualTo(0));
        }

        [Test]
        public void MetalCanBeExchangedForThreeWood()
        {
            Game game = new Game(new[]
            {
                CreatePlayer(0, "Player 1", resourceCounts: Resources(ResourceType.Metal, 1)),
                CreatePlayer(1, "Player 2")
            });

            bool exchanged = game.TryExchangeMetalForWood(0);

            PlayerPublicState state = game.Players[0].ToPublicState();
            Assert.That(exchanged, Is.True);
            Assert.That(state.ResourceCounts[ResourceType.Metal], Is.EqualTo(0));
            Assert.That(state.ResourceCounts[ResourceType.Wood], Is.EqualTo(3));
        }

        [Test]
        public void MetalCanBeExchangedForStoneAndWood()
        {
            Game game = new Game(new[]
            {
                CreatePlayer(0, "Player 1", resourceCounts: Resources(ResourceType.Metal, 1)),
                CreatePlayer(1, "Player 2")
            });

            bool exchanged = game.TryExchangeMetalForStoneAndWood(0);

            PlayerPublicState state = game.Players[0].ToPublicState();
            Assert.That(exchanged, Is.True);
            Assert.That(state.ResourceCounts[ResourceType.Metal], Is.EqualTo(0));
            Assert.That(state.ResourceCounts[ResourceType.Stone], Is.EqualTo(1));
            Assert.That(state.ResourceCounts[ResourceType.Wood], Is.EqualTo(1));
        }

        [Test]
        public void MetalExchangeFailsWithoutEnoughMetal()
        {
            Game game = CreateSetupGame();

            bool exchangedForWood = game.TryExchangeMetalForWood(0);
            bool exchangedForStoneAndWood = game.TryExchangeMetalForStoneAndWood(0);

            PlayerPublicState state = game.Players[0].ToPublicState();
            Assert.That(exchangedForWood, Is.False);
            Assert.That(exchangedForStoneAndWood, Is.False);
            Assert.That(state.ResourceCounts[ResourceType.Metal], Is.EqualTo(0));
            Assert.That(state.ResourceCounts[ResourceType.Stone], Is.EqualTo(0));
            Assert.That(state.ResourceCounts[ResourceType.Wood], Is.EqualTo(0));
        }

        [Test]
        public void ResourceExchangeCanHappenAfterResourceCollection()
        {
            Game game = CreateSetupGame();

            game.CollectResources(0);
            bool exchanged = game.TryExchangeStoneForWood(0);

            PlayerPublicState state = game.Players[0].ToPublicState();
            Assert.That(exchanged, Is.True);
            Assert.That(state.ResourceCounts[ResourceType.Stone], Is.EqualTo(2));
            Assert.That(state.ResourceCounts[ResourceType.Wood], Is.EqualTo(2));
        }

        [Test]
        public void ResourceExchangeCanHappenBeforeBuyingAUnit()
        {
            Game game = new Game(new[]
            {
                CreatePlayer(0, "Player 1", resourceCounts: Resources(ResourceType.Stone, 1)),
                CreatePlayer(1, "Player 2")
            });

            bool exchanged = game.TryExchangeStoneForWood(0);
            bool bought = game.TryBuyUnit(0, UnitShape.Circle);

            PlayerPublicState state = game.Players[0].ToPublicState();
            Assert.That(exchanged, Is.True);
            Assert.That(bought, Is.True);
            Assert.That(state.ResourceCounts[ResourceType.Wood], Is.EqualTo(1));
            Assert.That(state.UnitCounts[UnitShape.Circle], Is.EqualTo(1));
        }

        [Test]
        public void ResourceExchangeCanHappenMultipleTimesInTheSameSpendPhase()
        {
            Game game = new Game(new[]
            {
                CreatePlayer(0, "Player 1", resourceCounts: Resources(ResourceType.Stone, 2)),
                CreatePlayer(1, "Player 2")
            });

            bool firstExchange = game.TryExchangeStoneForWood(0);
            bool secondExchange = game.TryExchangeStoneForWood(0);

            PlayerPublicState state = game.Players[0].ToPublicState();
            Assert.That(firstExchange, Is.True);
            Assert.That(secondExchange, Is.True);
            Assert.That(state.ResourceCounts[ResourceType.Stone], Is.EqualTo(0));
            Assert.That(state.ResourceCounts[ResourceType.Wood], Is.EqualTo(4));
        }

        [Test]
        public void ResourceExchangeCanHappenBeforeBaseUpgrade()
        {
            Game game = new Game(new[]
            {
                CreatePlayer(
                    0,
                    "Player 1",
                    resourceCounts: new EnumCountSet<ResourceType>(
                        new Dictionary<ResourceType, int>
                        {
                            [ResourceType.Stone] = 1,
                            [ResourceType.Metal] = 1
                        })),
                CreatePlayer(1, "Player 2")
            });

            bool exchanged = game.TryExchangeMetalForStoneAndWood(0);
            bool upgraded = game.TryUpgradeBase(0, BaseType.Stone);

            PlayerPublicState state = game.Players[0].ToPublicState();
            Assert.That(exchanged, Is.True);
            Assert.That(upgraded, Is.True);
            Assert.That(state.BaseType, Is.EqualTo(BaseType.Stone));
            Assert.That(state.ResourceCounts[ResourceType.Stone], Is.EqualTo(0));
            Assert.That(state.ResourceCounts[ResourceType.Wood], Is.EqualTo(1));
            Assert.That(state.ResourceCounts[ResourceType.Metal], Is.EqualTo(0));
        }

        [Test]
        public void ResourceExchangeCanHappenBeforeTriangleSacrifice()
        {
            Game game = new Game(new[]
            {
                CreatePlayer(
                    0,
                    "Player 1",
                    unitCounts: Units(UnitShape.Triangle, 1),
                    resourceCounts: Resources(ResourceType.Stone, 1)),
                CreatePlayer(1, "Player 2")
            });

            bool exchanged = game.TryExchangeStoneForWood(0);
            bool sacrificed = game.TrySacrificeUnitToDrawActionCard(0, UnitShape.Triangle);

            PlayerPublicState state = game.Players[0].ToPublicState();
            Assert.That(exchanged, Is.True);
            Assert.That(sacrificed, Is.True);
            Assert.That(state.ResourceCounts[ResourceType.Wood], Is.EqualTo(2));
            Assert.That(state.UnitCounts[UnitShape.Triangle], Is.EqualTo(0));
            Assert.That(state.ActionCardCount, Is.EqualTo(1));
        }

        [Test]
        public void ResourceExchangeDoesNotAllowTradingUp()
        {
            Game game = new Game(new[]
            {
                CreatePlayer(0, "Player 1", resourceCounts: Resources(ResourceType.Wood, 3)),
                CreatePlayer(1, "Player 2")
            });

            bool boughtTriangle = game.TryBuyUnit(0, UnitShape.Triangle);
            bool upgradedToStone = game.TryUpgradeBase(0, BaseType.Stone);

            PlayerPublicState state = game.Players[0].ToPublicState();
            Assert.That(boughtTriangle, Is.False);
            Assert.That(upgradedToStone, Is.False);
            Assert.That(state.ResourceCounts[ResourceType.Wood], Is.EqualTo(3));
            Assert.That(state.ResourceCounts[ResourceType.Stone], Is.EqualTo(0));
            Assert.That(state.ResourceCounts[ResourceType.Metal], Is.EqualTo(0));
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
            Game game = new Game(new[]
            {
                CreatePlayer(0, "Player 1", unitCounts: Units(UnitShape.Triangle, 1)),
                CreatePlayer(1, "Player 2")
            });

            bool sacrificed = game.TrySacrificeUnitToDrawActionCard(0, UnitShape.Triangle);

            PlayerPublicState state = game.Players[0].ToPublicState();
            Assert.That(sacrificed, Is.True);
            Assert.That(state.UnitCounts[UnitShape.Triangle], Is.EqualTo(0));
            Assert.That(state.ActionCardCount, Is.EqualTo(1));
            Assert.That(game.ActionDeck.Count, Is.EqualTo(49));
        }

        [TestCase(UnitShape.Square)]
        [TestCase(UnitShape.Circle)]
        public void OnlyTrianglesCanBeSacrificedForActionCards(UnitShape unitShape)
        {
            Game game = new Game(new[]
            {
                CreatePlayer(0, "Player 1", unitCounts: Units(unitShape, 1)),
                CreatePlayer(1, "Player 2")
            });

            bool sacrificed = game.TrySacrificeUnitToDrawActionCard(0, unitShape);

            PlayerPublicState state = game.Players[0].ToPublicState();
            Assert.That(sacrificed, Is.False);
            Assert.That(state.UnitCounts[unitShape], Is.EqualTo(1));
            Assert.That(state.ActionCardCount, Is.EqualTo(0));
            Assert.That(game.ActionDeck.Count, Is.EqualTo(50));
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
        public void ActionPhaseChoiceCanBeResetForANewTurn()
        {
            Game game = CreateSetupGame();

            game.TryPassActionPhase(0);
            bool reset = game.TryResetActionPhaseChoiceForNextTurn(0);

            Assert.That(reset, Is.True);
            Assert.That(game.GetActionPhaseChoice(0), Is.EqualTo(ActionPhaseChoice.None));
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
        public void BattleRoyaleActionPhaseChoiceStartsPendingBattleRoyale()
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
                    actionCards: new ActionCardHand(new[] { ActionCardType.ResourceTheft })),
                CreatePlayer(
                    1,
                    "Player 2",
                    resourceCounts: new EnumCountSet<ResourceType>(
                        new Dictionary<ResourceType, int>
                        {
                            [ResourceType.Stone] = 1
                        })));

            bool started = game.TryStartBattleRoyale(0, UnitShape.Square);
            bool playedActionCard = game.TryPlayResourceTheft(0, 1, ResourceType.Stone);

            PlayerPublicState state = game.Players[0].ToPublicState();
            Assert.That(started, Is.True);
            Assert.That(playedActionCard, Is.False);
            Assert.That(game.GetActionPhaseChoice(0), Is.EqualTo(ActionPhaseChoice.BattleRoyale));
            Assert.That(game.HasPendingBattleRoyale, Is.True);
            Assert.That(state.UnitCounts[UnitShape.Square], Is.EqualTo(0));
        }

        [Test]
        public void ActivePlayerCanPlayRaidBaseDuringActionPhase()
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

            Assert.That(played, Is.True);
            Assert.That(game.HasPendingAction, Is.True);
            Assert.That(game.GetActionPhaseChoice(0), Is.EqualTo(ActionPhaseChoice.ActionCard));
            Assert.That(game.Players[0].ToPublicState().ActionCardCount, Is.EqualTo(0));
            Assert.That(game.Players[0].ToPublicState().UnitCounts[UnitShape.Square], Is.EqualTo(1));
        }

        [Test]
        public void RaidBaseCannotBePlayedWithoutRaidBaseCard()
        {
            Game game = CreateActionCardGame(
                CreatePlayer(
                    0,
                    "Player 1",
                    unitCounts: new EnumCountSet<UnitShape>(
                        new Dictionary<UnitShape, int>
                        {
                            [UnitShape.Square] = 1
                        })),
                CreatePlayer(1, "Player 2"));

            bool played = game.TryPlayRaidBase(0, 1, UnitShape.Square);

            Assert.That(played, Is.False);
            Assert.That(game.HasPendingAction, Is.False);
            Assert.That(game.GetActionPhaseChoice(0), Is.EqualTo(ActionPhaseChoice.None));
        }

        [Test]
        public void RaidBaseCannotBePlayedWithoutRaidingUnit()
        {
            Game game = CreateActionCardGame(
                CreatePlayer(
                    0,
                    "Player 1",
                    actionCards: new ActionCardHand(new[] { ActionCardType.RaidBase })),
                CreatePlayer(1, "Player 2"));

            bool played = game.TryPlayRaidBase(0, 1, UnitShape.Triangle);

            Assert.That(played, Is.False);
            Assert.That(game.HasPendingAction, Is.False);
            Assert.That(game.Players[0].ToPublicState().ActionCardCount, Is.EqualTo(1));
        }

        [Test]
        public void RaidBaseCardGoesToDiscardAfterResolution()
        {
            Game game = CreateRaidGame(UnitShape.Square);

            bool played = game.TryPlayRaidBase(0, 1, UnitShape.Square);
            bool resolved = game.ResolvePendingAction();

            Assert.That(played, Is.True);
            Assert.That(resolved, Is.True);
            Assert.That(game.ActionDeck.CountOfDiscard(ActionCardType.RaidBase), Is.EqualTo(1));
        }

        [Test]
        public void RaidBaseCannotBeResolvedTwice()
        {
            Game game = CreateRaidGame(UnitShape.Square);

            game.TryPlayRaidBase(0, 1, UnitShape.Square);
            bool firstResolve = game.ResolvePendingAction();
            bool secondResolve = game.ResolvePendingAction();

            Assert.That(firstResolve, Is.True);
            Assert.That(secondResolve, Is.False);
        }

        [Test]
        public void CounterCanStopRaidBase()
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
                CreatePlayer(
                    1,
                    "Player 2",
                    actionCards: new ActionCardHand(new[] { ActionCardType.Counter })));

            bool played = game.TryPlayRaidBase(0, 1, UnitShape.Square);
            bool countered = game.TryRespondWithCounter(1);
            bool resolved = game.ResolvePendingAction();

            Assert.That(played, Is.True);
            Assert.That(countered, Is.True);
            Assert.That(resolved, Is.False);
            Assert.That(game.Players[1].ToPublicState().BasePoints, Is.EqualTo(3));
            Assert.That(game.Players[0].ToPublicState().UnitCounts[UnitShape.Square], Is.EqualTo(0));
            Assert.That(game.ActionDeck.CountOfDiscard(ActionCardType.RaidBase), Is.EqualTo(1));
            Assert.That(game.ActionDeck.CountOfDiscard(ActionCardType.Counter), Is.EqualTo(1));
        }

        [Test]
        public void CounterCanRespondToAnotherCounterDuringRaidBaseChain()
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
                    actionCards: new ActionCardHand(new[] { ActionCardType.RaidBase, ActionCardType.Counter })),
                CreatePlayer(
                    1,
                    "Player 2",
                    actionCards: new ActionCardHand(new[] { ActionCardType.Counter })));

            bool played = game.TryPlayRaidBase(0, 1, UnitShape.Square);
            bool firstCounter = game.TryRespondWithCounter(1);
            bool secondCounter = game.TryRespondWithCounter(0);
            bool resolved = game.ResolvePendingAction();

            Assert.That(played, Is.True);
            Assert.That(firstCounter, Is.True);
            Assert.That(secondCounter, Is.True);
            Assert.That(resolved, Is.True);
            Assert.That(game.Players[1].ToPublicState().BasePoints, Is.EqualTo(2));
            Assert.That(game.ActionDeck.CountOfDiscard(ActionCardType.Counter), Is.EqualTo(2));
        }

        [Test]
        public void OddNumberOfCountersStopsRaidBase()
        {
            Game game = CreateActionCardGame(
                CreatePlayer(
                    0,
                    "Player 1",
                    unitCounts: new EnumCountSet<UnitShape>(
                        new Dictionary<UnitShape, int>
                        {
                            [UnitShape.Circle] = 1
                        }),
                    actionCards: new ActionCardHand(new[] { ActionCardType.RaidBase })),
                CreatePlayer(
                    1,
                    "Player 2",
                    actionCards: new ActionCardHand(new[] { ActionCardType.Counter })));

            game.TryPlayRaidBase(0, 1, UnitShape.Circle);
            game.TryRespondWithCounter(1);
            bool resolved = game.ResolvePendingAction();

            Assert.That(resolved, Is.False);
            Assert.That(game.Players[1].ToPublicState().BasePoints, Is.EqualTo(3));
        }

        [Test]
        public void EvenNumberOfCountersAllowsRaidBaseToContinue()
        {
            Game game = CreateActionCardGame(
                CreatePlayer(
                    0,
                    "Player 1",
                    unitCounts: new EnumCountSet<UnitShape>(
                        new Dictionary<UnitShape, int>
                        {
                            [UnitShape.Circle] = 1
                        }),
                    actionCards: new ActionCardHand(new[] { ActionCardType.RaidBase, ActionCardType.Counter })),
                CreatePlayer(
                    1,
                    "Player 2",
                    actionCards: new ActionCardHand(new[] { ActionCardType.Counter })));

            game.TryPlayRaidBase(0, 1, UnitShape.Circle);
            game.TryRespondWithCounter(1);
            game.TryRespondWithCounter(0);
            bool resolved = game.ResolvePendingAction();

            Assert.That(resolved, Is.True);
            Assert.That(game.Players[1].ToPublicState().BasePoints, Is.EqualTo(2));
        }

        [Test]
        public void PublicActionCardCountsUpdateAfterRaidBaseAndCounters()
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
                CreatePlayer(
                    1,
                    "Player 2",
                    actionCards: new ActionCardHand(new[] { ActionCardType.Counter })));

            game.TryPlayRaidBase(0, 1, UnitShape.Square);
            game.TryRespondWithCounter(1);

            Assert.That(game.Players[0].ToPublicState().ActionCardCount, Is.EqualTo(0));
            Assert.That(game.Players[1].ToPublicState().ActionCardCount, Is.EqualTo(0));
            Assert.That(HasPublicActionCardIdentityProperty(typeof(PlayerPublicState)), Is.False);
        }

        [Test]
        public void SuccessfulRaidBaseDamagesBaseAndDiscardsRaidingUnit()
        {
            Game game = CreateRaidGame(UnitShape.Square);

            bool played = game.TryPlayRaidBase(0, 1, UnitShape.Square);
            bool resolved = game.ResolvePendingAction();

            Assert.That(played, Is.True);
            Assert.That(resolved, Is.True);
            Assert.That(game.Players[1].ToPublicState().BasePoints, Is.EqualTo(2));
            Assert.That(game.Players[0].ToPublicState().UnitCounts[UnitShape.Square], Is.EqualTo(0));
        }

        [Test]
        public void RaidBaseResolutionEliminatesTargetWhenBaseReachesZero()
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
                CreatePlayer(1, "Player 2", playerBase: new Base(BaseType.Wood, 1)));

            game.TryPlayRaidBase(0, 1, UnitShape.Square);
            bool resolved = game.ResolvePendingAction();

            Assert.That(resolved, Is.True);
            Assert.That(game.Players[1].ToPublicState().BasePoints, Is.EqualTo(0));
            Assert.That(game.Players[1].ToPublicState().IsEliminated, Is.True);
        }

        [TestCase(UnitShape.Triangle, UnitShape.Square, 2)]
        [TestCase(UnitShape.Triangle, UnitShape.Circle, 3)]
        [TestCase(UnitShape.Square, UnitShape.Triangle, 1)]
        [TestCase(UnitShape.Square, UnitShape.Circle, 2)]
        [TestCase(UnitShape.Circle, UnitShape.Triangle, 1)]
        [TestCase(UnitShape.Circle, UnitShape.Square, 1)]
        public void DefenderCanStopRaidBaseWithMinimumValidUnitGroup(
            UnitShape raidingUnitShape,
            UnitShape defendingUnitShape,
            int defendingUnitCount)
        {
            Game game = CreateRaidDefenseGame(raidingUnitShape, defendingUnitShape, defendingUnitCount);

            bool played = game.TryPlayRaidBase(0, 1, raidingUnitShape);
            bool defended = game.TryDefendPendingActionWithUnits(1, defendingUnitShape, defendingUnitCount);
            bool resolved = game.ResolvePendingAction();

            Assert.That(played, Is.True);
            Assert.That(defended, Is.True);
            Assert.That(resolved, Is.False);
            Assert.That(game.Players[1].ToPublicState().BasePoints, Is.EqualTo(3));
            Assert.That(game.Players[0].ToPublicState().UnitCounts[raidingUnitShape], Is.EqualTo(0));
            Assert.That(game.Players[1].ToPublicState().UnitCounts[defendingUnitShape], Is.EqualTo(0));
        }

        [Test]
        public void DefenderCannotDefendWithSameShapeMatchingUnits()
        {
            Game game = CreateRaidDefenseGame(UnitShape.Square, UnitShape.Square, 1);

            bool played = game.TryPlayRaidBase(0, 1, UnitShape.Square);
            bool defended = game.TryDefendPendingActionWithUnits(1, UnitShape.Square, 1);

            Assert.That(played, Is.True);
            Assert.That(defended, Is.False);
        }

        [Test]
        public void DefenderCannotDefendWithInsufficientUnits()
        {
            Game game = CreateRaidDefenseGame(UnitShape.Triangle, UnitShape.Square, 2);

            bool played = game.TryPlayRaidBase(0, 1, UnitShape.Triangle);
            bool defended = game.TryDefendPendingActionWithUnits(1, UnitShape.Square, 1);

            Assert.That(played, Is.True);
            Assert.That(defended, Is.False);
        }

        [Test]
        public void DefenderCannotOverCommitBeyondMinimumValidDefendingGroup()
        {
            Game game = CreateRaidDefenseGame(UnitShape.Circle, UnitShape.Square, 2);

            bool played = game.TryPlayRaidBase(0, 1, UnitShape.Circle);
            bool defended = game.TryDefendPendingActionWithUnits(1, UnitShape.Square, 2);

            Assert.That(played, Is.True);
            Assert.That(defended, Is.False);
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

        [Test]
        public void StartingBattleRoyaleRequiresStarterToOwnCommittedUnit()
        {
            Game game = CreateActionCardGame(CreatePlayer(0, "Player 1"), CreatePlayer(1, "Player 2"));

            bool started = game.TryStartBattleRoyale(0, UnitShape.Triangle);

            Assert.That(started, Is.False);
            Assert.That(game.HasPendingBattleRoyale, Is.False);
            Assert.That(game.GetActionPhaseChoice(0), Is.EqualTo(ActionPhaseChoice.None));
        }

        [Test]
        public void StartingBattleRoyaleCommitsExactlyOneStarterUnit()
        {
            Game game = CreateBattleRoyaleGame(
                CreatePlayer(0, "Player 1", unitCounts: Units(UnitShape.Square, 2)),
                CreatePlayer(1, "Player 2"));

            bool started = game.TryStartBattleRoyale(0, UnitShape.Square);

            Assert.That(started, Is.True);
            Assert.That(game.Players[0].ToPublicState().UnitCounts[UnitShape.Square], Is.EqualTo(1));
        }

        [Test]
        public void StartingBattleRoyaleRecordsStarterAsCurrentWinnerAndCommittedPlay()
        {
            Game game = CreateBattleRoyaleGame(
                CreatePlayer(0, "Player 1", unitCounts: Units(UnitShape.Circle, 1)),
                CreatePlayer(1, "Player 2"));

            bool started = game.TryStartBattleRoyale(0, UnitShape.Circle);

            Assert.That(started, Is.True);
            Assert.That(game.BattleRoyaleCurrentWinningPlayerIndex, Is.EqualTo(0));
            Assert.That(game.BattleRoyaleCurrentWinningShape, Is.EqualTo(UnitShape.Circle));
            Assert.That(game.BattleRoyaleCurrentWinningCount, Is.EqualTo(1));
            Assert.That(game.BattleRoyaleCurrentActingPlayerIndex, Is.EqualTo(1));
        }

        [Test]
        public void MixedShapeBattleRoyalePlaysAreRejected()
        {
            Game game = CreateBattleRoyaleGame(
                CreatePlayer(0, "Player 1", unitCounts: Units(UnitShape.Square, 1)),
                CreatePlayer(
                    1,
                    "Player 2",
                    unitCounts: new EnumCountSet<UnitShape>(
                        new Dictionary<UnitShape, int>
                        {
                            [UnitShape.Triangle] = 1,
                            [UnitShape.Square] = 1
                        })));

            game.TryStartBattleRoyale(0, UnitShape.Square);
            bool played = game.TryPlayBattleRoyaleUnits(
                1,
                new Dictionary<UnitShape, int>
                {
                    [UnitShape.Triangle] = 1,
                    [UnitShape.Square] = 1
                });

            Assert.That(played, Is.False);
            Assert.That(game.BattleRoyaleCurrentWinningPlayerIndex, Is.EqualTo(0));
        }

        [Test]
        public void SameShapeBattleRoyalePlaysAreRejectedEvenWithHigherCount()
        {
            Game game = CreateBattleRoyaleGame(
                CreatePlayer(0, "Player 1", unitCounts: Units(UnitShape.Square, 1)),
                CreatePlayer(1, "Player 2", unitCounts: Units(UnitShape.Square, 2)));

            game.TryStartBattleRoyale(0, UnitShape.Square);
            bool played = game.TryPlayBattleRoyaleUnits(1, UnitShape.Square, 2);

            Assert.That(played, Is.False);
            Assert.That(game.Players[1].ToPublicState().UnitCounts[UnitShape.Square], Is.EqualTo(2));
        }

        [Test]
        public void InsufficientBattleRoyalePlaysAreRejected()
        {
            Game game = CreateBattleRoyaleGame(
                CreatePlayer(0, "Player 1", unitCounts: Units(UnitShape.Triangle, 1)),
                CreatePlayer(1, "Player 2", unitCounts: Units(UnitShape.Square, 1)));

            game.TryStartBattleRoyale(0, UnitShape.Triangle);
            bool played = game.TryPlayBattleRoyaleUnits(1, UnitShape.Square, 1);

            Assert.That(played, Is.False);
            Assert.That(game.BattleRoyaleCurrentWinningPlayerIndex, Is.EqualTo(0));
            Assert.That(game.Players[1].ToPublicState().UnitCounts[UnitShape.Square], Is.EqualTo(1));
        }

        [TestCase(UnitShape.Square, UnitShape.Triangle, 1)]
        [TestCase(UnitShape.Circle, UnitShape.Triangle, 1)]
        [TestCase(UnitShape.Triangle, UnitShape.Square, 2)]
        [TestCase(UnitShape.Circle, UnitShape.Square, 1)]
        [TestCase(UnitShape.Square, UnitShape.Circle, 2)]
        [TestCase(UnitShape.Triangle, UnitShape.Circle, 3)]
        public void DocumentedBattleRoyaleCombatPlaysBeatCurrentWinningPlay(
            UnitShape startingShape,
            UnitShape challengingShape,
            int challengingCount)
        {
            Game game = CreateBattleRoyaleGame(
                CreatePlayer(0, "Player 1", unitCounts: Units(startingShape, 1)),
                CreatePlayer(1, "Player 2", unitCounts: Units(challengingShape, challengingCount)));

            game.TryStartBattleRoyale(0, startingShape);
            bool played = game.TryPlayBattleRoyaleUnits(1, challengingShape, challengingCount);

            Assert.That(played, Is.True);
            Assert.That(game.BattleRoyaleCurrentWinningPlayerIndex, Is.EqualTo(1));
            Assert.That(game.BattleRoyaleCurrentWinningShape, Is.EqualTo(challengingShape));
            Assert.That(game.BattleRoyaleCurrentWinningCount, Is.EqualTo(challengingCount));
        }

        [Test]
        public void PassingRemovesPlayerFromCurrentBattleRoyale()
        {
            Game game = CreateBattleRoyaleGame(
                CreatePlayer(0, "Player 1", unitCounts: Units(UnitShape.Square, 1)),
                CreatePlayer(1, "Player 2", unitCounts: Units(UnitShape.Triangle, 1)),
                CreatePlayer(2, "Player 3", unitCounts: Units(UnitShape.Triangle, 1)));

            game.TryStartBattleRoyale(0, UnitShape.Square);
            bool passed = game.TryPassBattleRoyale(1);
            bool rejoined = game.TryPlayBattleRoyaleUnits(1, UnitShape.Triangle, 1);

            Assert.That(passed, Is.True);
            Assert.That(rejoined, Is.False);
            Assert.That(game.Players[1].ToPublicState().UnitCounts[UnitShape.Triangle], Is.EqualTo(1));
        }

        [Test]
        public void IfEveryOtherPlayerPassesCurrentWinnerWins()
        {
            Game game = CreateBattleRoyaleGame(
                CreatePlayer(0, "Player 1", unitCounts: Units(UnitShape.Square, 1)),
                CreatePlayer(1, "Player 2"),
                CreatePlayer(2, "Player 3"));

            game.TryStartBattleRoyale(0, UnitShape.Square);
            bool firstPass = game.TryPassBattleRoyale(1);
            bool secondPass = game.TryPassBattleRoyale(2);

            Assert.That(firstPass, Is.True);
            Assert.That(secondPass, Is.True);
            Assert.That(game.HasPendingBattleRoyale, Is.False);
            Assert.That(game.Players[0].ToPublicState().ActionCardCount, Is.EqualTo(1));
            Assert.That(game.Players[0].ToPublicState().UnitCounts[UnitShape.Square], Is.EqualTo(1));
        }

        [Test]
        public void IfNobodyJoinsAfterStarterTheStarterWins()
        {
            Game game = CreateBattleRoyaleGame(
                CreatePlayer(0, "Player 1", unitCounts: Units(UnitShape.Circle, 1)),
                CreatePlayer(1, "Player 2"));

            game.TryStartBattleRoyale(0, UnitShape.Circle);
            bool passed = game.TryPassBattleRoyale(1);

            Assert.That(passed, Is.True);
            Assert.That(game.HasPendingBattleRoyale, Is.False);
            Assert.That(game.Players[0].ToPublicState().ActionCardCount, Is.EqualTo(1));
            Assert.That(game.Players[0].ToPublicState().UnitCounts[UnitShape.Circle], Is.EqualTo(1));
        }

        [Test]
        public void WinnerKeepsOneUnitAndDrawsOneActionCardAfterBattleRoyale()
        {
            Game game = CreateBattleRoyaleGame(
                CreatePlayer(0, "Player 1", unitCounts: Units(UnitShape.Triangle, 1)),
                CreatePlayer(1, "Player 2", unitCounts: Units(UnitShape.Square, 2)));

            game.TryStartBattleRoyale(0, UnitShape.Triangle);
            game.TryPlayBattleRoyaleUnits(1, UnitShape.Square, 2);
            bool passed = game.TryPassBattleRoyale(0);

            Assert.That(passed, Is.True);
            Assert.That(game.HasPendingBattleRoyale, Is.False);
            Assert.That(game.Players[1].ToPublicState().ActionCardCount, Is.EqualTo(1));
            Assert.That(game.Players[1].ToPublicState().UnitCounts[UnitShape.Square], Is.EqualTo(1));
        }

        [Test]
        public void AllNonWinningCommittedUnitsAreDiscardedAfterBattleRoyale()
        {
            Game game = CreateBattleRoyaleGame(
                CreatePlayer(0, "Player 1", unitCounts: Units(UnitShape.Square, 1)),
                CreatePlayer(1, "Player 2", unitCounts: Units(UnitShape.Triangle, 1)));

            game.TryStartBattleRoyale(0, UnitShape.Square);
            game.TryPlayBattleRoyaleUnits(1, UnitShape.Triangle, 1);
            game.TryPassBattleRoyale(0);

            Assert.That(game.Players[0].ToPublicState().UnitCounts[UnitShape.Square], Is.EqualTo(0));
            Assert.That(game.Players[1].ToPublicState().UnitCounts[UnitShape.Triangle], Is.EqualTo(1));
        }

        [Test]
        public void BattleRoyaleCannotStartAfterPlayerUsedActionPhaseChoice()
        {
            Game game = CreateBattleRoyaleGame(
                CreatePlayer(0, "Player 1", unitCounts: Units(UnitShape.Square, 1)),
                CreatePlayer(1, "Player 2"));

            bool passed = game.TryPassActionPhase(0);
            bool started = game.TryStartBattleRoyale(0, UnitShape.Square);

            Assert.That(passed, Is.True);
            Assert.That(started, Is.False);
            Assert.That(game.HasPendingBattleRoyale, Is.False);
        }

        [Test]
        public void BattleRoyaleStateClearsAfterResolution()
        {
            Game game = CreateBattleRoyaleGame(
                CreatePlayer(0, "Player 1", unitCounts: Units(UnitShape.Square, 1)),
                CreatePlayer(1, "Player 2"));

            game.TryStartBattleRoyale(0, UnitShape.Square);
            game.TryPassBattleRoyale(1);

            Assert.That(game.HasPendingBattleRoyale, Is.False);
            Assert.That(game.BattleRoyaleCurrentWinningPlayerIndex, Is.Null);
            Assert.That(game.BattleRoyaleCurrentWinningShape, Is.Null);
            Assert.That(game.BattleRoyaleCurrentWinningCount, Is.Null);
        }

        [Test]
        public void RaidBaseDamageThatReducesBaseToZeroEliminatesTarget()
        {
            Game game = CreateEliminationRaidGame();

            game.TryPlayRaidBase(0, 1, UnitShape.Square);
            bool resolved = game.ResolvePendingAction();

            Assert.That(resolved, Is.True);
            Assert.That(game.Players[1].ToPublicState().BasePoints, Is.EqualTo(0));
            Assert.That(game.Players[1].ToPublicState().IsEliminated, Is.True);
        }

        [Test]
        public void RaidBaseDamageAboveZeroDoesNotEliminateTarget()
        {
            Game game = CreateRaidGame(UnitShape.Square);

            game.TryPlayRaidBase(0, 1, UnitShape.Square);
            game.ResolvePendingAction();

            Assert.That(game.Players[1].ToPublicState().BasePoints, Is.EqualTo(2));
            Assert.That(game.Players[1].ToPublicState().IsEliminated, Is.False);
        }

        [Test]
        public void EliminatedPlayerHoldingsAreDiscarded()
        {
            Game game = CreateActionCardGame(
                CreatePlayer(
                    0,
                    "Player 1",
                    unitCounts: Units(UnitShape.Square, 1),
                    actionCards: new ActionCardHand(new[] { ActionCardType.RaidBase })),
                CreatePlayer(
                    1,
                    "Player 2",
                    playerBase: new Base(BaseType.Wood, 1),
                    unitCounts: Units(UnitShape.Circle, 2),
                    resourceCounts: Resources(ResourceType.Metal, 3),
                    actionCards: new ActionCardHand(new[] { ActionCardType.Counter, ActionCardType.UnitKill })));

            game.TryPlayRaidBase(0, 1, UnitShape.Square);
            game.ResolvePendingAction();

            PlayerPublicState eliminated = game.Players[1].ToPublicState();
            Assert.That(eliminated.UnitCounts[UnitShape.Circle], Is.EqualTo(0));
            Assert.That(eliminated.ResourceCounts[ResourceType.Metal], Is.EqualTo(0));
            Assert.That(eliminated.ActionCardCount, Is.EqualTo(0));
            // Eliminator reward draws may reshuffle eliminated cards out of the discard pile.
            Assert.That(game.Players[1].ActionCards.Cards, Is.Empty);
        }

        [Test]
        public void EliminatorDrawsOneActionCardAfterEliminationCleanup()
        {
            Game game = new Game(new[]
            {
                CreatePlayer(
                    0,
                    "Player 1",
                    unitCounts: Units(UnitShape.Square, 1),
                    actionCards: new ActionCardHand(new[] { ActionCardType.RaidBase })),
                CreatePlayer(
                    1,
                    "Player 2",
                    playerBase: new Base(BaseType.Wood, 1),
                    actionCards: new ActionCardHand(new[] { ActionCardType.Counter }))
            });

            game.TryPlayRaidBase(0, 1, UnitShape.Square);
            game.ResolvePendingAction();

            Assert.That(game.Players[0].ToPublicState().ActionCardCount, Is.EqualTo(1));
            Assert.That(game.ActionDeck.CountOfDiscard(ActionCardType.Counter), Is.EqualTo(1));
        }

        [Test]
        public void EliminatorRewardDrawNoOpsWhenDeckAndDiscardPileAreEmpty()
        {
            Game game = CreateEliminationRaidGame();

            game.TryPlayRaidBase(0, 1, UnitShape.Square);
            game.ResolvePendingAction();

            Assert.That(game.Players[0].ToPublicState().ActionCardCount, Is.EqualTo(0));
        }

        [Test]
        public void EliminatedPlayerCannotTakeLaterActions()
        {
            Game game = CreateActionCardGame(
                CreatePlayer(
                    0,
                    "Player 1",
                    unitCounts: Units(UnitShape.Square, 1),
                    resourceCounts: Resources(ResourceType.Stone, 2),
                    actionCards: new ActionCardHand(new[] { ActionCardType.RaidBase })),
                CreatePlayer(
                    1,
                    "Player 2",
                    playerBase: new Base(BaseType.Wood, 1),
                    unitCounts: Units(UnitShape.Square, 1),
                    resourceCounts: Resources(ResourceType.Stone, 2),
                    actionCards: new ActionCardHand(new[] { ActionCardType.ResourceTheft })));

            game.TryPlayRaidBase(0, 1, UnitShape.Square);
            game.ResolvePendingAction();

            Assert.Throws<InvalidOperationException>(() => game.TryStartBattleRoyale(1, UnitShape.Square));
            Assert.Throws<InvalidOperationException>(() => game.TryPlayResourceTheft(1, 0, ResourceType.Stone));
            Assert.Throws<InvalidOperationException>(() => game.TryBuyUnit(1, UnitShape.Square));
            Assert.Throws<InvalidOperationException>(() => game.CollectResources(1));
            Assert.Throws<InvalidOperationException>(() => game.TryUpgradeBase(1, BaseType.Stone));
            Assert.Throws<InvalidOperationException>(() => game.TrySacrificeUnitToDrawActionCard(1, UnitShape.Square));
        }

        [Test]
        public void GameDoesNotEndWhileMoreThanOnePlayerRemainsActive()
        {
            Game game = new Game(new[]
            {
                CreatePlayer(
                    0,
                    "Player 1",
                    unitCounts: Units(UnitShape.Square, 1),
                    actionCards: new ActionCardHand(new[] { ActionCardType.RaidBase })),
                CreatePlayer(1, "Player 2", playerBase: new Base(BaseType.Wood, 1)),
                CreatePlayer(2, "Player 3")
            });

            game.TryPlayRaidBase(0, 1, UnitShape.Square);
            game.ResolvePendingAction();

            Assert.That(game.IsGameOver, Is.False);
            Assert.That(game.WinningPlayerIndex, Is.Null);
        }

        [Test]
        public void GameEndsWhenOnlyOnePlayerRemainsAndRecordsWinner()
        {
            Game game = CreateEliminationRaidGame();

            game.TryPlayRaidBase(0, 1, UnitShape.Square);
            game.ResolvePendingAction();

            Assert.That(game.IsGameOver, Is.True);
            Assert.That(game.WinningPlayerIndex, Is.EqualTo(0));
        }

        [Test]
        public void EliminatingDownToOnePlayerEndsGame()
        {
            Game game = new Game(new[]
            {
                CreatePlayer(
                    0,
                    "Player 1",
                    unitCounts: Units(UnitShape.Square, 2),
                    actionCards: new ActionCardHand(new[] { ActionCardType.RaidBase })),
                CreatePlayer(1, "Player 2", playerBase: new Base(BaseType.Wood, 1)),
                CreatePlayer(2, "Player 3", playerBase: new Base(BaseType.Wood, 1), isEliminated: true)
            });

            game.TryPlayRaidBase(0, 1, UnitShape.Square);
            game.ResolvePendingAction();

            Assert.That(game.IsGameOver, Is.True);
            Assert.That(game.WinningPlayerIndex, Is.EqualTo(0));
        }

        private static Game CreateGame(int playerCount)
        {
            return new Game(Enumerable.Range(0, playerCount).Select(index => CreatePlayer(index, $"Player {index + 1}")));
        }

        private static Game CreateActionCardGame(Player playerOne, Player playerTwo)
        {
            return new Game(new[] { playerOne, playerTwo }, new ActionCardDeck(Array.Empty<ActionCardType>()));
        }

        private static Game CreateBattleRoyaleGame(params Player[] players)
        {
            return new Game(players);
        }

        private static Game CreateEliminationRaidGame()
        {
            return CreateActionCardGame(
                CreatePlayer(
                    0,
                    "Player 1",
                    unitCounts: Units(UnitShape.Square, 1),
                    actionCards: new ActionCardHand(new[] { ActionCardType.RaidBase })),
                CreatePlayer(1, "Player 2", playerBase: new Base(BaseType.Wood, 1)));
        }

        private static Game CreateRaidGame(UnitShape raidingUnitShape)
        {
            return CreateActionCardGame(
                CreatePlayer(
                    0,
                    "Player 1",
                    unitCounts: new EnumCountSet<UnitShape>(
                        new Dictionary<UnitShape, int>
                        {
                            [raidingUnitShape] = 1
                        }),
                    actionCards: new ActionCardHand(new[] { ActionCardType.RaidBase })),
                CreatePlayer(1, "Player 2"));
        }

        private static Game CreateRaidDefenseGame(
            UnitShape raidingUnitShape,
            UnitShape defendingUnitShape,
            int defendingUnitCount)
        {
            return CreateActionCardGame(
                CreatePlayer(
                    0,
                    "Player 1",
                    unitCounts: new EnumCountSet<UnitShape>(
                        new Dictionary<UnitShape, int>
                        {
                            [raidingUnitShape] = 1
                        }),
                    actionCards: new ActionCardHand(new[] { ActionCardType.RaidBase })),
                CreatePlayer(
                    1,
                    "Player 2",
                    unitCounts: new EnumCountSet<UnitShape>(
                        new Dictionary<UnitShape, int>
                        {
                            [defendingUnitShape] = defendingUnitCount
                        })));
        }

        private static EnumCountSet<UnitShape> Units(UnitShape unitShape, int count)
        {
            return new EnumCountSet<UnitShape>(
                new Dictionary<UnitShape, int>
                {
                    [unitShape] = count
                });
        }

        private static EnumCountSet<ResourceType> Resources(ResourceType resourceType, int count)
        {
            return new EnumCountSet<ResourceType>(
                new Dictionary<ResourceType, int>
                {
                    [resourceType] = count
                });
        }

        private static Player CreatePlayer(
            int index,
            string name,
            Base? playerBase = null,
            EnumCountSet<UnitShape>? unitCounts = null,
            EnumCountSet<ResourceType>? resourceCounts = null,
            ActionCardHand? actionCards = null,
            bool isEliminated = false)
        {
            return new Player(
                index,
                name,
                playerBase ?? new Base(BaseType.Wood, 3),
                unitCounts,
                resourceCounts,
                actionCards,
                isEliminated);
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

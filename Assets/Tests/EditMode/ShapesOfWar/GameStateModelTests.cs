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
        public void PlayerEliminationStateDefaultsToNotEliminated()
        {
            Player player = CreatePlayer(0, "Player 1");

            Assert.That(player.IsEliminated, Is.False);
            Assert.That(player.ToPublicState().IsEliminated, Is.False);
        }

        private static Game CreateGame(int playerCount)
        {
            return new Game(Enumerable.Range(0, playerCount).Select(index => CreatePlayer(index, $"Player {index + 1}")));
        }

        private static Player CreatePlayer(
            int index,
            string name,
            EnumCountSet<UnitShape>? unitCounts = null,
            EnumCountSet<ResourceType>? resourceCounts = null,
            ActionCardHand? actionCards = null)
        {
            return new Player(
                index,
                name,
                new Base(BaseType.Wood, 3),
                unitCounts,
                resourceCounts,
                actionCards);
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

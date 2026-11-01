using System;
using NUnit.Framework;
using AbandonedAsylum.Core;
using AbandonedAsylum.Interfaces;

namespace AbandonedAsylum.Tests
{
    [TestFixture]
    public class ServiceLocatorTests
    {
        private interface IMockService { string GetName(); }
        private class MockService : IMockService
        {
            public string GetName() => "MockServiceAlpha";
        }

        private IServiceLocator locator;

        [SetUp]
        public void SetUp()
        {
            locator = new ServiceLocator();
        }

        [TearDown]
        public void TearDown()
        {
            locator.Reset();
        }

        [Test]
        public void Register_And_Resolve_Service_ReturnsSameInstance()
        {
            var service = new MockService();
            locator.Register<IMockService>(service);

            var resolved = locator.Resolve<IMockService>();

            Assert.IsNotNull(resolved);
            Assert.AreSame(service, resolved);
            Assert.AreEqual("MockServiceAlpha", resolved.GetName());
        }

        [Test]
        public void Resolve_UnregisteredService_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                locator.Resolve<IMockService>();
            });
        }

        [Test]
        public void TryResolve_RegisteredService_ReturnsTrueAndInstance()
        {
            var service = new MockService();
            locator.Register<IMockService>(service);

            bool success = locator.TryResolve<IMockService>(out var resolved);

            Assert.IsTrue(success);
            Assert.AreSame(service, resolved);
        }

        [Test]
        public void TryResolve_UnregisteredService_ReturnsFalseAndNull()
        {
            bool success = locator.TryResolve<IMockService>(out var resolved);

            Assert.IsFalse(success);
            Assert.IsNull(resolved);
        }

        [Test]
        public void Unregister_Service_RemovesFromContainer()
        {
            var service = new MockService();
            locator.Register<IMockService>(service);
            Assert.IsTrue(locator.Contains<IMockService>());

            locator.Unregister<IMockService>();

            Assert.IsFalse(locator.Contains<IMockService>());
            Assert.IsFalse(locator.TryResolve<IMockService>(out _));
        }

        [Test]
        public void Register_NullService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                locator.Register<IMockService>(null);
            });
        }

        [Test]
        public void Reset_ClearsAllServices()
        {
            locator.Register<IMockService>(new MockService());
            Assert.IsTrue(locator.Contains<IMockService>());

            locator.Reset();

            Assert.IsFalse(locator.Contains<IMockService>());
        }

        private class MockGameStateService : IGameStateService
        {
            public GameState CurrentState { get; set; } = GameState.Playing;
            public int PortalsDestroyed { get; set; } = 0;
            public int TargetPortalsToDestroy => 3;
            public int ZombiesKilled { get; set; } = 0;
            public float ElapsedSurvivalTime => 10.0f;
            public event Action<GameState> OnGameStateChanged { add { } remove { } }
            public event Action<int, int> OnPortalProgressChanged { add { } remove { } }
            public void StartMission() { CurrentState = GameState.Playing; }
            public void RetryMission() { CurrentState = GameState.Playing; }
            public void ReturnToMainMenu() { CurrentState = GameState.MainMenu; }
            public void OnPortalDestroyed() { PortalsDestroyed++; }
            public void OnPlayerDied() { CurrentState = GameState.GameOver; }
            public void OnZombieKilled() { ZombiesKilled++; }
            public void TriggerVictory() { CurrentState = GameState.Victory; }
        }

        [Test]
        public void GameStateService_ContractResolution_MatchesServiceLocator()
        {
            var mockState = new MockGameStateService();
            locator.Register<IGameStateService>(mockState);

            Assert.IsTrue(locator.TryResolve<IGameStateService>(out var resolved));
            Assert.AreEqual(GameState.Playing, resolved.CurrentState);
            resolved.OnPortalDestroyed();
            Assert.AreEqual(1, resolved.PortalsDestroyed);
            resolved.OnPlayerDied();
            Assert.AreEqual(GameState.GameOver, resolved.CurrentState);
        }
    }
}

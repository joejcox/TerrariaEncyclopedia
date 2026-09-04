using System;
using TerrariaEncyclopedia.Qol;
using Xunit;

public sealed class InventoryQolTests
{
    [Fact]
    public void SortInventoryDelegatesToNativeAction()
    {
        var game = new FakeGame();
        Assert.Equal(InventoryQolResult.Completed, new InventoryQolController(game).Invoke(InventoryQolAction.SortInventory));
        Assert.Equal(1, game.InventorySorts);
    }

    [Fact]
    public void SortChestRequiresAnOpenChest()
    {
        var game = new FakeGame { HasOpenChest = false };
        Assert.Equal(InventoryQolResult.Unavailable, new InventoryQolController(game).Invoke(InventoryQolAction.SortChest));
        Assert.Equal(0, game.ChestSorts);
        game.HasOpenChest = true;
        Assert.Equal(InventoryQolResult.Completed, new InventoryQolController(game).Invoke(InventoryQolAction.SortChest));
        Assert.Equal(1, game.ChestSorts);
    }

    [Fact]
    public void MenuAndMissingPlayerAreNoOps()
    {
        var game = new FakeGame { IsGameMenu = true };
        Assert.Equal(InventoryQolResult.Unavailable, new InventoryQolController(game).Invoke(InventoryQolAction.SortInventory));
        game.IsGameMenu = false; game.HasLocalPlayer = false;
        Assert.Equal(InventoryQolResult.Unavailable, new InventoryQolController(game).Invoke(InventoryQolAction.SortInventory));
        Assert.Equal(0, game.InventorySorts);
    }

    [Fact]
    public void QuickStackUsesNativeOperationsAndIsEdgeTriggered()
    {
        var game = new FakeGame(); var controller = new InventoryQolController(game);
        Assert.True(controller.TryInvokeEdge(true, InventoryQolAction.QuickStackNearby, out var result));
        Assert.Equal(InventoryQolResult.Completed, result);
        Assert.False(controller.TryInvokeEdge(true, InventoryQolAction.QuickStackNearby, out _));
        Assert.False(controller.TryInvokeEdge(false, InventoryQolAction.QuickStackNearby, out _));
        Assert.True(controller.TryInvokeEdge(true, InventoryQolAction.QuickStackNearby, out _));
        Assert.Equal(2, game.NearbyChestCalls);
    }

    [Fact]
    public void QuickStackUnavailableDoesNotCallNativeOperations()
    {
        var game = new FakeGame { CanQuickStack = false };
        Assert.Equal(InventoryQolResult.Unavailable, new InventoryQolController(game).Invoke(InventoryQolAction.QuickStackNearby));
        Assert.Equal(0, game.NearbyChestCalls);
    }

    [Fact]
    public void NativeFailureIsReturnedAndReported()
    {
        var game = new FakeGame { ThrowOnInventorySort = true };
        InventoryQolDiagnostic diagnostic = null;
        var result = new InventoryQolController(game, value => diagnostic = value).Invoke(InventoryQolAction.SortInventory);
        Assert.Equal(InventoryQolResult.Failed, result);
        Assert.NotNull(diagnostic);
        Assert.Equal(InventoryQolAction.SortInventory, diagnostic.Action);
        Assert.IsType<InvalidOperationException>(diagnostic.Exception);
    }

    private sealed class FakeGame : IInventoryQolGame
    {
        public bool IsGameMenu { get; set; }
        public bool HasLocalPlayer { get; set; } = true;
        public bool CanSortInventory { get; set; } = true;
        public bool HasOpenChest { get; set; } = true;
        public bool CanQuickStack { get; set; } = true;
        public int InventorySorts { get; private set; }
        public int ChestSorts { get; private set; }
        public int NearbyChestCalls { get; private set; }
        public bool ThrowOnInventorySort { get; set; }
        public void SortInventory() { if (ThrowOnInventorySort) throw new InvalidOperationException("boom"); InventorySorts++; }
        public void SortChest() => ChestSorts++;
        public void SendQuickStackToNearbyChests() => NearbyChestCalls++;
    }
}

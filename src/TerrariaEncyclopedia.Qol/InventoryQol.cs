using System;
using System.Collections.Generic;

namespace TerrariaEncyclopedia.Qol
{
    public interface IInventoryQolGame
    {
        bool IsGameMenu { get; }
        bool HasLocalPlayer { get; }
        bool CanSortInventory { get; }
        bool HasOpenChest { get; }
        bool CanQuickStack { get; }
        void SortInventory();
        void SortChest();
        void SendQuickStackToNearbyChests();
    }

    public enum InventoryQolAction
    {
        SortInventory,
        SortChest,
        QuickStackNearby
    }

    public enum InventoryQolResult
    {
        Unavailable,
        Completed,
        Failed
    }

    public sealed class InventoryQolDiagnostic
    {
        public InventoryQolDiagnostic(InventoryQolAction action, InventoryQolResult result, Exception exception)
        { Action = action; Result = result; Exception = exception; }
        public InventoryQolAction Action { get; }
        public InventoryQolResult Result { get; }
        public Exception Exception { get; }
    }

    public sealed class InventoryQolController
    {
        private readonly IInventoryQolGame game;
        private readonly Action<InventoryQolDiagnostic> report;
        private readonly HashSet<InventoryQolAction> heldActions = new HashSet<InventoryQolAction>();

        public InventoryQolController(IInventoryQolGame game, Action<InventoryQolDiagnostic> report = null)
        {
            this.game = game;
            this.report = report;
        }

        public InventoryQolResult Invoke(InventoryQolAction action)
        {
            if (game == null || game.IsGameMenu || !game.HasLocalPlayer)
                return InventoryQolResult.Unavailable;

            if (action == InventoryQolAction.SortInventory && !game.CanSortInventory)
                return InventoryQolResult.Unavailable;
            if (action == InventoryQolAction.SortChest && !game.HasOpenChest)
                return InventoryQolResult.Unavailable;
            if (action == InventoryQolAction.QuickStackNearby && !game.CanQuickStack)
                return InventoryQolResult.Unavailable;

            try
            {
                switch (action)
                {
                    case InventoryQolAction.SortInventory:
                        game.SortInventory();
                        break;
                    case InventoryQolAction.SortChest:
                        game.SortChest();
                        break;
                    case InventoryQolAction.QuickStackNearby:
                        // This action is deliberately limited to nearby chests.
                        game.SendQuickStackToNearbyChests();
                        break;
                    default:
                        return InventoryQolResult.Unavailable;
                }
                return InventoryQolResult.Completed;
            }
            catch (Exception exception)
            {
                report?.Invoke(new InventoryQolDiagnostic(action, InventoryQolResult.Failed, exception));
                return InventoryQolResult.Failed;
            }
        }

        public bool TryInvokeEdge(bool pressed, InventoryQolAction action, out InventoryQolResult result)
        {
            result = InventoryQolResult.Unavailable;
            if (!pressed)
            {
                heldActions.Remove(action);
                return false;
            }
            if (!heldActions.Add(action))
                return false;
            result = Invoke(action);
            return true;
        }

        public void Release() => heldActions.Clear();
    }
}

using System;

namespace TerrariaEncyclopedia.UI
{
    internal enum EncyclopediaSection
    {
        Items,
        Npcs,
        BossChecklist
    }

    internal sealed class EncyclopediaSectionSelection
    {
        private readonly Action unfocusItems;
        private readonly Action unfocusNpcs;

        public EncyclopediaSectionSelection(Action unfocusItems, Action unfocusNpcs)
        {
            this.unfocusItems = unfocusItems ?? throw new ArgumentNullException(nameof(unfocusItems));
            this.unfocusNpcs = unfocusNpcs ?? throw new ArgumentNullException(nameof(unfocusNpcs));
        }

        public EncyclopediaSection Current { get; private set; } = EncyclopediaSection.Items;

        public void SwitchTo(EncyclopediaSection next)
        {
            if (Current == next)
                return;

            if (Current == EncyclopediaSection.Items) unfocusItems();
            if (Current == EncyclopediaSection.Npcs) unfocusNpcs();

            Current = next;
        }
    }
}

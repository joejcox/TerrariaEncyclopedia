using System;

namespace TerrariaEncyclopedia.UI
{
    internal enum EncyclopediaSection
    {
        Items,
        Npcs,
        Events,
        BossChecklist
    }

    internal sealed class EncyclopediaSectionSelection
    {
        private readonly Action unfocusItems;
        private readonly Action unfocusNpcs;
        private readonly Action unfocusEvents;

        public EncyclopediaSectionSelection(
            Action unfocusItems,
            Action unfocusNpcs,
            Action unfocusEvents)
        {
            this.unfocusItems = unfocusItems ?? throw new ArgumentNullException(nameof(unfocusItems));
            this.unfocusNpcs = unfocusNpcs ?? throw new ArgumentNullException(nameof(unfocusNpcs));
            this.unfocusEvents = unfocusEvents ?? throw new ArgumentNullException(nameof(unfocusEvents));
        }

        public EncyclopediaSection Current { get; private set; } = EncyclopediaSection.Items;

        public void SwitchTo(EncyclopediaSection next)
        {
            if (Current == next)
                return;

            if (Current == EncyclopediaSection.Items)
                unfocusItems();
            else if (Current == EncyclopediaSection.Npcs)
                unfocusNpcs();
            else if (Current == EncyclopediaSection.Events)
                unfocusEvents();

            Current = next;
        }
    }
}

using TerrariaEncyclopedia.Runtime;
using TerrariaEncyclopedia.Runtime.UI.Input;
using TerrariaEncyclopedia.Runtime.UI.Rendering;
using TerrariaEncyclopedia.Runtime.UI.Tooltips;

namespace TerrariaEncyclopedia.UI
{
    /// <summary>Live Terraria presentation services supplied by the in-process host.</summary>
    public interface IEncyclopediaFeatureRenderer : IUiRenderer
    {
        RenderRectangle Viewport { get; }

        ITextEntrySession TextEntry { get; }

        IItemTooltipSource ItemTooltips { get; }
    }
}

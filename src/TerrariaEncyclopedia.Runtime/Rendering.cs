using System;

namespace TerrariaEncyclopedia.Runtime
{
    public readonly struct RenderRectangle
    {
        public RenderRectangle(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public int X { get; }

        public int Y { get; }

        public int Width { get; }

        public int Height { get; }
    }

    public readonly struct RenderColour
    {
        public RenderColour(byte red, byte green, byte blue, byte alpha = 255)
        {
            Red = red;
            Green = green;
            Blue = blue;
            Alpha = alpha;
        }

        public byte Red { get; }

        public byte Green { get; }

        public byte Blue { get; }

        public byte Alpha { get; }
    }

    public enum TerrariaIconKind
    {
        Item,
        Npc,
        Buff
    }

    public readonly struct TerrariaIcon
    {
        public TerrariaIcon(TerrariaIconKind kind, int id)
        {
            Kind = kind;
            Id = id;
        }

        public TerrariaIconKind Kind { get; }

        public int Id { get; }
    }

    public interface IFeatureRenderer
    {
        IDisposable PushClip(RenderRectangle bounds);

        void DrawText(string text, InputPoint position, RenderColour colour);

        void DrawTerrariaIcon(
            TerrariaIcon icon,
            RenderRectangle destination,
            RenderColour colour);
    }
}

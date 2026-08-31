namespace TerrariaEncyclopedia.Runtime.UI.Input
{
    public interface ITextEntrySession
    {
        void Begin(string ownerId);

        string Read(string currentText, int maxLength);

        void End(string ownerId);
    }

    public sealed class NullTextEntrySession : ITextEntrySession
    {
        public static NullTextEntrySession Instance { get; } = new NullTextEntrySession();

        private NullTextEntrySession()
        {
        }

        public void Begin(string ownerId)
        {
        }

        public string Read(string currentText, int maxLength)
        {
            return currentText ?? string.Empty;
        }

        public void End(string ownerId)
        {
        }
    }
}

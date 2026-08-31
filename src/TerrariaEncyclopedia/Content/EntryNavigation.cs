using System.Collections.Generic;

namespace TerrariaEncyclopedia.Content
{
    public sealed class EntryNavigation
    {
        private readonly Stack<int> history = new Stack<int>();

        public int? CurrentId { get; private set; }

        public bool CanGoBack => history.Count > 0;

        public void Open(int id)
        {
            if (CurrentId.HasValue)
                history.Push(CurrentId.Value);
            CurrentId = id;
        }

        public bool TryGoBack(out int id)
        {
            if (history.Count == 0)
            {
                id = 0;
                return false;
            }

            id = history.Pop();
            CurrentId = id;
            return true;
        }

        public void Home()
        {
            history.Clear();
            CurrentId = null;
        }
    }
}

using System;

namespace TerrariaEncyclopedia.Hosting
{
    internal sealed class FeatureFailureCleanup
    {
        private readonly Action unload;
        private readonly Action releaseTextEntry;
        private readonly Action restoreInputBlock;

        public FeatureFailureCleanup(
            Action unload,
            Action releaseTextEntry,
            Action restoreInputBlock)
        {
            this.unload = unload ?? throw new ArgumentNullException(nameof(unload));
            this.releaseTextEntry = releaseTextEntry
                ?? throw new ArgumentNullException(nameof(releaseTextEntry));
            this.restoreInputBlock = restoreInputBlock
                ?? throw new ArgumentNullException(nameof(restoreInputBlock));
        }

        public void Run()
        {
            try
            {
                unload();
            }
            finally
            {
                try
                {
                    releaseTextEntry();
                }
                finally
                {
                    restoreInputBlock();
                }
            }
        }
    }
}

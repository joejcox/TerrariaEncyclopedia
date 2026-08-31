using System;
using System.Collections.Generic;

namespace TerrariaEncyclopedia.Runtime
{
    public sealed class FirstPartyFeature
    {
        public FirstPartyFeature(string id, IFeature implementation)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("A feature ID is required.", nameof(id));
            }

            Id = id;
            Implementation = implementation ??
                throw new ArgumentNullException(nameof(implementation));
        }

        public string Id { get; }

        public IFeature Implementation { get; }
    }

    public sealed class FirstPartyFeatureRegistry
    {
        private readonly FirstPartyFeature[] features;

        public FirstPartyFeatureRegistry(params FirstPartyFeature[] features)
        {
            if (features == null)
            {
                throw new ArgumentNullException(nameof(features));
            }

            this.features = (FirstPartyFeature[])features.Clone();
            var ids = new HashSet<string>(StringComparer.Ordinal);

            foreach (FirstPartyFeature feature in this.features)
            {
                if (feature == null)
                {
                    throw new ArgumentException(
                        "Registered features cannot contain null.",
                        nameof(features));
                }

                if (!ids.Add(feature.Id))
                {
                    throw new ArgumentException(
                        "Feature IDs must be unique. Duplicate: " + feature.Id,
                        nameof(features));
                }
            }
        }

        public IReadOnlyList<FirstPartyFeature> Features => Array.AsReadOnly(features);
    }
}

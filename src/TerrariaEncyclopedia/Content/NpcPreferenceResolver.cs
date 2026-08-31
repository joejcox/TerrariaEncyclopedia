using System;

namespace TerrariaEncyclopedia.Content
{
    public static class NpcPreferenceResolver
    {
        /// <summary>
        /// Uses the complete wiki table when present, falls back to Terraria's live personality
        /// data otherwise, and always retains live unique-behaviour markers such as the Princess.
        /// </summary>
        public static NpcPersonalityEntry Resolve(
            NpcPreferencesWikiInfo wiki,
            NpcPersonalityEntry live)
        {
            if (wiki == null)
                return live;

            var result = new NpcPersonalityEntry
            {
                HasUniqueBehavior = live != null && live.HasUniqueBehavior
            };

            AddAll(result, wiki.LoveBiomes, wiki.LoveNeighbors, Affection.Love);
            AddAll(result, wiki.LikeBiomes, wiki.LikeNeighbors, Affection.Like);
            AddAll(result, wiki.DislikeBiomes, wiki.DislikeNeighbors, Affection.Dislike);
            AddAll(result, wiki.HateBiomes, wiki.HateNeighbors, Affection.Hate);
            return result;
        }

        private static void AddAll(
            NpcPersonalityEntry result,
            string[] biomes,
            string[] neighbours,
            Affection level)
        {
            foreach (string label in biomes ?? Array.Empty<string>())
                result.Biomes.Add(new PersonalityPreference { Level = level, Label = label });
            foreach (string label in neighbours ?? Array.Empty<string>())
                result.Neighbors.Add(new PersonalityPreference { Level = level, Label = label });
        }
    }
}

using System;
using System.Collections.Generic;
using ThreeInARow.Domain.Ids;

namespace ThreeInARow.Domain.Board
{
    public static class BoardContentIds
    {
        public static readonly ContentId Ember = "gem.ember";
        public static readonly ContentId Tide = "gem.tide";
        public static readonly ContentId Venom = "gem.venom";
        public static readonly ContentId Volt = "gem.volt";
        public static readonly ContentId PrismGem = "gem.prism";

        public static readonly ContentId NoSpecial = "special.none";
        public static readonly ContentId Spark = "special.spark";
        public static readonly ContentId Current = "special.current";
        public static readonly ContentId Spore = "special.spore";
        public static readonly ContentId Charge = "special.charge";
        public static readonly ContentId Prism = "special.prism";

        public static readonly ContentId Frozen = "status.frozen";
        public static readonly ContentId Cracked = "status.cracked";
        public static readonly ContentId Anchored = "status.anchored";
    }

    /// <summary>
    /// Board-facing content contract. Session B uses the built-in catalog; the Content layer can later
    /// adapt ScriptableObject definitions to this interface without adding content branches to the resolver.
    /// </summary>
    public interface IBoardContentCatalog
    {
        IReadOnlyList<ContentId> SpawnableGemIds { get; }
        bool IsNormalGem(ContentId gemId);
        ContentId GetMatchFourSpecial(ContentId gemId);
    }

    public sealed class MvpBoardContentCatalog : IBoardContentCatalog
    {
        private static readonly ContentId[] Spawnable =
        {
            BoardContentIds.Ember,
            BoardContentIds.Tide,
            BoardContentIds.Venom,
            BoardContentIds.Volt
        };

        public static readonly MvpBoardContentCatalog Instance = new MvpBoardContentCatalog();

        private MvpBoardContentCatalog() { }

        public IReadOnlyList<ContentId> SpawnableGemIds => Spawnable;

        public bool IsNormalGem(ContentId gemId)
        {
            return gemId.Equals(BoardContentIds.Ember)
                || gemId.Equals(BoardContentIds.Tide)
                || gemId.Equals(BoardContentIds.Venom)
                || gemId.Equals(BoardContentIds.Volt);
        }

        public ContentId GetMatchFourSpecial(ContentId gemId)
        {
            if (gemId.Equals(BoardContentIds.Ember)) return BoardContentIds.Spark;
            if (gemId.Equals(BoardContentIds.Tide)) return BoardContentIds.Current;
            if (gemId.Equals(BoardContentIds.Venom)) return BoardContentIds.Spore;
            if (gemId.Equals(BoardContentIds.Volt)) return BoardContentIds.Charge;
            throw new ArgumentException("A match-four special requires a normal gem ID.", nameof(gemId));
        }
    }
}

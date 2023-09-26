using MemoryPack;

[MemoryPackable]
public partial class NotifyGroup
{
    public readonly List<Player> Players;

    [MemoryPackIgnore]
    public readonly DateTime FirstCreated;

    public readonly int Score;
    [MemoryPackInclude]
    private readonly String? LowerRankName = null;
    [MemoryPackInclude]
    private readonly int? LowerRankScore = null;
    [MemoryPackInclude]
    private readonly String? HighRankName = null;
    [MemoryPackInclude]
    private readonly int? HighRankScore = null;

    [MemoryPackConstructor]
    internal NotifyGroup(List<Player> players, string? LowerRankName, int? LowerRankScore, string? HighRankName, int? HighRankScore, int Score)
    {
        this.Players = players;
        this.LowerRankName = LowerRankName;
        this.LowerRankScore = LowerRankScore;
        this.HighRankName = HighRankName;
        this.HighRankScore = HighRankScore;
        this.Score = Score;
    }
    internal NotifyGroup(List<Player> players, string? LowerRankName, int? LowerRankScore, string? HighRankName, int? HighRankScore, int Score, DateTime dt) : this(players, LowerRankName, LowerRankScore, HighRankName, HighRankScore, Score)
    {
        FirstCreated = dt;
    }
}

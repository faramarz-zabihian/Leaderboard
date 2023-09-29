
public class ScoreGroup
{
    internal bool HasVitualUser = false;
    public ScoreGroup(int score)
    {
        this.Score = score;
    }
    public int Score { get; }
    public SortedList<int, Player> Players = new();
    public int GroupCount;
    public Player GetDelegate()
    {
        return Players.GetValueAtIndex(0);
    }
}
public class ScoreGroup
{
    public ScoreGroup(int score)
    {
        this.Score = score;
    }
    public int Score { get; }
    public List<Player> Players = new();
    public int GroupCount;
    internal Player getDelegate()
    {
        return Players[0];
    }
}
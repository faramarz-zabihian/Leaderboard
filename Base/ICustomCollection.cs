namespace LeaderBoard.Base
{
    interface ICustomCollection
    {
        ScoreGroup? Value(int  index);
        int Count();
    }
}


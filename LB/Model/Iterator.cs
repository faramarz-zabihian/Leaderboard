namespace LeaderBoard.Base
{
    public interface ITerator<T>
    {
        public abstract bool MoveForward();
        public abstract bool MoveBackward();
        public abstract ScoreGroup? CurrentItem();
    }
}


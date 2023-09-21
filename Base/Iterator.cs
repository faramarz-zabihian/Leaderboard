namespace LeaderBoard.Base
{
    interface Iterator<T>
    {
        public abstract bool MoveForward();
        public abstract bool MoveBackward();
        public abstract ScoreGroup? CurrentItem();     
    }
}


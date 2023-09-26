namespace LeaderBoard.Base
{
    public class LeaderBoardIterartor : ITerator<ScoreGroup>
    {
        private readonly ICustomCollection collection;
        private int current = -1;

        internal LeaderBoardIterartor(ICustomCollection collection)
        {
            this.collection = collection;
        }
        internal LeaderBoardIterartor(ICustomCollection collection, int index) : this(collection)
        {
            if (index < collection.Count() && index >= 0)
                current = index;
        }

        // Implementing Iterartor interface
        public bool MoveForward()
        {
            ++current;
            if (current < collection.Count())
                return true;

            current = collection.Count();
            return false;
        }
        public bool MoveBackward()
        {
            --current;
            if (current > -1)
                return true;

            current = -1;
            return false;
        }

        // Gets current iteration item
        public ScoreGroup? CurrentItem()
        {
            return collection.Value(current);
        }
    }
}


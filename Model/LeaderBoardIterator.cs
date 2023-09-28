namespace LeaderBoard.Base
{
    public class LeaderBoardIterator : ITerator<ScoreGroup>
    {
        private readonly ICustomCollection collection;
        private int current = -1;

        internal LeaderBoardIterator(ICustomCollection collection)
        {
            this.collection = collection;
        }
        internal LeaderBoardIterator clone()
        {
            return new LeaderBoardIterator(collection, current);
        }
        internal LeaderBoardIterator(ICustomCollection collection, int index) : this(collection)
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

            current = collection.Count(); // one past the collection
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

        public bool Last()
        {
            current = collection.Count() - 1;
            return current > 0;
        }
    }
}


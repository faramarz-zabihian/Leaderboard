namespace LeaderBoard.Base;
public class SparseArray : ICustomCollection
{
    SortedList<int, ScoreGroup> items = new SortedList<int, ScoreGroup>();
    /*private int find_leftmost(int value)
    {
        int L = 0;
        int R = items.Count - 1;
        int m;
        while (L < R)
        {
            m = (L + R) / 2;
            if (items.GetKeyAtIndex(m) < value)
                L = m + 1;
            else
                R = m;
        }
        return L;
    }*/


    // Indexer
    public ITerator<ScoreGroup> this[int score]
    {
        get
        {
            int index = items.IndexOfKey(score);
            return index == -1 ? GetIterator(-1) : GetIterator(index);
        }
    }
    public int Length
    {
        get { return items.Count; }
    }

    public LeaderBoardIterartor GetIteraror()
    {
        return new LeaderBoardIterartor(this, -1);
    }
    LeaderBoardIterartor GetIterator(int index)
    {
        return new LeaderBoardIterartor(this, index);
    }

    int ICustomCollection.Count()
    {
        return items.Count();
    }
    ScoreGroup? ICustomCollection.Value(int index)
    {
        return index < 0 || index >= items.Count ? null : items.GetValueAtIndex(index);
    }

    internal LeaderBoardIterartor Add(int score, ScoreGroup value)
    {
        // Int32 index = items.IndexOfKey(score); // should find topest index before score and return an iterator
        items.Add(score, value);
        return GetIterator(items.IndexOfKey(score));
    }
    internal LeaderBoardIterartor Remove(int score)
    {
        int index = items.IndexOfKey(score);
        items.Remove(score);
        return GetIterator(index);
    }
}
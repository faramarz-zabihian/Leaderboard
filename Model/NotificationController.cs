using MemoryPack;

namespace LeaderBoard.Base
{
    public class NotificationController
    {
        private int interval;
        private int by_groups = 0;
        private DateTime check_time;
        private int invokations = 0;
        public int Invokations { get; } = 0;
        private readonly SortedDictionary<int, NotifyGroup> groups = new();
        private int singles = 0;

        public int SinglesItemsCount { get { return singles; } }
        public int GroupedCount { get { return by_groups; } }
        public int InvokesCount { get { return by_groups; } }
        internal bool TryGetValue(int key, out NotifyGroup? group)
        {
            return groups.TryGetValue(key, out group);
        }

        public NotificationController(int millis)
        {
            interval = millis;
            check_time = DateTime.Now.AddMilliseconds(interval);
        }
        internal void Check()
        {
            if (DateTime.Now > check_time)
            {
                var notify_array = groups.Values.ToArray();
                var events = new List<NotifyGroup>();
                foreach (var it in notify_array)
                    if (it.FirstCreated < check_time)
                    {
                        groups.Remove(it.Score);
                        events.Add(it);
                        by_groups += it.Players.Count;
                    }

                if (events.Count > 0)
                {
                    invokations++;
                    var bytes = MemoryPackSerializer.Serialize<List<NotifyGroup>>(events);
                    var t = new Task(() => MemoryPackSerializer.Deserialize<List<NotifyGroup>>(bytes));
                    t.Start();
                    events.Clear();
                }
                check_time = DateTime.Now.AddMilliseconds(interval);
            }
        }
        public NotifyGroup[] GetList()
        { 
            return groups.Values.ToArray();
        }
        internal void Add(int key, NotifyGroup notifyGroup)
        {
            groups.Add(key, notifyGroup);
            singles++;
        }

        internal bool Remove(int score)
        {
            return groups.Remove(score);
        }

        internal void AddSinglesCounter()
        {
            singles++;
        }
    }
}

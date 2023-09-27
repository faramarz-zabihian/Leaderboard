using MemoryPack;
using System.Text.RegularExpressions;

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
        int n = 0;
        internal void Check()
        {
            if (DateTime.Now > check_time)
            {
                DateTime pick_time = check_time.AddMilliseconds(- interval);
                var ts = check_time - pick_time;
                var list = groups.Where(kv => kv.Value.FirstCreated < pick_time).Select(p => p.Value).ToList();
                n++;
                if (n == 2)
                {
                    n = n;
                }
                if (list.Count > 0)
                {                 
                    var bytes = MemoryPackSerializer.Serialize<List<NotifyGroup>>(list);
                    var t = new Task(() =>
                                            {
                                                // send as a stream of bytes to another server
                                                // these lines are only for test
                                                var readyToSend = MemoryPackSerializer.Deserialize<List<NotifyGroup>>(bytes);
                                                invokations++;
                                            }
                                     );
                    t.Start();                    
                    list.ForEach(p =>
                    {
                        groups.Remove(p.Score);
                        by_groups += p.Players.Count;
                    });
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

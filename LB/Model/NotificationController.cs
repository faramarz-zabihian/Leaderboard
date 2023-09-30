using MemoryPack;

namespace LeaderBoard.Base
{
    public class NotificationController
    {
        private readonly SortedDictionary<int, NotifyGroup> groups = new();
        private readonly int interval;
        private DateTime check_time;

        public int task_invokations = 0;
        public int singlesCounter = 0;
        public int groupedCounter = 0;
        public int totalSent = 0;

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
                var list = groups
                        .Where(kv => kv.Value.FirstCreated < check_time.AddMilliseconds(-interval))
                        .Select(p => p.Value)
                        .ToList();
                if (list.Count > 0)
                {
                    var bytes = MemoryPackSerializer.Serialize<List<NotifyGroup>>(list);
                    var t = new Task(() =>
                                            {
                                                //Console.WriteLine(bytes.Length);
                                                // send as a stream of bytes to another server
                                                // these lines are only for test
                                                //var readyToSend = MemoryPackSerializer.Deserialize<List<NotifyGroup>>(bytes);
                                                task_invokations++;
                                            }
                                     );
                    t.Start();
                    list.ForEach(p =>
                    {
                        groups.Remove(p.Score);
                        totalSent += p.Players.Count;
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
        }

        internal bool Remove(int score)
        {
            return groups.Remove(score);
        }
    }
}

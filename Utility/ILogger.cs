using LeaderBoard.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeaderBoard.Utility
{
    public interface ILogger
    {
        void Log(LB lb, NotificationController nc);
    }
}

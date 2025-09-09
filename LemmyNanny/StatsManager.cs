using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LemmyNanny
{
    public class StatsManager
    {
        public  int Posts { get; set; }
        public  int Comments { get; set; }
        public  int CommentsFlagged { get; set; }
        public  int PostsFlagged { get; set; }

        public  DateTime StartTime { get; set; }
        public  TimeSpan ElapsedTime => DateTime.Now - StartTime;
        public  List<Processed> History { get; set; } = [];
        /*
    add webhook to go to domain, takes unique instance ids and collates data
    heartbeat with stats for running
    live updating dashboard to share on twitch
 */
    }
}

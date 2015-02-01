using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace clipbrd
{
    public class ClipBoardDataChangedEventArgs : EventArgs
    {
        DateTime time;

        public DateTime Time
        {
            get { return time; }
            set { time = value; }
        }
        public ClipBoardDataChangedEventArgs(DateTime time)
        {
            this.time = time;
        }

        public override string ToString()
        {
            return string.Format("Az esemény bekövetkezésének az ideje: {0}",time);
        }
    }
}

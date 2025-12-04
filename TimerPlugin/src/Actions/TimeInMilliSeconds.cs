namespace Loupedeck.TimerPlugin.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class TimeInMilliSeconds
    {
        private Int32 _seconds;
        private Int32 _minutes;
        private Int32 _hours;

        public Int32 Seconds { get => this._seconds; set => this._seconds = value; }
        public Int32 Minutes { get => this._minutes; set => this._minutes = value; }
        public Int32 Hours { get => this._hours; set => this._hours = value; }

        public Int32 ReturnMilliSeconds()
        {
            var timeSpan = new TimeSpan(this._hours, this._minutes, this._seconds);
            return timeSpan.Equals(TimeSpan.Zero) ? 1 : (Int32)timeSpan.TotalMilliseconds;
        }

    }
}

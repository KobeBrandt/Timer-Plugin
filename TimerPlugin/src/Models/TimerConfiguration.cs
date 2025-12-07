namespace Loupedeck.TimerPlugin.Models
{
    using System;
    using System.Collections.Generic;

    public class TimerConfiguration
    {
        public List<TimerPreset> Timers { get; set; } = new List<TimerPreset>();
    }

    public class TimerPreset
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Hours { get; set; }
        public int Minutes { get; set; }
        public int Seconds { get; set; }
        public string Haptic { get; set; } = "jingle";
        public bool IsActive { get; set; } = true;

        public int GetTotalMilliseconds()
        {
            return (Hours * 3600 + Minutes * 60 + Seconds) * 1000;
        }

        public string GetDisplayTime()
        {
            return $"{Hours:D2}:{Minutes:D2}:{Seconds:D2}";
        }
    }
}

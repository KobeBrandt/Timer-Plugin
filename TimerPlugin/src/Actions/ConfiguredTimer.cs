namespace Loupedeck.TimerPlugin.Actions
{
    using System;
    using System.Timers;
    using Loupedeck;
    using Loupedeck.TimerPlugin.Services;

    public class ConfiguredTimer : PluginDynamicCommand
    {
        private System.Timers.Timer _delayTimer;
        private System.Timers.Timer _countdownTimer;
        private DateTime _startTime;
        private int _totalMilliseconds;
        private int _remainingMilliseconds;

        public ConfiguredTimer()
            : base("Configured Timers", "Timers created in settings", "Timers")
        {
            _countdownTimer = new System.Timers.Timer(1000);
            _countdownTimer.Elapsed += OnCountdownTick;
            _countdownTimer.AutoReset = true;

            // Subscribe to configuration changes to update parameters
            TimerConfigurationService.Instance.ConfigurationChanged += OnConfigurationChanged;
            UpdateParameters();
        }

        private void OnConfigurationChanged(object sender, EventArgs e)
        {
            UpdateParameters();
        }

        private void UpdateParameters()
        {
            try
            {
                // Clear existing parameters
                this.RemoveAllParameters();

                // Add parameters for each configured timer
                var config = TimerConfigurationService.Instance.GetConfiguration();
                foreach (var timer in config.Timers)
                {
                    if (timer.IsActive)
                    {
                        this.AddParameter(timer.Id, timer.Name, "Configured Timers");
                    }
                }

                PluginLog.Info($"Updated timer parameters: {config.Timers.Count} timer(s)");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to update timer parameters");
            }
        }

        protected override void RunCommand(string actionParameter)
        {
            try
            {
                PluginLog.Info($"Starting configured timer: {actionParameter}");

                _delayTimer?.Stop();
                _countdownTimer.Stop();

                var timer = TimerConfigurationService.Instance.GetTimer(actionParameter);
                if (timer == null)
                {
                    PluginLog.Warning($"Timer not found: {actionParameter}");
                    return;
                }

                _totalMilliseconds = timer.GetTotalMilliseconds();
                if (_totalMilliseconds <= 0)
                {
                    PluginLog.Warning($"Timer has no duration: {timer.Name}");
                    return;
                }

                _startTime = DateTime.Now;
                _remainingMilliseconds = _totalMilliseconds;

                _delayTimer = new System.Timers.Timer(_totalMilliseconds);
                _delayTimer.Elapsed += (s, e) => OnDelayElapsed(timer.Haptic);
                _delayTimer.AutoReset = false;
                _delayTimer.Start();
                _countdownTimer.Start();

                this.ActionImageChanged();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to start timer");
            }
        }

        protected override string GetCommandDisplayName(string actionParameter, PluginImageSize imageSize)
        {
            if (string.IsNullOrEmpty(actionParameter))
            {
                return "Timer\r\nNot Set";
            }

            var timer = TimerConfigurationService.Instance.GetTimer(actionParameter);
            if (timer == null)
            {
                return "Timer\r\nNot Found";
            }

            if (_remainingMilliseconds > 0)
            {
                var remainingTime = TimeSpan.FromMilliseconds(_remainingMilliseconds);
                return $"{timer.Name}\r\n{remainingTime:hh\\:mm\\:ss}";
            }
            else
            {
                return $"{timer.Name}\r\n{timer.GetDisplayTime()}";
            }
        }

        private void OnDelayElapsed(string haptic)
        {
            _countdownTimer.Stop();
            _remainingMilliseconds = 0;

            if (!string.IsNullOrEmpty(haptic))
            {
                this.Plugin.PluginEvents.RaiseEvent(haptic);
            }

            this.ActionImageChanged();
        }

        private void OnCountdownTick(object sender, ElapsedEventArgs e)
        {
            var elapsed = (int)(DateTime.Now - _startTime).TotalMilliseconds;
            _remainingMilliseconds = Math.Max(0, _totalMilliseconds - elapsed);
            if (_remainingMilliseconds <= 0)
            {
                _countdownTimer.Stop();
            }
            this.ActionImageChanged();
        }
    }
}

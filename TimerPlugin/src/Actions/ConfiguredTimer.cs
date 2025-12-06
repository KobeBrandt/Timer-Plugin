namespace Loupedeck.TimerPlugin.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Timers;
    using Loupedeck;
    using Loupedeck.TimerPlugin.Services;
    using Loupedeck.TimerPlugin.Models;

    public class ConfiguredTimer : PluginDynamicCommand
    {
        private System.Timers.Timer _countdownTimer;
        private DateTime _startTime;
        private int _remainingMilliseconds;
        private TimerPreset _currentTimer;
        private bool _isRunning;

        // Track timer IDs and display names
        private readonly Dictionary<string, string> _timerIds = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _timerDisplayNames = new Dictionary<string, string>();

        public ConfiguredTimer()
            : base()
        {
            this.DisplayName = "Configured Timers";
            this.Description = "Run pre-configured timers";
            this.GroupName = "Timers";

            this._countdownTimer = new System.Timers.Timer(1000);
            this._countdownTimer.Elapsed += this.OnCountdownTick;
            this._countdownTimer.AutoReset = true;

            // Subscribe to configuration changes
            TimerConfigurationService.Instance.ConfigurationChanged += this.OnConfigurationChanged;

            // Load initial parameters
            this.ScanAndUpdateTimers();
        }

        private void OnConfigurationChanged(object sender, EventArgs e)
        {
            // Make sure we're subscribed to future changes
            try
            {
                TimerConfigurationService.Instance.ConfigurationChanged -= this.OnConfigurationChanged;
                TimerConfigurationService.Instance.ConfigurationChanged += this.OnConfigurationChanged;
            }
            catch (InvalidOperationException)
            {
                // Service still not ready
            }

            this.ScanAndUpdateTimers();
            this.ActionImageChanged();
        }

        public void RefreshParameters()
        {
            this.ScanAndUpdateTimers();
        }

        private void ScanAndUpdateTimers()
        {
            try
            {
                // Check if service is initialized
                if (!TimerConfigurationService.IsInitialized)
                {
                    PluginLog.Info("TimerConfigurationService not initialized yet, skipping parameter update");
                    return;
                }

                var config = TimerConfigurationService.Instance.GetConfiguration();
                var currentTimerIds = new HashSet<string>();

                PluginLog.Info($"Scanning {config.Timers.Count} timers from configuration");
                
                // Add parameters for each active timer
                foreach (var timer in config.Timers)
                {
                    if (timer.IsActive)
                    {
                        currentTimerIds.Add(timer.Id);
                        
                        // Format: HH:MM:SS display
                        var duration = $"{timer.Hours:D2}:{timer.Minutes:D2}:{timer.Seconds:D2}";
                        var displayName = $"{timer.Name} ({duration})";
                        
                        if (!this._timerIds.ContainsKey(timer.Id))
                        {
                            // Add each timer as a parameter
                            this.AddParameter(timer.Id, displayName, this.GroupName);
                            this._timerIds[timer.Id] = timer.Id;
                            this._timerDisplayNames[timer.Id] = displayName;
                            PluginLog.Info($"Added timer parameter: {timer.Id} - {displayName}");
                        }
                        else
                        {
                            // Update parameter if name changed  
                            this.RemoveParameter(timer.Id);
                            this.AddParameter(timer.Id, displayName, this.GroupName);
                            this._timerDisplayNames[timer.Id] = displayName;
                            PluginLog.Info($"Updated timer parameter: {timer.Id} - {displayName}");
                        }
                    }
                }

                // Remove parameters for timers that are no longer active or were deleted
                var removedTimers = this._timerIds.Keys.Except(currentTimerIds).ToList();
                foreach (var timerId in removedTimers)
                {
                    this.RemoveParameter(timerId);
                    this._timerIds.Remove(timerId);
                    this._timerDisplayNames.Remove(timerId);
                    PluginLog.Info($"Removed timer parameter: {timerId}");
                }

                PluginLog.Info("Timer parameters updated successfully");
                this.ActionImageChanged();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to update timer parameters");
            }
        }

        protected override void RunCommand(string actionParameter)
        {
            // Parse the selected timer
            var config = TimerConfigurationService.Instance.GetConfiguration();
            _currentTimer = config.Timers.Find(t => t.Id == actionParameter);
            
            if (_currentTimer == null)
            {
                PluginLog.Error($"Timer not found: {actionParameter}");
                return;
            }

            PluginLog.Info($"Starting timer: {_currentTimer.Name}");

            // If already running, stop it (toggle behavior)
            if (_isRunning)
            {
                this._countdownTimer.Stop();
                _isRunning = false;
                _remainingMilliseconds = 0;
                this.ActionImageChanged(actionParameter);
                PluginLog.Info($"Timer stopped: {actionParameter}");
                return;
            }

            var totalMilliseconds = _currentTimer.GetTotalMilliseconds();
            if (totalMilliseconds <= 0)
            {
                PluginLog.Warning($"Timer has no duration: {_currentTimer.Name}");
                return;
            }

            this._startTime = DateTime.Now;
            this._remainingMilliseconds = totalMilliseconds;
            this._isRunning = true;

            // Set up delay timer for haptic feedback
            var delayTimer = new System.Timers.Timer(totalMilliseconds);
            delayTimer.Elapsed += (s, e) => OnDelayElapsed(_currentTimer.Haptic);
            delayTimer.AutoReset = false;
            delayTimer.Start();

            this._countdownTimer.Start();
            this.ActionImageChanged(actionParameter);
        }

        private void OnCountdownTickHandler(object sender, ElapsedEventArgs e)
        {
            // Placeholder for cleanup
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

            if (_currentTimer != null && _currentTimer.Id == actionParameter && _isRunning && _remainingMilliseconds > 0)
            {
                var remainingTime = TimeSpan.FromMilliseconds(_remainingMilliseconds);
                
                // Format based on image size
                if (imageSize == PluginImageSize.Width60)
                {
                    // Compact format for small buttons
                    if (remainingTime.TotalHours >= 1)
                        return $"{timer.Name}\r\n{remainingTime:h\\:mm\\:ss}";
                    else
                        return $"{timer.Name}\r\n{remainingTime:mm\\:ss}";
                }
                else
                {
                    // Full format for larger buttons
                    return $"{timer.Name}\r\n{remainingTime:hh\\:mm\\:ss}";
                }
            }
            else
            {
                return $"{timer.Name}\r\n{timer.GetDisplayTime()}";
            }
        }

        private void OnDelayElapsed(string haptic)
        {
            this._countdownTimer.Stop();
            this._remainingMilliseconds = 0;
            this._isRunning = false;

            if (!string.IsNullOrEmpty(haptic))
            {
                this.Plugin.PluginEvents.RaiseEvent(haptic);
            }

            this.ActionImageChanged();
        }

        private void OnCountdownTick(object sender, ElapsedEventArgs e)
        {
            if (!this._isRunning) return;

            var elapsed = (int)(DateTime.Now - this._startTime).TotalMilliseconds;
            this._remainingMilliseconds = Math.Max(0, _currentTimer.GetTotalMilliseconds() - elapsed);
            
            if (this._remainingMilliseconds <= 0)
            {
                this._countdownTimer.Stop();
                this._isRunning = false;
            }
            
            this.ActionImageChanged();
        }
    }
}

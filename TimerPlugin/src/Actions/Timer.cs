namespace Loupedeck.TimerPlugin.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Timers;

    public class Timer : ActionEditorCommand
    {
        private const String EventName = "buttonPress";
        private System.Timers.Timer _delayTimer;

        private Int32 _selectedTime = 0;
        private String _selectedHaptic = "";
        public Timer()
        {
            // Set basic properties
            this.Name = "Timer";
            this.DisplayName = "Timer";
            this.GroupName = "Timers";
            this.Description = "A timer that uses the haprics when finished";

            // Add controls for user configuration
            this.ActionEditor.AddControlEx(
    new ActionEditorSlider(name: "Time", labelText: "Time (sec):", description: "Adjust time")
      .SetValues(minimumValue: 1, maximumValue: 60, defaultValue: 15, step: 1));

            this.ActionEditor.AddControlEx(
    new ActionEditorListbox(name: "hapticAlarm", labelText: "Select a haptic:"));
            // Subscribe to events
            this.ActionEditor.ControlValueChanged += this.OnControlValueChanged;
        }

        private void OnControlValueChanged(Object sender, ActionEditorControlValueChangedEventArgs e)
        {
            if (e.ControlName.EqualsNoCase("Time"))
            {
                this._selectedTime = (Int32)e.ActionEditorState.GetControlValue("Time").ParseInt32();

                // Update display name based on user input
                e.ActionEditorState.SetDisplayName($"Timer:\n {this._selectedTime}");

                PluginLog.Info("Starting setting up timer");
                Int32 timeInSeconds = this._selectedTime * 1;
                this._delayTimer = new System.Timers.Timer(timeInSeconds * 1000); // time in milliseconds
                this._delayTimer.Elapsed += OnDelayElapsed;
                this._delayTimer.AutoReset = false; // One-shot timer
                PluginLog.Info("Time set for: " + timeInSeconds * 1000);
            }
            if (e.ControlName.EqualsNoCase("hapticAlarm"))
            {
                this._selectedHaptic = (Int32)e.ActionEditorState.GetControlValue("hapticAlarm").ParseInt32();


            }
        }

        protected override Boolean OnLoad()
        {
            this.Plugin.PluginEvents.AddEvent(EventName, "Button Press", "This haptic event is sent when the user presses the button");
            return true;
        }

        protected override Boolean RunCommand(ActionEditorActionParameters actionParameters)
        {
            PluginLog.Info("button pressed");
            PluginLog.Info("Starting timer");

            this._delayTimer.Stop(); // Stop any existing timer
            this._delayTimer.Start();
            return true;

        }
        private void OnDelayElapsed(object sender, ElapsedEventArgs e)
        {
            PluginLog.Info("Starting haptics");
            // Trigger event on UI thread if needed, but RaiseEvent should be thread-safe
            this.Plugin.PluginEvents.RaiseEvent(EventName);
        }
    }
}

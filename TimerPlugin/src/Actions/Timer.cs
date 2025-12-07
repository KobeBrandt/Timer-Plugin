namespace Loupedeck.TimerPlugin.Actions
{
    using System;
    using System.Drawing;
    using System.Net.Mime;
    using System.Timers;

    public class Timer : ActionEditorCommand
    {
        private System.Timers.Timer _delayTimer;

        private String _selectedHaptic = "";
        private TimeInMilliSeconds time = new();
        public Timer()
        {
            // Set basic properties
            this.Name = "Timer";
            this.DisplayName = "Timer";
            this.GroupName = "Timers";
            this.Description = "A timer that uses the haprics when finished";



            // Add controls for user configuration
            PluginLog.Info("Adding controls");

            this.ActionEditor.AddControlEx(
new ActionEditorListbox(name: "hapticAlarm", labelText: "Select a haptic:"));



            this.ActionEditor.AddControlEx(
    new ActionEditorSlider(name: "TimeSec", labelText: "Time (sec):", description: "Adjust seconds")
        .SetValues(minimumValue: 0, maximumValue: 59, defaultValue: 0, step: 1));

            this.ActionEditor.AddControlEx(
new ActionEditorSlider(name: "TimeMin", labelText: "Time (min):", description: "Adjust minutes")
.SetValues(minimumValue: 0, maximumValue: 59, defaultValue: 0, step: 1));

            this.ActionEditor.AddControlEx(
    new ActionEditorSlider(name: "TimeHour", labelText: "Time (hour):", description: "Adjust hours")
        .SetValues(minimumValue: 0, maximumValue: 12, defaultValue: 0, step: 1));



            PluginLog.Info("Added controls");

            // Subscribe to events
            this.ActionEditor.ListboxItemsRequested += this.OnListboxItemsRequested;

            this.ActionEditor.ControlValueChanged += this.OnControlValueChanged;
        }

        private void OnListboxItemsRequested(Object sender, ActionEditorListboxItemsRequestedEventArgs e)
        {
            if (e.ControlName.EqualsNoCase("hapticAlarm"))
            {
                // Add items to the listbox
                e.AddItem(name: "jingle", displayName: "Jingle", description: "jingle haptic");
                e.AddItem(name: "knock", displayName: "Knock", description: "knock haptic");
                e.AddItem(name: "ringing", displayName: "Ringing", description: "ringing haptic");
                

                e.SetSelectedItemName("jingle");
            }
        }

        private void OnControlValueChanged(Object sender, ActionEditorControlValueChangedEventArgs e)
        {
            if (e.ControlName.EqualsNoCase("hapticAlarm"))
            {
                this._selectedHaptic = e.ActionEditorState.GetControlValue("hapticAlarm").Trim();
                PluginLog.Info("Starting haptics: " + this._selectedHaptic);
                this.Plugin.PluginEvents.RaiseEvent(this._selectedHaptic);
            }
            if (e.ControlName.EqualsNoCase("TimeSec"))
            {
                this.time.Seconds = (Int32)e.ActionEditorState.GetControlValue("TimeSec").ParseInt32();
            }
            if (e.ControlName.EqualsNoCase("TimeMin"))
            {
                this.time.Minutes = (Int32)e.ActionEditorState.GetControlValue("TimeMin").ParseInt32();
            }
            if (e.ControlName.EqualsNoCase("TimeHour"))
            {
                this.time.Hours = (Int32)e.ActionEditorState.GetControlValue("TimeHour").ParseInt32();
            }
            try
            {
                PluginLog.Info("Starting setting timer for " + this.time.ReturnMilliSeconds());
                this._delayTimer = new System.Timers.Timer(this.time.ReturnMilliSeconds());
                this._delayTimer.Elapsed += this.OnDelayElapsed;
                this._delayTimer.AutoReset = false;
                PluginLog.Info("Time set for: " + this.time.ReturnMilliSeconds());
            }
            catch (Exception ex) {
                PluginLog.Error(ex.Message);
            }

            // Update display name based on user input
            // PluginLog.Info($"Timer\n {this._selectedHaptic} \n {this.time.Hours}:{this.time.Minutes}:{this.time.Seconds}");
            // e.ActionEditorState.SetDisplayName($"Timer\n {this._selectedHaptic} \n {this.time.Hours}:{this.time.Minutes}:{this.time.Seconds}");
        }

        protected override Boolean OnLoad()
        {
            this.Plugin.PluginEvents.AddEvent("knock", "Knock", "The haptic knock event");
            this.Plugin.PluginEvents.AddEvent("ringing", "Ringing", "The haptic ringing event");
            this.Plugin.PluginEvents.AddEvent("jingle", "Jingle", "The haptic jingle event");
            return true;
        }

        protected override Boolean RunCommand(ActionEditorActionParameters actionParameters)
        {
            PluginLog.Info("button pressed");
            PluginLog.Info("Starting timer");

            this._delayTimer.Stop(); // Stop any existing timer
            this._delayTimer.Start();
            
            this.ActionImageChanged();
            
            return true;
        }
        private void OnDelayElapsed(object sender, ElapsedEventArgs e)
        {
            PluginLog.Info("Starting haptics: " + this._selectedHaptic);
            // Trigger event on UI thread if needed, but RaiseEvent should be thread-safe
            this.Plugin.PluginEvents.RaiseEvent(this._selectedHaptic);
        }

        protected override BitmapImage GetCommandImage(ActionEditorActionParameters actionParameters, Int32 imageWidth,
            Int32 imageHeight)
        {
            PluginLog.Info($"{this.time.Hours}:{this.time.Minutes}:{this.time.Seconds}");
            using (var bitmapBuilder = new BitmapBuilder(imageWidth, imageHeight))
            {
                
                bitmapBuilder.DrawText($"{this.time.Hours}:{this.time.Minutes}:{this.time.Seconds}");

                return bitmapBuilder.ToImage();
            }
        }
    }
}

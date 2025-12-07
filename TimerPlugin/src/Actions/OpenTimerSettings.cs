namespace Loupedeck.TimerPlugin.Actions
{
    using System;
    using System.Diagnostics;
    using Loupedeck;

    public class OpenTimerSettings : PluginDynamicCommand
    {
        public OpenTimerSettings()
            : base("Open Timer Settings", "Configure your timers", "Timers")
        {
        }

        protected override void RunCommand(string actionParameter)
        {
            try
            {
                var url = "http://localhost:34521";
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
                PluginLog.Info("Opened timer settings in browser");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to open timer settings");
            }
        }

        protected override string GetCommandDisplayName(string actionParameter, PluginImageSize imageSize)
        {
            return "Timer\r\nSettings";
        }
    }
}

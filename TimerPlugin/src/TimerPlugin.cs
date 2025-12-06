namespace Loupedeck.TimerPlugin
{
    using System;
    using Loupedeck.TimerPlugin.Services;

    // This class contains the plugin-level logic of the Loupedeck plugin.

    public class TimerPlugin : Plugin
    {
        // Gets a value indicating whether this is an API-only plugin.
        public override Boolean UsesApplicationApiOnly => true;

        // Gets a value indicating whether this is a Universal plugin or an Application plugin.
        public override Boolean HasNoApplication => true;

        // Initializes a new instance of the plugin class.
        public TimerPlugin()
        {
            // Initialize the plugin log.
            PluginLog.Init(this.Log);

            // Initialize the plugin resources.
            PluginResources.Init(this.Assembly);

            // Initialize configuration service early so actions can access it
            var pluginDataDir = this.GetPluginDataDirectory();
            TimerConfigurationService.Initialize(pluginDataDir);
        }

        // This method is called when the plugin is loaded.
        public override void Load()
        {
            // Set plugin reference for haptic feedback
            WebConfigurationService.Instance.SetPlugin(this);
            
            // Start web configuration server
            WebConfigurationService.Instance.Start();

            // Add haptic events
            this.PluginEvents.AddEvent("knock", "Knock", "The haptic knock event");
            this.PluginEvents.AddEvent("ringing", "Ringing", "The haptic ringing event");
            this.PluginEvents.AddEvent("jingle", "Jingle", "The haptic jingle event");

            PluginLog.Info("Timer Plugin loaded successfully");
        }

        // This method is called when the plugin is unloaded.
        public override void Unload()
        {
            WebConfigurationService.Instance?.Dispose();
            TimerConfigurationService.Instance?.Dispose();
            PluginLog.Info("Timer Plugin unloaded");
        }
    }
}

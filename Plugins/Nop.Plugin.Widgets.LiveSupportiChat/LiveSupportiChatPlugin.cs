using Nop.Core.Plugins;
using Nop.Services.Cms;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Routing;

namespace Nop.Plugin.Widgets.LiveSupportiChat
{
    public class LiveSupportiChatPlugin : BasePlugin, IWidgetPlugin
    {
        private readonly ISettingService settingsService;
        private readonly LiveSupportiChatSettings settings;

        public LiveSupportiChatPlugin(ISettingService settingService, LiveSupportiChatSettings settings)
        {
            this.settingsService = settingService;
            this.settings = settings;
        }

        /// <summary>
        /// Gets widget zones where this widget should be rendered
        /// </summary>
        /// <returns>Widget zones</returns>
        public IList<string> GetWidgetZones()
        {
            return !string.IsNullOrWhiteSpace(settings.WidgetZone)
                       ? new List<string>() { settings.WidgetZone }
                       : new List<string>() { "body_end_html_tag_before" };
        }

        /// <summary>
        /// Gets a route for provider configuration
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "WidgetsLiveSupportiChat";
            routeValues = new RouteValueDictionary() { { "Namespaces", "Nop.Plugin.Widgets.LiveSupportiChat.Controllers" }, { "area", null } };
        }

        /// <summary>
        /// Gets a route for displaying widget
        /// </summary>
        /// <param name="widgetZone">Widget zone where it's displayed</param>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetDisplayWidgetRoute(string widgetZone, out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "PublicInfo";
            controllerName = "WidgetsLiveSupportiChat";
            routeValues = new RouteValueDictionary()
            {
                {"Namespaces", "Nop.Plugin.Widgets.LiveSupportiChat.Controllers"},
                {"area", null},
                {"widgetZone", widgetZone}
            };
        }

        /// <summary>
        /// Install plugin
        /// </summary>
        public override void Install()
        {
            LiveSupportiChatSettings settings = new LiveSupportiChatSettings()
            {
                WidgetZone = "body_end_html_tag_before",
                WidgetCodeSnippet = String.Empty
            };
            this.settingsService.SaveSetting(settings);

            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.LiveSupportiChat.LiveChat", "Live Chat");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.LiveSupportiChat.WidgetCodeSnippet", "Code snippet");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.LiveSupportiChat.WidgetCodeSnippet.Hint", "Enter your code snippet from LiveSupporti here.");

            base.Install();
        }

        /// <summary>
        /// Uninstall plugin
        /// </summary>
        public override void Uninstall()
        {
            settingsService.DeleteSetting<LiveSupportiChatSettings>();

            this.DeletePluginLocaleResource("Plugins.Widgets.LiveSupportiChat.LiveChat");
            this.DeletePluginLocaleResource("Plugins.Widgets.LiveSupportiChat.WidgetCodeSnippet");
            this.DeletePluginLocaleResource("Plugins.Widgets.LiveSupportiChat.WidgetCodeSnippet.Hint");

            base.Uninstall();
        }
    }
}

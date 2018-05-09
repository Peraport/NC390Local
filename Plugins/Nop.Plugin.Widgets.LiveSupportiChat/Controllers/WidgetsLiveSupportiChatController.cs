using Nop.Plugin.Widgets.LiveSupportiChat.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Web.Framework.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Nop.Plugin.Widgets.LiveSupportiChat.Controllers
{
    public class WidgetsLiveSupportiChatController : BasePluginController
    {
        private readonly LiveSupportiChatSettings settings;
        private readonly ISettingService settingService;
        private readonly ILocalizationService localizationService;

        public WidgetsLiveSupportiChatController(LiveSupportiChatSettings settings, ISettingService settingService, ILocalizationService localizationService)
        {
            this.settings = settings;
            this.settingService = settingService;
            this.localizationService = localizationService;
        }

        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {
            ConfigurationModel model = new ConfigurationModel()
            {
                WidgetCodeSnippet = settings.WidgetCodeSnippet
            };

            return View("~/Plugins/Widgets.LiveSupportiChat/Views/WidgetsLiveSupportiChat/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //save settings
            settings.WidgetCodeSnippet = model.WidgetCodeSnippet;
            settingService.SaveSetting(settings);

            SuccessNotification(localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        [ChildActionOnly]
        public ActionResult PublicInfo(string widgetZone)
        {
            PublicInfoModel model = new PublicInfoModel()
            {
                WidgetCodeSnippet = settings.WidgetCodeSnippet
            };

            return View("~/Plugins/Widgets.LiveSupportiChat/Views/WidgetsLiveSupportiChat/PublicInfo.cshtml", model);
        }
    }
}

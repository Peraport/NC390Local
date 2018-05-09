using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Nop.Plugin.Widgets.LiveSupportiChat.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        public ConfigurationModel()
        {
        }

        [NopResourceDisplayName("Plugins.Widgets.LiveSupportiChat.WidgetCodeSnippet")]
        [AllowHtml]
        public string WidgetCodeSnippet { get; set; }
    }
}

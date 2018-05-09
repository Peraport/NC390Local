using Nop.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Widgets.LiveSupportiChat
{
    public class LiveSupportiChatSettings : ISettings
    {
        public string WidgetCodeSnippet { get; set; }
        public string WidgetZone { get; set; }
    }
}

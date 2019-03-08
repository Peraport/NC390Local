using System.Web.Mvc;
using System.Web.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.Peraport.WidgetFinancial
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            routes.MapRoute("Plugin.Peraport.WidgetFinancial.EfaturaDetail",
                 "Fi/EfaturaDetail",
                 new { controller = "Fi", action = "EfaturaDetail" },
                 new[] { "Nop.Plugin.Peraport.WidgetFinancial.Controllers" }
            );

            routes.MapRoute("Plugin.Peraport.WidgetFinancial.Efatura",
                 "Fi/Efatura",
                 new { controller = "Fi", action = "Efatura" },
                 new[] { "Nop.Plugin.Peraport.WidgetFinancial.Controllers" }
            );

            routes.MapRoute("Plugin.Peraport.WidgetFinancial.OrderTracking",
                 "Fi/KargoTakip",
                 new { controller = "Fi", action = "OrderTracking" },
                 new[] { "Nop.Plugin.Peraport.WidgetFinancial.Controllers" }
            );
            routes.MapRoute("Plugin.Peraport.WidgetFinancial.Dashboard",
                 "Fi/Ozet",
                 new { controller = "Fi", action = "Dashboard" },
                 new[] { "Nop.Plugin.Peraport.WidgetFinancial.Controllers" }
            );

            routes.MapRoute("Plugin.Peraport.WidgetFinancial.Faturalar",
                 "Fi/Faturalarim",
                 new { controller = "Fi", action = "Documents" },
                 new[] { "Nop.Plugin.Peraport.WidgetFinancial.Controllers" }
            );

            routes.MapRoute("Plugin.Peraport.WidgetFinancial.Siparisler",
                 "Fi/Siparislerim",
                 new { controller = "Fi", action = "Orders" },
                 new[] { "Nop.Plugin.Peraport.WidgetFinancial.Controllers" }
            );

            routes.MapRoute("Plugin.Peraport.WidgetFinancial.Detaylar",
                 "Fi/Hesap-Detaylari",
                 new { controller = "Fi", action = "He" },
                 new[] { "Nop.Plugin.Peraport.WidgetFinancial.Controllers" }
            );

            routes.MapRoute("Plugin.Peraport.WidgetFinancial.Yaslandirma",
                 "Fi/Hesap-Yaslandirma",
                 new { controller = "Fi", action = "HeYas" },
                 new[] { "Nop.Plugin.Peraport.WidgetFinancial.Controllers" }
            );

            routes.MapRoute("Plugin.Peraport.WidgetFinancial.HesapOzetV",
                 "Fi/Hesap-Ozeti",
                 new { controller = "Fi", action = "HesapOzetV" },
                 new[] { "Nop.Plugin.Peraport.WidgetFinancial.Controllers" }
            );

            routes.MapRoute("Plugin.Peraport.WidgetFinancial.KKOdeme",
                 "Fi/KrediKartiOdeme",
                 new { controller = "IsPay", action = "KKPayment" },
                 new[] { "Nop.Plugin.Peraport.WidgetFinancial.Controllers" }
            );

            routes.MapRoute("Plugin.Peraport.WidgetFinancial.KKFatura",
                 "Fi/KrediKartiFaturaOdeme",
                 new { controller = "Fi", action = "Test" },
                 new[] { "Nop.Plugin.Peraport.WidgetFinancial.Controllers" }
            );

            //Cancel
            routes.MapRoute("Plugin.Peraport.WidgetFinancial.PaymentFail",
                 "Plugins/Fi/PaymentFail",
                 new { controller = "Fi", action = "PaymentFail" },
                 new[] { "Nop.Plugin.Peraport.WidgetFinancial.Controllers" }
            );
            //Success
            routes.MapRoute("Plugin.Peraport.WidgetFinancial.PaymentSuccess",
                 "Fi/PaymentSuccess",
                 new { controller = "Fi", action = "PaymentSuccess" },
                 new[] { "Nop.Plugin.Peraport.WidgetFinancial.Controllers" }
            );


            routes.MapRoute("Plugin.Peraport.WidgetFinancial.Pos",
                 "Pos/KKHome",
                 new { controller = "Pos", action = "Index" },
                 new[] { "Nop.Plugin.Peraport.WidgetFinancial.Controllers" }
            );
        }
        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}

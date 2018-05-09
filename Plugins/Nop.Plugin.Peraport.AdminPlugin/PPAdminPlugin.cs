using Nop.Core.Plugins;
using Nop.Web.Framework.Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Routing;

namespace Nop.Plugin.Peraport.AdminPlugin
{
    public class PPAdminPlugin : BasePlugin, IAdminMenuPlugin
    {
        public void ManageSiteMap(SiteMapNode rootNode)
        {
            var menuItemCustomerSync = new SiteMapNode()
            {
                SystemName = "DevPlugin",
                Title = "Müşteri Senkronizasyon",
                ControllerName = "PPCustomer",
                ActionName = "CustomerSync",
                Visible = true,
                RouteValues = new RouteValueDictionary() { { "area", null } },
            };
            var menuItemProductAktar = new SiteMapNode()
            {
                SystemName = "DevPlugin",
                Title = "Ürün Senkronizasyon",
                ControllerName = "PPProduct",
                ActionName = "ProductSync",
                Visible = true,
                RouteValues = new RouteValueDictionary() { { "area", null } },
            };

            var menuItemProductDescpription = new SiteMapNode()
            {
                SystemName = "DevPlugin",
                Title = "Ürün Açıklamaları Güncelle",
                ControllerName = "PPProduct",
                ActionName = "UpdateProductDescription",
                Visible = true,
                RouteValues = new RouteValueDictionary() { { "area", null } },
            };

            var menuItemOrderAktar = new SiteMapNode()
            {
                SystemName = "DevPlugin",
                Title = "Sipariş Senkronizasyon",
                ControllerName = "PPOrder",
                ActionName = "OrderSync",
                Visible = true,
                RouteValues = new RouteValueDictionary() { { "area", null } },
            };

            var pluginNode = rootNode.ChildNodes.FirstOrDefault(x => x.SystemName == "Third party plugins");
            if (pluginNode != null)
            {
                pluginNode.ChildNodes.Add(menuItemCustomerSync);
                pluginNode.ChildNodes.Add(menuItemProductAktar);
                pluginNode.ChildNodes.Add(menuItemProductDescpription);
                pluginNode.ChildNodes.Add(menuItemOrderAktar);

            }
            else
            {
                rootNode.ChildNodes.Add(menuItemCustomerSync);
                rootNode.ChildNodes.Add(menuItemProductAktar);
                rootNode.ChildNodes.Add(menuItemProductDescpription);
                pluginNode.ChildNodes.Add(menuItemOrderAktar);
            }
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
            controllerName = "PPAdmin";
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Peraport.AdminPlugin.Controllers" }, { "area", null } };
        }
        /// <summary>
        /// Install plugin
        /// </summary>
        public override void Install()
        {
            PluginManager.MarkPluginAsInstalled(this.PluginDescriptor.SystemName);
        }
        /// <summary>
        /// Uninstall plugin
        /// </summary>
        public override void Uninstall()
        {
            PluginManager.MarkPluginAsUninstalled(this.PluginDescriptor.SystemName);
        }
    }
}

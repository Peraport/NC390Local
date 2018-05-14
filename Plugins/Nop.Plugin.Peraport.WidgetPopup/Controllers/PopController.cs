using Nop.Core;
using Nop.Services.Authentication;
using Nop.Services.Configuration;
using Nop.Services.Payments;
using Nop.Web.Framework.Controllers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Nop.Plugin.Peraport.WidgetPopup.Controllers
{
    public class PopController : BasePluginController
    {
        #region ctor
        private readonly IWorkContext _workContext;
        private readonly ISettingService _settingService;
        private readonly IAuthenticationService _authService;
        private readonly IWebHelper _webHelper;
        private readonly HttpContextBase _httpContext;


        //private readonly IOrderModelFactory _orderModelFactory;
        //private readonly IOrderService _orderService;
        //private readonly IPaymentService _paymentService;
        //private readonly IStoreService _storeService;


        public PopController(
                                        IWorkContext workContext,
                                        ISettingService settingService,
                                        IAuthenticationService authService,
                                        IWebHelper webHelper,
                                      HttpContextBase httpContext
            //IStoreService storeService,
            //IOrderModelFactory orderModelFactory,
            //IOrderService orderService,
            //IPaymentService paymentService
            )
        {
            _workContext = workContext;
            _settingService = settingService;
            _authService = authService;
            _webHelper = webHelper;
            _httpContext = httpContext;
            //_storeService = storeService;
            //_orderModelFactory = orderModelFactory;
            //_orderService = orderService;
            //_paymentService = paymentService;

        }
        #endregion

        public ActionResult PublicInfo(string widgetZone, object additionalData = null)
        {
            try
            {
                var a=_webHelper.GetUrlReferrer();
                var RuleRoles = new List<string>();
                
                RuleRoles.Add("Administrators"); RuleRoles.Add("EXP"); RuleRoles.Add("SHOP"); RuleRoles.Add("VDM");
                var userroles = _workContext.CurrentCustomer.CustomerRoles.Select(x => x.Name).ToList();

                ViewBag.ShowMe = RuleRoles.Where(x => userroles.Contains(x)).FirstOrDefault() != null && _httpContext.Request.Path=="/" ? "OK" : "";
                return View("~/Plugins/Peraport.WidgetPopup/Views/Pop/PublicInfo.cshtml");
            }
            catch (Exception Ex)
            {
                return View("~/Plugins/Peraport.WidgetPopup/Views/Pop/PublicInfo.cshtml");
            }

        }


        [AdminAuthorize]
        public ActionResult Configure()
        {
            return View("~/Plugins/Peraport.WidgetPopup/Views/Pop/Configure.cshtml");
        }

        [HttpPost]
        [AdminAuthorize]
        public ActionResult Configure(FormCollection form)
        {
            return View("~/Plugins/Peraport.WidgetPopup/Views/Pop/Configure.cshtml");
        }
    }
}

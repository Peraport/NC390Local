using Nop.Core;
using Nop.Plugin.Peraport.AdminPlugin.Models;
using Nop.Services.Authentication;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Orders;
using Nop.Services.Stores;
using Nop.Web.Factories;
using Nop.Web.Framework.Controllers;
using Nop.Web.PPService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Routing;

namespace Nop.Plugin.Peraport.AdminPlugin.Controllers
{
    public class PPAdminController : BasePluginController
    {
        #region ctor
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly ISettingService _settingService;
        private readonly IAuthenticationService _authService;
        private readonly IWebHelper _webHelper;

        private readonly IOrderModelFactory _orderModelFactory;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;

        private readonly ICustomerService _customerService;

        public PPAdminController(IWorkContext workContext,
                                       IStoreService storeService,
                                       ISettingService settingService,
                                       IAuthenticationService authService,
                                       ICustomerService customerService,
                                       IWebHelper webHelper,
                                        IOrderService orderService,
                                        IOrderProcessingService orderProcessingService
            )
        {
            _workContext = workContext;
            _storeService = storeService;
            _settingService = settingService;
            _authService = authService;
            _webHelper = webHelper;
            _orderService = orderService;
            _orderProcessingService = orderProcessingService;
            _customerService = customerService;

        }
        #endregion


        [AdminAuthorize]
        public ActionResult AdminPluginTest()
        {
            return View("~/Plugins/Peraport.AdminPlugin/Views/PPAdmin/AdminPluginTest.cshtml");
        }


        [AdminAuthorize]
        public ActionResult Configure()
        {
            return View("~/Plugins/Peraport.AdminPlugin/Views/PPAdmin/Configure.cshtml");
        }

        public ActionResult SiparisCokla()
        {
            return View("~/Plugins/Peraport.AdminPlugin/Views/PPAdmin/GeneralView.cshtml");
        }

        [AdminAuthorize]
        public ActionResult SiparisCoklaProcess(int Oid, string Cuids)
        {
            var rm = new ResultModel { NAME="Sipariş Çoklama", NOTE= "Sipariş No:" + Oid + " Müşteri Kodları :" + Cuids, LOGS = new List<string>() };
            var order = _orderService.GetOrderById(Oid);
            if (order == null || order.Deleted )
            {//Sipariş yok
                rm.LOGS.Add("XXX Sipariş Bulunamadı ya da silinmiş.");
            }
            else
            {
                /**/

                var _cuids = Cuids.Split(',');
                NopServiceClient client = new NopServiceClient();
                client.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Anonymous;
                var matches = client.getMatchingList("BASE_STR", "Customer", _cuids);
                foreach (var item in matches)
                {
                    var customer = _customerService.GetCustomerById((int)item.MATCH_ID);
                    if (customer != null)
                    {
                        order.Customer = customer;
                        order.CustomerId = customer.Id;
                        order.CreatedOnUtc = DateTime.Now;
                        if (customer.Addresses.Count > 0)
                        {
                            var adr = customer.Addresses.First();
                            order.BillingAddress = adr;
                            order.BillingAddressId = adr.Id;

                            order.ShippingAddress = adr;
                            order.ShippingAddressId = adr.Id;
                            try
                            {
                                _orderService.InsertOrder(order);
                                //_orderProcessingService.ReOrder(order);
                                rm.LOGS.Add("OK ->"+item.BASE_STR + " için sipariş Oluşturuldu ");
                            }
                            catch (Exception Ex)
                            {
                                rm.LOGS.Add("XX Sipariş ("+ item.BASE_STR + ") Oluşturulurken Hata Oluştu : " + Ex.Message);
                            }
                        }
                        else
                        {
                            rm.LOGS.Add("XX Müşteri Adresi Yok -> " + item.BASE_STR);
                        }
                    }
                    else
                    {//Customer Yok
                        rm.LOGS.Add("XX Müşteri Bulunamadı -> "+ item.BASE_STR);
                    }
                }
            }
            return View("~/Plugins/Peraport.AdminPlugin/Views/PPAdmin/GeneralViewPartial.cshtml",rm);
        }


        [HttpPost]
        [AdminAuthorize]
        public ActionResult Configure(FormCollection form)
        {
            var bm = new BasicModel();
            return View("~/Plugins/Peraport.AdminPlugin/Views/PPAdmin/Configure.cshtml", bm);
        }

    }
}

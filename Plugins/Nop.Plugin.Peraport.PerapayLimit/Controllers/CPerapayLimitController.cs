using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Peraport.PerapayLimit.Models;
using Nop.Services.Configuration;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;
using Nop.Web.PPService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Nop.Plugin.Peraport.PerapayLimit.Controllers
{
    public class CPerapayLimitController : BasePaymentController
    {
        #region ctor
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly ISettingService _settingService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IShoppingCartService _ShoppingCartService;
        private readonly IStoreContext _storeContext;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        public CPerapayLimitController(IWorkContext workContext,
            IOrderProcessingService orderProcessingService,
            IOrderService orderService,
            IStoreContext storeContext,
            IStoreService storeService,
            ISettingService settingService,
            IOrderTotalCalculationService orderTotalCalculationService,
            IShoppingCartService ShoppingCartService
            )
        {
            this._workContext = workContext;
            this._storeService = storeService;
            this._settingService = settingService;
            this._orderTotalCalculationService = orderTotalCalculationService;
            this._ShoppingCartService = ShoppingCartService;
            this._storeContext = storeContext;
            this._orderService = orderService;
            this._orderProcessingService = orderProcessingService;
        }
        #endregion

        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {
            var model = new PerapayLimitModel();
            return View("~/Plugins/Peraport.PerapayLimit/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure(PerapayLimitModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            return Configure();
        }


        [ChildActionOnly]
        public ActionResult PaymentInfo()
        {
            var customer = _workContext.CurrentCustomer;
            var cart = customer.ShoppingCartItems
                .Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart)
                .Where(sci => sci.StoreId == _storeContext.CurrentStore.Id)
                .ToList();

            var total = _orderTotalCalculationService.GetShoppingCartTotal(cart);


            NopServiceClient client = new NopServiceClient();
            client.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Anonymous;
            var finans = client.GetCustomerFinancial(customer.Id.ToString());

            var userMatch = client.getMatch("MATCH_ID", "Customer", customer.Id.ToString());

            var orders = _orderService.SearchOrders(0, 0, customer.Id);
            var orders1 = orders.Where(x => x.OrderStatus == OrderStatus.Processing).ToList();
            decimal ordersTotal = 0;
            if (orders1.Count > 0)
            {
                ordersTotal = orders1.Sum(x => x.OrderTotal);
            }
            var model = new PerapayLimitModel();
            model.Limit = finans.CURRENT_LIMIT - ordersTotal;
            model.Amount = total ?? 0;
            model.State = model.Limit >= model.Amount ? 1 : 0;
            return View("~/Plugins/Peraport.PerapayLimit/Views/PaymentInfo.cshtml", model);
        }

        public override IList<string> ValidatePaymentForm(FormCollection form)
        {
            var warnings = new List<string>();
            var s = form["State"];
            if (s.ToString() == "0")
                warnings.Add("Limit Yetersiz... Lütfen farklı bir ödeme yöntemi seçiniz.");
            return warnings;
        }

        public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest();
            return paymentInfo;
        }


    }
}

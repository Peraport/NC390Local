using Nop.Core.Plugins;
using Nop.Services.Payments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core.Domain.Orders;
using System.Web.Routing;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Peraport.PerapayLimit.Controllers;
using Nop.Core;
using Nop.Services.Stores;
using Nop.Services.Configuration;
using Nop.Services.Orders;
using Nop.Web.PPService;
namespace Nop.Plugin.Peraport.PerapayLimit
{
    public class PerapayLimitProcessor : BasePlugin, IPaymentMethod
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
        public PerapayLimitProcessor(IWorkContext workContext,
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
        public string PaymentMethodDescription
        {
            get
            {
                return "Müşteri Limitini Kullanarak Ödeme Yapılır";
            }
        }

        public RecurringPaymentType RecurringPaymentType
        {
            get { return RecurringPaymentType.NotSupported; }
        }

        public PaymentMethodType PaymentMethodType
        {
            get { return PaymentMethodType.Redirection; }
        }

        public bool SkipPaymentInfo
        {
            get { return false; }
        }

        public bool SupportCapture
        {
            get { return false; }
        }

        public bool SupportPartiallyRefund
        {
            get { return false; }
        }

        public bool SupportRefund
        {
            get { return false; }
        }

        public bool SupportVoid
        {
            get { return false; }
        }

        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            throw new NotImplementedException();
        }

        public bool CanRePostProcessPayment(Order order)
        {
            throw new NotImplementedException();
        }

        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            throw new NotImplementedException();
        }

        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            decimal additional = cart.Aggregate(0.0m, (current, shoppingCartItem) => current + shoppingCartItem.Product.Price);
            return additional * (decimal)0.0;
        }

        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "CPerapayLimit";
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Peraport.PerapayLimit.Controllers" }, { "area", null } };
        }

        public Type GetControllerType()
        {
            return typeof(CPerapayLimitController);
        }

        public void GetPaymentInfoRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "PaymentInfo";
            controllerName = "CPerapayLimit";
            routeValues = new RouteValueDictionary() { { "Namespaces", "Nop.Plugin.Peraport.PerapayLimit.Controllers" }, { "area", null } };
        }

        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            return false;
        }

        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            /*Sipariş Oluştu*/
            var order = postProcessPaymentRequest.Order;
            order.PaymentStatus = PaymentStatus.Paid;
            order.OrderStatus = OrderStatus.Processing;
            _orderService.UpdateOrder(order);

            try
            {
                //Siparişi Erp ye akratıyoruz.
                NopServiceClient client = new NopServiceClient();
                client.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Anonymous;
                var a = client.CopyOrdersNopToErp("Id", order.Id.ToString());
            }
            catch (Exception ex)
            {
            }

        }

        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            /*Sipariş oluşmadan önce. Validate olduktan sonra.*/
            return new ProcessPaymentResult() { NewPaymentStatus = PaymentStatus.Pending };
        }

        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            throw new NotImplementedException();
        }

        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            throw new NotImplementedException();
        }

        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            var result = new VoidPaymentResult();
            result.NewPaymentStatus = PaymentStatus.Voided;
            return result;
        }
    }
}

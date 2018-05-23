using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web;
using System.Web.Routing;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Services.Configuration;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;
using Nop.Plugin.Payments.PeraPay.Models;
using Nop.Services.Localization;
using Nop.Web.PPService;
using System.ServiceModel.Security;
using System.Globalization;
using Nop.Services.Authentication;

namespace Nop.Plugin.Payments.PeraPay.Controllers
{
    public class PayController : BasePaymentController
    {
        #region ctor
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly PayPaymentSettings _PayPaymentSettings;
        private readonly IShoppingCartService _ShoppingCartService;
        private readonly IStoreContext _storeContext;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IAuthenticationService _authService;

        public PayController(IWorkContext workContext,
            IOrderProcessingService orderProcessingService,
            IOrderService orderService,
            IAuthenticationService authService,
            IStoreContext storeContext,
            IStoreService storeService,
            ISettingService settingService,
            PayPaymentSettings PayPaymentSettings,
            IOrderTotalCalculationService orderTotalCalculationService,
            IShoppingCartService ShoppingCartService,
            ILocalizationService localizationService
            )
        {
            this._workContext = workContext;
            this._storeService = storeService;
            this._settingService = settingService;
            this._localizationService = localizationService;
            this._PayPaymentSettings = PayPaymentSettings;
            _authService = authService;
            this._orderTotalCalculationService = orderTotalCalculationService;
            this._ShoppingCartService = ShoppingCartService;
            this._storeContext = storeContext;
            this._orderService = orderService;
            this._orderProcessingService = orderProcessingService;
        }
        #endregion

        public bool AfterKKFail(Order order)
        {
            _orderProcessingService.ReOrder(order);
            _orderService.DeleteOrder(order);
            return true;
        }
        public bool AfterKKSuccess(Order order, string Amount, string Banka, string Taksit)
        {
            try
            {
                NopServiceClient client = new NopServiceClient();
                client.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Anonymous;

                //Ödemeyi Erp ye aktarma
                List<MATCHING_S> MatchList = new List<MATCHING_S>();
                MATCHING_S m1 = new MATCHING_S { TABLE_NAME = "Payment", BASE_ID = order.Id, MATCH_ID = order.CustomerId, BASE_STR = "" };
                MatchList.Add(m1);

                var xxx = client.MatchTwoRecord(MatchList.ToArray());

                var amount = Amount;
                decimal d = decimal.Parse(amount, CultureInfo.InvariantCulture);
                var customer = _authService.GetAuthenticatedCustomer();
                ErpPayment p = new ErpPayment { DATE = DateTime.Now, TOTAL = d, NOPCUSTOMER_ID = customer.Id, ERPCUSTOMER_ID = -1, ERPCUSTOMER_CODE = "", NOTE = "", CODE = "WEB" };
                var r = client.CopyNopPaymentToErp(p);

                string paynote = "";
                if (r.CODE == "OK")
                    paynote = "İşlem Başarılı. " + amount + " TL kartınızdan çekildi ve hesabınıza aktarıldı.";
                else
                    paynote = "UYARI. " + amount + " TL kartınızdan çekildi ancak hesabınıza aktarılırken bir problem oluştu. Konuyla ilgili Lütfen Müşteri Temsilcinize bilgi veriniz.";

                //Siparişi Güncelle
                order.OrderStatus = OrderStatus.Processing;
                order.PaymentStatus = PaymentStatus.Paid;
                OrderNote on1 = new OrderNote();
                on1.Note = "Tutar : " + Amount + "Banka : " + Banka + " Taksit : " + Taksit + " Note : " + paynote;
                on1.OrderId = order.Id;
                on1.DisplayToCustomer = true;
                on1.CreatedOnUtc = DateTime.UtcNow;
                order.OrderNotes.Add(on1);
                _orderService.UpdateOrder(order);

                //Siparişi Erp ye akratıyoruz.
                var a = client.CopyOrdersNopToErp("Id", order.Id.ToString());

                return true;

            }
            catch (Exception ex)
            {
                return true;
            }
        }

        [ValidateInput(false)]
        public ActionResult LPSuccessGB(FormCollection form)
        {
            string oid = Session["ORDERID"].ToString();
            string amount = Session["AMOUNT"].ToString();
            string banka = Session["BANKA"].ToString();
            string taksit = Session["TAKSIT"].ToString();

            Order order = _orderService.GetOrderById(Convert.ToInt32(oid));
            if (order == null || order.Deleted || _workContext.CurrentCustomer.Id != order.CustomerId)
                return new HttpUnauthorizedResult();

            AfterKKSuccess(order, amount, banka, taksit);
            return RedirectToAction("Completed", "Checkout", new { area = "" });
        }

        [ValidateInput(false)]
        public ActionResult LPFailGB()
        {
            try
            {
                if (Session["ORDERID"] != null)
                {
                    string oid = Session["ORDERID"].ToString();

                    var order = _orderService.GetOrderById(Convert.ToInt32(oid));
                    if (order == null || order.Deleted || _workContext.CurrentCustomer.Id != order.CustomerId)
                        return new HttpUnauthorizedResult();

                    AfterKKFail(order);

                    string returnMessage = "";
                    string errorMessage = "";
                    string refNo = "";
                    returnMessage = Request.Form["mderrormessage"];
                    errorMessage = Request.Form["errmsg"];
                    refNo = Request.Form["mdstatus"];
                    if (errorMessage == "") errorMessage = returnMessage;
                    var res = returnMessage + " Ref No: " + refNo;

                    if (errorMessage.Contains("0809")) errorMessage += "Bayi Kodu ve Kredi Kartı numaranız sisteme kayıtlı olmadığı için işleminiz gerçekleşmemiştir. Lütfen müşteri temsilciniz ile iletişime geçiniz. ";

                    ViewBag.Amount = Request.Form["amount"];
                    ViewBag.Warning = errorMessage;
                    ViewBag.Message = res;

                    return View("~/Plugins/Payments.PeraPay/Views/Pay/PeraPayFail.cshtml", new PeraPayBasicModel { NAME = "Hata Kodu : " + refNo + "   Hata Mesajı : " + errorMessage });
                }
                else
                    return View("~/Plugins/Payments.PeraPay/Views/Pay/PeraPayFail.cshtml", new PeraPayBasicModel { NAME = "Banka ile iletişim Kurulamadı. (SESSİONOIDNULL)" });
            }
            catch (Exception ex)
            {
                return View("~/Plugins/Payments.PeraPay/Views/Pay/PeraPayFail.cshtml", new PeraPayBasicModel { NAME = "Banka ile iletişim Kurulamadı. (CATCHEX)" });
            }
        }

        [ValidateInput(false)]
        public ActionResult LPSuccessYKB(FormCollection form)
        {
                string oid = Session["ORDERID"].ToString();
                string amount = Session["AMOUNT"].ToString();
                string banka = Session["BANKA"].ToString();
                string taksit = Session["TAKSIT"].ToString();

                Order order = _orderService.GetOrderById(Convert.ToInt32(oid));
                if (order == null || order.Deleted || _workContext.CurrentCustomer.Id != order.CustomerId) return new HttpUnauthorizedResult();

                AfterKKSuccess(order, amount, banka, taksit);
                return RedirectToAction("Completed", "Checkout", new { area = "" });
        }

        [ValidateInput(false)]
        public ActionResult LPFailYKB()
        {
            try
            {
                if (Session["ORDERID"] != null)
                {
                    string oid = Session["ORDERID"].ToString();

                    var order = _orderService.GetOrderById(Convert.ToInt32(oid));
                    if (order == null || order.Deleted || _workContext.CurrentCustomer.Id != order.CustomerId)
                        return new HttpUnauthorizedResult();

                    AfterKKFail(order);

                    string returnMessage = "";
                    string errorMessage = "";
                    string refNo = "";
                    returnMessage = Request.Params["returnmessage"];
                    errorMessage = Request.Params["errmsg"];
                    refNo = Request.Params["ykbrefno"];
                    var res = returnMessage + " Ref No: " + refNo;
                    ViewBag.Amount = Request.Params["amount"];
                    ViewBag.Warning = errorMessage;
                    ViewBag.Message = res;

                    return View("~/Plugins/Payments.PeraPay/Views/Pay/PeraPayFail.cshtml", new PeraPayBasicModel { NAME = "Hata Kodu : " + refNo + "   Hata Mesajı : " + errorMessage });
                }
                else
                    return View("~/Plugins/Payments.PeraPay/Views/Pay/PeraPayFail.cshtml", new PeraPayBasicModel { NAME = "Banka ile iletişim Kurulamadı. (SESSİONOIDNULL)" });
            }
            catch (Exception ex)
            {
                return View("~/Plugins/Payments.PeraPay/Views/Pay/PeraPayFail.cshtml", new PeraPayBasicModel { NAME = "Banka ile iletişim Kurulamadı. (CATCHEX)" });
            }
        }

        [ValidateInput(false)]
        public ActionResult LPSuccess(string RC, string Message, string TransactionId, string AuthCode, string MaskedPan)
        {
            string oid = Session["ORDERID"].ToString();
            string amount = Session["AMOUNT"].ToString();
            string banka = Session["BANKA"].ToString();
            string taksit = Session["TAKSIT"].ToString();

            Order order = _orderService.GetOrderById(Convert.ToInt32(oid));
            if (order == null || order.Deleted || _workContext.CurrentCustomer.Id != order.CustomerId)
                return new HttpUnauthorizedResult();

            AfterKKSuccess(order, amount, banka, taksit);
            return RedirectToAction("Completed", "Checkout", new { area = "" });
        }


        public ActionResult OrderToCart(string oid)
        {
            int OrderId = Convert.ToInt32(oid);
            var order = _orderService.GetOrderById(OrderId);
            if (order == null || order.Deleted || _workContext.CurrentCustomer.Id != order.CustomerId)
                return new HttpUnauthorizedResult();

            _orderProcessingService.ReOrder(order);
            _orderService.DeleteOrder(order);

            PeraPayBasicModel model = new PeraPayBasicModel { NAME = "Banka ile iletişim Kurulamadı." };

            return View("~/Plugins/Payments.PeraPay/Views/Pay/PeraPayFail.cshtml", model);
        }

        [ValidateInput(false)]
        public ActionResult LPCancel(string RC, string ErrorCode, string Message, string TransactionId)
        {
            try
            {
                if (Session["ORDERID"] != null)
                {
                    string oid = Session["ORDERID"].ToString();

                    var order = _orderService.GetOrderById(Convert.ToInt32(oid));
                    if (order == null || order.Deleted || _workContext.CurrentCustomer.Id != order.CustomerId)
                        return new HttpUnauthorizedResult();

                    AfterKKFail(order);

                    if (RC != null && RC != "")
                        return View("~/Plugins/Payments.PeraPay/Views/Pay/PeraPayFail.cshtml", new PeraPayBasicModel { NAME = "Hata Kodu : " + RC + "   Hata Mesajı : " + Message });
                    else
                        return View("~/Plugins/Payments.PeraPay/Views/Pay/PeraPayFail.cshtml", new PeraPayBasicModel { NAME = "Banka ile iletişim Kurulamadı. (RCNULL)" });
                }
                else
                    return View("~/Plugins/Payments.PeraPay/Views/Pay/PeraPayFail.cshtml", new PeraPayBasicModel { NAME = "Banka ile iletişim Kurulamadı. (SESSİONOIDNULL)" });

            }
            catch (Exception ex)
            {
                return View("~/Plugins/Payments.PeraPay/Views/Pay/PeraPayFail.cshtml", new PeraPayBasicModel { NAME = "Banka ile iletişim Kurulamadı. (CATCHEX)" });
            }
        }


        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {

            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var PP3DPaymentSettings = _settingService.LoadSetting<PayPaymentSettings>(storeScope);

            var model = new PeraPayConfigurationModel();
            model.UseSandbox = PP3DPaymentSettings.UseSandbox;
            model.TransactModeId = Convert.ToInt32(PP3DPaymentSettings.TransactMode);
            model.TransactionKey = PP3DPaymentSettings.TransactionKey;
            model.LoginId = PP3DPaymentSettings.LoginId;
            model.AdditionalFee = PP3DPaymentSettings.AdditionalFee;
            model.AdditionalFeePercentage = PP3DPaymentSettings.AdditionalFeePercentage;
            model.TransactModeValues =

                new SelectList(new List<SelectListItem>
{
    new SelectListItem { Selected = true, Text = string.Empty, Value = "-1"},
    new SelectListItem { Selected = false, Text = "Homeowner", Value = "1"},
    new SelectListItem { Selected = false, Text = "Contractor", Value = "2"},
});


            //PP3DPaymentSettings.TransactMode.ToSelectList();

            model.ActiveStoreScopeConfiguration = storeScope;
            if (storeScope > 0)
            {
                model.UseSandbox_OverrideForStore = _settingService.SettingExists(PP3DPaymentSettings, x => x.UseSandbox, storeScope);
                model.TransactModeId_OverrideForStore = _settingService.SettingExists(PP3DPaymentSettings, x => x.TransactMode, storeScope);
                model.TransactionKey_OverrideForStore = _settingService.SettingExists(PP3DPaymentSettings, x => x.TransactionKey, storeScope);
                model.LoginId_OverrideForStore = _settingService.SettingExists(PP3DPaymentSettings, x => x.LoginId, storeScope);
                model.AdditionalFee_OverrideForStore = _settingService.SettingExists(PP3DPaymentSettings, x => x.AdditionalFee, storeScope);
                model.AdditionalFeePercentage_OverrideForStore = _settingService.SettingExists(PP3DPaymentSettings, x => x.AdditionalFeePercentage, storeScope);
            }

            return View("~/Plugins/Payments.PeraPay/Views/Pay/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure(PeraPayConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var pp3dPaymentSettings = _settingService.LoadSetting<PayPaymentSettings>(storeScope);

            //save settings
            pp3dPaymentSettings.UseSandbox = model.UseSandbox;
            pp3dPaymentSettings.TransactMode = (TransactMode)model.TransactModeId;
            pp3dPaymentSettings.TransactionKey = model.TransactionKey;
            pp3dPaymentSettings.LoginId = model.LoginId;
            pp3dPaymentSettings.AdditionalFee = model.AdditionalFee;
            pp3dPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            if (model.UseSandbox_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(pp3dPaymentSettings, x => x.UseSandbox, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(pp3dPaymentSettings, x => x.UseSandbox, storeScope);

            if (model.TransactModeId_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(pp3dPaymentSettings, x => x.TransactMode, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(pp3dPaymentSettings, x => x.TransactMode, storeScope);

            if (model.TransactionKey_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(pp3dPaymentSettings, x => x.TransactionKey, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(pp3dPaymentSettings, x => x.TransactionKey, storeScope);

            if (model.LoginId_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(pp3dPaymentSettings, x => x.LoginId, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(pp3dPaymentSettings, x => x.LoginId, storeScope);

            if (model.AdditionalFee_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(pp3dPaymentSettings, x => x.AdditionalFee, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(pp3dPaymentSettings, x => x.AdditionalFee, storeScope);

            if (model.AdditionalFeePercentage_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(pp3dPaymentSettings, x => x.AdditionalFeePercentage, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(pp3dPaymentSettings, x => x.AdditionalFeePercentage, storeScope);

            //now clear settings cache
            _settingService.ClearCache();

            return Configure();
        }


        [ChildActionOnly]
        public ActionResult PaymentInfo()
        {
            var model = new PeraPayPaymentInfoModel();
            var customer = _workContext.CurrentCustomer;
            var cart = customer.ShoppingCartItems
                .Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart)
                .Where(sci => sci.StoreId == _storeContext.CurrentStore.Id)
                .ToList();

            var total = _orderTotalCalculationService.GetShoppingCartTotal(cart);
            model.Tutar = total.ToString();
            return View("~/Plugins/Payments.PeraPay/Views/Pay/PaymentInfo.cshtml", model);
        }


        public override IList<string> ValidatePaymentForm(FormCollection form)
        {
            var warnings = new List<string>();
            var secenek = form["banka"];
            if (secenek == "") warnings.Add("Lütfen Banka ve Taksit Tutarı seçiniz...");
            return warnings;
        }

        public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest();
            paymentInfo.CustomValues.Add("Banka", form["banka"]);
            return paymentInfo;
        }
    }
}
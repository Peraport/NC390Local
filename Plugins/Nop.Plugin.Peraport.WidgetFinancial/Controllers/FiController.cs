using Nop.Admin.Models.Orders;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Peraport.WidgetFinancial.Models;
using Nop.Services.Authentication;
using Nop.Services.Configuration;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Stores;
using Nop.Web.Factories;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Security;
using Nop.Web.PPService;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel.Security;
using System.Text;
using System.Web.Mvc;

namespace Nop.Plugin.Peraport.WidgetFinancial.Controllers
{
    public class FiController : BasePluginController
    {
        #region ctor
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly ISettingService _settingService;
        private readonly IAuthenticationService _authService;
        private readonly IWebHelper _webHelper;

        private readonly IOrderModelFactory _orderModelFactory;
        private readonly IOrderService _orderService;
        private readonly IPaymentService _paymentService;
        public FiModel _fi;
        public FinanceModel _fim;
        public string cargoCustomerNumber = "315611334";
        public string cargoCustomerPassword = "LSGYIHCL";

        public FiController(
                                        IWorkContext workContext,
                                        IStoreService storeService,
                                        ISettingService settingService,
                                        IAuthenticationService authService,
                                        IWebHelper webHelper,
                                        IOrderModelFactory orderModelFactory,
                                        IOrderService orderService,
                                        IPaymentService paymentService
            )
        {
            _workContext = workContext;
            _storeService = storeService;
            _settingService = settingService;
            _authService = authService;
            _webHelper = webHelper;
            _orderModelFactory = orderModelFactory;
            _orderService = orderService;
            _paymentService = paymentService;

            #region _fi

            _fi = new FiModel();// getFi();

            var customer = _authService.GetAuthenticatedCustomer();
            if (customer != null)
            {
                NopServiceClient client = new NopServiceClient();
                client.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Anonymous;
                _fim = client.GetCustomerFinancial(customer.Id.ToString());

                #region Sipariş Status Update
                var orders = _orderService.SearchOrders(0, 0, customer.Id);
                if (orders.Count > 0)
                {
                    var aaa = orders.Select(x => x.Id).ToArray();
                    var b = client.GetNopOrdersErpInvoiced(aaa);
                    foreach (var item in b)
                    {
                        var order = _orderService.GetOrderById(item);
                        order.OrderStatus = OrderStatus.Complete;
                        _orderService.UpdateOrder(order);
                    }
                }
                #endregion

                #region CurrentLimit Hesaplama (Compated olmayan siparişler de ekleniyor...)

                orders = _orderService.SearchOrders(0, 0, customer.Id);
                var orders1 = orders.Where(x => x.OrderStatus == OrderStatus.Processing).ToList();
                decimal ordersTotal = 0;
                if (orders1.Count > 0)
                {
                    ordersTotal = orders1.Sum(x => x.OrderTotal);
                }
                _fim.CURRENT_LIMIT -= ordersTotal;
                #endregion

                _fi.LIMIT = _fim.LIMIT;
                _fi.BAKIYE = _fim.BAKIYE;
                _fi.CURRENT_LIMIT = _fim.CURRENT_LIMIT;
                _fi.CODE = _fim.ERPCUSTOMER_CODE;
                _fi.NAME = _fim.ERPCUSTOMER_NAME;// "TEST İLETİŞİM LTD. ŞTİ.";// bakiye.Val2;
                _fi.isAuth = customer != null;
            }
            else { }

            #endregion

        }
        #endregion
        public FinanceReportModel GetDocuments(long Id)
        {
            try
            {
                NopServiceClient client = new NopServiceClient();
                client.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Anonymous;
                var a = client.GetCustomerFinancialReport(Id.ToString());
                return a;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        [NopHttpsRequirement(SslRequirement.Yes)]
        public virtual ActionResult CustomerOrdersForShippingTracking()
        {
            var model = _orderModelFactory.PrepareCustomerOrderListModel();
            ShipTrackingModel stm = new ShipTrackingModel();
            /*
                ServiceMusKarTakipSoapClient client = new ServiceMusKarTakipSoapClient();
                var a = client.MusteriKargoTakipByTarih("35615719", "356TST2425XGHPRFTG", DateTime.Today.AddDays(-20).ToString("dd.MM.yyyy"), DateTime.Today.AddDays(-5).ToString("dd.MM.yyyy"), "1");
             */
            return View(model);
        }

        public ActionResult OrderTracking(int oid)
        {
            return Redirect(GetMngUrl(oid));

            //try
            //{
            //    return View("~/Plugins/Peraport.WidgetFinancial/Views/Fi/OrderTracking.cshtml", GetOrderTracking(oid));
            //}
            //catch (Exception)
            //{
            //    return View("~/Plugins/Peraport.WidgetFinancial/Views/Fi/OrderTracking.cshtml", new ShipTrackingModel());
            //}
        }

        public string GetMngUrl(int oid)
        {
            NopServiceClient client = new NopServiceClient();
            client.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Anonymous;
            var _model = client.GetCustomerLimit("MNG",oid.ToString());
            return _model.CUSTOMER_CODE;
        }

        public ShipTrackingModel GetOrderTracking(int oid)
        {
            ShipTrackingModel stm = new ShipTrackingModel();
            try
            {
                stm.ProcessResult = "Kullanıcı Hatası";
                var customer = _workContext.CurrentCustomer;
                stm.ProcessResult = "Sipariş";
                var order = _orderService.GetOrderById(oid);
                /*
                    ServiceMusKarTakipSoapClient client = new ServiceMusKarTakipSoapClient();
                    var xxx = client.MusteriKargoTakipBySiparis(cargoCustomerNumber, cargoCustomerPassword, oid.ToString(), "1");
                */
                stm.OrderId = oid;
                stm.InvoiceCode = "";
                stm.OrderDate = order.CreatedOnUtc.ToString("dd.MM.yyyy");
                stm.SenderName = customer.SystemName;
                stm.CustomerName = order.Customer.SystemName;
                stm.CustomerHandleDate = DateTime.Now.ToString();
                stm.CustomerHandleName = "test müşteri";
            }
            catch (Exception ex)
            {
                stm.InvoiceCode = "";
                stm.OrderDate = "";
                stm.SenderName = "";
                stm.CustomerName = "";
                stm.CustomerHandleDate = "";
                stm.CustomerHandleName = "";
            }
            return stm;
        }

        public List<Order> GetOrders()//Kullanıcı Tüm siparişleri
        {
            OrderListModel model = new OrderListModel();
            if (_workContext.CurrentVendor != null)
            {
                model.VendorId = _workContext.CurrentVendor.Id;
            }
            var orderStatusIds = !model.OrderStatusIds.Contains(0) ? model.OrderStatusIds : null;
            var paymentStatusIds = !model.PaymentStatusIds.Contains(0) ? model.PaymentStatusIds : null;
            var shippingStatusIds = !model.ShippingStatusIds.Contains(0) ? model.ShippingStatusIds : null;
            var filterByProductId = 0;
            var orders = _orderService.SearchOrders(storeId: model.StoreId,
                vendorId: model.VendorId,
                productId: filterByProductId,
                warehouseId: model.WarehouseId,
                paymentMethodSystemName: model.PaymentMethodSystemName,
                createdFromUtc: null,//startDateValue,
                createdToUtc: null,//endDateValue,
                osIds: orderStatusIds,
                psIds: paymentStatusIds,
                ssIds: shippingStatusIds,
                billingEmail: model.BillingEmail,
                billingLastName: model.BillingLastName,
                billingCountryId: model.BillingCountryId,
                orderNotes: model.OrderNotes,
                pageIndex: 0,
                pageSize: 999);
            var cust = _authService.GetAuthenticatedCustomer();
            return orders.Where(x => x.Customer.Id == cust.Id).ToList();
        }

        public DateTime ConvertToDateDef(string s, DateTime def)
        {
            try
            {
                return Convert.ToDateTime(s);
            }
            catch (Exception)
            {
                return def;
            }
        }

        public ActionResult Orders(long Id = 1)
        {
            try
            {
                ViewBag.StartDate = DateTime.Now.Year - 1 + "-01-01";
                ViewBag.EndDate = DateTime.Now.ToString("yyy-MM-dd");
                ViewBag.OrderType = Id;
                return View("~/Plugins/Peraport.WidgetFinancial/Views/Fi/Orders.cshtml");
            }
            catch (Exception ex)
            {
                return View();
            }

        }

        public ActionResult OrdersPartial(int OrderType = 1, string StartDate = "", string EndDate = "")
        {
            DateTime sd = ConvertToDateDef(StartDate, DateTime.Today.AddYears(-10));
            DateTime ed = ConvertToDateDef(EndDate, DateTime.Today.AddDays(1)).AddDays(1);
            var orders = GetOrders();
            if (orders != null)
            {
                orders = orders.Where(x => x.CreatedOnUtc <= ed && x.CreatedOnUtc >= sd).ToList();
                if (OrderType == 2) orders = orders.Where(x => x.OrderStatusId == 10).ToList();//Bekleyen Siparişler
                if (OrderType == 3) orders = orders.Where(x => x.OrderStatusId == 20).ToList();//Yoldaki Siparişler
            }
            return View("~/Plugins/Peraport.WidgetFinancial/Views/Fi/OrdersPartial.cshtml", orders);
        }

        public ActionResult Documents(long Id = 1)
        {
            try
            {
                ViewBag.StartDate = DateTime.Now.Year - 1 + "-01-01";
                ViewBag.EndDate = DateTime.Now.ToString("yyy-MM-dd");
                ViewBag.OrderType = Id;
                return View("~/Plugins/Peraport.WidgetFinancial/Views/Fi/Documents.cshtml");
            }
            catch (Exception ex)
            {
                return View();
            }

        }
        public ActionResult DocumentsPartial(int OrderType = 1, string StartDate = "", string EndDate = "")
        {
            try
            {
                var sd = ConvertToDateDef(StartDate, DateTime.Today);
                var ed = ConvertToDateDef(EndDate, DateTime.Today);
                var customer = _authService.GetAuthenticatedCustomer();
                if (customer != null)
                {
                    NopServiceClient client = new NopServiceClient();
                    client.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Anonymous;
                    var invoicemodel = client.GetCustomerFinancialReport(customer.Id.ToString());

                    decimal kismiodenmis = 0;
                    invoicemodel = SetYaslandirma(invoicemodel, customer.Id, out kismiodenmis);

                    if (OrderType == 2)
                        invoicemodel.ROWS = invoicemodel.ROWS.Where(x => x.STATE == "N" || x.STATE == "D").ToArray();

                    if (OrderType == 3)
                        invoicemodel.ROWS = invoicemodel.ROWS.Where(x => x.STATE == "D").ToArray();

                    return View("~/Plugins/Peraport.WidgetFinancial/Views/Fi/DocumentsPartial.cshtml", invoicemodel);
                }
                return View("~/Plugins/Peraport.WidgetFinancial/Views/Fi/DocumentsPartial.cshtml");
            }
            catch (Exception ex)
            {
                return View();
            }

        }

        public ActionResult He(long Id = 1)
        {
            try
            {
                ViewBag.StartDate = DateTime.Now.Year - 1 + "-01-01";
                ViewBag.EndDate = DateTime.Now.ToString("yyy-MM-dd");
                ViewBag.OrderType = Id;
                return View("~/Plugins/Peraport.WidgetFinancial/Views/Fi/He.cshtml");
            }
            catch (Exception ex)
            {
                return View();
            }

        }
        public ActionResult HePartial(int OrderType = 1, string StartDate = "", string EndDate = "")
        {
            try
            {
                var sd = ConvertToDateDef(StartDate, DateTime.Today);
                var ed = ConvertToDateDef(EndDate, DateTime.Today);
                var customer = _authService.GetAuthenticatedCustomer();
                if (customer != null)
                {
                    NopServiceClient client = new NopServiceClient();
                    client.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Anonymous;
                    var heModel = client.GetCustomerFinancialHesapEkstre(customer.Id.ToString(), StartDate, EndDate);

                    return View("~/Plugins/Peraport.WidgetFinancial/Views/Fi/HePartial.cshtml", heModel);
                }
                return View("~/Plugins/Peraport.WidgetFinancial/Views/Fi/HePartial.cshtml");
            }
            catch (Exception ex)
            {
                return View();
            }

        }

        public ActionResult HeYas()
        {
            try
            {
                var customer = _authService.GetAuthenticatedCustomer();
                if (customer != null)
                {
                    NopServiceClient client = new NopServiceClient();
                    client.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Anonymous;
                    var heModel = client.GetCustomerFinancialYaslandirmaEkstre(customer.Id.ToString());

                    return View("~/Plugins/Peraport.WidgetFinancial/Views/Fi/HeYasPartial.cshtml", heModel);
                }
                return View("~/Plugins/Peraport.WidgetFinancial/Views/Fi/HeYasPartial.cshtml");
            }
            catch (Exception ex)
            {
                return View();
            }

        }


        public ActionResult Dashboard()
        {
            var customer = _authService.GetAuthenticatedCustomer();
            DashModel model = new DashModel { NAME = _fi.NAME, LIMIT = _fi.LIMIT, BAKIYE = _fi.BAKIYE, CURRENT_LIMIT = _fi.CURRENT_LIMIT };
            var orders = GetOrders();
            if (orders != null)
            {
                model.ALL_ORDERS_QTY = orders.Count();
                model.All_ORDERS_VAL = orders.Sum(x => x.OrderTotal);

                var orders1 = orders.Where(x => x.OrderStatusId == 10).ToList();//Bekleyen Siparişler
                model.NOT_YET_ORDERS_QTY = orders1.Count();
                model.NOT_YET_ORDERS_VAL = orders1.Sum(x => x.OrderTotal);

                var orders2 = orders.Where(x => x.OrderStatusId == 20).ToList();//Yoldaki Siparişler
                model.SHIPPED_ORDERS_QTY = orders2.Count();
                model.SHIPPED_ORDERS_VAL = orders2.Sum(x => x.OrderTotal);
            }

            var documents = GetDocuments(customer.Id);
            if (documents.ROWS != null)
            {
                decimal kismiodenmis = 0;
                documents = SetYaslandirma(documents, customer.Id, out kismiodenmis);
                model.ALL_DOCUMENTS_QTY = documents.ROWS != null ? documents.ROWS.Count() : 0;
                model.ALL_DOCUMENTS_VAL = documents.ROWS != null ? documents.ROWS.Sum(x => x.TOTALPRICE) : 0;

                var notpaydocuments = documents.ROWS.Where(x => x.STATE == "N" || x.STATE == "D").ToList();
                model.NOT_PAID_DOCUMENTS_QTY = notpaydocuments != null ? notpaydocuments.Count() : 0;
                model.NOT_PAID_DOCUMENTS_VAL = notpaydocuments != null ? notpaydocuments.Sum(x => x.TOTALPRICE) - kismiodenmis : 0;

                var delayeddocuments = documents.ROWS.Where(x => x.STATE == "D").ToList();
                model.DELAYED_DOCUMENTS_QTY = delayeddocuments != null ? delayeddocuments.Count() : 0;
                model.DELAYED_DOCUMENTS_VAL = delayeddocuments != null ? delayeddocuments.Sum(x => x.TOTALPRICE) - kismiodenmis : 0;
            }

            return PartialView("~/Plugins/Peraport.WidgetFinancial/Views/Fi/DashBoard.cshtml", model);
        }

        public FinanceReportModel SetYaslandirma(FinanceReportModel d, long Id, out decimal kismiodenmis)
        {
            kismiodenmis = 0;
            NopServiceClient client = new NopServiceClient();
            client.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Anonymous;
            var b = client.GetCustomerFinancialYaslandirmaEkstre(Id.ToString());

            var b1 = b.ROWS.GroupBy(z => z.DATE).Select(y => new Documentx { DATE = y.Key, PRICE = y.Sum(_ => _.PRICE) }).ToArray();
            foreach (var item in d.ROWS.OrderBy(x => x.DATE).ToArray())
            {
                var i = b1.Where(x => x.DATE == item.DATE).FirstOrDefault();
                if (i != null)
                {
                    //if (i.PRICE == 0) item.PAYMENTSTATUS = 0;
                    if (i.PRICE >= item.TOTALPRICE)
                    {//bu fatura turarından daha fazla veya eşit ödeme yapılmış.
                        i.PRICE -= item.TOTALPRICE;
                        item.STATE = "Y";//Ödendi  D:Gecikti N:Ödenmedi
                    }
                    else if (i.PRICE > 0)
                    {
                        //item.TOTALPRICE -= i.PRICE;
                        kismiodenmis += i.PRICE;
                        i.PRICE = 0;
                    }
                }
            }
            return d;
        }

        [AdminAuthorize]
        public ActionResult Configure()
        {
            var m = new FiModel();
            m.NAME = "Deneme";
            m.BAKIYE = 127000;
            return View("~/Plugins/Widgets.MyCustomWidget/Views/Fi/Configure.cshtml", m);
        }

        public void CopyNopOrderToErp(int custid)
        {
            NopServiceClient client = new NopServiceClient();
            client.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Anonymous;
            var userMatch = client.getMatch("MATCH_ID", "Customer", custid.ToString());
            var cserp = client.GetGeneralSettings("CS_ERP");
            List<int> osIds = new List<int> { 10, 20 };
            var orders = _orderService.SearchOrders(0, 0, custid, 0, 0, 0, 0, null, null, null, osIds);//İşlemde olan siparişler
            //////// Aktarılmamış siparişleri bul
            var matchList = client.getMatchingList("MATCH_ID", "Order", orders.Select(x => x.Id.ToString()).ToArray());//Aktarılanlar.
            var ml = matchList.Select(x => x.MATCH_ID).ToList();
            var nomatchOrders = orders.Where(x => !ml.Contains(x.Id)).ToList();//Aktarılanların içinde olmayan siparişler
                                                                               ///////
                                                                               //      ...

        }

        [HttpPost]
        [AdminAuthorize]
        public ActionResult Configure(FormCollection form)
        {
            var m = new FiModel();
            m.NAME = "Deneme";
            m.BAKIYE = 127000;
            return View("~/Plugins/Peraport.WidgetFinancial/Views/Fi/Configure.cshtml", m);
        }


        public ActionResult PublicInfo(string widgetZone, object additionalData = null)
        {
            try
            {
                return View("~/Plugins/Peraport.WidgetFinancial/Views/Fi/PublicInfo.cshtml", _fi);
            }
            catch (Exception Ex)
            {
                var customer = _authService.GetAuthenticatedCustomer();
                var m = new FiModel();
                m.NAME = "Servise Ulaşılamadı";
                m.BAKIYE = 0;
                m.LIMIT = 0;
                m.isAuth = customer != null;
                return View("~/Plugins/Peraport.WidgetFinancial/Views/Fi/PublicInfo.cshtml", m);
            }

        }

        public ActionResult PeraLeftMenu()
        {
            return PartialView("~/Plugins/Peraport.WidgetFinancial/Views/Fi/PeraLeftMenu.cshtml");
        }

        public ActionResult FaturaTakip()//Yaşlandırma ve Fatura Ödeme
        {
            try
            {
                var customer = _authService.GetAuthenticatedCustomer();
                if (customer != null)
                {
                    NopServiceClient client = new NopServiceClient();
                    client.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Anonymous;
                    var heModel = client.GetCustomerFinancialYaslandirmaEkstre(customer.Id.ToString());

                    return View("~/Plugins/Peraport.WidgetFinancial/Views/Fi/FaturaTakip.cshtml", heModel);
                }
                return View("~/Plugins/Peraport.WidgetFinancial/Views/Fi/FaturaTakip.cshtml");
            }
            catch (Exception ex)
            {
                return View();
            }
        }


        public ActionResult Banka()
        {
            return View("~/Plugins/Peraport.WidgetFinancial/Views/Fi/Banka.cshtml");
        }
        [HttpPost]
        public ActionResult Banka(FormCollection form)
        {
            try
            {
                var customer = _authService.GetAuthenticatedCustomer();
                NopServiceClient client = new NopServiceClient();
                client.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Anonymous;
                var m = new List<MATCHING_S>();
                m.Add(new MATCHING_S { TABLE_NAME = "Payment", MATCH_ID = customer.Id, MATCH_STR = form["price"] + "_" + form["banka"] + "_" + DateTime.Now, BASE_STR = form["note"] });
                var a = client.MatchTwoRecord(m.ToArray());
                return Json("<font color=green>KAYDINIZ BAŞARI İLE OLUŞTURULMUŞTUR...</font>", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json("<font color=red>KAYDINIZ ALINAMADI. LÜTFEN TEKRAR DENEYİNİZ...</font>", JsonRequestBehavior.AllowGet);
            }

            //return View("~/Plugins/Peraport.WidgetFinancial/Views/Fi/Banka.cshtml");
        }

        public ActionResult Infos()
        {
            var customer = _authService.GetAuthenticatedCustomer();
            NopServiceClient client = new NopServiceClient();
            client.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Anonymous;
            MATCHING_S m = client.getMatch("MATCH_ID", "Info", customer.Id.ToString());
            InfoModel im = new InfoModel();
            if (m != null)
            {
                im.ERP_CODE = m.BASE_STR;

                var b = m.MATCH_STR.Split('_');
                try
                {
                    im.VD = b[0];
                    im.VN = b[1];
                    im.UNVAN = b[2];
                    im.EMAIL = b[3];
                    im.TEL = b[4];
                }
                catch (Exception)
                {
                }
            }
            else
            {
                im.UNVAN = customer.SystemName;
                if (customer.Addresses.Count > 0)
                {
                    var adres = customer.Addresses.FirstOrDefault();
                    im.UNVAN = adres.Company;
                    im.TEL = adres.PhoneNumber;
                    im.EMAIL = adres.Email;
                    im.ERP_CODE = customer.Username;
                }
            }

            return View("~/Plugins/Peraport.WidgetFinancial/Views/Fi/Infos.cshtml", im);
        }
        [HttpPost]
        public ActionResult Infos(FormCollection form)
        {
            try
            {
                var customer = _authService.GetAuthenticatedCustomer();
                NopServiceClient client = new NopServiceClient();
                client.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Anonymous;
                var m = new List<MATCHING_S>();
                m.Add(new MATCHING_S { TABLE_NAME = "Info", BASE_STR = customer.Username, MATCH_ID = customer.Id, MATCH_STR = form["vd"] + "_" + form["vn"] + "_" + form["unvan"] + "_" + form["email"] + "_" + form["tel"] });
                var a = client.MatchTwoRecord(m.ToArray());
                return Json("<font color=green>KAYDINIZ BAŞARI İLE OLUŞTURULMUŞTUR...</font>", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json("<font color=red>KAYDINIZ ALINAMADI. LÜTFEN TEKRAR DENEYİNİZ...</font>", JsonRequestBehavior.AllowGet);
            }

            //return View("~/Plugins/Peraport.WidgetFinancial/Views/Fi/Banka.cshtml");
        }

        //Faturaları Seç
        public ActionResult Test()
        {
            var customer = _authService.GetAuthenticatedCustomer();
            NopServiceClient client = new NopServiceClient();
            client.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Anonymous;
            var faturalar = client.GetInvoicesFromErp(customer.Id.ToString());
            return View("~/Plugins/Peraport.WidgetFinancial/Views/Fi/Test.cshtml", faturalar);
        }
        public ActionResult KKPayForm(FiModelFatura f)
        {
            return PartialView("~/Plugins/Peraport.WidgetFinancial/Views/Fi/KKPay.cshtml", f);
        }

        public ActionResult FaturaTahsilat(FiModelFatura[] f)
        {
            return RedirectToAction("KKPayment", "IsPay", new FiModelFatura { ID = 1, TUTAR = f.Sum(x => x.TUTAR) });
            //işlemler....

            //return "0";
        }

        [HttpPost]/*FaturaTakip den geliyor. Fatura ödeme Bölümü*/
        public ActionResult KKPay(FormCollection form)
        {
            var Zaman = DateTime.Now;
            string _OrderId = "" + Zaman.Year + Zaman.Month + Zaman.Day + Zaman.Hour + Zaman.Minute + Zaman.Second + Zaman.Millisecond;
            string _OrderDesc = "" + Zaman.Year + Zaman.Month + Zaman.Day + Zaman.Hour + Zaman.Minute + Zaman.Second + Zaman.Millisecond;


            //Gerçek ortamı adresleri;
            String sendTrnxUrl = "https://cpm.payflex.com.tr/CpWeb/SecurePayment?Ptkn=";
            String PostUrl = "https://cpm.payflex.com.tr/CpWeb/api/RegisterTransaction";

            String TransactionId = Guid.NewGuid().ToString("N");/*Her işlemde random olmalıdır.*/
            String Amount = form["Tutar"];
            Amount = Amount.Replace(",", "");

            String AmountCode = "949";
            String HostMerchantId = "662206572";// form["IsyeriNo"];
            String MerchantPassword = "A662206572";//form["IsyeriSifre"];
            String InstalmentCount = form["Taksit"];
            String OrderId = _OrderId;// "";// form["OrderId"];
            String OrderDescription = _OrderDesc;// "";// form["OrderDESC"];
            String TransactionType = "Sale";
            String IsSecure = "true"; //form["IsSecure"];
            String AllowNotEnrolledCard = "false";// form["IsSecure"]; 
            String SuccessUrl = _webHelper.GetStoreLocation() + "Fi/PaymentSuccess";
            String FailUrl = _webHelper.GetStoreLocation() + "Fi/PaymentFail";

            String post = "HostMerchantId=" + HostMerchantId + "&AmountCode=" + AmountCode + "&Amount=" + Amount + "&MerchantPassword=" + MerchantPassword + "&TransactionId=" + TransactionId + "&OrderID=" + OrderId + "&OrderDescription=" + OrderDescription + "&InstallmentCount=" + InstalmentCount + "&TransactionType=" + TransactionType + "&IsSecure=" + IsSecure + "&AllowNotEnrolledCard=" + AllowNotEnrolledCard + "&SuccessUrl=" + SuccessUrl + "&FailUrl=" + FailUrl;

            String response = GetResponseText(PostUrl, post);
            response = response.Replace("0 - ", "");
            //form1.Visible = false;
            Response.Write("<h1>Sonuç değerleri</h1>");
            Response.Write(response);
            string newURL = string.Format("{0}{1}", sendTrnxUrl, XmlParser(response));

            if (newURL.Contains("?Ptkn=null"))
            {
                Response.Redirect(_webHelper.GetStoreLocation() + "Fi/Test");
            }
            else
            {
                //Session["ORDERID"] = OrderId;
                Session["AMOUNT"] = Amount;
                Response.Redirect(newURL);
            }
            return View("~/Plugins/Peraport.WidgetFinancial/Views/Fi/KKPay.cshtml", new FiModelFatura { ID = 0, TUTAR = 0 });
        }

        public ActionResult PaymentFail(string RC, string ErrorCode, string Message, string TransactionId)
        {
            //string MaskedPan = "Kart Numarası";
            //var amount = Session["AMOUNT"].ToString();
            //decimal d = decimal.Parse(amount, CultureInfo.InvariantCulture);


            Session["AMOUNT"] = null;
            ViewBag.ResultCode = RC;
            ViewBag.Message = Message;



            //var customer = _authService.GetAuthenticatedCustomer();
            //ErpPayment p = new ErpPayment { DATE = DateTime.Now, TOTAL = d, NOPCUSTOMER_ID = customer.Id, ERPCUSTOMER_ID = -1, ERPCUSTOMER_CODE = "", CODE="WEB", NOTE = MaskedPan };
            //NopServiceClient client = new NopServiceClient();
            //var r = client.CopyNopPaymentToErp(p);

            //if (r.CODE == "OK")
            //    ViewBag.Note = "İşlem Başarılı. "+ amount +" TL kartınızdan çekildi ve hesabınıza aktarıldı.";
            //else
            //    ViewBag.Note = "UYARI. " + amount + " TL kartınızdan çekildi ancak hesabınıza aktarılırken bir problem oluştu. Konuyla ilgili Lütfen Müşteri Temsilcinize bilgi veriniz.";



            return View("~/Plugins/Peraport.WidgetFinancial/Views/Fi/PaymentFail.cshtml");
        }

        public ActionResult PaymentSuccess(string RC, string Message, string TransactionId, string AuthCode, string MaskedPan)
        {
            var amount = Session["AMOUNT"].ToString();
            decimal d = decimal.Parse(amount, CultureInfo.InvariantCulture);
            Session["AMOUNT"] = null;

            var customer = _authService.GetAuthenticatedCustomer();
            ErpPayment p = new ErpPayment { DATE = DateTime.Now, TOTAL = d, NOPCUSTOMER_ID = customer.Id, ERPCUSTOMER_ID = -1, ERPCUSTOMER_CODE = "", NOTE = MaskedPan, CODE = "WEB" };
            NopServiceClient client = new NopServiceClient();
            client.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Anonymous;
            var r = client.CopyNopPaymentToErp(p);

            if (r.CODE == "OK")
                ViewBag.Note = "İşlem Başarılı. " + amount + " TL kartınızdan çekildi ve hesabınıza aktarıldı.";
            else
                ViewBag.Note = "UYARI. " + amount + " TL kartınızdan çekildi ancak hesabınıza aktarılırken bir problem oluştu. Konuyla ilgili Lütfen Müşteri Temsilcinize bilgi veriniz.";


            ViewBag.TransactionId = TransactionId;
            ViewBag.AuthCode = AuthCode;
            ViewBag.MaskedPan = MaskedPan;
            ViewBag.ResultCode = RC;
            ViewBag.Message = Message;
            return View("~/Plugins/Peraport.WidgetFinancial/Views/Fi/PaymentSuccess.cshtml");
        }

        protected string XmlParser(string xmlString)
        {
            string CommonPaymentUrlNode = "";
            string PaymentTokenNode = "";
            string ErrorCodeNode = "";
            string ErrorMessageNode = "";


            var s = xmlString.Replace("{", "");
            xmlString = s.Replace("}", "");
            var stringArray = xmlString.Split(',');

            if (stringArray.Length == 4)
            {
                PaymentTokenNode = stringArray[1].Split(':')[1].Replace(@"""", "");
                ErrorCodeNode = stringArray[2].Split(':')[1].Replace(@"""", "");
                ErrorMessageNode = stringArray[3].Split(':')[1].Replace(@"""", "");

                //Response.Write(PaymentTokenNode + "<br/>")

            }
            else
            {
                ErrorCodeNode = "";
                CommonPaymentUrlNode = "";
                PaymentTokenNode = "";
            }

            Response.Write("ErrorCodeNode: " + ErrorCodeNode + "<br/>CommonPaymentUrl: " + CommonPaymentUrlNode + "<br/>PaymentTokenNode: " + PaymentTokenNode + "<br/>ErrorMessageNode: " + ErrorMessageNode + "<br/>");
            return PaymentTokenNode;
        }
        protected string GetResponseText(string url, string postData)
        {
            byte[] dataStream = Encoding.UTF8.GetBytes(postData);
            HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(url);
            webRequest.Method = "POST";
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.ContentLength = dataStream.Length;
            webRequest.KeepAlive = false;
            webRequest.Timeout = 30 * 1000; // 30 saniye
            //webRequest.Proxy.Credentials = System.Net.CredentialCache.DefaultNetworkCredentials;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | (SecurityProtocolType)768 | (SecurityProtocolType)3072 | SecurityProtocolType.Tls;

            string responseFromServer = "";

            //if (this.proxySettings != null)
            //{
            /*WebProxy proxy = new WebProxy("iproxy", 8080);
            proxy.Credentials = new NetworkCredential("kullanıcıadı", "sifre", "INNOVA");
            webRequest.Proxy = proxy;*/
            //}

            using (Stream newStream = webRequest.GetRequestStream())
            {
                newStream.Write(dataStream, 0, dataStream.Length);
                newStream.Close();
            }

            using (WebResponse webResponse = webRequest.GetResponse())
            {
                using (StreamReader reader = new StreamReader(webResponse.GetResponseStream()))
                {
                    responseFromServer = reader.ReadToEnd();
                    reader.Close();
                }

                webResponse.Close();
            }

            return responseFromServer;
        }



    }
}
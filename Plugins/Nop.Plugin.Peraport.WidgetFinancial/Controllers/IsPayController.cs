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

namespace Nop.Plugin.Peraport.WidgetFinancial.Controllers
{
    public class IsPayController : BasePluginController
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


        public IsPayController(
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

        public ActionResult IsPaySuccess(string RC, string Message, string TransactionId, string AuthCode, string MaskedPan)
        {
            /*Limit Güncelleme işlemi yapılacak...*/

            ViewBag.TransactionId = TransactionId;
            ViewBag.AuthCode = AuthCode;
            ViewBag.MaskedPan = MaskedPan;
            ViewBag.ResultCode = RC;
            ViewBag.Message = Message;
            return View("~/Plugins/Peraport.WidgetFinancial/Views/IsPay/IsPaySuccess.cshtml");
        }
        public ActionResult IsPayFail(string RC, string ErrorCode, string Message, string TransactionId)
        {
            ViewBag.ResultCode = RC;
            ViewBag.Message = Message;
            return View("~/Plugins/Peraport.WidgetFinancial/Views/IsPay/IsPayFail.cshtml");
        }

        public ActionResult KKPayment()
        {
            return View("~/Plugins/Peraport.WidgetFinancial/Views/IsPay/KKPayment.cshtml");
        }

        [HttpPost]
        public ActionResult KKPayment(FormCollection form)
        {
            /*
             1-Formu Göster
             2-Bilgileri Al
             3-Kontrolleri Sağla
             4-Bankaya Gönder
             5-Sonu işle ve Göster
             */
            //Bilgileri Al
            //var paymentInfo = new ProcessPaymentRequest();
            ////paymentInfo.CreditCardType = form["CreditCardType"];
            ////paymentInfo.CreditCardName = form["CardHolderName"];
            ////paymentInfo.CreditCardNumber = form["CardNumber"];
            ////paymentInfo.CreditCardExpireMonth = int.Parse(form["ExpireMonth"]);
            ////paymentInfo.CreditCardExpireYear = int.Parse(form["ExpireYear"]);
            ////paymentInfo.CreditCardCvv2 = form["CardCode"];
            //paymentInfo.OrderTotal = Convert.ToDecimal(0.5);


            var Zaman = DateTime.Now;
            string _OrderId = "" + Zaman.Year + Zaman.Month + Zaman.Day + Zaman.Hour + Zaman.Minute + Zaman.Second + Zaman.Millisecond;
            string _OrderDesc = "" + Zaman.Year + Zaman.Month + Zaman.Day + Zaman.Hour + Zaman.Minute + Zaman.Second + Zaman.Millisecond;


            //Gerçek ortamı adresleri;
            String sendTrnxUrl = "https://cpm.payflex.com.tr/CpWeb/SecurePayment?Ptkn=";
            String PostUrl = "https://cpm.payflex.com.tr/CpWeb/api/RegisterTransaction";

            String TransactionId = Guid.NewGuid().ToString("N");/*Her işlemde random olmalıdır.*/
            String Amount = form["Tutar"];
            decimal amount = -1;
            Amount = Amount.Replace(",", ".");
            try
            {
                amount = Convert.ToDecimal(Amount,CultureInfo.GetCultureInfo("en-us"));
            }
            catch (Exception)
            {

            }
            
            if (amount != -1) Amount = amount.ToString("0.00");
            Amount = Amount.Replace(",", ".");
            String AmountCode = "949";
            String HostMerchantId = "662206572";// form["IsyeriNo"];
            String MerchantPassword = "A662206572";//form["IsyeriSifre"];
            String InstalmentCount = form["Taksit"];
            String OrderId = _OrderId;// "";// form["OrderId"];
            String OrderDescription = _OrderDesc;// "";// form["OrderDESC"];
            String TransactionType = "Sale";
            String IsSecure = "true"; //form["IsSecure"];
            String AllowNotEnrolledCard = "false";// form["IsSecure"]; 
            String SuccessUrl = _webHelper.GetStoreLocation() + "IsPay/IsPaySuccess";//"http://localhost:15536/IsPay/IsPaySuccess";// form["SuccessUrl"];
            String FailUrl = _webHelper.GetStoreLocation() + "IsPay/IsPayFail";//"http://localhost:15536/IsPay/IsPayFail";// form["FailUrl"];

            String post = "HostMerchantId=" + HostMerchantId + "&AmountCode=" + AmountCode + "&Amount=" + Amount + "&MerchantPassword=" + MerchantPassword + "&TransactionId=" + TransactionId + "&OrderID=" + OrderId + "&OrderDescription=" + OrderDescription + "&InstallmentCount=" + InstalmentCount + "&TransactionType=" + TransactionType + "&IsSecure=" + IsSecure + "&AllowNotEnrolledCard=" + AllowNotEnrolledCard + "&SuccessUrl=" + SuccessUrl + "&FailUrl=" + FailUrl;

            String response = GetResponseText(PostUrl, post);
            response = response.Replace("0 - ", "");
            //form1.Visible = false;
            Response.Write("<h1>Sonuç değerleri</h1>");
            Response.Write(response);
            string newURL = string.Format("{0}{1}", sendTrnxUrl, XmlParser(response));
            Response.Redirect(newURL);

            return View("~/Plugins/Peraport.WidgetFinancial/Views/IsPay/KKPayment.cshtml");
        }
    }
}

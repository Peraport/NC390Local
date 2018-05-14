using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Infrastructure;
using Nop.Core.Plugins;
using Nop.Plugin.Payments.PeraPay.Controllers;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Tax;
using System.IO;
using System.Net;
using System.Xml.Serialization;
using static Nop.Services.Payments.PaymentExtensions;
using System.Xml;

namespace Nop.Plugin.Payments.PeraPay
{
    public class PeraPayPlugin : BasePlugin, IPaymentMethod
    {
        #region var
        private const string formSend = "<form style=\"background: #eee url('../Content/Images/background2.png') center bottom no-repeat; height: 1200px;\" " +
          " action=\"{14}\" method=\"POST\" />" +
          "<input type=\"hidden\" name=\"clientid\" value=\"{0}\" />" +
          "<input type=\"hidden\" name=\"amount\" value=\"{1}\" />" +
          "<input type=\"hidden\" name=\"oid\" value=\"{2}\" />" +
          "<input type=\"hidden\" name=\"okurl\" value=\"{3}\" />" +
          "<input type=\"hidden\" name=\"failurl\" value=\"{4}\" />" +
          "<input type=\"hidden\" name=\"islemtipi\" value=\"{5}\" />" +
          "<input type=\"hidden\" name=\"taksit\" value=\"{6}\" />" +
          "<input type=\"hidden\" name=\"rnd\" value=\"{7}\" />" +
          "<input type=\"hidden\" name=\"hash\" value=\"{8}\" />" +
          "<input type=\"hidden\" name=\"storetype\" value=\"{9}\" />" +
          "<input type=\"hidden\" name=\"firmaadi\" value=\"{10}\" />" +
          "<input type=\"hidden\" name=\"refreshtime\" value=\"{11}\" />" +
          "<input type=\"hidden\" name=\"lang\" value=\"{12}\" />" +
          "<input type=\"hidden\" name=\"currency\" value=\"{13}\" />" +
          "<input type=\"hidden\" name=\"Fismi\" value=\"is\" />" +
          "<input type=\"hidden\" name=\"faturaFirma\" value=\"faturaFirma\" />" +
          "<input type=\"hidden\" name=\"Fadres\" value=\"XXX\" />" +
          "<input type=\"hidden\" name=\"Fadres2\" value=\"XXX\" />" +
          "<input type=\"hidden\" name=\"Fil\" value=\"XXX\" />" +
          "<input type=\"hidden\" name=\"Filce\" value=\"XXX\" />" +
          "<input type=\"hidden\" name=\"Fpostakodu\" value=\"34900\" />" +
          "<input type=\"hidden\" name=\"tel\" value=\"XXX\" />" +
          "<input type=\"hidden\" name=\"fulkekod\" value=\"tr\" />" +
          "<input type=\"hidden\" name=\"nakliyeFirma\" value=\"nakfi\" />" +
          "<input type=\"hidden\" name=\"tismi\" value=\"XXX\" />" +
          "<input type=\"hidden\" name=\"tadres\" value=\"XXX\" />" +
          "<input type=\"hidden\" name=\"tadres2\" value=\"XXX\" />" +
          "<input type=\"hidden\" name=\"til\" value=\"XXX\" />" +
          "<input type=\"hidden\" name=\"tilce\" value=\"XXX\" />" +
          "<input type=\"hidden\" name=\"tpostakodu\" value=\"34900\" />" +
          "<input type=\"hidden\" name=\"tulkekod\" value=\"usa\" />" +
          "<input type=\"hidden\" name=\"itemnumber1\" value=\"a1\" />" +
          "<input type=\"hidden\" name=\"productcode1\" value=\"a2\" />" +
          "<input type=\"hidden\" name=\"qty1\" value=\"3\" />" +
          "<input type=\"hidden\" name=\"desc1\" value=\"a4 desc\" />" +
          "<input type=\"hidden\" name=\"id1\" value=\"a5\" />" +
          "<input type=\"hidden\" name=\"price1\" value=\"6.25\" />" +
          "<input type=\"hidden\" name=\"total1\" value=\"7.50\" />" +
          "<font face=\"Helvetica\" size=\"3\" color=\"#606060\">" +
          "<center>" +
          "<br />" +
          "<br />" +
          "<h2>Ödeme için Bankaya Yönlendirileceksiniz. <h2> <br />{15} <br />" +
          "<br />" +
          "<input type=\"submit\" value=\"Banka Ödeme Sayfası\"/>" +
          "</center>" +
          "</form>";
        #endregion

        #region  Ctor
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly HttpContextBase _httpContext;
        private readonly CurrencySettings _currencySettings;
        private readonly IWebHelper _webHelper;
        private readonly ISettingService _settingService;
        private readonly ICurrencyService _currencyService;
        private readonly ICheckoutAttributeParser _checkoutAttributeParser;
        private readonly PayPaymentSettings _PayPaymentSettings;
        public PeraPayPlugin(
            PayPaymentSettings PayPaymentSettings,
                                      ISettingService settingService, ICurrencyService currencyService,
                                      CurrencySettings currencySettings, IWebHelper webHelper,
                                      ICheckoutAttributeParser checkoutAttributeParser, ITaxService taxService,
                                      IOrderTotalCalculationService orderTotalCalculationService,
                                      HttpContextBase httpContext)
        {
            this._settingService = settingService;
            this._currencyService = currencyService;
            this._currencySettings = currencySettings;
            this._webHelper = webHelper;
            this._checkoutAttributeParser = checkoutAttributeParser;
            this._PayPaymentSettings = PayPaymentSettings;
            this._orderTotalCalculationService = orderTotalCalculationService;
            this._httpContext = httpContext;
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

            //Response.Write("ErrorCodeNode: " + ErrorCodeNode + "<br/>CommonPaymentUrl: " + CommonPaymentUrlNode + "<br/>PaymentTokenNode: " + PaymentTokenNode + "<br/>ErrorMessageNode: " + ErrorMessageNode + "<br/>");
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

        public string PaymentMethodDescription
        {
            get
            {
                return "Kredi Kartı 3D Kullanılarak Ödeme Yapılır";
            }
        }

        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "Pay";
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Payments.PeraPay.Controllers" }, { "area", null } };
        }

        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            //Burası 3D yönlendirme PayPal Standart
            return new ProcessPaymentResult() { NewPaymentStatus = PaymentStatus.Pending };

            //var result = new ProcessPaymentResult();
            //result.NewPaymentStatus = PaymentStatus.Voided;
            //return result;

        }

        #region Garanti
        private const string formSendGR = "<form id=\"GarantiForm\" style=\"background: #eee url('../Content/Images/background2.png') center bottom no-repeat; height: 1200px;\" " +
          " action=\"{0}\" method=\"POST\" />" +
          "<input type=\"hidden\" name=\"secure3dsecuritylevel\" value=\"3D_OOS_FULL\" />" +
          "<input type=\"hidden\" name=\"mode\" value=\"{1}\" />" +
          "<input type=\"hidden\" name=\"apiversion\" value=\"{2}\" />" +
          "<input type=\"hidden\" name=\"terminalprovuserid\" value=\"{3}\" />" +
          "<input type=\"hidden\" name=\"terminaluserid\" value=\"{4}\" />" +
          "<input type=\"hidden\" name=\"terminalid\" value=\"{5}\" />" +
          "<input type=\"hidden\" name=\"terminalmerchantid\" value=\"{6}\" />" +
          "<input type=\"hidden\" name=\"orderid\" value=\"{7}\" />" +
          "<input type=\"hidden\" name=\"customeremailaddress\" value=\"info@kavsan.com.tr\" />" +
          "<input type=\"hidden\" name=\"customeripaddress\" value=\"127.0.0.1\" />" +
          "<input type=\"hidden\" name=\"txntype\" value=\"{8}\" />" +
          "<input type=\"hidden\" name=\"txnamount\" value=\"{9}\" />" +
          "<input type=\"hidden\" name=\"txncurrencycode\" value=\"{10}\" />" +
          "<input type=\"hidden\" name=\"companyname\" value=\"Kavsan\" />" +
          "<input type=\"hidden\" name=\"txninstallmentcount\" value=\"{11}\" />" +
          "<input type=\"hidden\" name=\"successurl\" value=\"{12}\" />" +
          "<input type=\"hidden\" name=\"errorurl\" value=\"{13}\" />" +
          "<input type=\"hidden\" name=\"secure3dhash\" value=\"{14}\" />" +
          "<input type=\"hidden\" name=\"lang\" value=\"{15}\" />" +
          "<input type=\"hidden\" name=\"motoind\" value=\"{16}\" />" +
          "<input type=\"hidden\" name=\"txntimestamp\" value=\"{17}\" />" +
          "<input type=\"hidden\" name=\"refreshtime\" value=\"{18}\" />" +
          "<font face=\"Helvetica\" size=\"3\" color=\"#606060\">" +
          "<center>" +
          "<br />" +
          "<br />" +
          "<h2>Ödeme Yönlendirme<h2> <br />{19} <br />" +
          "<br />" +
          "<input type=\"submit\" value=\"Garanti Bankası Ödeme Sayfasına Devam Et\"/>" +
          "</center>" +
          "</form> <script>document.getElementById('GarantiForm').submit();</script>";
        public string GetSHA1(string SHA1Data)
        {
            SHA1 sha = new SHA1CryptoServiceProvider();
            string HashedPassword = SHA1Data;
            byte[] hashbytes = Encoding.GetEncoding("ISO-8859-9").GetBytes(HashedPassword);
            byte[] inputbytes = sha.ComputeHash(hashbytes);
            return GetHexaDecimal(inputbytes);
        }
        public string GetHexaDecimal(byte[] bytes)
        {
            StringBuilder s = new StringBuilder();
            int length = bytes.Length;
            for (int n = 0; n <= length - 1; n++)
            {
                s.Append(String.Format("{0,2:x}", bytes[n]).Replace(" ", "0"));
            }
            return s.ToString();
        }
        #endregion
        public static Dictionary<string, object> DeserializeCustomValues(string customValuesXml)
        {
            if (string.IsNullOrWhiteSpace(customValuesXml))
            {
                return new Dictionary<string, object>();
            }

            var serializer = new XmlSerializer(typeof(DictionarySerializer));

            using (var textReader = new StringReader(customValuesXml))
            {
                using (var xmlReader = XmlReader.Create(textReader))
                {
                    var ds = serializer.Deserialize(xmlReader) as DictionarySerializer;
                    if (ds != null)
                        return ds.Dictionary;
                    return new Dictionary<string, object>();
                }
            }
        }

        /*Yönlendirme 3D  Paypal standart gibi*/
        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            var Zaman = DateTime.Now;
            var secenekDict = DeserializeCustomValues(postProcessPaymentRequest.Order.CustomValuesXml);
            var secenek = secenekDict["Banka"].ToString().Split('_');

            _httpContext.Session["BANKA"] = secenek[0];

            if (secenek[0] == "Garanti")
            {
                #region Garanti Bankası      
                //String badr = "https://sanalposprov.garanti.com.tr/servlet/gt3dengine";
                String badr = "https://sanalposprov.garanti.com.tr/VPServlet";
                

                String refreshtime = "1";
                string strMode = "PROD";// "TEST";
                string strApiVersion = "v0.01";
                string strTerminalProvUserID = "PROVOOS";
                string strType = "sales";
                string strAmount = Convert.ToInt32((postProcessPaymentRequest.Order.OrderTotal * 100)).ToString();
                string strCurrencyCode = "949";
                string strInstallmentCount = secenek[1];//taksit sayısı
                string strTerminalUserID = "PROVOOS";
                string strOrderID = postProcessPaymentRequest.Order.Id.ToString();
                //string strCustomeripaddress = "127.0.0.1";
                string strTerminalID = "10021483";// "30691299";
                string _strTerminalID = "010021483";// "030691299"; //Basına 0 eklenerek 9 digite tamamlanmalıdır.
                string strTerminalMerchantID = "9363379";// "7000679"; //Üye syeri Numarası
                string strStoreKey = "54414d45524f54544954414d45524f545449373432303134";// "12345678"; //3D Secure sifreniz
                string strProvisionPassword = "Tamerotti74";// "123qweASD"; //Terminal UserID sifresi
                string strSuccessURL = _webHelper.GetStoreLocation() + "Pay/LPSuccess";
                string strErrorURL = _webHelper.GetStoreLocation() + "Pay/LPCancel";
                //string strCompanyName = "Medisol";
                string strlang = "tr";
                string strMotoInd = "N";
                string strtimestamp = DateTime.Now.ToString();
                string SecurityData = GetSHA1(strProvisionPassword + _strTerminalID).ToUpper();
                string HashData = GetSHA1(strTerminalID + strOrderID + strAmount + strSuccessURL + strErrorURL + strType + strInstallmentCount + strStoreKey + SecurityData).ToUpper();

                _httpContext.Session["ORDERID"] = strOrderID;
                _httpContext.Session["AMOUNT"] = strAmount;


                var resultString = string.Format(formSendGR, badr, strMode, strApiVersion, strTerminalProvUserID, strTerminalUserID, strTerminalID,
                    strTerminalMerchantID, strOrderID, strType, strAmount, strCurrencyCode, strInstallmentCount, strSuccessURL, strErrorURL,
                         HashData, strlang, strMotoInd, strtimestamp, refreshtime, secenek[0]);
                var fs = resultString;
                _httpContext.Response.Clear();
                _httpContext.Response.Write(fs);
                _httpContext.Response.End();
                #endregion
            }
            else if (secenek[0] == "Isbank")//working
            {
                #region İş Bankası      
                String PostUrl = " https://cpm.vpos.isbank.com.tr/CpWeb/api/RegisterTransaction";
                String sendTrnxUrl = "https://cpm.vpos.isbank.com.tr/CpWeb/SecurePayment?Ptkn=";
                //Gerçek ortamı adresleri;
                //String sendTrnxUrl = "https://cpm.payflex.com.tr/CpWeb/SecurePayment?Ptkn=";
                //String PostUrl = "https://cpm.payflex.com.tr/CpWeb/api/RegisterTransaction";

                String TransactionId = Guid.NewGuid().ToString("N");/*Her işlemde random olmalıdır.*/
                String Amount = postProcessPaymentRequest.Order.OrderTotal.ToString("0.00").Replace(',', '.');
                String AmountCode = "949";
                String HostMerchantId = "662206572";
                String MerchantPassword = "A662206572";
                String InstalmentCount = secenek[1];
                String OrderId = postProcessPaymentRequest.Order.Id.ToString();
                String OrderDescription = postProcessPaymentRequest.Order.Id.ToString();
                String TransactionType = "Sale";
                String IsSecure = "true";
                String AllowNotEnrolledCard = "false";
                String SuccessUrl = _webHelper.GetStoreLocation() + "Pay/LPSuccess";
                String FailUrl = _webHelper.GetStoreLocation() + "Pay/LPCancel";

                String post = "HostMerchantId=" + HostMerchantId + "&AmountCode=" + AmountCode + "&Amount=" + Amount + "&MerchantPassword=" + MerchantPassword + "&TransactionId=" + TransactionId + "&OrderID=" + OrderId + "&OrderDescription=" + OrderDescription + "&InstallmentCount=" + InstalmentCount + "&TransactionType=" + TransactionType + "&IsSecure=" + IsSecure + "&AllowNotEnrolledCard=" + AllowNotEnrolledCard + "&SuccessUrl=" + SuccessUrl + "&FailUrl=" + FailUrl;

                String response = GetResponseText(PostUrl, post);
                response = response.Replace("0 - ", "");
                string newURL = string.Format("{0}{1}", sendTrnxUrl, XmlParser(response));
                if (newURL.Contains("?Ptkn=null"))
                {
                    _httpContext.Response.Redirect(_webHelper.GetStoreLocation() + "Pay/OrderToCart?oid=" + OrderId);
                }
                else
                {
                    _httpContext.Session["ORDERID"] = OrderId;
                    _httpContext.Session["AMOUNT"] = Amount;
                    _httpContext.Response.Redirect(newURL);
                }
                #endregion
            }
            else if (secenek[0] == "YapiKredi")
            {
                #region YapıKredi
                #endregion
            }
        }

        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            return false;

        }

        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            decimal additional = cart.Aggregate(0.0m, (current, shoppingCartItem) => current + shoppingCartItem.Product.Price);
            return additional * (decimal)0.0;
        }

        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
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

        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            throw new NotImplementedException();
        }

        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            throw new NotImplementedException();
        }

        public bool CanRePostProcessPayment(Order order)
        {
            return true;
        }

        public void GetPaymentInfoRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "PaymentInfo";
            controllerName = "Pay";
            routeValues = new RouteValueDictionary() { { "Namespaces", "Nop.Plugin.Payments.PeraPay.Controllers" }, { "area", null } };
        }

        public Type GetControllerType()
        {
            return typeof(PayController);
        }
    }
}
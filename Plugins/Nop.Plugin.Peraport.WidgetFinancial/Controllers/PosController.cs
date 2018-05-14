using Nop.Core;
using Nop.Services.Authentication;
using Nop.Services.Configuration;
using Nop.Web.Framework.Controllers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Nop.Plugin.Peraport.WidgetFinancial.Controllers
{
    public class PosController : BasePluginController
    {
        #region ctor
        private readonly IWorkContext _workContext;
        private readonly ISettingService _settingService;
        private readonly IAuthenticationService _authService;
        private readonly IWebHelper _webHelper;
        private readonly HttpContextBase _httpContext;


        public PosController(
                                        IWorkContext workContext,
                                        ISettingService settingService,
                                        IAuthenticationService authService,
                                        IWebHelper webHelper,
                                      HttpContextBase httpContext
            )
        {
            _workContext = workContext;
            _settingService = settingService;
            _authService = authService;
            _webHelper = webHelper;
            _httpContext = httpContext;
        }
        #endregion

        public string Base64Encode(string data)
        {
            string str2;
            try
            {
                byte[] buffer = new byte[data.Length];
                str2 = Convert.ToBase64String(Encoding.ASCII.GetBytes(data));
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
            return str2;
        }

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

        #region tanımlar

        private const string formSendGR = "<form style=\"background: #eee url('../Content/Images/background2.png') center bottom no-repeat; height: 1200px;\" " +
          " action=\"{0}\" method=\"POST\" />" +
          "<input type=\"hidden\" name=\"secure3dsecuritylevel\" value=\"3D_OOS_FULL\" />" +
          "<input type=\"hidden\" name=\"mode\" value=\"{1}\" />" +
          "<input type=\"hidden\" name=\"apiversion\" value=\"{2}\" />" +
          "<input type=\"hidden\" name=\"terminalprovuserid\" value=\"{3}\" />" +
          "<input type=\"hidden\" name=\"terminaluserid\" value=\"{4}\" />" +
          "<input type=\"hidden\" name=\"terminalid\" value=\"{5}\" />" +
          "<input type=\"hidden\" name=\"terminalmerchantid\" value=\"{6}\" />" +
          "<input type=\"hidden\" name=\"orderid\" value=\"{7}\" />" +
          "<input type=\"hidden\" name=\"customeremailaddress\" value=\"info@a321.com.tr\" />" +
          "<input type=\"hidden\" name=\"customeripaddress\" value=\"127.0.0.1\" />" +
          "<input type=\"hidden\" name=\"txntype\" value=\"{8}\" />" +
          "<input type=\"hidden\" name=\"txnamount\" value=\"{9}\" />" +
          "<input type=\"hidden\" name=\"txncurrencycode\" value=\"{10}\" />" +
          "<input type=\"hidden\" name=\"companyname\" value=\"A321\" />" +
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
          "<h2>Banka sayfasına yönlendiriliyorsunuz...<h2>  <br />" +
          "<br />" +

          "</center>" +
          "</form>"
            + "<script> document.forms[0].submit();</script>"
            ;

        private const string formSendYK = "<form style=\"background: #eee url('../Content/Images/background2.png') center bottom no-repeat; height: 1200px;\" " +
          " name = \"mercForm\"  action=\"{0}\" method=\"POST\" />" +
          "<input type=\"hidden\" name=\"posnetID\" value=\"{1}\" />" +
          "<input type=\"hidden\" name=\"mid\" value=\"{2}\" />" +
          "<input type=\"hidden\" name=\"xid\" value=\"{3}\" />" +
          "<input type=\"hidden\" name=\"tranType\" value=\"{4}\" />" +
          "<input type=\"hidden\" name=\"instalment\" value=\"{5}\" />" +
            "<input type=\"hidden\" name=\"amount\" value=\"{6}\" />" +
          "<input type=\"hidden\" name=\"lang\" value=\"{7}\" />" +
          "<input type=\"hidden\" name=\"currencyCode\" value=\"{8}\" />" +
          "<input type=\"hidden\" name=\"merchantReturnSuccessURL\" value=\"{9}\" />" +
          "<input type=\"hidden\" name=\"merchantReturnFailURL\" value=\"{10}\" />" +
          "<font face=\"Helvetica\" size=\"3\" color=\"#606060\">" +
          "<center>" +
          "<br />" +
          "<br />" +
          "<h2>Banka sayfasına yönlendiriliyorsunuz...<h2>  <br />" +
          "<br />" +

          "</center>" +
          "</form>"
            + "<script> document.forms[0].submit();</script>"
            ;
        #endregion

        // GET: Pos
        public ActionResult Index()
        {
            return View("~/Plugins/Peraport.WidgetFinancial/Views/Pos/Index.cshtml");
        }

        [HttpPost]
        public ActionResult Index(FormCollection f)
        {
            string banka = f["banka"];
            var b = banka.Split('-');

            string taksit = "";


            string bnk = b[0];
            taksit = b[1];
            if (taksit == "0") taksit = "";

            var Zaman = DateTime.Now;
            string orderid = "" + Zaman.Year + Zaman.Month + Zaman.Day + Zaman.Hour + Zaman.Minute + Zaman.Second + Zaman.Millisecond;
            string _OrderDesc = "" + Zaman.Year + Zaman.Month + Zaman.Day + Zaman.Hour + Zaman.Minute + Zaman.Second + Zaman.Millisecond;


            #region YK

            if (bnk == "YK")
            {
                string badr = "https://www.posnet.ykb.com/3DSWebService/OOS";//Gerçek  test için badr = "http://setmpos.ykb.com/3DSWebService/OOS";
                string posnetid = "285544";//A321
                string mid = "6701010031";//A321
                string amount = "1";
                string currencyCode = "TL";
                string instalment = taksit; //Taksit Sayısı. Bos gönderilirse taksit yapılmaz
                string s = "0";
                for (int i = 1; i < 20 - orderid.Length; i++) { s = s + "0"; }
                string xid = s + orderid.ToString();
                string trantype = "Sale";
                string successURL = _webHelper.GetStoreLocation() + "Pos/LPSuccessYKB";
                string errorURL = _webHelper.GetStoreLocation() + "Pos/LPFailYKB";
                string lang = "tr";
                string strtimestamp = DateTime.Now.ToString();

                var resultString = string.Format(formSendYK, badr, posnetid, mid, xid, trantype, instalment, amount, lang, currencyCode, successURL, errorURL, bnk);
                var fs = resultString;
                HttpContext.Response.Clear();
                HttpContext.Response.Write(fs);
                HttpContext.Response.End();
            }


            #endregion

            #region Garanti

            if (bnk == "GB")
            {
                if (taksit == "1") { taksit = ""; }
                String badr = "https://sanalposprov.garanti.com.tr/servlet/gt3dengine";

                String refreshtime = "1";
                string strMode = "PROD";// "TEST";
                string strApiVersion = "v0.01";
                string strTerminalProvUserID = "PROVOOS";
                string strType = "sales";
                string strAmount = Convert.ToInt32((1 * 100)).ToString();
                string strCurrencyCode = "949";
                string strInstallmentCount = taksit; //Taksit Sayısı. Bos gönderilirse taksit yapılmaz
                string strTerminalUserID = "PROVOOS";
                string strOrderID = orderid;
                string strTerminalID = "10149385";
                string _strTerminalID = "010149385";//Basına 0 eklenerek 9 digite tamamlanmalıdır.
                string strTerminalMerchantID = "0254181";//Üye syeri Numarası
                string strStoreKey = "A321EDA007ADA34000937155A321EDA007ADA34000937155"; //3D Secure sifreniz
                string strProvisionPassword = "TelekomA321.";//Terminal UserID sifresi
                string strSuccessURL = _webHelper.GetStoreLocation() + "Pos/LPSuccessGB";
                string strErrorURL = _webHelper.GetStoreLocation() + "Pos/LPFailGB";
                string strlang = "tr";
                string strMotoInd = "N";
                string strtimestamp = DateTime.Now.ToString();
                string SecurityData = GetSHA1(strProvisionPassword + _strTerminalID).ToUpper();
                string HashData = GetSHA1(strTerminalID + strOrderID + strAmount + strSuccessURL + strErrorURL +
                strType + strInstallmentCount + strStoreKey + SecurityData).ToUpper();

                var resultString = string.Format(formSendGR, badr, strMode, strApiVersion, strTerminalProvUserID, strTerminalUserID, strTerminalID,
                    strTerminalMerchantID, strOrderID, strType, strAmount, strCurrencyCode, taksit, strSuccessURL, strErrorURL,
                         HashData, strlang, strMotoInd, strtimestamp, refreshtime);
                var fs = resultString;
                HttpContext.Response.Clear();
                HttpContext.Response.Write(fs);
                HttpContext.Response.End();
            }

            #endregion

            #region İş Bankası

            if (bnk == "IB")
            {

                String PostUrl = " https://cpm.vpos.isbank.com.tr/CpWeb/api/RegisterTransaction";
                String sendTrnxUrl = "https://cpm.vpos.isbank.com.tr/CpWeb/SecurePayment?Ptkn=";
                //Gerçek ortamı adresleri;
                //String sendTrnxUrl = "https://cpm.payflex.com.tr/CpWeb/SecurePayment?Ptkn=";
                //String PostUrl = "https://cpm.payflex.com.tr/CpWeb/api/RegisterTransaction";

                String TransactionId = Guid.NewGuid().ToString("N");/*Her işlemde random olmalıdır.*/
                String Amount = "1.00";
                Amount = Amount.Replace(",", "");

                String AmountCode = "949";
                String HostMerchantId = "662206572";// form["IsyeriNo"];
                String MerchantPassword = "A662206572";//form["IsyeriSifre"];
                String InstalmentCount = taksit;
                String OrderId = orderid;// "";// form["OrderId"];
                String OrderDescription = _OrderDesc;// "";// form["OrderDESC"];
                String TransactionType = "Sale";
                String IsSecure = "true"; //form["IsSecure"];
                String AllowNotEnrolledCard = "false";// form["IsSecure"]; 
                String SuccessUrl = _webHelper.GetStoreLocation() + "Pos/LPSuccessIB";
                String FailUrl = _webHelper.GetStoreLocation() + "Pos/LPFailIB";

                String post = "HostMerchantId=" + HostMerchantId + "&AmountCode=" + AmountCode + "&Amount=" + Amount + "&MerchantPassword=" + MerchantPassword + "&TransactionId=" + TransactionId + "&OrderID=" + OrderId + "&OrderDescription=" + OrderDescription + "&InstallmentCount=" + InstalmentCount + "&TransactionType=" + TransactionType + "&IsSecure=" + IsSecure + "&AllowNotEnrolledCard=" + AllowNotEnrolledCard + "&SuccessUrl=" + SuccessUrl + "&FailUrl=" + FailUrl;

                String response = GetResponseText(PostUrl, post);
                response = response.Replace("0 - ", "");
                //form1.Visible = false;
                Response.Write("<h1>Sonuç değerleri</h1>");
                Response.Write(response);
                string newURL = string.Format("{0}{1}", sendTrnxUrl, XmlParser(response));

                if (newURL.Contains("?Ptkn=null"))
                {
                    Response.Redirect(HttpContext.Request.Url.Scheme + "://" + HttpContext.Request.Url.Host + ":" + HttpContext.Request.Url.Port + "/Pos/LPCancel?Err=BankaUlaşılamaz");
                }
                else
                {
                    Response.Redirect(newURL);
                }
            }

            #endregion

            return View();
        }


        #region IB Utils
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
        #endregion



        [ValidateInput(false)]
        public ActionResult LPSuccessYKB(FormCollection form)
        {
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
            return View("~/Plugins/Peraport.WidgetFinancial/Views/Pos/SuccessYKB.cshtml");
        }

        [ValidateInput(false)]
        public ActionResult LPFailYKB()
        {
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
            return View("~/Plugins/Peraport.WidgetFinancial/Views/Pos/FailYKB.cshtml");
        }


        [ValidateInput(false)]
        public ActionResult LPSuccessIB(FormCollection form)
        {
            string returnMessage = "";
            string errorMessage = "";
            string refNo = "";
            returnMessage = Request.Params["returnmessage"];
            errorMessage = Request.Params["Message"];
            refNo = Request.Params["Rc"];

            var res = returnMessage + " Ref No: " + refNo;

            ViewBag.Amount = Request.Params["amount"];
            ViewBag.Warning = errorMessage;
            ViewBag.Message = res;
            return View("~/Plugins/Peraport.WidgetFinancial/Views/Pos/SuccessYKB.cshtml");
        }

        [ValidateInput(false)]
        public ActionResult LPFailIB()
        {
            string returnMessage = "";
            string errorMessage = "";
            string refNo = "";
            returnMessage = Request.Params["returnmessage"];
            errorMessage = Request.Params["Message"];
            refNo = Request.Params["Rc"];
            var res = returnMessage + " Ref No: " + refNo;
            ViewBag.Amount = Request.Params["amount"];
            ViewBag.Warning = errorMessage;
            ViewBag.Message = res;

            return View("~/Plugins/Peraport.WidgetFinancial/Views/Pos/FailYKB.cshtml");
        }


        [ValidateInput(false)]
        public ActionResult LPSuccessGB(FormCollection form)
        {
            var res = "";
            if (res == "")
            {
                foreach (var item in Request.Form.AllKeys)
                {
                    res += item + " -> " + Request.Form[item] + " | ";
                }
            }

            ViewBag.Message = res;
            return View("~/Plugins/Peraport.WidgetFinancial/Views/Pos/SuccessYKB.cshtml");
        }

        [ValidateInput(false)]
        public ActionResult LPFailGB()
        {
            string returnMessage = "";
            string errorMessage = "";
            string refNo = "";
            returnMessage = Request.Form["mderrormessage"];
            errorMessage = Request.Form["errmsg"];
            refNo = Request.Form["mdstatus"];

            var res = returnMessage + " Ref No: " + refNo;

            ViewBag.Amount = Request.Form["amount"];
            ViewBag.Warning = errorMessage;
            ViewBag.Message = res;
            return View("~/Plugins/Peraport.WidgetFinancial/Views/Pos/FailYKB.cshtml");
        }

    }

}

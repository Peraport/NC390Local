using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Plugin.Peraport.AdminPlugin.Models;
using Nop.Services.Authentication;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;
using Nop.Web.PPService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Security;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Routing;

namespace Nop.Plugin.Peraport.AdminPlugin.Controllers
{
    public class PPCustomerController : BasePluginController
    {
        #region ctor
        private IEncryptionService _encryptionService;
        private readonly ICustomerService _customerService;
        private readonly CustomerSettings _customerSettings;
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly ISettingService _settingService;
        private readonly IAuthenticationService _authService;
        private readonly IWebHelper _webHelper;

        public PPCustomerController(IWorkContext workContext,
                                       IStoreService storeService,
                                       ISettingService settingService,
                                       IAuthenticationService authService,
                                       ICustomerService customerService,
                                       IEncryptionService encryptionService,
                                       IWebHelper webHelper,
            CustomerSettings customerSettings)
        {
            _customerSettings = customerSettings;
            _encryptionService = encryptionService;
            _customerService = customerService;
            _workContext = workContext;
            _storeService = storeService;
            _settingService = settingService;
            _authService = authService;
            _webHelper = webHelper;
        }
        #endregion

        public List<ErpUser> GetErpCustomers()
        {
            NopServiceClient client = new NopServiceClient();
            client.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Anonymous;
            var csErp = client.GetGeneralSettings("CS_ERP");
            string sorgu = ""
            //+ " SELECT"
            //+ " C.LOGICALREF,CARDTYPE,CODE,DEFINITION_,ADDR1,ADDR2,CITY,TOWN,COUNTRY,EMAILADDR,SPECODE,SPECODE2,SPECODE3,SPECODE4,SPECODE5,CN.ACCRISKLIMIT"
            //+ " FROM[LogoDB].[dbo].[LG_517_CLCARD] C"
            //+ " left join[LogoDB].[dbo].LG_517_01_CLRNUMS CN on C.LOGICALREF=CN.CLCARDREF";

            + " SELECT"
            + " C.LOGICALREF"//1
            + " ,CARDTYPE,CODE,DEFINITION_,ADDR1,ADDR2,CITY,TOWN,COUNTRY,EMAILADDR,SPECODE,SPECODE2,SPECODE3,SPECODE4,SPECODE5"
            + " ,CN.ACCRISKLIMIT"//16
            + " ,SUBSTRING(CAST(C.LOGICALREF AS NVARCHAR), 4, 5) + SUBSTRING(CODE, 1, 7) AS 'SIFRE'"//17
            + " FROM[LogoDB].[dbo].[LG_517_CLCARD] C"
            + " left join[LogoDB].[dbo].LG_517_01_CLRNUMS CN on C.LOGICALREF = CN.CLCARDREF"
            + " where SPECODE3 in('VDM','SHOP','EXP','FSK') or C.LOGICALREF in(40906,40907,40908,40909)";

            var resultQuery = client.GetWSData(csErp.VALUE_STR, sorgu);

            List<ErpUser> ErpUsersTemp = new List<ErpUser>();
            foreach (var item in resultQuery)
            {
                if (item.Val3 != "")
                {
                    var ts = new ErpUser
                    {

                        ID = Convert.ToInt32(item.Val1),
                        PASSWORD = item.Val17,
                        CODE = item.Val3,
                        NAME = item.Val4,
                        ADDRESS = item.Val5,
                        ADDRESS2 = item.Val6,
                        EMAIL = item.Val10,
                        CITY = item.Val7,
                        COUNTRY = item.Val9,
                        LIMIT = Convert.ToDecimal(item.Val16),
                        SPECODE = item.Val11,
                        SPECODE2 = item.Val12,
                        SPECODE3 = item.Val13,//Role
                        SPECODE4 = item.Val14,
                        SPECODE5 = item.Val15,
                    };
                    if (ts.EMAIL == "") ts.EMAIL = ts.CODE;
                    ErpUsersTemp.Add(ts);
                }
            }
            return ErpUsersTemp;
        }

        [AdminAuthorize]
        public ActionResult CustomerSync()
        {
            return View("~/Plugins/Peraport.AdminPlugin/Views/PPCustomer/CustomerSync.cshtml");
        }

        [AdminAuthorize]
        public ActionResult CustomerSyncPartial(int ProcessType = 0)
        {
            try
            {
                if (ProcessType == 0)
                {
                    ViewBag.ProcessType = 1;
                    return View("~/Plugins/Peraport.AdminPlugin/Views/PPCustomer/CustomerSyncPartial.cshtml", null);
                }
                var customer = _authService.GetAuthenticatedCustomer();

                NopServiceClient client = new NopServiceClient();
                client.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Anonymous;
                var userMatch = client.getMatch("MATCH_ID", "Customer", customer.Id.ToString());

                var csErp = client.GetGeneralSettings("CS_ERP");
                var NopCustomers = _customerService.GetAllCustomers();
                List<ErpUser> UsersNote = new List<ErpUser>();
                switch (ProcessType)
                {
                    case 1:
                        #region AddCustomers
                        var ErpUsersTemp = GetErpCustomers();
                        List<ErpUser> ErpUsers = new List<ErpUser>();
                        foreach (var item in ErpUsersTemp)
                        {
                            bool okEm = true;// item.EMAIL.Length > 5;
                            bool okAd = true;//item.ADDRESS.Length > 5;
                            if (okEm && okAd)
                            {
                                ErpUsers.Add(item);
                            }
                            else
                            {
                                string uyari = "";
                                uyari += okEm == false ? "Email Eksik." : "";
                                uyari += okAd == false ? "Adres Eksik." : "";
                                UsersNote.Add(new ErpUser { CITY = " X ", CODE = item.CODE, NAME = item.NAME, NOTE = uyari });
                            }
                        }
                        //create role
                        var crs = ErpUsers.GroupBy(x => x.SPECODE3).Select(y => y.Key).Where(z => z != "").ToList();
                        var ncroles = _customerService.GetAllCustomerRoles();
                        var ncrolesstr = _customerService.GetAllCustomerRoles().Select(y => y.Name).ToList();
                        foreach (var item in crs)
                        {
                            if (!ncrolesstr.Contains(item))
                            {
                                _customerService.InsertCustomerRole(new CustomerRole { Name = item, SystemName = item, Active = true });
                                UsersNote.Add(new ErpUser { CITY = " R ", CODE = "", NAME = "", NOTE = "Müşteri Rolü Eklendi." });
                            }
                        }
                        ///end of role
                        List<MATCHING_S> MatchList = new List<MATCHING_S>();
                        var HaveMatchList = client.getMatchingList("BASE_STR", "Customer", ErpUsers.Select(x => x.CODE).ToArray());
                        foreach (var item in ErpUsers)
                        {
                            try
                            {
                                var match = HaveMatchList.Where(x => x.BASE_ID == item.ID && x.BASE_STR == item.CODE).FirstOrDefault();
                                Customer karsi = null;
                                if (match != null)
                                    karsi = NopCustomers.Where(x => x.Id == match.MATCH_ID).FirstOrDefault();
                                //
                                if (karsi == null)
                                {//yok. eklenecek
                                    DateTime today = DateTime.Today;
                                    Customer nc = new Customer
                                    {
                                        Email = item.EMAIL,
                                        AdminComment = "Logo Aktarılan",//item.CODE,
                                        Username = item.CODE,
                                        CreatedOnUtc = today,
                                        LastActivityDateUtc = today,
                                        Active = true,
                                        RegisteredInStoreId = 1,
                                        SystemName = item.NAME
                                    };
                                    nc.Addresses.Add(new Address { Company = item.NAME, Email = item.EMAIL, Address1 = item.ADDRESS, City = item.CITY, CreatedOnUtc = today });
                                    //CustomerRole cr1 = _customerService.GetCustomerRoleBySystemName("Administrator");
                                    //nc.CustomerRoles.Add(cr1);
                                    CustomerRole cr2 = _customerService.GetCustomerRoleBySystemName("Registered");
                                    CustomerRole cr3 = _customerService.GetCustomerRoleBySystemName(item.SPECODE3);
                                    nc.CustomerRoles.Add(cr2);
                                    nc.CustomerRoles.Add(cr3);

                                    _customerService.InsertCustomer(nc);

                                    MATCHING_S m = new MATCHING_S { TABLE_NAME = "Customer", BASE_ID = item.ID, MATCH_ID = nc.Id, BASE_STR = item.CODE, MATCH_STR = item.EMAIL };
                                    MatchList.Add(m);

                                    var customerPassword = new CustomerPassword
                                    {
                                        Customer = nc,
                                        PasswordFormat = PasswordFormat.Hashed,
                                        CreatedOnUtc = DateTime.UtcNow
                                    };

                                    var saltKey = _encryptionService.CreateSaltKey(5);
                                    customerPassword.PasswordSalt = saltKey;
                                    customerPassword.Password = _encryptionService.CreatePasswordHash(item.PASSWORD, saltKey, _customerSettings.HashedPasswordFormat);
                                    _customerService.InsertCustomerPassword(customerPassword);
                                    UsersNote.Add(new ErpUser { EMAIL = nc.Email, CODE = nc.AdminComment, CITY = " + ", NAME = item.NAME, NOTE = "Kayıt eklendi." });
                                }
                                else
                                {//var. Değişiklik var mı ona bakılacak
                                 /*Customer*/// Email ve şifre etkilenmiyor. 
                                    if (item.EMAIL != karsi.Email)
                                    {
                                        karsi.Email = item.EMAIL;
                                        _customerService.UpdateCustomer(karsi);
                                        UsersNote.Add(new ErpUser { EMAIL = karsi.Email, CODE = karsi.AdminComment, CITY = " * ", NAME = item.NAME, NOTE = "Email adresi güncellendi." });
                                    }

                                    /*Adress*/
                                    if (item.ADDRESS != "")
                                    {
                                        Address a = karsi.Addresses.FirstOrDefault();
                                        if (a != null)
                                        {
                                            a.Address1 = item.ADDRESS;
                                            a.Company = item.NAME;
                                            a.City = item.CITY;
                                            a.Email = item.EMAIL;
                                            _customerService.UpdateCustomer(karsi);
                                        }
                                        else
                                        {//yeni adres ekle
                                            customer.Addresses.Add(new Address { Company = item.NAME, Address1 = item.ADDRESS, City = item.CITY, Email = item.EMAIL });
                                            UsersNote.Add(new ErpUser { EMAIL = karsi.Email, CODE = karsi.AdminComment, CITY = "A *", NAME = item.NAME, NOTE = "Adres Eklendi." });
                                        }
                                    }
                                    //if (item.ADDRESS != "")
                                    //{
                                    //    var aok = false;
                                    //    foreach (var adress in karsi.Addresses)
                                    //    {
                                    //        aok = aok || item.ADDRESS == adress.Address1;
                                    //    }
                                    //    if (!aok)
                                    //    {
                                    //        customer.Addresses.Add(new Address { Company = item.NAME, Address1 = item.ADDRESS, City = item.CITY, Email = item.EMAIL });
                                    //        UsersNote.Add(new ErpUser { EMAIL = karsi.Email, CODE = karsi.AdminComment, CITY = "A *", NAME = item.NAME, NOTE = "Adres Eklendi." });
                                    //    }
                                    //}
                                    /*Adress*/
                                    //if (item.ADDRESS2 != "")
                                    //{
                                    //    var aok = false;
                                    //    foreach (var adress in karsi.Addresses)
                                    //    {
                                    //        aok = aok || item.ADDRESS == adress.Address1;
                                    //    }
                                    //    if (!aok)
                                    //    {
                                    //        customer.Addresses.Add(new Address { Company = item.NAME, Address1 = item.ADDRESS2, City = item.CITY, Email = item.EMAIL });
                                    //        UsersNote.Add(new ErpUser { EMAIL = karsi.Email, CODE = karsi.AdminComment, CITY = "A *", NAME = item.NAME, NOTE = "Adres Eklendi." });
                                    //    }
                                    //}
                                }
                            }
                            catch (Exception e)
                            {
                                UsersNote.Add(new ErpUser { EMAIL = "Customer", CODE = item.CODE, CITY = "M", NAME = "", NOTE = "Eklenirken Hata Oluştu. Diğerlerine devam edilecek" });
                            }

                            // Müşteriler eklendi. şimdi eşleştirme yapılacak
                        }
                        if (MatchList.Count > 0)
                        {
                            var xxx = client.MatchTwoRecord(MatchList.ToArray());
                            UsersNote.Add(new ErpUser { EMAIL = "Matching", CODE = "", CITY = "M", NAME = MatchList.Count.ToString(), NOTE = "Adet Kullanıcı Match Edildi." });
                        }
                        #endregion
                        break;
                    case 2:
                        #region UpdatePasswords
                        var ErpCustomers = GetErpCustomers();
                        var matchList = client.getMatchingList("MATCH_ID", "Customer", NopCustomers.Select(x => x.Id.ToString()).ToArray());
                        foreach (var _customer in NopCustomers)
                        {
                            var match = matchList.Where(x => x.MATCH_ID == _customer.Id).LastOrDefault();
                            if (match != null)
                            {
                                ErpUser _erpcustomer = ErpCustomers.Where(x => x.ID == match.BASE_ID).LastOrDefault();
                                if (_erpcustomer != null && _erpcustomer.PASSWORD.Length > 3 && _erpcustomer.CODE != "320.01.10")
                                {
                                    var customerPassword = new CustomerPassword
                                    {
                                        Customer = _customer,
                                        PasswordFormat = PasswordFormat.Hashed,
                                        CreatedOnUtc = DateTime.UtcNow
                                    };

                                    var saltKey = _encryptionService.CreateSaltKey(5);
                                    customerPassword.PasswordSalt = saltKey;
                                    customerPassword.Password = _encryptionService.CreatePasswordHash(_erpcustomer.PASSWORD, saltKey, _customerSettings.HashedPasswordFormat);
                                    _customerService.InsertCustomerPassword(customerPassword);

                                    UsersNote.Add(new ErpUser { CITY = " OK ", CODE = _erpcustomer.CODE, NAME = _customer.SystemName, NOTE = " Şifre Güncellendi." });
                                }
                                else
                                {
                                    UsersNote.Add(new ErpUser { CITY = " X ", CODE = "X", NAME = _customer.SystemName, NOTE = " Kullanıcı Erp de yok" });
                                }
                            }
                            else
                            {
                                UsersNote.Add(new ErpUser { CITY = " X ", CODE = "X", NAME = _customer.SystemName, NOTE = " Kullanıcı Eşleştirmesi yok" });
                            }
                        }
                        #endregion
                        break;
                    case 3:
                        #region Info
                        client = new NopServiceClient();
                        client.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Anonymous;
                        var M = client.getMatchingList("MATCH_ID", "Info", null);

                        List<InfoModel> im = new List<InfoModel>();

                        foreach (var m in M.OrderBy(x => x.BASE_STR))
                        {
                            var b = m.MATCH_STR.Split('_');
                            try
                            {
                                im.Add(new InfoModel { ERP_CODE = m.BASE_STR, VD = b[0], VN = b[1], UNVAN = b[2], EMAIL = b[3], TEL = b[4] });
                            }
                            catch (Exception)
                            {
                            }
                        }
                        ViewBag.Infos = im;
                        #endregion
                        break;
                    case 4:
                        #region Ek Adresler
                        var Adresler = client.GetAddressFromErp();
                        foreach (var _adres in Adresler)
                        {
                            if (_adres.NOP_ID > 0)
                            {
                                var _nc = NopCustomers.Where(x=>x.Id==_adres.NOP_ID).FirstOrDefault();
                                if (_nc != null)
                                {//müşteri bulundu.
                                    //Adres varmı diye bak
                                    if (_nc.Addresses.Where(x => x.Address1 == _adres.ADDRESS).FirstOrDefault() == null)
                                    {
                                        if(_adres.EMAIL.Length>3 && _adres.ADDRESS.Length > 10)
                                        {
                                            _nc.Addresses.Add(new Address { Company = _adres.NAME, Address1 = _adres.ADDRESS, City = _adres.CITY, Email = _adres.EMAIL, CustomAttributes = _adres.CODE + _adres.NAME, CreatedOnUtc = DateTime.Now });
                                            _customerService.UpdateCustomer(_nc);
                                            UsersNote.Add(new ErpUser { EMAIL = _adres.EMAIL, CODE = _nc.AdminComment, CITY = "A *", NAME = _adres.NAME, NOTE = "Adres Eklendi." });
                                        }
                                        else
                                            UsersNote.Add(new ErpUser { EMAIL = _adres.EMAIL, CODE = _nc.AdminComment, CITY = "X *", NAME = _adres.NAME+" -Email:"+_adres.EMAIL, NOTE = "Adres Geçerli Gözükmüyor.-"+_adres.ADDRESS });

                                    }
                                    else
                                    {
                                        UsersNote.Add(new ErpUser { EMAIL = _adres.EMAIL, CODE = _nc.AdminComment, CITY = "VAR *", NAME = _adres.NAME, NOTE = "Adres Daha Önceden Eklenmiş." });
                                    }
                                }

                            }
                            else
                            {
                                UsersNote.Add(new ErpUser { CITY = " X ", CODE = "X", NAME = _adres.CODE, NOTE = "Kullanıcı Eşleştirmesi yok" });
                            }
                        }
                        #endregion
                        break;

                }

                return View("~/Plugins/Peraport.AdminPlugin/Views/PPCustomer/CustomerSyncPartial.cshtml", UsersNote);
            }
            catch (Exception Ex)
            {
                List<ErpUser> bm = new List<ErpUser>();
                return View("~/Plugins/Peraport.AdminPlugin/Views/PPCustomer/CustomerSyncPartial.cshtml", bm);
            }
        }
    }
}

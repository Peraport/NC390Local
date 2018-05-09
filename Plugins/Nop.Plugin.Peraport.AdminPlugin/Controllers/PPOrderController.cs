using Nop.Core;
using Nop.Core.Domain.Customers;
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
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Nop.Plugin.Peraport.AdminPlugin.Controllers
{
    public class PPOrderController : BasePluginController
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

        public PPOrderController(
            IWorkContext workContext,
            IStoreService storeService,
            ISettingService settingService,
            IAuthenticationService authService,
            ICustomerService customerService,
            IEncryptionService encryptionService,
            IWebHelper webHelper,
            CustomerSettings customerSettings
         )
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

        public ActionResult OrderListPartial()
        {
            NopServiceClient client = new NopServiceClient();
            client.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Anonymous;
            var model = client.GetNewOrdersFromNop("","");
            return View("~/Plugins/Peraport.AdminPlugin/Views/PPOrder/OrderListPartial.cshtml",model);
        }


        [AdminAuthorize]
        public ActionResult OrderSync()
        {
            NopServiceClient client = new NopServiceClient();
            client.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Anonymous;
            return View("~/Plugins/Peraport.AdminPlugin/Views/PPOrder/OrderSync.cshtml");
        }

        [AdminAuthorize]
        public ActionResult OrderSyncPartial(int ProcessType = 0)
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
                var a = client.GetNewOrdersFromNop("","");

            }
            catch (Exception ex)
            {

            }
            return null;
        }
    }
}

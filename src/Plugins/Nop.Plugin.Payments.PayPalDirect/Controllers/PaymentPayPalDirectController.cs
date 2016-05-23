using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Nop.Core;
using Nop.Plugin.Payments.PayPalDirect.Models;
using Nop.Plugin.Payments.PayPalDirect.Validators;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Payments;
using Nop.Services.Stores;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Payments.PayPalDirect.Controllers
{
    public class PaymentPayPalDirectController : BasePaymentController
    {
        #region Fields
        private readonly ILocalizationService _localizationService;
        private readonly ISettingService _settingService;
        private readonly IStoreService _storeService;
        private readonly IWorkContext _workContext;
        #endregion

        #region Ctor
        public PaymentPayPalDirectController(ILocalizationService localizationService,
            ISettingService settingService,
            IStoreService storeService,
            IWorkContext workContext)
        {
            this._localizationService = localizationService;
            this._settingService = settingService;
            this._storeService = storeService;
            this._workContext = workContext;
        }
        #endregion

        #region Methods
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {
            //load settings for a chosen store scope
            var storeScope = GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var payPalDirectPaymentSettings = _settingService.LoadSetting<PayPalDirectPaymentSettings>(storeScope);

            var model = new ConfigurationModel
            {
                ClientId = payPalDirectPaymentSettings.ClientId,
                ClientSecret = payPalDirectPaymentSettings.ClientSecret,
                UseSandbox = payPalDirectPaymentSettings.UseSandbox,
                TransactModeId = (int)payPalDirectPaymentSettings.TransactMode,
                AdditionalFee = payPalDirectPaymentSettings.AdditionalFee,
                AdditionalFeePercentage = payPalDirectPaymentSettings.AdditionalFeePercentage,
                TransactModeValues = payPalDirectPaymentSettings.TransactMode.ToSelectList(),
                ActiveStoreScopeConfiguration = storeScope
            };
            if (storeScope > 0)
            {
                model.ClientId_OverrideForStore = _settingService.SettingExists(payPalDirectPaymentSettings, x => x.ClientId, storeScope);
                model.ClientSecret_OverrideForStore = _settingService.SettingExists(payPalDirectPaymentSettings, x => x.ClientSecret, storeScope);
                model.UseSandbox_OverrideForStore = _settingService.SettingExists(payPalDirectPaymentSettings, x => x.UseSandbox, storeScope);
                model.TransactModeId_OverrideForStore = _settingService.SettingExists(payPalDirectPaymentSettings, x => x.TransactMode, storeScope);
                model.AdditionalFee_OverrideForStore = _settingService.SettingExists(payPalDirectPaymentSettings, x => x.AdditionalFee, storeScope);
                model.AdditionalFeePercentage_OverrideForStore = _settingService.SettingExists(payPalDirectPaymentSettings, x => x.AdditionalFeePercentage, storeScope);
            }

            return View("~/Plugins/Payments.PayPalDirect/Views/PaymentPayPalDirect/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var payPalDirectPaymentSettings = _settingService.LoadSetting<PayPalDirectPaymentSettings>(storeScope);

            //save settings
            payPalDirectPaymentSettings.ClientId = model.ClientId;
            payPalDirectPaymentSettings.ClientSecret = model.ClientSecret;
            payPalDirectPaymentSettings.UseSandbox = model.UseSandbox;
            payPalDirectPaymentSettings.TransactMode = (TransactMode)model.TransactModeId;
            payPalDirectPaymentSettings.AdditionalFee = model.AdditionalFee;
            payPalDirectPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            if (model.ClientId_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(payPalDirectPaymentSettings, x => x.ClientId, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(payPalDirectPaymentSettings, x => x.ClientId, storeScope);

            if (model.ClientSecret_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(payPalDirectPaymentSettings, x => x.ClientSecret, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(payPalDirectPaymentSettings, x => x.ClientSecret, storeScope);

            if (model.UseSandbox_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(payPalDirectPaymentSettings, x => x.UseSandbox, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(payPalDirectPaymentSettings, x => x.UseSandbox, storeScope);

            if (model.TransactModeId_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(payPalDirectPaymentSettings, x => x.TransactMode, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(payPalDirectPaymentSettings, x => x.TransactMode, storeScope);

            if (model.AdditionalFee_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(payPalDirectPaymentSettings, x => x.AdditionalFee, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(payPalDirectPaymentSettings, x => x.AdditionalFee, storeScope);

            if (model.AdditionalFeePercentage_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(payPalDirectPaymentSettings, x => x.AdditionalFeePercentage, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(payPalDirectPaymentSettings, x => x.AdditionalFeePercentage, storeScope);

            //now clear settings cache
            _settingService.ClearCache();

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        [ChildActionOnly]
        public ActionResult PaymentInfo()
        {
            var model = new PaymentInfoModel();

            model.CreditCardTypes = new List<SelectListItem>
            {
                new SelectListItem { Text = "Visa", Value = "visa" },
                new SelectListItem { Text = "Master card", Value = "MasterCard" },
                new SelectListItem { Text = "Discover", Value = "Discover" },
                new SelectListItem { Text = "Amex", Value = "Amex" },
            };

            //years
            for (var i = 0; i < 15; i++)
            {
                var year = (DateTime.Now.Year + i).ToString();
                model.ExpireYears.Add(new SelectListItem
                {
                    Text = year,
                    Value = year,
                });
            }

            //months
            for (var i = 1; i <= 12; i++)
            {
                var text = (i < 10) ? "0" + i : i.ToString();
                model.ExpireMonths.Add(new SelectListItem
                {
                    Text = text,
                    Value = i.ToString(),
                });
            }

            //set postback values
            var form = Request.Form;
            model.CardNumber = form["CardNumber"];
            model.CardCode = form["CardCode"];
            var selectedCcType = model.CreditCardTypes.FirstOrDefault(x => x.Value.Equals(form["CreditCardType"], StringComparison.InvariantCultureIgnoreCase));
            if (selectedCcType != null)
                selectedCcType.Selected = true;
            var selectedMonth = model.ExpireMonths.FirstOrDefault(x => x.Value.Equals(form["ExpireMonth"], StringComparison.InvariantCultureIgnoreCase));
            if (selectedMonth != null)
                selectedMonth.Selected = true;
            var selectedYear = model.ExpireYears.FirstOrDefault(x => x.Value.Equals(form["ExpireYear"], StringComparison.InvariantCultureIgnoreCase));
            if (selectedYear != null)
                selectedYear.Selected = true;

            return View("~/Plugins/Payments.PayPalDirect/Views/PaymentPayPalDirect/PaymentInfo.cshtml", model);
        }

        [NonAction]
        public override IList<string> ValidatePaymentForm(FormCollection form)
        {
            var warnings = new List<string>();

            //validate
            var validator = new PaymentInfoValidator(_localizationService);
            var model = new PaymentInfoModel
            {
                CardNumber = form["CardNumber"],
                CardCode = form["CardCode"],
                ExpireMonth = form["ExpireMonth"],
                ExpireYear = form["ExpireYear"]
            };
            var validationResult = validator.Validate(model);
            if (!validationResult.IsValid)
                warnings.AddRange(validationResult.Errors.Select(error => error.ErrorMessage));

            return warnings;
        }

        [NonAction]
        public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
        {
            return new ProcessPaymentRequest
            { 
                CreditCardType = form["CreditCardType"],
                CreditCardNumber = form["CardNumber"],
                CreditCardExpireMonth = int.Parse(form["ExpireMonth"]),
                CreditCardExpireYear = int.Parse(form["ExpireYear"]),
                CreditCardCvv2 = form["CardCode"]
            };
        }
        #endregion
    }
}
using System.Collections.Generic;
using System.Web.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Admin.Models.Messages
{
    public class CampaignListModel : BaseNopModel
    {
        public CampaignListModel()
        {
            AvailableCustomerRoles = new List<SelectListItem>();
            AvailableStores = new List<SelectListItem>();
        }

        [NopResourceDisplayName("Admin.Promotions.Campaigns.List.Stores")]
        public int StoreId { get; set; }
        public IList<SelectListItem> AvailableStores { get; set; }

        [NopResourceDisplayName("Admin.Promotions.Campaigns.List.CustomerRoles")]
        public int CustomerRoleId { get; set; }
        public IList<SelectListItem> AvailableCustomerRoles { get; set; }
    }
}
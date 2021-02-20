using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MSTC.Web.Model.Partials
{
    public class MemberOptionsModel
    {
        public MemberOptionsModel()
        {
            OptionalExtras = new List<string>();
        }

        public bool MembershipExpired { get; set; }
        public DateTime MembershipExpiry { get; set; }
        public string MembershipType { get; set; }
        public List<string> OptionalExtras { get; set; }
        public bool IsBankLinked { get; set; }

        //Swim Subs
        public bool ShowBuySwimSubs1 { get; set; }
        public string BuySwimSubs1Text { get; set; }
        public bool ShowBuySwimSubs2 { get; set; }
        public string BuySwimSubs2Text { get; set; }

        public bool EnableMemberRenewal { get; set; }
        public bool ShowMemberAdminLink { get; set; }
        public bool ShowIceLink { get; set; }

        //OWS Props
        public bool EnableOpenWater { get; set; }
        public string OWSNumber { get; set; }
        public bool OwsIndemnityAccepted { get; set; }
        public string OwsIndemnityDocLink { get; set; }

        //Page Urls
        public string RenewalPageUrl { get; set; }
        public string MemberAdminPageUrl { get; set; }
        public string EventBookingPageUrl { get; set; }
        public string EventAdminPageUrl { get; set; }
        public string UnlinkBankPageUrl { get; set; }
    }
}
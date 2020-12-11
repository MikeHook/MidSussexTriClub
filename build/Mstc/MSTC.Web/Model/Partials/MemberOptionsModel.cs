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

        public bool EnableMemberRenewal { get; set; }

        public string MemberRenewalPage { get; set; }

        public bool MembershipExpired { get; set; }

        public DateTime MembershipExpiry { get; set; }

        public string MembershipType { get; set; }

        public List<string> OptionalExtras { get; set; }

        public bool ShowBuySwimSubs1 { get; set; }

        public bool ShowBuySwimSubs2 { get; set; }

        public bool ShowMemberAdminLink { get; set; }

        public bool ShowIceLink { get; set; }

        public bool EnableOpenWater { get; set; }

        public string OWSNumber { get; set; }

        public bool OwsIndemnityAccepted { get; set; }
    }
}
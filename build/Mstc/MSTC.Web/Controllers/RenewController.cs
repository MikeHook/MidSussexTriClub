using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Mstc.Core.Domain;
using Mstc.Core.Providers;
using Newtonsoft.Json;
using Umbraco.Web.Mvc;

namespace MSTC.Web.Controllers
{
    public class RenewController : SurfaceController
    {
        protected SessionProvider _sessionProvider;
        MemberProvider _memberProvider;
        MembershipCostCalculator _membershipCostCalculator;

        public RenewController( )
        {
            _sessionProvider = new SessionProvider();
            _memberProvider = new MemberProvider(Services);
            _membershipCostCalculator = new MembershipCostCalculator();
        }

        [AcceptVerbs(HttpVerbs.Post | HttpVerbs.Get)]
        public ActionResult RenewOptions()
        {
            var member = _memberProvider.GetLoggedInMember();
            if (member == null)
            {
                return CurrentUmbracoPage();
            }

            var membershipType = member.GetValue<MembershipTypeEnum>(MemberProperty.membershipType);

            var model = new RegistrationDetails(_membershipCostCalculator);            
            model.MemberOptions.IsRenewing = membershipType != MembershipTypeEnum.Guest;
            model.MemberOptions.IsUpgrading = membershipType == MembershipTypeEnum.Guest;            

            return PartialView("Registration/MemberOptions", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Renew(RegistrationDetails model)
        {
            var member = _memberProvider.GetLoggedInMember();
            if (!ModelState.IsValid || member == null)
            {
                return CurrentUmbracoPage();
            }

            _sessionProvider.RenewalOptions = model.MemberOptions;

            var membershipType = member.GetValue<MembershipTypeEnum>(MemberProperty.membershipType);
            bool isRenewing = membershipType != MembershipTypeEnum.Guest;

            Logger.Info(typeof(RenewController), $"Member {(isRenewing ? "renewal" : "upgrade")} request: {member.Email}, {JsonConvert.SerializeObject(model.MemberOptions)}");

            PaymentStates state = isRenewing ? PaymentStates.MemberRenewal : PaymentStates.MemberUpgrade;
            _sessionProvider.CanProcessPaymentCompletion = true;
            var redirectUrl = $"/Payment?state={state}";
            return Redirect(redirectUrl);
        }
    }
}
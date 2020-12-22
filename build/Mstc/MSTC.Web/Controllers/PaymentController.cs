using System;
using System.Configuration;
using System.Text;
using System.Web.Mvc;
using System.Web.Security;
using Mstc.Core.Domain;
using Mstc.Core.Dto;
using Mstc.Core.Providers;
using MSTC.Web.Model;
using Newtonsoft.Json;
using Umbraco.Core.Models;
using Umbraco.Web.Mvc;

namespace MSTC.Web.Controllers
{
    public class PaymentController : SurfaceController
    {
        protected SessionProvider _sessionProvider;
        protected GoCardlessProvider _goCardlessProvider;
        EmailProvider _emailProvider;
        MemberProvider _memberProvider;


        public PaymentController( )
        {
            //TODO - Use IoC?
            _sessionProvider = new SessionProvider();
            _goCardlessProvider = new GoCardlessProvider();
            _emailProvider = new EmailProvider();
            _memberProvider = new MemberProvider(Services);
        }        

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UnlinkBank()
        {
            if (!ModelState.IsValid)
            {
                return CurrentUmbracoPage();
            }

            IMember member = _memberProvider.GetLoggedInMember();
            if (member == null)
            {
                return CurrentUmbracoPage();
            }

            member.SetValue(MemberProperty.directDebitMandateId, "");
            Services.MemberService.Save(member);

            TempData["IsUnlinked"] = true;
            return CurrentUmbracoPage();
        }      

    }
}
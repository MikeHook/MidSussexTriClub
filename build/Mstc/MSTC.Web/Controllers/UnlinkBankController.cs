using System.Web.Mvc;
using Mstc.Core.Domain;
using Mstc.Core.Providers;
using Umbraco.Core.Models;
using Umbraco.Web.Mvc;

namespace MSTC.Web.Controllers
{
    public class UnlinkBankController : SurfaceController
    {
        MemberProvider _memberProvider;

        public UnlinkBankController( )
        {
            _memberProvider = new MemberProvider(Services);
        }
    
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Unlink()
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
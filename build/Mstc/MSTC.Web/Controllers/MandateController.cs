using System.Linq;
using System.Web.Mvc;
using Mstc.Core.Domain;
using Mstc.Core.Dto;
using Mstc.Core.Providers;
using Umbraco.Core;
using Umbraco.Core.Services;
using Umbraco.Web.Mvc;

namespace MSTC.Web.Controllers
{
    [MemberAuthorize(AllowType = "Member")]
    public class MandateController : Controller
    {
        protected IMemberService _memberService;
        protected SessionProvider _sessionProvider;
        protected GoCardlessProvider _goCardlessProvider;
        protected MemberProvider _memberProvider;

        public MandateController()
        {
            _sessionProvider = new SessionProvider();
            _goCardlessProvider = new GoCardlessProvider();
            _memberService = ApplicationContext.Current.Services.MemberService;
            _memberProvider = new MemberProvider(_memberService);
        }

        [HttpGet]
        public ActionResult Index()
        {
            var member = _memberProvider.GetLoggedInMember();
            string state = Request.QueryString["state"];
            bool canProcessPaymentCompletion = _sessionProvider.CanProcessPaymentCompletion;
            if (member == null || !canProcessPaymentCompletion || string.IsNullOrEmpty(state))
            {
                return View(false);
            }

            var fullName = member.Name;
            string familyName = fullName;
            string givenName = string.Empty;
            if (fullName.Contains(" "))
            {
                var names = fullName.Split(' ');
                familyName = names.Last();
                givenName = string.Join(" ", names.Take(names.Length - 1));
            }

            var customerDto = new CustomerDto()
            {
                GivenName = givenName,
                FamilyName = familyName,
                AddressLine1 = member.GetValue<string>(MemberProperty.Address1),
                City = member.GetValue<string>(MemberProperty.City),
                PostalCode = member.GetValue<string>(MemberProperty.Postcode),
                Email = member.Email
            };           
            
            string rootUrl = string.Format("{0}://{1}{2}", Request.Url.Scheme, Request.Url.Host, Request.Url.Port == 80 ? string.Empty : ":" + Request.Url.Port);
            string mandateSuccessUrl = string.Format("{0}/payment?state={1}", rootUrl, state);

            var redirectResponse = _goCardlessProvider.CreateRedirectRequest(customerDto, "Mid Sussex Tri Club DD Mandate Setup", _sessionProvider.SessionId,
                mandateSuccessUrl);

            _sessionProvider.GoCardlessRedirectFlowId = redirectResponse.Id;
            return Redirect(redirectResponse.RedirectUrl);
        }

    }
}
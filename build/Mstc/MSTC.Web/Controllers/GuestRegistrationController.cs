using System;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Security;
using Mstc.Core.ContentModels;
using Mstc.Core.Domain;
using Mstc.Core.Dto;
using Mstc.Core.Providers;
using MSTC.Web.Model;
using Newtonsoft.Json;
using Umbraco.Web;
using Umbraco.Web.Mvc;
using Umbraco.Web.PublishedContentModels;

namespace MSTC.Web.Controllers
{
    public class GuestRegistrationController : SurfaceController
    {
        protected SessionProvider _sessionProvider;
        protected GoCardlessProvider _goCardlessProvider;
        EmailProvider _emailProvider;
        MemberProvider _memberProvider;
        MembershipCostCalculator _membershipCostCalculator;

        GuestRegistration _guestRegistrationPage;

        public GuestRegistrationController( )
        {
            _sessionProvider = new SessionProvider();
            _goCardlessProvider = new GoCardlessProvider();
            _emailProvider = new EmailProvider();
            _memberProvider = new MemberProvider(Services);
            _membershipCostCalculator = new MembershipCostCalculator();

            UmbracoHelper umbracoHelper = new UmbracoHelper(UmbracoContext);
            var guestContent = umbracoHelper?.TypedContentAtRoot().DescendantsOrSelf(GuestRegistration.ModelTypeAlias).FirstOrDefault();
            if (guestContent != null)
            {
                _guestRegistrationPage = new GuestRegistration(guestContent);
            }          
        }

        [AcceptVerbs(HttpVerbs.Post | HttpVerbs.Get)]
        public ActionResult PersonalDetails()
        {
            return PartialView("Registration/PersonalDetails", new PersonalDetails());
        }        

        [AcceptVerbs(HttpVerbs.Post | HttpVerbs.Get)]
        public ActionResult GuestOptions()
        {
            bool showOWSSignup = _guestRegistrationPage?.OWssignupEnabled ?? false;
            return PartialView("Registration/GuestOptions", new GuestOptions(showOWSSignup));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(PersonalDetails personalDetails, GuestOptions guestOptions)
        {
            if (!ModelState.IsValid)
            {
                return CurrentUmbracoPage();
            }

            var existingMember = Services.MemberService.GetByEmail(personalDetails.Email);

            if (existingMember != null)
            {
                ModelState.AddModelError("Email", "There is already a member registered with the supplied email address.");
                return CurrentUmbracoPage();
            }

            if (_guestRegistrationPage == null || !_guestRegistrationPage.GuestCodes.Contains(guestOptions.GuestCode, StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("GuestCode", "The guest code does not match any available codes.");
                return CurrentUmbracoPage();
            }           

            Logger.Info(typeof(RegistrationController), $"New guest registration request: {JsonConvert.SerializeObject(personalDetails)}");

            string rootUrl = string.Format("{0}://{1}{2}", Request.Url.Scheme, Request.Url.Host,
                Request.Url.Port == 80 ? string.Empty : ":" + Request.Url.Port);
            string successUrl = string.Format("{0}/the-club/confirm-registration", rootUrl);

            //TODO - Register guest
            var member = _memberProvider.CreateMember(personalDetails, new string[] { MSTCRoles.Guest });
            var memberOptions = new MemberOptions()
            {
                MembershipType = MembershipTypeEnum.Guest,
                OpenWaterIndemnityAcceptance = guestOptions.OpenWaterIndemnityAcceptance,
                GuestCode = guestOptions.GuestCode,
            };
            _memberProvider.UpdateMemberDetails(member, personalDetails, memberOptions);

            //Login the member
            FormsAuthentication.SetAuthCookie(member.Username, true);

            string content = GetRegEmailContent(personalDetails, guestOptions);
            _emailProvider.SendEmail(EmailProvider.MembersEmail, EmailProvider.SupportEmail,
                "New MSTC guest registration", content);

            var model = new RegistrationCompleteModel()
            {
                PromptForConfirmation = false,
                IsRegistered = true
            };
            TempData["Model"] = model;

            return Redirect(successUrl);
        }

        private string GetRegEmailContent(PersonalDetails personalDets, GuestOptions guestOptions)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("<p>A new guest has been registered with the club</p><p>Guest details:</p>");

            stringBuilder.AppendFormat("{0}: <strong>{1}</strong><br/>", "Name", $"{personalDets.FirstName} {personalDets.LastName}");
            stringBuilder.AppendFormat("{0}: <strong>{1}</strong><br/>", "Email", $"{personalDets.Email}");
            stringBuilder.AppendFormat("{0}: <strong>{1}</strong><br/>", "Phone number", $"{personalDets.PhoneNumber}");
            stringBuilder.AppendFormat("{0}: <strong>{1}</strong><br/>", "Gender", $"{personalDets.Gender.ToString()}");
            stringBuilder.AppendFormat("{0}: <strong>{1}</strong><br/>", "Date of Birth", $"{personalDets.DateOfBirth.Value.ToString("dd MMM yyyy")}");
            stringBuilder.AppendFormat("{0}: <strong>{1}</strong><br/>", "Membership Type", $"{MembershipTypeEnum.Guest.ToString()}");

            return stringBuilder.ToString();
        }
    }
}
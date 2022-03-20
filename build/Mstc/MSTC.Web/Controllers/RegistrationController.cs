using System;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Security;
using GoCardless;
using Mstc.Core.ContentModels;
using Mstc.Core.Domain;
using Mstc.Core.Dto;
using Mstc.Core.Providers;
using MSTC.Web.Model;
using Newtonsoft.Json;
using Umbraco.Web;
using Umbraco.Web.Mvc;
using Umbraco.Web.PublishedContentModels;
using System.Net.Mail;

namespace MSTC.Web.Controllers
{
    public class RegistrationController : SurfaceController
    {
        protected SessionProvider _sessionProvider;
        protected GoCardlessProvider _goCardlessProvider;
        EmailProvider _emailProvider;
        MemberProvider _memberProvider;
        MembershipCostCalculator _membershipCostCalculator;  

        public RegistrationController( )
        {
            //TODO - Use IoC?
            _sessionProvider = new SessionProvider();
            _goCardlessProvider = new GoCardlessProvider();
            _emailProvider = new EmailProvider();
            _memberProvider = new MemberProvider(Services);
            _membershipCostCalculator = new MembershipCostCalculator();
        }

        [AcceptVerbs(HttpVerbs.Post | HttpVerbs.Get)]
        public ActionResult PersonalDetails()
        {
            return PartialView("Registration/PersonalDetails", new PersonalDetails());
        }

        [AcceptVerbs(HttpVerbs.Post | HttpVerbs.Get)]
        public ActionResult MemberOptions()
        {
            return PartialView("Registration/MemberOptions", new MemberOptions(_membershipCostCalculator));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(MemberOptions memberOptions, PersonalDetails personalDetails)
        {
            if (!ModelState.IsValid)
            {
                return CurrentUmbracoPage();
            }

            var member = Services.MemberService.GetByEmail(personalDetails.Email);

            if (member != null)
            {
                ModelState.AddModelError("Email", "There is already a member registered with the supplied email address.");
                return CurrentUmbracoPage();
            }

            var model = new RegistrationDetails() { PersonalDetails = personalDetails, MemberOptions = memberOptions };
            _sessionProvider.RegistrationDetails = model;

            Logger.Info(typeof(RegistrationController), $"New member registration request: {JsonConvert.SerializeObject(model)}");     

            var customerDto = new CustomerDto()
            {
                GivenName = model.PersonalDetails.FirstName,
                FamilyName = model.PersonalDetails.LastName,
                AddressLine1 = model.PersonalDetails.Address1,
                City = model.PersonalDetails.City,
                PostalCode = model.PersonalDetails.Postcode,
                Email = model.PersonalDetails.Email
            };

            string scheme = _goCardlessProvider.Environment == GoCardlessClient.Environment.LIVE ? "Https" : Request.Url.Scheme;
            string rootUrl = string.Format("{0}://{1}{2}", scheme, Request.Url.Host, Request.Url.Port == 80 ? string.Empty : ":" + Request.Url.Port);
            string successUrl = string.Format("{0}/the-club/confirm-registration", rootUrl);

            var redirectResponse = _goCardlessProvider.CreateRedirectRequest(Logger, customerDto, "MSTC Member Registration", _sessionProvider.SessionId,
                successUrl);

            if (redirectResponse.HasError)
            {
                ModelState.AddModelError("", redirectResponse.Error);
                return CurrentUmbracoPage();
            }

            _sessionProvider.GoCardlessRedirectFlowId = redirectResponse.Id;
            return Redirect(redirectResponse.RedirectUrl);
        }

        public ActionResult RenderRegistrationComplete()
        {
            var registrationDetails = _sessionProvider.RegistrationDetails;
            if (registrationDetails == null)
            {
                  return PartialView("Registration/RegistrationComplete", new RegistrationCompleteModel());
            }

            int costInPence = _membershipCostCalculator.Calculate(registrationDetails.MemberOptions, DateTime.Now);

            var model = new RegistrationCompleteModel()
            {
                PromptForConfirmation = registrationDetails != null,
                PaymentDescription = MemberProvider.GetPaymentDescription(registrationDetails.MemberOptions),
                Cost = (costInPence / 100m).ToString("N2")
            };
    
            return PartialView("Registration/RegistrationComplete", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmPayment(RegistrationCompleteModel model)
        {
            model.PromptForConfirmation = false;
            RegistrationDetails registrationDetails = _sessionProvider.RegistrationDetails;
            if (registrationDetails == null || string.IsNullOrWhiteSpace(_sessionProvider.GoCardlessRedirectFlowId))
            {
                TempData["Model"] = model;
                return CurrentUmbracoPage();
            }

            string mandateId = _goCardlessProvider.CompleteRedirectRequest(_sessionProvider.GoCardlessRedirectFlowId, _sessionProvider.SessionId);
            registrationDetails.PersonalDetails.DirectDebitMandateId = mandateId;

            var regDetails = registrationDetails.PersonalDetails;
            int costInPence = _membershipCostCalculator.Calculate(registrationDetails.MemberOptions, DateTime.Now);
            model.Cost = (costInPence / 100m).ToString("N2");
            var paymentDescription = MemberProvider.GetPaymentDescription(registrationDetails.MemberOptions);
            var paymentResponse = _goCardlessProvider.CreatePayment(Logger, regDetails.DirectDebitMandateId, regDetails.Email, costInPence, paymentDescription);
                        
            model.IsRegistered = paymentResponse == PaymentResponseDto.Success;

            if (model.IsRegistered)
            {                
                var member = _memberProvider.CreateMember(regDetails, new string[] { MSTCRoles.Member });
                _memberProvider.UpdateMemberDetails(member, registrationDetails.PersonalDetails, registrationDetails.MemberOptions);

                //Login the member
                FormsAuthentication.SetAuthCookie(member.Username, true);          

                string content = GetRegEmailContent(registrationDetails, true);
                _emailProvider.SendEmail(EmailProvider.MembersEmail, EmailProvider.SupportEmail,
                    "New MSTC member registration", content);

                // NEW - send welcome email automatically
                string filePath = ConfigurationManager.AppSettings["welcomePack"];
                Attachment attachment = new Attachment(filePath);

                _emailProvider.SendEmail(registrationDetails.PersonalDetails.Email, EmailProvider.MembersEmail,
                    "Welcome to MSTC", GetWelcomeEmailContent(registrationDetails), attachment);                

                _sessionProvider.RegistrationDetails = null;
                _sessionProvider.GoCardlessRedirectFlowId = null;
                TempData["Model"] = model;
                return CurrentUmbracoPage();                             
            }
            else
            {
                string content = GetRegEmailContent(registrationDetails, false);
                _emailProvider.SendEmail(EmailProvider.SupportEmail, EmailProvider.SupportEmail,
                    "MSTC member registration problem", content);
            }

            TempData["Model"] = model;
            return CurrentUmbracoPage();
        }

        private string GetRegEmailContent(RegistrationDetails registrationDetails, bool isRegistered)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("<p>A new member has {0} been registered with the club</p><p>Member details:</p>", isRegistered ? "" : "NOT");

            var personalDets = registrationDetails.PersonalDetails;
            var memberOptions = registrationDetails.MemberOptions;
            stringBuilder.AppendFormat("{0}: <strong>{1}</strong><br/>", "Name", $"{personalDets.FirstName} {personalDets.LastName}");
            stringBuilder.AppendFormat("{0}: <strong>{1}</strong><br/>", "Email", $"{personalDets.Email}");
            stringBuilder.AppendFormat("{0}: <strong>{1}</strong><br/>", "Phone number", $"{personalDets.PhoneNumber}");
            stringBuilder.AppendFormat("{0}: <strong>{1}</strong><br/>", "Gender", $"{personalDets.Gender.ToString()}");
            stringBuilder.AppendFormat("{0}: <strong>{1}</strong><br/>", "Date of Birth", $"{personalDets.DateOfBirth.Value.ToString("dd MMM yyyy")}");
            stringBuilder.AppendFormat("{0}: <strong>{1}</strong><br/>", "Membership Type", $"{memberOptions.MembershipType.ToString()}");
            stringBuilder.AppendFormat("{0}: <strong>{1}</strong><br/>", "England Athletics Membership", memberOptions.EnglandAthleticsMembership ? "Yes" : "No");
            stringBuilder.AppendFormat("{0}: <strong>{1}</strong><br/>", MemberProvider.GetSwimSub1Description(DateTime.Now), memberOptions.SwimSubs1 ? "Yes" : "No");
            stringBuilder.AppendFormat("{0}: <strong>{1}</strong><br/>", MemberProvider.GetSwimSub2Description(DateTime.Now), memberOptions.SwimSubs2 ? "Yes" : "No");
            stringBuilder.AppendFormat("{0}: <strong>{1}</strong><br/>", "Open water swimming?", memberOptions.OpenWaterIndemnityAcceptance.Value ? "Yes" : "No");

            return stringBuilder.ToString();
        }


        private string GetWelcomeEmailContent(RegistrationDetails registrationDetails)
        {
            var stringBuilder = new StringBuilder();
            var personalDets = registrationDetails.PersonalDetails;

            stringBuilder.AppendFormat("Hi {0},<br/><br/>", $"{personalDets.FirstName}");
            stringBuilder.Append("I enclose our welcome pack, which should tell you everything you need to know about the club.<br/><br/>");
            stringBuilder.Append("We have a closed Facebook group, which is really a good way of staying in touch with members and hearing what is happening. If you would like to join just send a request via Facebook. The group can be found here: <a href=\"https://www.facebook.com/groups/2363043378/\">Mid Sussex Triathlon Club</a><br/><br/>");
            stringBuilder.Append("We also have 2 WhatsApp groups set up that you can join this first one is for run specific chat and notices: ");
            stringBuilder.Append("<a href=\"https://chat.whatsapp.com/FSHGalpygyz8uyFzPFowoF\">MSTC - Run Group</a><br/>");
            stringBuilder.Append("This other is for all other training chat mainly OW swimming and cycling: ");
            stringBuilder.Append("<a href=\"https://chat.whatsapp.com/GGL5zIOYMxq8E7LymlwMIk\">MSTC - Training Group</a><br/><br/>");
            stringBuilder.Append("We are almost back to normal, our training plan below may be still subject to change is currently:<br/>");
            stringBuilder.Append("Monday – pool swim Cumnor house - uncoached<br/>");
            stringBuilder.Append("Tuesday - pool swim Ardingly College Winter and Spring, then Summer/Autumn only; Ardingly 18.00 OW swim - uncoached<br/>");
            stringBuilder.Append("Wednesday - Spin triangle - coached<br/>");
            stringBuilder.Append("Thursday - 7.30pm run and pool swim 8.30pm Dolphin - coached<br/>");
            stringBuilder.Append("Friday – off<br/>");
            stringBuilder.Append("Saturday – Summer/Autumn only; Ardingly 8.15 OW swim. Uncoached but we hope to set up a OW class for every 3rd week for 30 mins. After swim rides and runs may be set up<br/>");
            stringBuilder.Append("Winter Strength and Conditioning - coached<br/>");
            stringBuilder.Append("Sunday – Group social road rides<br/><br/>");
            stringBuilder.Append("We also have a Google Group that is used for club communication. I will add you to the group.<br/><br/>");
            stringBuilder.Append("If you have any questions just drop me an email.<br/><br/>");
            stringBuilder.Append("I look forward to meeting you at training sometime soon.<br/><br/>");
            stringBuilder.Append("Kind Regards,<br/>");
            stringBuilder.Append("Simon Barton<br/>");
            stringBuilder.Append("Membership Sec<br/>");

            return stringBuilder.ToString();
        }
    }
}
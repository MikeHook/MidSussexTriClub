using System;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Security;
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
    public class RegistrationController : SurfaceController
    {
        protected SessionProvider _sessionProvider;
        protected GoCardlessProvider _goCardlessProvider;
        EmailProvider _emailProvider;
        MemberProvider _memberProvider;
        MembershipCostCalculator _membershipCostCalculator;

        RegistrationDetails _model;

        public RegistrationController( )
        {
            //TODO - Use IoC?
            _sessionProvider = new SessionProvider();
            _goCardlessProvider = new GoCardlessProvider();
            _emailProvider = new EmailProvider();
            _memberProvider = new MemberProvider(Services);
            _membershipCostCalculator = new MembershipCostCalculator();
        
            _model = new RegistrationDetails(_membershipCostCalculator, false);
        }

        [AcceptVerbs(HttpVerbs.Post | HttpVerbs.Get)]
        public ActionResult PersonalDetails()
        {
            return PartialView("Registration/PersonalDetails", _model);
        }

        [AcceptVerbs(HttpVerbs.Post | HttpVerbs.Get)]
        public ActionResult MemberOptions()
        {
            return PartialView("Registration/MemberOptions", _model);
        }

        [AcceptVerbs(HttpVerbs.Post | HttpVerbs.Get)]
        public ActionResult GuestOptions()
        {
            _model = new RegistrationDetails(_membershipCostCalculator, true);
            return PartialView("Registration/GuestOptions", _model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegistrationDetails model)
        {
            if (!ModelState.IsValid)
            {
                return CurrentUmbracoPage();
            }

            var member = Services.MemberService.GetByEmail(model.PersonalDetails.Email);

            if (member != null)
            {
                ModelState.AddModelError("PersonalDetails.Email", "There is already a member registered with the supplied email address.");
                return CurrentUmbracoPage();
            }        

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
            string rootUrl = string.Format("{0}://{1}{2}", Request.Url.Scheme, Request.Url.Host,
                Request.Url.Port == 80 ? string.Empty : ":" + Request.Url.Port);
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
                _memberProvider.UpdateMemberDetails(member, registrationDetails);

                //Login the member
                FormsAuthentication.SetAuthCookie(member.Username, true);          

                string content = GetRegEmailContent(registrationDetails, true);
                _emailProvider.SendEmail(EmailProvider.MembersEmail, EmailProvider.SupportEmail,
                    "New MSTC member registration", content);

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
    }
}
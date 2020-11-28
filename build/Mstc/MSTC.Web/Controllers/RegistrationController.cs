using System;
using System.Configuration;
using System.Web.Mvc;
using System.Web.Security;
using Mstc.Core.Domain;
using Mstc.Core.Dto;
using Mstc.Core.Providers;
using MSTC.Web.Model;
using Newtonsoft.Json;
using Umbraco.Web.Mvc;

namespace MSTC.Web.Controllers
{
    public class RegistrationController : SurfaceController
    {
        protected SessionProvider _sessionProvider;
        protected GoCardlessProvider _goCardlessProvider;
        EmailProvider _emailProvider;
        MemberProvider _memberProvider;


        public RegistrationController( )
        {
            //TODO - Use IoC?
            _sessionProvider = new SessionProvider();
            _goCardlessProvider = new GoCardlessProvider();
            _emailProvider = new EmailProvider();
            _memberProvider = new MemberProvider(Services.MemberService);
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

            var redirectResponse = _goCardlessProvider.CreateRedirectRequest(customerDto, "MSTC Member Registration", _sessionProvider.SessionId,
                successUrl);

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

            int costInPence = MembershipCostCalculator.Calculate(registrationDetails.MemberOptions, DateTime.Now);

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
            var registrationDetails = _sessionProvider.RegistrationDetails;
            if (registrationDetails == null || string.IsNullOrWhiteSpace(_sessionProvider.GoCardlessRedirectFlowId))
            {
                return PartialView("Registration/RegistrationComplete", new RegistrationCompleteModel());
            }

            string mandateId = _goCardlessProvider.CompleteRedirectRequest(_sessionProvider.GoCardlessRedirectFlowId, _sessionProvider.SessionId);
            registrationDetails.PersonalDetails.DirectDebitMandateId = mandateId;

            var regDetails = registrationDetails.PersonalDetails;
            int costInPence = MembershipCostCalculator.Calculate(registrationDetails.MemberOptions, DateTime.Now);
            var paymentDescription = MemberProvider.GetPaymentDescription(registrationDetails.MemberOptions);
            var paymentResponse = _goCardlessProvider.CreatePayment(Logger, regDetails.DirectDebitMandateId, regDetails.Email, costInPence, paymentDescription);

            model.PromptForConfirmation = false;
            model.IsRegistered = paymentResponse == PaymentResponseDto.Success;

            if (model.IsRegistered)
            {
                
                var member = _memberProvider.CreateMember(regDetails, new string[] { MSTCRoles.Member });
                _memberProvider.UpdateMemberDetails(member, registrationDetails);

                //Login the member
                FormsAuthentication.SetAuthCookie(member.Username, true);

                string content = string.Format("<p>A new member has registered with the club</p><p>Member details: {0}</p>",
                    JsonConvert.SerializeObject(registrationDetails, Formatting.Indented));
                var passwordObfuscator = new PasswordObfuscator();
                content = passwordObfuscator.ObfuscateString(content);

                _emailProvider.SendEmail(EmailProvider.MembersEmail, EmailProvider.SupportEmail,
                    "New MSTC member registration", content);

                _sessionProvider.RegistrationDetails = null;
                _sessionProvider.GoCardlessRedirectFlowId = null;
            }
            else
            {
                string content = string.Format("<p>A new member has NOT been registered with the club</p><p>Member details: {0}</p>",
                    JsonConvert.SerializeObject(registrationDetails, Formatting.Indented));
                var passwordObfuscator = new PasswordObfuscator();
                content = passwordObfuscator.ObfuscateString(content);

                _emailProvider.SendEmail(EmailProvider.SupportEmail, EmailProvider.SupportEmail,
                    "MSTC member registration problem", content);
            }

            return CurrentUmbracoPage();
        }
    }
}
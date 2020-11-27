using System;
using System.Web.Mvc;
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

        public RegistrationController( )
        {
            //TODO - Use IoC?
            _sessionProvider = new SessionProvider();
            _goCardlessProvider = new GoCardlessProvider();
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

            _sessionProvider.RegistrationFullDetails = model;

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
            var registrationFullDetails = _sessionProvider.RegistrationFullDetails;
            if (registrationFullDetails == null)
            {
                return PartialView("Registration/RegistrationComplete", new RegistrationCompleteModel());
            }

            int costInPence = MembershipCostCalculator.Calculate(registrationFullDetails.MemberOptions, DateTime.Now);

            var model = new RegistrationCompleteModel()
            {
                PromptForConfirmation = registrationFullDetails != null,
                PaymentDescription = MemberProvider.GetPaymentDescription(registrationFullDetails.MemberOptions),
                Cost = (costInPence / 100m).ToString("N2")
            };

            return PartialView("Registration/RegistrationComplete", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmPayment(RegistrationCompleteModel model)
        {
            return CurrentUmbracoPage();
        }
    }
}
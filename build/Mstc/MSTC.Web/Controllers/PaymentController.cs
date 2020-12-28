using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Mstc.Core.Domain;
using Mstc.Core.Dto;
using Mstc.Core.Providers;
using MSTC.Web.Model;
using Umbraco.Core;
using Umbraco.Core.Services;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;

namespace MSTC.Web.Controllers
{
    [MemberAuthorize(AllowType = "Member")]
    public class PaymentController : Controller
    {
        protected IMemberService _memberService;
        protected SessionProvider _sessionProvider;
        protected GoCardlessProvider _goCardlessProvider;
        protected EmailProvider _emailProvider;
        protected MemberProvider _memberProvider;

        public PaymentController()
        {
            _sessionProvider = new SessionProvider();
            _goCardlessProvider = new GoCardlessProvider();
            _emailProvider = new EmailProvider();
            _memberService = ApplicationContext.Current.Services.MemberService;
            _memberProvider = new MemberProvider(_memberService);
        }

        /*
        public override ActionResult Index(RenderModel model)
        {
            _sessionProvider = new SessionProvider();
            _goCardlessProvider = new GoCardlessProvider();
            _emailProvider = new EmailProvider();
            _memberProvider = new MemberProvider(Services);

            var member = _memberProvider.GetLoggedInMember();
            string state = Request.QueryString["state"];
            bool canProcessPaymentCompletion = _sessionProvider.CanProcessPaymentCompletion;
            if (member == null || !canProcessPaymentCompletion || string.IsNullOrEmpty(state))
            {
                //TODO - Show Error
                return base.Index(model);
            }


            // Do some stuff here, then return the base method
            return base.Index(model);
        }*/

        [HttpGet]
        public ActionResult Index()
        {
            var model = new PaymentModel();

            var member = _memberProvider.GetLoggedInMember();
            string state = Request.QueryString["state"];
            bool canProcessPaymentCompletion = _sessionProvider.CanProcessPaymentCompletion;
            if (member == null || !canProcessPaymentCompletion || string.IsNullOrEmpty(state))
            {
                model.HasPaymentDetails = false;
                return View(model);
            }

            var paymentState = (PaymentStates)Enum.Parse(typeof(PaymentStates), state);
            string mandateId = member.GetValue<string>(MemberProperty.directDebitMandateId);
            bool requiresFlowRedirect = string.IsNullOrWhiteSpace(_sessionProvider.GoCardlessRedirectFlowId) == false;
            if (requiresFlowRedirect)
            {
                mandateId = _goCardlessProvider.CompleteRedirectRequest(_sessionProvider.GoCardlessRedirectFlowId, _sessionProvider.SessionId);
                member.SetValue(MemberProperty.directDebitMandateId, mandateId);
                _memberService.Save(member);
                _sessionProvider.GoCardlessRedirectFlowId = null;
            }
            else if (string.IsNullOrEmpty(mandateId))
            {
                return RedirectToMandatePage(paymentState);
            }

            model.HasPaymentDetails = true;
            model.PaymentDescription = paymentState.GetAttributeOfType<DescriptionAttribute>().Description;

            var membershipType = member.GetValue<MembershipTypeEnum>(MemberProperty.membershipType);
            int costInPence = (paymentState == PaymentStates.MemberRenewal || paymentState == PaymentStates.MemberUpgrade)
                ? MembershipCostCalculator.Calculate(_sessionProvider.RenewalOptions, DateTime.Now)
                : MembershipCostCalculator.PaymentStateCost(paymentState, membershipType);

            model.Cost = (costInPence / 100m);

            return View(model);
        }

        [HttpGet]
        public ActionResult Mandate()
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
           
            //var mandateSuccessUrl = $"/Payment?state={state}";
            string rootUrl = string.Format("{0}://{1}{2}", Request.Url.Scheme, Request.Url.Host, Request.Url.Port == 80 ? string.Empty : ":" + Request.Url.Port);
            string mandateSuccessUrl = string.Format("{0}/payment?state={1}", rootUrl, state);

            var redirectResponse = _goCardlessProvider.CreateRedirectRequest(customerDto, "Mid Sussex Tri Club DD Mandate Setup", _sessionProvider.SessionId,
                mandateSuccessUrl);

            _sessionProvider.GoCardlessRedirectFlowId = redirectResponse.Id;
            return Redirect(redirectResponse.RedirectUrl);
        }


        private ActionResult RedirectToMandatePage(PaymentStates paymentState)
        {
            //var memberPaymentPage = CurrentPage as Umbraco.Web.PublishedContentModels.MemberPayment;
            //var mandatePageUrl = $"{memberPaymentPage.MandatePage.Url}?state={paymentState}";
            var mandatePageUrl = $"/payment/mandate?state={paymentState}";
            return Redirect(mandatePageUrl);
        }
    }
}
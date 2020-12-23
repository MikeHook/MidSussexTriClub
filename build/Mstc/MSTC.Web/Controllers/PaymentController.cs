using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        [HttpGet]
        public ActionResult Payment()
        {
            var model = new PaymentModel();

            var member = _memberProvider.GetLoggedInMember();
            string state = Request.QueryString["state"];
            bool canProcessPaymentCompletion = _sessionProvider.CanProcessPaymentCompletion;
            if (member == null || !canProcessPaymentCompletion || string.IsNullOrEmpty(state))
            {
                model.HasPaymentDetails = false;
                return PartialView("Payment/Payment", model);
            }

            var paymentState = (PaymentStates)Enum.Parse(typeof(PaymentStates), state);
            string mandateId = member.GetValue<string>(MemberProperty.directDebitMandateId);
            bool requiresFlowRedirect = string.IsNullOrWhiteSpace(_sessionProvider.GoCardlessRedirectFlowId) == false;
            if (requiresFlowRedirect)
            {
                mandateId = _goCardlessProvider.CompleteRedirectRequest(_sessionProvider.GoCardlessRedirectFlowId, _sessionProvider.SessionId);
                member.SetValue(MemberProperty.directDebitMandateId, mandateId);
                Services.MemberService.Save(member);
                _sessionProvider.GoCardlessRedirectFlowId = null;
            }
            else if (string.IsNullOrEmpty(mandateId))
            {
                return RedirectToMandatePage(paymentState);                    
            }
                        
            model.PaymentDescription = paymentState.GetAttributeOfType<DescriptionAttribute>().Description;

            var membershipType = member.GetValue<MembershipTypeEnum>(MemberProperty.membershipType);
            int costInPence = (paymentState == PaymentStates.MemberRenewal || paymentState == PaymentStates.MemberUpgrade)
                ? MembershipCostCalculator.Calculate(_sessionProvider.RenewalOptions, DateTime.Now)
                : MembershipCostCalculator.PaymentStateCost(paymentState, membershipType);

            model.Cost = (costInPence / 100m);

            return PartialView("Payment/Payment", model);
        }

        [HttpGet]
        public ActionResult RenewMandate()
        {
            var member = _memberProvider.GetLoggedInMember();
            string state = Request.QueryString["state"];
            bool canProcessPaymentCompletion = _sessionProvider.CanProcessPaymentCompletion;
            if (member == null || !canProcessPaymentCompletion || string.IsNullOrEmpty(state))
            {   
                return CurrentUmbracoPage();
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

            var renewMandatePage = CurrentPage as Umbraco.Web.PublishedContentModels.RenewDdmandate;
            var mandateSuccessUrl = $"{renewMandatePage.PaymentPage.Url}?state={state}";                

            var redirectResponse = _goCardlessProvider.CreateRedirectRequest(customerDto, "Mid Sussex Tri Club DD Mandate Setup", _sessionProvider.SessionId,
                mandateSuccessUrl);

            _sessionProvider.GoCardlessRedirectFlowId = redirectResponse.Id;
            return Redirect(redirectResponse.RedirectUrl);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Payment(PaymentModel model)
        {
            var member = _memberProvider.GetLoggedInMember();
            string state = Request.QueryString["state"];
            bool canProcessPaymentCompletion = _sessionProvider.CanProcessPaymentCompletion;
            if (member == null || !canProcessPaymentCompletion || string.IsNullOrEmpty(state))
            {
                TempData["Model"] = model;
                return CurrentUmbracoPage();
            }

            var paymentState = (PaymentStates)Enum.Parse(typeof(PaymentStates), state);
            var paymentResponse = CreatePayment(member, paymentState);

            if (paymentResponse == PaymentResponseDto.Success)
            {
                ProcessPaymentState(model, member, paymentState);
                model.PaymentConfirmed = true;
            }
            else if (paymentResponse == PaymentResponseDto.MandateError)
            {
                return RedirectToMandatePage(paymentState);
            }
            else
            {
                model.ShowPaymentFailed = true;
            }

            _sessionProvider.CanProcessPaymentCompletion = false;

            TempData["Model"] = model;
            return CurrentUmbracoPage();
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

        private ActionResult RedirectToMandatePage(PaymentStates paymentState)
        {
            var memberPaymentPage = CurrentPage as Umbraco.Web.PublishedContentModels.MemberPayment;
            var mandatePageUrl = $"{memberPaymentPage.MandatePage.Url}?state={paymentState}";
            return Redirect(mandatePageUrl);
        }

        private PaymentResponseDto CreatePayment(IMember member, PaymentStates paymentState)
        {
            var membershipType = member.GetValue<MembershipTypeEnum>(MemberProperty.membershipType);
            string mandateId = member.GetValue<string>(MemberProperty.directDebitMandateId);
            string email = member.Email;
    
            int costInPence = (paymentState == PaymentStates.MemberRenewal || paymentState == PaymentStates.MemberUpgrade)
                ? MembershipCostCalculator.Calculate(_sessionProvider.RenewalOptions, DateTime.Now)
                : MembershipCostCalculator.PaymentStateCost(paymentState, membershipType);
            string description = paymentState.GetAttributeOfType<DescriptionAttribute>().Description;

            return _goCardlessProvider.CreatePayment(Logger, mandateId, email, costInPence, description);
        }

        private void ProcessPaymentState(PaymentModel model, IMember member, PaymentStates paymentState)
        {
            switch (paymentState)
            {               
                case PaymentStates.SS05991:
                    {
                        member.SetValue(MemberProperty.swimSubs1, string.Format("Swim Subs Apr - Sept {0}", DateTime.Now.Year));
                        model.ShowSwimSubsConfirmation = true;
                        break;
                    }
                case PaymentStates.SS05992:              
                    {
                        var janToMarch = new List<int>() { 1, 2, 3 };
                        int year1 = janToMarch.Any(m => m == DateTime.Now.Month) ? DateTime.Now.Year - 1 : DateTime.Now.Year;
                        member.SetValue(MemberProperty.swimSubs2, string.Format("Swim Subs Oct {0} - Mar {1}", year1, year1 + 1));               
                        model.ShowSwimSubsConfirmation = true;        
                        break;
                    }
                case PaymentStates.MemberRenewal:
                case PaymentStates.MemberUpgrade:
                    {      
                        _memberProvider.UpdateMemberOptions(member, _sessionProvider.RenewalOptions, isUpgrade: paymentState == PaymentStates.MemberUpgrade);
                        model.ShowRenewed = true;
                        break;
                    }
            }

            Services.MemberService.Save(member);
        }

    }
}
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
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;
using Umbraco.Core.Logging;

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
        protected ILogger _logger;

        public PaymentController()
        {
            _sessionProvider = new SessionProvider();
            _goCardlessProvider = new GoCardlessProvider();
            _emailProvider = new EmailProvider();
            _memberService = ApplicationContext.Current.Services.MemberService;
            _memberProvider = new MemberProvider(_memberService);
            _logger = ApplicationContext.Current.ProfilingLogger.Logger;
        }       

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

            MapPaymentStateToModel(model, member, paymentState);

            return View(model);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(PaymentModel model)
        {
            var member = _memberProvider.GetLoggedInMember();
            string state = Request.QueryString["state"];
            bool canProcessPaymentCompletion = _sessionProvider.CanProcessPaymentCompletion;
            if (member == null || !canProcessPaymentCompletion || string.IsNullOrEmpty(state))
            {
                model.HasPaymentDetails = false;
                return View(model);            
            }

            var paymentState = (PaymentStates)Enum.Parse(typeof(PaymentStates), state);
            MapPaymentStateToModel(model, member, paymentState);
            
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

            return View(model);
        }


        private ActionResult RedirectToMandatePage(PaymentStates paymentState)
        {
            var mandatePageUrl = $"/mandate?state={paymentState}";
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

            return _goCardlessProvider.CreatePayment(_logger, mandateId, email, costInPence, description);
        }

        private void MapPaymentStateToModel(PaymentModel model, IMember member, PaymentStates paymentState)
        {
            model.HasPaymentDetails = true;
            model.PaymentDescription = paymentState.GetAttributeOfType<DescriptionAttribute>().Description;

            var membershipType = member.GetValue<MembershipTypeEnum>(MemberProperty.membershipType);
            int costInPence = (paymentState == PaymentStates.MemberRenewal || paymentState == PaymentStates.MemberUpgrade)
                ? MembershipCostCalculator.Calculate(_sessionProvider.RenewalOptions, DateTime.Now)
                : MembershipCostCalculator.PaymentStateCost(paymentState, membershipType);

            model.Cost = (costInPence / 100m);
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

            _memberService.Save(member);
        }
    }
}
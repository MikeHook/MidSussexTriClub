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
using Umbraco.Web;

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
        protected MembershipCostCalculator _membershipCostCalculator;

        public PaymentController()
        {
            _sessionProvider = new SessionProvider();
            _goCardlessProvider = new GoCardlessProvider();
            _emailProvider = new EmailProvider();
            _memberService = ApplicationContext.Current.Services.MemberService;
            _memberProvider = new MemberProvider(_memberService);
            _logger = ApplicationContext.Current.ProfilingLogger.Logger;
            _membershipCostCalculator = new MembershipCostCalculator();
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

            if (state == "owsSignup")
            {
                ProcessPaymentState(model, member, paymentState);
                model.PaymentConfirmed = true;
            }

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
            string mandateId = member.GetValue<string>(MemberProperty.directDebitMandateId);
            string email = member.Email;

            int costInPence = GetCostInPence(member, paymentState);   
            string description = paymentState.GetAttributeOfType<DescriptionAttribute>().Description;

            return _goCardlessProvider.CreatePayment(_logger, mandateId, email, costInPence, description);
        }

        private void MapPaymentStateToModel(PaymentModel model, IMember member, PaymentStates paymentState)
        {
            model.HasPaymentDetails = true;
            model.PaymentDescription = paymentState.GetAttributeOfType<DescriptionAttribute>().Description;           

            int costInPence = GetCostInPence(member, paymentState);
            model.Cost = (costInPence / 100m);
        }

        private int GetCostInPence(IMember member, PaymentStates paymentState)
        {
            int costInPence = 0;
            var membershipType = member.GetValue<MembershipTypeEnum>(MemberProperty.membershipType);
            switch (paymentState)
            {
                case PaymentStates.MemberRenewal:
                case PaymentStates.MemberUpgrade:
                    costInPence = _membershipCostCalculator.Calculate(_sessionProvider.RenewalOptions, DateTime.Now);
                    break;
                case PaymentStates.TrainingCredits:
                    costInPence = _sessionProvider.TrainingCreditsInPence;
                    break;
                case PaymentStates.OwsSignup:
                    costInPence = _membershipCostCalculator.OwsSignupCostPence;
                    break;
                default:
                    costInPence = _membershipCostCalculator.PaymentStateCost(paymentState, membershipType);
                    break;
            }
            return costInPence;
        }

        private void ProcessPaymentState(PaymentModel model, IMember member, PaymentStates paymentState)
        {
            switch (paymentState)
            {
                case PaymentStates.SS05991:
                    {
                        member.SetValue(MemberProperty.swimSubs1, MemberProvider.GetSwimSub1Description(DateTime.Now));
                        model.ShowSwimSubsConfirmation = true;
                        break;
                    }
                case PaymentStates.SS05992:
                    {
                        member.SetValue(MemberProperty.swimSubs2, MemberProvider.GetSwimSub2Description(DateTime.Now));
                        model.ShowSwimSubsConfirmation = true;
                        break;
                    }
                case PaymentStates.SS05993:
                    {
                        member.SetValue(MemberProperty.swimSubs3, MemberProvider.GetSwimSub3Description(DateTime.Now));
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
                case PaymentStates.TrainingCredits:
                    {
                        var credits = member.GetValue<int>(MemberProperty.TrainingCredits);
                        credits = credits + (_sessionProvider.TrainingCreditsInPence / 100);
                        member.SetValue(MemberProperty.TrainingCredits, credits);
                        model.ShowCreditsConfirmation = true;
                        break;
                    }
                case PaymentStates.OwsSignup:
                    {
                        _memberProvider.AcceptOpenWaterWaiver(member);
                        model.ShowOwsSignupConfirmation = true;
                        break;
                    }
            }

            _memberService.Save(member);
        }





    }
}
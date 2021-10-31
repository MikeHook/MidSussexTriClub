using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.UI.WebControls;
using Mstc.Core.Domain;
using Mstc.Core.Providers;
using Mstc.Core.ContentModels;
using MSTC.Web.Model.Partials;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Web.Mvc;

namespace MSTC.Web.Controllers
{
    public class MemberEditController : SurfaceController
    {
        const int _profileImageFolderId = 2273;
        const int _serviceImageFolderId = 2271;

        protected SessionProvider _sessionProvider;
        protected GoCardlessProvider _goCardlessProvider;
        EmailProvider _emailProvider;
        MemberProvider _memberProvider;
        MembershipCostCalculator _membershipCostCalculator;

        public MemberEditController( )
        {
            //TODO - Use IoC?
            _sessionProvider = new SessionProvider();
            _goCardlessProvider = new GoCardlessProvider();
            _emailProvider = new EmailProvider();
            _memberProvider = new MemberProvider(Services);
            _membershipCostCalculator = new MembershipCostCalculator();
        }

        [AcceptVerbs(HttpVerbs.Post | HttpVerbs.Get)]
        public ActionResult MemberImage()
        {
            var member = _memberProvider.GetLoggedInMember();
            var model = new MemberImageModel();
            if (member != null)
            {
                model.ProfileImageId = member.GetValue<string>(MemberProperty.ProfileImage);
            }                

            return PartialView("Member/EditMemberImage", model);
        }

        [AcceptVerbs(HttpVerbs.Post | HttpVerbs.Get)]
        public ActionResult MemberDetails()
        {
            var member = _memberProvider.GetLoggedInMember();
            var model = member != null ? MapMemberDetails(member) : new MemberDetailsModel();

            return PartialView("Member/EditMemberDetails", model);
        }

        [AcceptVerbs(HttpVerbs.Post | HttpVerbs.Get)]
        public ActionResult MemberOptions()
        {
            var memberEditPage = CurrentPage as MemberEdit;            

            IMember member = _memberProvider.GetLoggedInMember();
            var model = new MemberOptionsModel();
            if (member != null)
            {
                var memberType = member.GetValue<MembershipTypeEnum>(MemberProperty.membershipType);
                var isGuest = memberType == MembershipTypeEnum.Guest;
                var membershipExpiry = member.GetValue<DateTime>(MemberProperty.MembershipExpiry);

                model.MembershipExpired = membershipExpiry < DateTime.Now;
                model.MembershipExpiry = membershipExpiry;
                model.MembershipType = memberType.ToString();
                model.IsBankLinked = !string.IsNullOrEmpty(member.GetValue<string>(MemberProperty.directDebitMandateId));

                string swimSubs1 = member.GetValue<string>(MemberProperty.swimSubs1);
                model.ShowBuySwimSubs1 = !model.EnableMemberRenewal && !isGuest && _membershipCostCalculator.SwimSubs1Enabled
                    && string.IsNullOrEmpty(swimSubs1) && DateTime.Now.Month < 10 && DateTime.Now.Month > 2;
                string swimSubs2 = member.GetValue<string>(MemberProperty.swimSubs2);
                model.ShowBuySwimSubs2 = !model.EnableMemberRenewal && !isGuest && _membershipCostCalculator.SwimSubs2Enabled && string.IsNullOrEmpty(swimSubs2);
                model.OptionalExtras = GetOptionalExtras(member);
                decimal swimSubsCost = _membershipCostCalculator.SwimsSubsCostInPence(memberType) / 100;
                model.BuySwimSubs1Text = string.Format("Buy {0} @ £{1:N2}", MemberProvider.GetSwimSub1Description(DateTime.Now), swimSubsCost);
                model.BuySwimSubs2Text = string.Format("Buy {0} @ £{1:N2}", MemberProvider.GetSwimSub2Description(DateTime.Now), swimSubsCost);

                model.EnableMemberRenewal = memberEditPage.RenewalsEnabled && DateTime.Now.Month > 2 && !isGuest && membershipExpiry.Year <= DateTime.Now.Year;

                model.ShowIceLink = Roles.IsUserInRole(MSTCRoles.Coach) || Roles.IsUserInRole(MSTCRoles.MemberAdmin);
                model.ShowMemberAdminLink = Roles.IsUserInRole(MSTCRoles.MemberAdmin);
                model.ShowEventAdminLink = Roles.IsUserInRole(MSTCRoles.EventAdminViewer);

                model.EnableOpenWater = memberEditPage.OWsenabled && !isGuest && !model.MembershipExpired;
                model.OWSNumber = member.GetValue<string>(MemberProperty.SwimAuthNumber);
                model.OwsIndemnityAccepted = member.GetValue<bool>(MemberProperty.OpenWaterIndemnityAcceptance);
                model.OwsIndemnityDocLink = memberEditPage.IndemnityWaiverDoc?.Url;
                model.OwsSignupFee = (decimal) _membershipCostCalculator.OwsSignupCostPence / 100;

                model.RenewalPageUrl = memberEditPage.RenewalPage?.Url;
                model.MemberAdminPageUrl = memberEditPage.MemberAdminPage?.Url;
                model.EventBookingPageUrl = memberEditPage.EventBookingPage?.Url;
                model.EventAdminPageUrl = memberEditPage.EventAdminPage?.Url;
                model.UnlinkBankPageUrl = memberEditPage.UnlinkBankPage?.Url;
                
            }         

            return PartialView("Member/EditMemberOptions", model);
        }

        [AcceptVerbs(HttpVerbs.Post | HttpVerbs.Get)]
        public ActionResult TrainingCredits()
        {
            var memberEditPage = CurrentPage as MemberEdit;

            var member = _memberProvider.GetLoggedInMember();
            var model = new TrainingCreditsModel();
            if (member!= null)
            {
                model.EventBookingPageUrl = memberEditPage.EventBookingPage?.Url;
                model.CurrentTrainingCredits = member.GetValue<int>(MemberProperty.TrainingCredits);
                var memberType = member.GetValue<MembershipTypeEnum>(MemberProperty.membershipType);
                var isGuest = memberType == MembershipTypeEnum.Guest;
                model.IsTrainingCreditsEnabled = !isGuest || memberEditPage.GuestTrainingCreditsEnabled;
            }

            return PartialView("Member/TrainingCredits", model);
        }

        [HttpPost]
        public ActionResult PaymentRedirect(PaymentStates state)
        {
            var member = _memberProvider.GetLoggedInMember();
            if (member == null)
            {
                return CurrentUmbracoPage();
            }

            _sessionProvider.CanProcessPaymentCompletion = true;
            var redirectUrl = $"/Payment?state={state}";
            return Redirect(redirectUrl);
        }

        [HttpPost]
        public ActionResult CreditsPaymentRedirect(TrainingCreditsModel model)
        {
            var member = _memberProvider.GetLoggedInMember();
            if (!ModelState.IsValid || member == null)
            {
                return CurrentUmbracoPage();
            }        

            _sessionProvider.TrainingCreditsInPence = (model.TrainingCreditsToBuy * 100);
            _sessionProvider.CanProcessPaymentCompletion = true;
            var redirectUrl = $"/Payment?state={PaymentStates.TrainingCredits}";
            return Redirect(redirectUrl);
        }

        [HttpPost]
        public ActionResult UpdateMemberImage(MemberImageModel model)
        {
            var member = _memberProvider.GetLoggedInMember();
            if (!ModelState.IsValid || member == null)
            {
                return CurrentUmbracoPage();
            }

            if (model.ProfileImageFile != null)
            {
                var imageUdi = SaveImage(model.ProfileImageFile, _profileImageFolderId);
                member.SetValue(MemberProperty.ProfileImage, imageUdi);
            } else if (model.RemoveImage)
            {
                member.SetValue(MemberProperty.ProfileImage, "");
            }

            Services.MemberService.Save(member);

            return RedirectToCurrentUmbracoUrl();
        }        

        [HttpPost]
        public ActionResult UpdateMemberDetails(MemberDetailsModel model)
        {
            var member = _memberProvider.GetLoggedInMember();
            if (!ModelState.IsValid || member == null)
            {
                return CurrentUmbracoPage();
            }

            if (!string.Equals(member.Email.Trim(), model.Email.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                FormsAuthentication.SignOut();
                FormsAuthentication.SetAuthCookie(model.Email, true);

                string emailContent = GetEmailChangeContent(model, member.Email);                

                _emailProvider.SendEmail(EmailProvider.MembersEmail, EmailProvider.SupportEmail,
                    "MSTC member email updated", emailContent);
            }

            SetMemberDetails(member, model);

            return RedirectToCurrentUmbracoUrl(); 
        }

        [HttpPost]
        public ActionResult SyncCredits()
        {
            var currentMember = _memberProvider.GetLoggedInMember();
            if (!ModelState.IsValid || currentMember == null)
            {
                return CurrentUmbracoPage();
            }

            var members = _memberProvider.GetAll().ToList();
            foreach(var member in members)
            {
                int trainingCredits = member.GetValue<int>(MemberProperty.CreditsRemainingLastYear) + member.GetValue<int>(MemberProperty.CreditsBought) - member.GetValue<int>(MemberProperty.CreditsUsed);
                member.SetValue(MemberProperty.TrainingCredits, trainingCredits);
                Services.MemberService.Save(member);
            }

            return RedirectToCurrentUmbracoUrl();
        }

        [HttpPost]
        public ActionResult RepublishBlog()
        {
            var currentMember = _memberProvider.GetLoggedInMember();
            if (!ModelState.IsValid || currentMember == null)
            {
                return CurrentUmbracoPage();
            }

            var blogPostContentType = Services.ContentTypeService.GetContentType("blogpost");
            var blogPosts = Services.ContentService.GetContentOfContentType(blogPostContentType.Id);

            foreach (var blogPost in blogPosts)
            {
                Services.ContentService.Save(blogPost);
                Services.ContentService.Publish(blogPost);
            }

            return RedirectToCurrentUmbracoUrl();
        }



        public MemberDetailsModel MapMemberDetails(IMember member)
        {
            return new MemberDetailsModel()
            {
                Name = member.Name,
                Email = member.Email,
                Gender = member.GetValue<GenderEnum>(MemberProperty.Gender),
                DateOfBirth = member.GetValue<DateTime>(MemberProperty.DateOfBirth),
                Address1 = member.GetValue<string>(MemberProperty.Address1),
                City = member.GetValue<string>(MemberProperty.City),
                Postcode = member.GetValue<string>(MemberProperty.Postcode),
                PhoneNumber = member.GetValue<string>(MemberProperty.Phone),
                BTFNumber = member.GetValue<string>(MemberProperty.BTFNumber),
                MedicalConditions = member.GetValue<string>(MemberProperty.MedicalConditions),
                EmergencyContactName = member.GetValue<string>(MemberProperty.EmergencyContactName),
                EmergencyContactPhone = member.GetValue<string>(MemberProperty.EmergencyContactNumber),
                ShowService = member.GetValue<bool>(MemberProperty.ShowService),
                ServiceWebsite = member.GetValue<string>(MemberProperty.ServiceLinkAddress),
                ServiceName = member.GetValue<string>(MemberProperty.ServiceLinkText),
                ServiceImageId = member.GetValue<string>(MemberProperty.ServiceImage),
                ServiceDescription = member.GetValue<string>(MemberProperty.ServiceDescription)
            };
        }

        public void SetMemberDetails(IMember member, MemberDetailsModel model)
        {
            member.Name = model.Name;
            member.Email = model.Email;
            member.Username = model.Email;
            member.SetValue(MemberProperty.Gender, (int)model.Gender);
            member.SetValue(MemberProperty.DateOfBirth, model.DateOfBirth);
            member.SetValue(MemberProperty.Address1, model.Address1);
            member.SetValue(MemberProperty.City, model.City);
            member.SetValue(MemberProperty.Postcode, model.Postcode);
            member.SetValue(MemberProperty.Phone, model.PhoneNumber);
            member.SetValue(MemberProperty.BTFNumber, model.BTFNumber);

            member.SetValue(MemberProperty.medicalConditions, model.MedicalConditions);
            member.SetValue(MemberProperty.emergencyContactName, model.EmergencyContactName);
            member.SetValue(MemberProperty.emergencyContactNumber, model.EmergencyContactPhone);

            member.SetValue(MemberProperty.ShowService, model.ShowService);
            member.SetValue(MemberProperty.ServiceLinkAddress, model.ServiceWebsite);
            member.SetValue(MemberProperty.ServiceLinkText, model.ServiceName);
            member.SetValue(MemberProperty.ServiceDescription, model.ServiceDescription);

            if (model.ServiceImageFile != null)
            {
                var imageUdi = SaveImage(model.ServiceImageFile, _serviceImageFolderId);               
                member.SetValue(MemberProperty.ServiceImage, imageUdi);
            }
            Services.MemberService.Save(member);
        }       

        private string SaveImage(HttpPostedFileBase postedFile, int parentFolderId)
        {
            IMediaService mediaService = Services.MediaService;
            IMedia media = mediaService.CreateMedia(postedFile.FileName, parentFolderId, "Image");
            media.SetValue("umbracoFile", postedFile.FileName, postedFile.InputStream);
            mediaService.Save(media);
            return media.GetUdi().ToString();
        }

        private string GetEmailChangeContent(MemberDetailsModel memberDetails, string oldEmail)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("<p>A member has updated their email address</p><p>Member details:</p>");

            stringBuilder.AppendFormat("{0}: <strong>{1}</strong><br/>", "Name", memberDetails.Name);
            stringBuilder.AppendFormat("{0}: <strong>{1}</strong><br/>", "Old Email", oldEmail);
            stringBuilder.AppendFormat("{0}: <strong>{1}</strong><br/>", "New Email", memberDetails.Email);           

            return stringBuilder.ToString();
        }

        private List<string> GetOptionalExtras(IMember member)
        {
            var options = new List<string>();

            string swimSubs1 = member.GetValue<string>(MemberProperty.swimSubs1);         
            if (!string.IsNullOrEmpty(swimSubs1))
            {
                options.Add(swimSubs1);
            }
            string swimSubs2 = member.GetValue<string>(MemberProperty.swimSubs2);   
            if (!string.IsNullOrEmpty(swimSubs2))
            {
                options.Add(swimSubs2);
            }
            if (member.GetValue<bool>(MemberProperty.EnglandAthleticsMembership))
            {
                options.Add("England Athletics Member");
            }
            if (options.Count == 0)
            {
                options.Add("None");
            }
            return options;
        }
    }
}
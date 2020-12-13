using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.UI.WebControls;
using Mstc.Core.Domain;
using Mstc.Core.Dto;
using Mstc.Core.Providers;
using MSTC.Web.Model;
using MSTC.Web.Model.Partials;
using Newtonsoft.Json;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Web.Mvc;
using Umbraco.Web.PublishedContentModels;

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


        public MemberEditController( )
        {
            //TODO - Use IoC?
            _sessionProvider = new SessionProvider();
            _goCardlessProvider = new GoCardlessProvider();
            _emailProvider = new EmailProvider();
            _memberProvider = new MemberProvider(Services);
        }

        [HttpGet]
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

        [HttpGet]
        public ActionResult MemberDetails()
        {
            var member = _memberProvider.GetLoggedInMember();
            var model = member != null ? MapMemberDetails(member) : new MemberDetailsModel();

            return PartialView("Member/EditMemberDetails", model);
        }

        [HttpGet]
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
               
                string swimSubs1 = member.GetValue<string>(MemberProperty.swimSubs1);
                model.ShowBuySwimSubs1 = !model.EnableMemberRenewal && !isGuest && string.IsNullOrEmpty(swimSubs1) && DateTime.Now.Month < 10 && DateTime.Now.Month > 2;
                string swimSubs2 = member.GetValue<string>(MemberProperty.swimSubs2);
                model.ShowBuySwimSubs2 = !model.EnableMemberRenewal && !isGuest && string.IsNullOrEmpty(swimSubs2);
                model.OptionalExtras = GetOptionalExtras(member);
                decimal swimSubsCost = MembershipCostCalculator.SwimsSubsCostInPence(memberType) / 100;
                model.BuySwimSubs1Text = string.Format("Buy {0} @ £{1:N2}", MemberProvider.GetSwimSub1Description(DateTime.Now, false), swimSubsCost);
                model.BuySwimSubs2Text = string.Format("Buy {0} @ £{1:N2}", MemberProvider.GetSwimSub2Description(DateTime.Now, false), swimSubsCost);

                model.EnableMemberRenewal = memberEditPage.RenewalsEnabled && DateTime.Now.Month > 2 && !isGuest && model.MembershipExpired;
                model.ShowIceLink = Roles.IsUserInRole(MSTCRoles.Coach) || Roles.IsUserInRole(MSTCRoles.MemberAdmin);
                model.ShowMemberAdminLink = Roles.IsUserInRole(MSTCRoles.MemberAdmin);

                model.EnableOpenWater = memberEditPage.OWsenabled && !isGuest && model.MembershipExpired;
                model.OWSNumber = member.GetValue<string>(MemberProperty.SwimAuthNumber);
                model.OwsIndemnityAccepted = member.GetValue<bool>(MemberProperty.OpenWaterIndemnityAcceptance);
                model.OwsIndemnityDocLink = memberEditPage.IndemnityWaiverDoc?.Url;

                model.RenewalPageUrl = memberEditPage.RenewalPage?.Url;
                model.MemberAdminPageUrl = memberEditPage.MemberAdminPage?.Url;
                model.ICEPageUrl = memberEditPage.ICepage?.Url;
                model.EventBookingPageUrl = memberEditPage.EventBookingPage?.Url;
            }         

            return PartialView("Member/EditMemberOptions", model);
        }

        [HttpPost]
        public ActionResult BuySwimSubs1()
        {
            var member = _memberProvider.GetLoggedInMember();
            if (member == null)
            {
                return CurrentUmbracoPage();
            }
            
            //TODO Implement this
            return RedirectToCurrentUmbracoUrl();
        }

        [HttpPost]
        public ActionResult BuySwimSubs2()
        {
            var member = _memberProvider.GetLoggedInMember();
            if (member == null)
            {
                return CurrentUmbracoPage();
            }

            //TODO Implement this
            return RedirectToCurrentUmbracoUrl();
        }

        [HttpPost]
        public ActionResult UnlinkBank()
        {
            var member = _memberProvider.GetLoggedInMember();
            if (member == null)
            {
                return CurrentUmbracoPage();
            }

            //TODO Implement this
            return RedirectToCurrentUmbracoUrl();
        }

        [HttpPost]
        public ActionResult AcceptOWSIndemnity()
        {
            var member = _memberProvider.GetLoggedInMember();
            if (member == null)
            {
                return CurrentUmbracoPage();
            }

            //TODO Implement this
            return RedirectToCurrentUmbracoUrl();
        }

        [HttpPost]
        public ActionResult MemberImage(MemberImageModel model)
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
        public ActionResult MemberDetails(MemberDetailsModel model)
        {
            var member = _memberProvider.GetLoggedInMember();
            if (!ModelState.IsValid || member == null)
            {
                return CurrentUmbracoPage();
            }

            if (member.Username != model.Email)
            {
                FormsAuthentication.SignOut();                
                FormsAuthentication.SetAuthCookie(model.Email, true);
            }

            string emailContent = GetEmailChangeContent(model, member.Email);

            SetMemberDetails(member, model);
           
            _emailProvider.SendEmail(EmailProvider.MembersEmail, EmailProvider.SupportEmail,
                "MSTC member email updated", emailContent);

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
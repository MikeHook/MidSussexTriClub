using System;
using System.Collections.Generic;
using System.Linq;
using Mstc.Core.DataAccess;
using Mstc.Core.Domain;
using Mstc.Core.Dto;
using System.Web.Security;
using Umbraco.Core.Services;
using Umbraco.Core.Models;
using Mstc.Core.ContentModels;
using Umbraco.Web;
using System.Globalization;
using System.Drawing;

namespace Mstc.Core.Providers
{
    public class MemberProvider
    {
        private readonly IMemberService _memberService;
        private static MemberRegistration _memberRegistration;

        public MemberProvider(ServiceContext serviceContext)
        {
            _memberService = serviceContext.MemberService;
            init();
        }

        public MemberProvider(IMemberService memberService)
        {
            _memberService = memberService;
            init();
        }

        public void init()
        {
            UmbracoHelper umbracoHelper = new UmbracoHelper(UmbracoContext.Current);
            var memberRegistrationContent = umbracoHelper?.TypedContentAtRoot().DescendantsOrSelf(MemberRegistration.ModelTypeAlias).FirstOrDefault();
            if (memberRegistrationContent != null)
            {
                _memberRegistration = new MemberRegistration(memberRegistrationContent);
            }
        }

        public static string SwimSubs1MonthStart => _memberRegistration != null ? new DateTime(2000, _memberRegistration.SwimSubs1MonthStart, 1).ToString("MMM", CultureInfo.InvariantCulture) : null;
        public static string SwimSubs1MonthEnd => _memberRegistration != null ? new DateTime(2000, _memberRegistration.SwimSubs1MonthEnd, 1).ToString("MMM", CultureInfo.InvariantCulture) : null;
        public static string SwimSubs2MonthStart => _memberRegistration != null ? new DateTime(2000, _memberRegistration.SwimSubs2MonthStart, 1).ToString("MMM", CultureInfo.InvariantCulture) : null;
        public static string SwimSubs2MonthEnd => _memberRegistration != null ? new DateTime(2000, _memberRegistration.SwimSubs2MonthEnd, 1).ToString("MMM", CultureInfo.InvariantCulture) : null;
        public static string SwimSubs3MonthStart => _memberRegistration != null ? new DateTime(2000, _memberRegistration.SwimSubs3MonthStart, 1).ToString("MMM", CultureInfo.InvariantCulture) : null;
        public static string SwimSubs3MonthEnd => _memberRegistration != null ? new DateTime(2000, _memberRegistration.SwimSubs3MonthEnd, 1).ToString("MMM", CultureInfo.InvariantCulture) : null;

        public static string GetSwimSub1Description(DateTime now)
        {


            int year = now.Year;
            if (now.Month > _memberRegistration.SwimSubs1MonthEnd)
            {
                year++;
            }
            return string.Format("Pool Swim subs {0} to {1} {2}", SwimSubs1MonthStart, SwimSubs1MonthEnd, year);
        }

        public static string GetSwimSub2Description(DateTime now)
        {
            int year = now.Year;
            if (now.Month > _memberRegistration.SwimSubs2MonthEnd)
            {
                year++;
            }
            return string.Format("Pool Swim subs {0} to {1} {2}", SwimSubs2MonthStart, SwimSubs2MonthEnd, year);
        }

        public static string GetSwimSub3Description(DateTime now)
        {
            int year = now.Year;
            if (now.Month > _memberRegistration.SwimSubs3MonthEnd)
            {
                year++;
            }
            return string.Format("Pool Swim subs {0} to {1} {2}", SwimSubs3MonthStart, SwimSubs3MonthEnd, year);
        }

        public static string GetPaymentDescription(MemberOptions membershipOptions)
        {
            List<string> descriptionList = new List<string>() { membershipOptions.MembershipType.ToString() };
            if (membershipOptions.SwimSubs1)
            {
                descriptionList.Add(GetSwimSub1Description(DateTime.Now));
            }
            if (membershipOptions.SwimSubs2)
            {
                descriptionList.Add(GetSwimSub2Description(DateTime.Now));
            }
            if (membershipOptions.SwimSubs3)
            {
                descriptionList.Add(GetSwimSub3Description(DateTime.Now));
            }
            if (membershipOptions.EnglandAthleticsMembership)
            {
                descriptionList.Add("England Athletics Membership");
            }
            if (membershipOptions.OpenWaterIndemnityAcceptance.HasValue && membershipOptions.OpenWaterIndemnityAcceptance.Value == true)
            {
                descriptionList.Add("Open Water Swimming");
            }

            return string.Join(", ", descriptionList);
        }

        public DateTime GetNewMemberExpiry(DateTime currentDate)
        {
            return currentDate.Month == 1 || currentDate.Month == 2
                ? new DateTime(DateTime.Now.Year, 4, 1)
                : new DateTime(DateTime.Now.Year + 1, 4, 1);
        }

        public IMember CreateMember(PersonalDetails regDetails, string[] roles)
        {
            IMember member = _memberService.CreateMember(regDetails.Email, regDetails.Email, $"{regDetails.FirstName} {regDetails.LastName}", "Member");
            _memberService.Save(member);
            _memberService.SavePassword(member, regDetails.Password);

            foreach (var role in roles)
            {
                _memberService.AssignRole(member.Id, role);
            }
            return member;
        }

        public void UpdateMemberDetails(IMember member, PersonalDetails personalDetails, MemberOptions memberOptions)
        {
            SetMemberDetails(member, personalDetails);
            var membershipExpiry = GetNewMemberExpiry(DateTime.Now);
            bool zeroSwimCredits = true;

            SetMembershipOptions(member, memberOptions, membershipExpiry, zeroSwimCredits);

            _memberService.Save(member);
        }

        public IMember GetLoggedInMember()
        {
            var membershipUser = Membership.GetUser();
            if (membershipUser == null)
            {
                return null;
            }

            return _memberService.GetByUsername(membershipUser.UserName);
        }

        public IEnumerable<IMember> GetAll()
        {
            int total;
            return _memberService.GetAll(0, 99999, out total);
        }

        public IEnumerable<MemberDetailsDto> GetAllMemberDetails()
        {
            var members = GetAll();
            return members.Select(m => MapToMemberDetails(m));
        }

        public void UpdateMemberOptions(IMember member, MemberOptions membershipOptions, bool isUpgrade)
        {
            var membershipExpiry = GetNewMemberExpiry(DateTime.Now);
            bool zeroSwimCredits = false;

            SetMembershipOptions(member, membershipOptions, membershipExpiry, zeroSwimCredits);

            if (isUpgrade)
            {
                string username = member.Email;
                Roles.RemoveUserFromRole(username, MSTCRoles.Guest);
                Roles.AddUserToRole(username, MSTCRoles.Member);
            }
        }

        public void AcceptOpenWaterWaiver(IMember member)
        {
            member.SetValue(MemberProperty.OpenWaterIndemnityAcceptance, true);

            //Set SwimAuthNumber
            MembershipTypeEnum membershipType = member.GetValue<MembershipTypeEnum>(MemberProperty.membershipType);
            int swimAuthNumber = GetSwimAuthNumber(membershipType);
            member.SetValue(MemberProperty.SwimAuthNumber, swimAuthNumber);
        }

        private void SetMemberDetails(IMember member, PersonalDetails registrationDetails)
        {
            member.SetValue(MemberProperty.Gender, (int)registrationDetails.Gender);
            member.SetValue(MemberProperty.DateOfBirth, registrationDetails.DateOfBirth);
            member.SetValue(MemberProperty.Address1, registrationDetails.Address1);
            member.SetValue(MemberProperty.City, registrationDetails.City);
            member.SetValue(MemberProperty.Postcode, registrationDetails.Postcode);
            member.SetValue(MemberProperty.Phone, registrationDetails.PhoneNumber);
            member.SetValue(MemberProperty.BTFNumber, registrationDetails.BTFNumber);

            member.SetValue(MemberProperty.medicalConditions, registrationDetails.MedicalConditions);
            member.SetValue(MemberProperty.emergencyContactName, registrationDetails.EmergencyContactName);
            member.SetValue(MemberProperty.emergencyContactNumber, registrationDetails.EmergencyContactPhone);
            member.SetValue(MemberProperty.directDebitMandateId, registrationDetails.DirectDebitMandateId);
        }

        private void SetMembershipOptions(IMember member, MemberOptions membershipOptions, DateTime membershipExpiry, bool zeroSwimCredits)
        {
            // Fix for swim subs renewal bug
            if (member.GetValue<string>(MemberProperty.swimSubs1)?.Length > 0)
            {
                member.SetValue(MemberProperty.swimSubs1ExpiryDate,
                    new DateTime(DateTime.Now.Year, _memberRegistration.SwimSubs1MonthEnd,
                    DateTime.DaysInMonth(DateTime.Now.Year, _memberRegistration.SwimSubs1MonthEnd)));
            }
            if (member.GetValue<string>(MemberProperty.swimSubs2)?.Length > 0)
            {
                member.SetValue(MemberProperty.swimSubs2ExpiryDate,
                    new DateTime(DateTime.Now.Year, _memberRegistration.SwimSubs2MonthEnd,
                    DateTime.DaysInMonth(DateTime.Now.Year, _memberRegistration.SwimSubs2MonthEnd)));
            }
            if (member.GetValue<string>(MemberProperty.swimSubs2)?.Length > 0)
            {
                member.SetValue(MemberProperty.swimSubs2ExpiryDate,
                    new DateTime(DateTime.Now.Year, _memberRegistration.SwimSubs3MonthEnd,
                    DateTime.DaysInMonth(DateTime.Now.Year, _memberRegistration.SwimSubs3MonthEnd)));
            }

            member.SetValue(MemberProperty.membershipType, (int)membershipOptions.MembershipType);
            member.SetValue(MemberProperty.OpenWaterIndemnityAcceptance, membershipOptions.OpenWaterIndemnityAcceptance);
            member.SetValue(MemberProperty.swimSubs1, membershipOptions.SwimSubs1 ? GetSwimSub1Description(DateTime.Now) : string.Empty);
            member.SetValue(MemberProperty.swimSubs2, membershipOptions.SwimSubs2 ? GetSwimSub2Description(DateTime.Now) : string.Empty);
            member.SetValue(MemberProperty.swimSubs3, membershipOptions.SwimSubs3 ? GetSwimSub3Description(DateTime.Now) : string.Empty);
            member.SetValue(MemberProperty.EnglandAthleticsMembership, membershipOptions.EnglandAthleticsMembership);
            member.SetValue(MemberProperty.Volunteering, membershipOptions.Volunteering);
            member.SetValue(MemberProperty.MembershipExpiry, membershipExpiry);

            if (zeroSwimCredits)
            {
                member.SetValue(MemberProperty.TrainingCredits, 0);

            }
            member.SetValue(MemberProperty.GuestCode, membershipOptions.GuestCode);
            member.SetValue(MemberProperty.ReferredByMember, membershipOptions.ReferredByMember);

            if (membershipOptions.OpenWaterIndemnityAcceptance.Value)
            {
                int swimAuthNumber = GetSwimAuthNumber(membershipOptions.MembershipType.Value);
                member.SetValue(MemberProperty.SwimAuthNumber, swimAuthNumber);
            }
        }

        private int GetSwimAuthNumber(MembershipTypeEnum membershipType)
        {
            var membersDetails = GetAllMemberDetails();

            //Calculate the next available swim auth number			
            IEnumerable<MemberDetailsDto> openWaterSwimmers = membersDetails.Where(m => m.SwimAuthNumber.HasValue);

            int swimAuthNumber = 0;
            if (membershipType != MembershipTypeEnum.Guest)
            {
                var swimMembers = openWaterSwimmers.Where(m => m.SwimAuthNumber < 1000).OrderBy(m => m.SwimAuthNumber);
                swimAuthNumber = swimMembers.Any() ? swimMembers.Last().SwimAuthNumber.Value + 1 : 1;
            }
            else
            {
                var guestSwimmers = openWaterSwimmers.Where(m => m.SwimAuthNumber > 1000 && m.SwimAuthNumber < 2000).OrderBy(m => m.SwimAuthNumber);
                swimAuthNumber = guestSwimmers.Any() ? guestSwimmers.Last().SwimAuthNumber.Value + 1 : 1001;
            }

            return swimAuthNumber;
        }

        public MemberDetailsDto MapToMemberDetails(IMember member)
        {
            var memberDetails = new MemberDetailsDto()
            {
                Name = member.Name,
                Email = member.Email,
                Phone = member.GetValue<string>(MemberProperty.Phone),
                MembershipType = member.GetValue<MembershipTypeEnum>(MemberProperty.membershipType),

                SwimSubs1 = member.GetValue<string>(MemberProperty.swimSubs1),
                SwimSubs2 = member.GetValue<string>(MemberProperty.swimSubs2),
                SwimSubs3 = member.GetValue<string>(MemberProperty.swimSubs3),
                EnglandAthleticsMembership = member.GetValue<bool>(MemberProperty.EnglandAthleticsMembership),
                OpenWaterIndemnityAcceptance = member.GetValue<bool>(MemberProperty.OpenWaterIndemnityAcceptance),
                Volunteering = member.GetValue<bool>(MemberProperty.Volunteering),
                MembershipExpiry = member.GetValue<DateTime>(MemberProperty.MembershipExpiry),

                SwimAuthNumber = member.GetValue<int?>(MemberProperty.SwimAuthNumber),
                TrainingCredits = member.GetValue<int>(MemberProperty.TrainingCredits),

                BtfNumber = member.GetValue<string>(MemberProperty.BTFNumber),
                Gender = member.GetValue<GenderEnum>(MemberProperty.Gender).ToString(),
                DateOfBirth = member.GetValue<DateTime>(MemberProperty.DateOfBirth),
                Address1 = member.GetValue<string>(MemberProperty.Address1),
                City = member.GetValue<string>(MemberProperty.City),
                Postcode = member.GetValue<string>(MemberProperty.Postcode),

                MedicalConditions = member.GetValue<string>(MemberProperty.MedicalConditions),
                EmergencyContactName = member.GetValue<string>(MemberProperty.EmergencyContactName),
                EmergencyContactNumber = member.GetValue<string>(MemberProperty.EmergencyContactNumber),
            };
            return memberDetails;
        }
    }
}
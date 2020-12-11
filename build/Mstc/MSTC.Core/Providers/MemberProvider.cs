using System;
using System.Collections.Generic;
using System.Linq;
using Mstc.Core.DataAccess;
using Mstc.Core.Domain;
using Mstc.Core.Dto;
using System.Web.Security;
using Umbraco.Core.Services;
using Umbraco.Core.Models;

namespace Mstc.Core.Providers
{
	/// <summary>
	/// Summary description for MemberProvider
	/// </summary>
	public class MemberProvider
	{
		private readonly IMemberService _memberService;

		public MemberProvider(ServiceContext serviceContext)
		{
			_memberService = serviceContext.MemberService;
		}

        public static string GetSwimSub1Description(DateTime now, bool includePricing)
        {
            int year = now.Year;
            if (now.Month == 1 || now.Month == 2)
            {
                year--;
            }
            return string.Format("Swim subs Apr to Sep {0}{1}", year, includePricing ? " - Standard &pound;30 / Concessions &pound;15" : "");
        }

        public static string GetSwimSub2Description(DateTime now, bool includePricing)
        {
            int year = now.Year;
            if (now.Month == 1 || now.Month == 2)
            {
                year--;
            }
            return string.Format("Swim subs Oct {0} to Mar {1}{2}", year, (year + 1), includePricing ? " - Standard &pound;30 / Concessions &pound;15" : "");
        }

        public static string GetPaymentDescription(MemberOptions membershipOptions)
		{
			List<string> descriptionList = new List<string>() { membershipOptions.MembershipType.ToString() };
			if (membershipOptions.SwimSubs1)
			{
				descriptionList.Add(GetSwimSub1Description(DateTime.Now, true));
			}
            if (membershipOptions.SwimSubs2)
            {
				descriptionList.Add(GetSwimSub1Description(DateTime.Now, true));
			}
		    if (membershipOptions.EnglandAthleticsMembership)
		    {
				descriptionList.Add("England Athletics Membership");
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

			foreach (var role in roles) {
				_memberService.AssignRole(member.Id, role);
			}
			return member;
		}

		public void UpdateMemberDetails(IMember member, RegistrationDetails regDetails)
		{
			SetMemberDetails(member, regDetails.PersonalDetails);
			var membershipExpiry = GetNewMemberExpiry(DateTime.Now);
			bool zeroSwimCredits = true;			

			SetMembershipOptions(member, regDetails.MemberOptions, membershipExpiry, zeroSwimCredits);

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

		//public void UpdateMemberOptions(umbraco.cms.businesslogic.member.Member member, MemberOptions membershipOptions, bool resetEventEntries, bool isUpgrade)
		//{
		//	IDictionary<String, object> currentmemdata = MemberHelper.Get(member);

		//	var membershipExpiry = GetNewMemberExpiry(DateTime.Now);
		//          bool zeroSwimCredits = false;

		//          SetMembershipOptions(currentmemdata, membershipOptions, membershipExpiry, zeroSwimCredits, resetEventEntries);

		//	foreach (Property property in (List<Property>)member.GenericProperties)
		//	{
		//		if (currentmemdata.ContainsKey(property.PropertyType.Alias))
		//			property.Value = currentmemdata[property.PropertyType.Alias];
		//	}

		//          if (isUpgrade)
		//          {
		//              string username = member.Email;
		//              Roles.RemoveUserFromRole(username, MSTCRoles.Guest);
		//              Roles.AddUserToRole(username, MSTCRoles.Member);
		//          }

		//          member.Save();
		//}

		//public void AcceptOpenWaterWaiver(umbraco.cms.businesslogic.member.Member member)
		//{
		//       IDictionary<String, object> currentmemdata = MemberHelper.Get(member);

		//       //Set OpenWaterIndemnityAcceptance
		//       currentmemdata[MemberProperty.OpenWaterIndemnityAcceptance] = true;

		//       //Set SwimAuthNumber
		//       MembershipTypeEnum membershipType;
		//    Enum.TryParse(currentmemdata[MemberProperty.membershipType].ToString(), out membershipType);
		//       int swimAuthNumber = GetSwimAuthNumber(membershipType);
		//       currentmemdata[MemberProperty.SwimAuthNumber] = swimAuthNumber;

		//       foreach (Property property in (List<Property>)member.GenericProperties)
		//       {
		//           if (currentmemdata.ContainsKey(property.PropertyType.Alias))
		//               property.Value = currentmemdata[property.PropertyType.Alias];
		//       }
		//       member.Save();
		//   }

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
			member.SetValue(MemberProperty.membershipType, (int)membershipOptions.MembershipType);
			member.SetValue(MemberProperty.OpenWaterIndemnityAcceptance, membershipOptions.OpenWaterIndemnityAcceptance);
	
			if (membershipOptions.SwimSubs1)
			{
				member.SetValue(MemberProperty.swimSubs1, GetSwimSub1Description(DateTime.Now, false));				
			}
			if (membershipOptions.SwimSubs2)
			{
				member.SetValue(MemberProperty.swimSubs2, GetSwimSub2Description(DateTime.Now, false));
			}

			member.SetValue(MemberProperty.EnglandAthleticsMembership, membershipOptions.EnglandAthleticsMembership);
			member.SetValue(MemberProperty.Volunteering, membershipOptions.Volunteering);
			member.SetValue(MemberProperty.MembershipExpiry, membershipExpiry);
		
			if (zeroSwimCredits)
			{
				member.SetValue(MemberProperty.CreditsBought, 0);
		
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
			//Calculate the next available swim auth number
			IMemberDal memberDal = new MemberDal(new DataConnection());
			IEnumerable<MemberOptionsDto> memberOptions = memberDal.GetMemberOptions();
			IEnumerable<MemberOptionsDto> openWaterSwimmers = memberOptions.Where(m => m.SwimAuthNumber.HasValue);

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
	}
}
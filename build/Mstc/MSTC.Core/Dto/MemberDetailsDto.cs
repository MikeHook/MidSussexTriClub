using System;
using Mstc.Core.Domain;

namespace Mstc.Core.Dto
{
	public class MemberDetailsDto
	{		
		public string Name { get; set; }
		public string Email { get; set; }
		public string Phone { get; set; }
		public MembershipTypeEnum? MembershipType { get; set; }
		public string MembershipTypeString => MembershipType?.ToString();
		public string SwimSubs1 { get; set; }
		public string SwimSubs2 { get; set; }
        public bool EnglandAthleticsMembership { get; set; }
        public bool OpenWaterIndemnityAcceptance { get; set; } 
		public bool Volunteering { get; set; }
		public DateTime? MembershipExpiry { get; set; }
		public int? SwimAuthNumber { get; set; }	
		public int TrainingCredits { get; set; }

		public string BtfNumber { get; set; }
		public string Gender { get; set; }
		public DateTime? DateOfBirth { get; set; }
		public string Address1 { get; set; }
		public string City { get; set; }	
		public string Postcode { get; set; }

		public string MedicalConditions { get; set; }
		public string EmergencyContactName { get; set; }
		public string EmergencyContactNumber { get; set; }

		public bool IsGuest => MembershipType.HasValue && MembershipType.Value == Domain.MembershipTypeEnum.Guest;

	}
}
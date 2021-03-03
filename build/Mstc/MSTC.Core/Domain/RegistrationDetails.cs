using System;
using System.Collections.Generic;

namespace Mstc.Core.Domain
{
	[Serializable]
	public class RegistrationDetails
	{
		public RegistrationDetails()
		{
			PersonalDetails = new PersonalDetails();
			MemberOptions = new MemberOptions();
		}

		public RegistrationDetails(List<Tuple<MembershipTypeEnum, string>> membershipTypes, bool isDiscounted)
		{
			PersonalDetails = new PersonalDetails();
			MemberOptions = new MemberOptions(membershipTypes);
			IsDiscounted = isDiscounted;
		}		

		public PersonalDetails PersonalDetails { get; set; }
		public MemberOptions MemberOptions { get; set; }
		public bool IsDiscounted { get; set; }
	}
}
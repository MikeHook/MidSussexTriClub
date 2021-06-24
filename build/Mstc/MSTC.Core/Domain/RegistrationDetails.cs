using System;
using System.Collections.Generic;
using Mstc.Core.Providers;

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

		public RegistrationDetails(MembershipCostCalculator membershipCostCalculator)
		{
			PersonalDetails = new PersonalDetails();
			MemberOptions = new MemberOptions(membershipCostCalculator);			
		}		

		public PersonalDetails PersonalDetails { get; set; }
		public MemberOptions MemberOptions { get; set; }
		
	}
}
using System;

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

		public PersonalDetails PersonalDetails { get; set; }
		public MemberOptions MemberOptions { get; set; }
	}
}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Mstc.Core.Providers;

namespace Mstc.Core.Domain
{
	[Serializable]
	public class GuestOptions
	{
		public GuestOptions()
		{
			IndemnityOptions = new List<Tuple<bool, string>>();
		}

		public GuestOptions(bool showOwsSignup)
		{
			string owsSignupText = $"I wish to take part in open water swimming.<br/>" +			
				$"I have reviewed the safety video, read and understand the spotter/kayak guidelines and the " +
				$"open water swimming indemnity document.<br />I agree to and accept the terms without qualification.";
			IndemnityOptions = new List<Tuple<bool, string>>()
			{
				new Tuple<bool, string>(true, owsSignupText	),
				new Tuple<bool, string>(false, "I do not wish to take part in open water swimming."),
			};

			ShowOwsSignup = showOwsSignup;
		}

		public List<Tuple<bool, string>> IndemnityOptions { get; set; }

		[Required, Display(Name = "Open water swimming indemnity waiver*")]
		public bool? OpenWaterIndemnityAcceptance { get; set; }	

		public string GuestCode { get; set; }

		public bool ShowOwsSignup { get; set; }
	}
}
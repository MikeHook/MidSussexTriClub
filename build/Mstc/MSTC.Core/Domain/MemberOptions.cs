using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Mstc.Core.Providers;

namespace Mstc.Core.Domain
{
	[Serializable]
	public class MemberOptions
	{
		public MemberOptions()
		{
			MembershipTypes = new List<Tuple<MembershipTypeEnum, string>>();
			IndemnityOptions = new List<Tuple<bool, string>>();
		}

		public MemberOptions(MembershipCostCalculator membershipCostCalculator)
		{
			MembershipTypes = membershipCostCalculator.MembershipTypes;
			string owsSignupText = $"I wish to take part in open water swimming - " +
				$"£{membershipCostCalculator.OwsSignupCostPence / 100} signup fee.<br />" +
				$"I have reviewed the safety video, read and understand the spotter/kayak guidelines and the " +
				$"open water swimming indemnity document.<br />I agree to and accept the terms without qualification " +
				$"and agree to be included in the duty rota for the safety team by attending sessions as a spotter or kayaker.";
			IndemnityOptions = new List<Tuple<bool, string>>()
			{
				new Tuple<bool, string>(true, owsSignupText	),
				new Tuple<bool, string>(false, "I do not wish to take part in open water swimming this season."),
			};

			ShowSwimSubs1 = 2 < DateTime.Now.Month && DateTime.Now.Month < 10;

			SwimSubsCost = membershipCostCalculator.SwimsSubsCostInPence(MembershipTypeEnum.Individual) / 100;
			EnglandAthleticsCost = membershipCostCalculator.EnglandAthleticsCostInPence / 100;
			OwsSignupCost = membershipCostCalculator.OwsSignupCostPence / 100;
		}

		public bool IsRenewing { get; set; }
		public bool IsUpgrading { get; set; }

		public List<Tuple<MembershipTypeEnum, string>> MembershipTypes { get; set; }

		public List<Tuple<bool, string>> IndemnityOptions { get; set; }

		[Required, Display(Name = "Membership*")]
		public MembershipTypeEnum? MembershipType { get; set; }

		public bool ShowSwimSubs1 { get; set; }

		public bool SwimSubs1 { get; set; }

		public bool SwimSubs2 { get; set; }

		public bool EnglandAthleticsMembership { get; set; }

		[Required, Display(Name = "Open water swimming indemnity waiver*")]
		public bool? OpenWaterIndemnityAcceptance { get; set; }

		[Required, Display(Name = "Volunteering agreement*"), Range(typeof(bool), "true", "true")]
		public bool Volunteering { get; set; }

		public string GuestCode { get; set; }

		public string ReferredByMember { get; set; }

		public decimal SwimSubsCost { get; set; }
		public decimal EnglandAthleticsCost { get; set; }
		public decimal OwsSignupCost { get; set; }
	}
}
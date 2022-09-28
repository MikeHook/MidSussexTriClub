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
			string owsSignupText = $"I wish to take part in open water swimming" +
				$" - £{membershipCostCalculator.OwsSignupCostPence / 100} signup fee.<br />" +
				$"I have reviewed the safety video, read and understand the spotter/kayak guidelines and the " +
				$"open water swimming indemnity document.<br />I agree to and accept the terms without qualification" +
				$" and agree to be included in the duty rota for the safety team by attending sessions as a spotter or kayaker.";
			IndemnityOptions = new List<Tuple<bool, string>>()
			{
				new Tuple<bool, string>(true, owsSignupText	),
				new Tuple<bool, string>(false, "I do not wish to take part in open water swimming this season."),
			};

			ShowSwimSubs1 = membershipCostCalculator.SwimSubs1Enabled;
			ShowSwimSubs2 = membershipCostCalculator.SwimSubs2Enabled;
			ShowSwimSubs3 = membershipCostCalculator.SwimSubs3Enabled;
			ShowEnglandAthletics = membershipCostCalculator.EnglandAthleticsEnabled;
			ShowOwsSignup = membershipCostCalculator.OwsSignupEnabled;
		
			SwimSubs1Cost = membershipCostCalculator.SwimsSubsCostInPence(MembershipTypeEnum.Individual, membershipCostCalculator.SwimSubs1CostPence) / 100;
            SwimSubs2Cost = membershipCostCalculator.SwimsSubsCostInPence(MembershipTypeEnum.Individual, membershipCostCalculator.SwimSubs2CostPence) / 100;
            SwimSubs3Cost = membershipCostCalculator.SwimsSubsCostInPence(MembershipTypeEnum.Individual, membershipCostCalculator.SwimSubs3CostPence) / 100;

            EnglandAthleticsCost = membershipCostCalculator.EnglandAthleticsCostInPence / 100;
			OwsSignupCost = membershipCostCalculator.OwsSignupCostPence / 100;

			IsDiscounted = membershipCostCalculator.DiscountedMonths.Contains(DateTime.Now.Month);
		}

		public bool IsDiscounted { get; set; }

		public bool IsRenewing { get; set; }
		public bool IsUpgrading { get; set; }

		public List<Tuple<MembershipTypeEnum, string>> MembershipTypes { get; set; }

		public List<Tuple<bool, string>> IndemnityOptions { get; set; }

		[Required, Display(Name = "Membership*")]
		public MembershipTypeEnum? MembershipType { get; set; }

		public bool ShowSwimSubs1 { get; set; }
		public bool ShowSwimSubs2 { get; set; }

        public bool ShowSwimSubs3 { get; set; }
        public bool ShowEnglandAthletics { get; set; }
		public bool ShowOwsSignup { get; set; }

		public bool SwimSubs1 { get; set; }

		public bool SwimSubs2 { get; set; }

        public bool SwimSubs3 { get; set; }

        public bool EnglandAthleticsMembership { get; set; }

		[Required, Display(Name = "Open water swimming indemnity waiver*")]
		public bool? OpenWaterIndemnityAcceptance { get; set; }

		[Required, Display(Name = "Volunteering agreement*"), Range(typeof(bool), "true", "true")]
		public bool Volunteering { get; set; }

		public string GuestCode { get; set; }

		public string ReferredByMember { get; set; }

		public decimal SwimSubs1Cost { get; set; }
        public decimal SwimSubs2Cost { get; set; }
        public decimal SwimSubs3Cost { get; set; }
        public decimal EnglandAthleticsCost { get; set; }
		public decimal OwsSignupCost { get; set; }
	}
}
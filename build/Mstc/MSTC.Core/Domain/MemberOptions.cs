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
			MembershipTypes = new List<Tuple<MembershipTypeEnum, string>>()
			{
				new Tuple<MembershipTypeEnum, string>(
					MembershipTypeEnum.Individual,
					$"Individual membership - &pound;{(decimal)MembershipCostCalculator.GetTypeCostPence(MembershipTypeEnum.Individual, DateTime.Now)/100}"),
				new Tuple<MembershipTypeEnum, string>(
					MembershipTypeEnum.Couple,
					$"Couple membership - &pound;{(decimal)MembershipCostCalculator.GetTypeCostPence(MembershipTypeEnum.Couple, DateTime.Now)/100}<br /> <i>Only select this option if your partner will also be renewing their membership - The membership secretary will be checking!</i>"),
								new Tuple<MembershipTypeEnum, string>(
					MembershipTypeEnum.Concession,
					$"Concession: Youth (16-17) / Student / Unemployed - &pound;{(decimal)MembershipCostCalculator.GetTypeCostPence(MembershipTypeEnum.Concession, DateTime.Now)/100}"),
			};

			IndemnityOptions = new List<Tuple<bool, string>>()
			{
				new Tuple<bool, string>(
					true,
					@"I wish to take part in open water swimming.<br />I have reviewed the safety video, read and understand the spotter/kayak guidelines and the open water swimming indemnity document.<br />I agree to and accept the terms without qualification and agree to be included in the duty rota for the safety team by attending sessions as a spotter or kayaker."),
								new Tuple<bool, string>(
					false,
					@"I do not wish to take part in open water swimming this season."),
			};

			ShowSwimSubs1 = 2 < DateTime.Now.Month && DateTime.Now.Month < 10;
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
	}
}
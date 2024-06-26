﻿using System;
using System.Collections.Generic;
using System.Linq;
using Mstc.Core.ContentModels;
using Mstc.Core.Domain;
using Umbraco.Web;

namespace Mstc.Core.Providers
{
	/// <summary>
	/// Summary description for MembershipCostCalcualtor
	/// </summary>
	public class MembershipCostCalculator
	{
		private MemberRegistration _memberRegistration;

		public MembershipCostCalculator()
		{
			UmbracoHelper umbracoHelper = new UmbracoHelper(UmbracoContext.Current);
			var memberRegistrationContent = umbracoHelper?.TypedContentAtRoot().DescendantsOrSelf(MemberRegistration.ModelTypeAlias).FirstOrDefault();
			if (memberRegistrationContent != null)
			{
				_memberRegistration = new MemberRegistration(memberRegistrationContent);
			}
		}

		public List<int> DiscountedMonths
		{
			get { return new List<int>() { 10, 11, 12, 1, 2 }; }
		}

		public bool SwimSubs1Enabled => _memberRegistration != null ? _memberRegistration.SwimSubs1Enabled : true;
		public bool SwimSubs2Enabled => _memberRegistration != null ? _memberRegistration.SwimSubs2Enabled : true;
        public bool SwimSubs3Enabled => _memberRegistration != null ? _memberRegistration.SwimSubs3Enabled : true;
        public bool EnglandAthleticsEnabled => _memberRegistration != null ? _memberRegistration.EnglandAthleticsEnabled : true;
		public bool OwsSignupEnabled => _memberRegistration != null ? _memberRegistration.OWssignupEnabled : true;

		public List<Tuple<MembershipTypeEnum, string>> MembershipTypes => new List<Tuple<MembershipTypeEnum, string>>()
			{
				new Tuple<MembershipTypeEnum, string>(
					MembershipTypeEnum.Individual,
					$"Individual membership - &pound;{(decimal)GetTypeCostPence(MembershipTypeEnum.Individual, DateTime.Now)/100}"),
				new Tuple<MembershipTypeEnum, string>(
					MembershipTypeEnum.Couple,
					$"Couple membership - &pound;{(decimal)GetTypeCostPence(MembershipTypeEnum.Couple, DateTime.Now)/100}<br /> <i>Only select this option if your partner will also be renewing their membership - The membership secretary will be checking!</i>"),
								new Tuple<MembershipTypeEnum, string>(
					MembershipTypeEnum.Concession,
					$"Concession: Youth (16-17) / Student / Unemployed - &pound;{(decimal)GetTypeCostPence(MembershipTypeEnum.Concession, DateTime.Now)/100}"),
			};

		public int SwimsSubsCostInPence(MembershipTypeEnum type, int swimSubsCost)
		{
			return type == MembershipTypeEnum.Concession ? swimSubsCost / 2 : swimSubsCost;
		}

		public int EnglandAthleticsCostInPence => (int)(_memberRegistration.EnglandAthleticsCost * 100);
		public int OwsSignupCostPence => (int)(_memberRegistration.OWssignupCost * 100);

        public int SwimSubs1CostPence => (int)(_memberRegistration.SwimSubs1Cost * 100);
        public int SwimSubs2CostPence => (int)(_memberRegistration.SwimSubs2Cost * 100);
        public int SwimSubs3CostPence => (int)(_memberRegistration.SwimSubs3Cost * 100);

        public int GetTypeCostPence(MembershipTypeEnum type, DateTime currentDate)
		{
			decimal cost = 50;
			if (_memberRegistration == null)
			{
				return 50;
			}
			
			switch (type)
			{
				case MembershipTypeEnum.Individual:
					cost = _memberRegistration.IndividualMemberCost;
					break;
				case MembershipTypeEnum.Couple:
					cost = _memberRegistration.CoupleMemberCost;
					break;
				case MembershipTypeEnum.Concession:
					cost = _memberRegistration.ConcessionMemberCost;
					break;
			};
			
			int costInPence = (int)(cost * 100);
			costInPence = DiscountedMonths.Contains(currentDate.Month)
				? costInPence / 2
				: costInPence;
			return costInPence;
		}

		public int Calculate(MemberOptions membershipOptions, DateTime currentDate)
		{
			var cost = GetTypeCostPence(membershipOptions.MembershipType.Value, currentDate);
			if (membershipOptions.SwimSubs1)
			{
				cost += SwimsSubsCostInPence(membershipOptions.MembershipType.Value, SwimSubs1CostPence);
			}
			if (membershipOptions.SwimSubs2)
			{
				cost += SwimsSubsCostInPence(membershipOptions.MembershipType.Value, SwimSubs2CostPence);
			}
            if (membershipOptions.SwimSubs3)
            {
                cost += SwimsSubsCostInPence(membershipOptions.MembershipType.Value, SwimSubs3CostPence);
            }

            if (membershipOptions.EnglandAthleticsMembership)
			{
				cost += EnglandAthleticsCostInPence;
			}
			if (membershipOptions.OpenWaterIndemnityAcceptance.HasValue && membershipOptions.OpenWaterIndemnityAcceptance.Value == true)
			{
				cost += OwsSignupCostPence;
			}
			return cost;
		}

		public int SwimCreditsCost(PaymentStates credits)
		{
			return (int)credits * 100;
		}

		public int PaymentStateCost(PaymentStates state, MembershipTypeEnum type)
		{
			switch (state)
			{
				case PaymentStates.SS05991:
                    {
                        return SwimsSubsCostInPence(type, SwimSubs1CostPence);
                    }
                case PaymentStates.SS05992:
					{
						return SwimsSubsCostInPence(type, SwimSubs2CostPence);
					}
                case PaymentStates.SS05993:
                    {
                        return SwimsSubsCostInPence(type, SwimSubs3CostPence);
                    }
            }

			throw new Exception("Unknown cost");
		}

	}
}
using System;
using System.Collections.Generic;
using Mstc.Core.Domain;

namespace Mstc.Core.Providers
{
	/// <summary>
	/// Summary description for MembershipCostCalcualtor
	/// </summary>
	public class MembershipCostCalculator
	{
		private static Dictionary<MembershipTypeEnum, int> TypeCosts = new Dictionary<MembershipTypeEnum, int>()
		{
			{MembershipTypeEnum.Individual, 4000},
			{MembershipTypeEnum.Couple,3500},
			{MembershipTypeEnum.Concession, 2000},
		};

		public static List<int> DiscountedMonths
		{
			get { return new List<int>() {10, 11, 12, 1, 2}; }
		}

        public static int SwimsSubsCostInPence(MembershipTypeEnum type)
        {
            return type == MembershipTypeEnum.Concession ? 1500 : 3000;
        }
	    public static int EnglandAthleticsCostInPence = 1600;
	    public static int OwsTasterCost = 600;

		public static int GetTypeCostPence(MembershipTypeEnum type, DateTime currentDate)
		{
            int cost = DiscountedMonths.Contains(currentDate.Month)                 
                ? TypeCosts[type]/2 
                : TypeCosts[type];
            return cost / 2; //COVID-19 Discount
		}

		public static int Calculate(MemberOptions membershipOptions, DateTime currentDate)
		{
			var cost = GetTypeCostPence(membershipOptions.MembershipType.Value, currentDate);		
			if (membershipOptions.SwimSubs1)
			{
				cost += SwimsSubsCostInPence(membershipOptions.MembershipType.Value);
			}
			if (membershipOptions.SwimSubs2)
			{
				cost += SwimsSubsCostInPence(membershipOptions.MembershipType.Value);
            }
		    if (membershipOptions.EnglandAthleticsMembership)
		    {
		        cost += EnglandAthleticsCostInPence;
		    }
	
			return cost;
		}

		public static int SwimCreditsCost(PaymentStates credits)
		{
			return (int) credits * 100;
		}

        public static int PaymentStateCost(PaymentStates state, MembershipTypeEnum type)
        {
            switch (state)
            {               
                case PaymentStates.SS05991:
                case PaymentStates.SS05992:        
                {
                    return SwimsSubsCostInPence(type);
                }
            }

            throw new Exception("Unknown cost");
        }

    }
}
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

        public static int PaymentStateCost(PaymentStates state, bool hasBTFNumber, MembershipTypeEnum type)
        {
            switch (state)
            {
                case PaymentStates.S00199C:
                case PaymentStates.S00299C:
                case PaymentStates.S00599C:
                case PaymentStates.S001099C:
                case PaymentStates.S001599C:
                case PaymentStates.S002499C:
                {
                    return SwimCreditsCost(state);
                    }
                case PaymentStates.E00D101C:
                case PaymentStates.E00D102C:
                    {
                    return 1500;
                }
                case PaymentStates.E00D103C:
                    {
                        return 1000;
                    }
                case PaymentStates.E00TRIOI201C:
                case PaymentStates.E00TRIMI203C:                
                case PaymentStates.E00TRISI205C:
                case PaymentStates.E00AOI207C:
                case PaymentStates.E00AMI209C:
                case PaymentStates.E00ASI211C:
                    {
                    return hasBTFNumber ? 2000 : 2400;
                }
                case PaymentStates.E00TRIOR202C:
                case PaymentStates.E00TRIMR204C:
                case PaymentStates.E00TRISR206C:
                case PaymentStates.E00AOR208C:
                case PaymentStates.E00AMR210C:
                case PaymentStates.E00ASR212C:
                    {
                    return hasBTFNumber ? 1000 : 1400;
                }
                case PaymentStates.E00S1KM301C:
                    {
                        return 1000;
                    }
                case PaymentStates.E00S3KM302C:
                    {
                        return 2000;
                    }
                case PaymentStates.SS05991:
                case PaymentStates.SS05992:
                case PaymentStates.SS05996:
                    {
                        return SwimsSubsCostInPence(type);
                    }
            }

            throw new Exception("Unknown cost");
        }

    }
}
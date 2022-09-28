using System.ComponentModel;

namespace Mstc.Core.Domain
{
	public enum PaymentStates
	{
        /*
		[Description("Swim Credits")] S00199C = 6,
        
        [Description("Duathlon - Short Individual")] E00D101C = 101,
        [Description("Duathlon - Standard Individual")] E00D102C = 102,
        [Description("Duathlon - Relay")] E00D103C = 103,

		[Description("Triathlon - Olympic Individual")] E00TRIOI201C = 201,
		[Description("Triathlon - Olympic Relay")] E00TRIOR202C = 202,
		[Description("Triathlon - Middle Individual")] E00TRIMI203C = 203,
		[Description("Triathlon - Middle Relay")] E00TRIMR204C = 204,
		[Description("Triathlon - Sprint Individual")] E00TRISI205C = 205,
		[Description("Triathlon - Sprint Relay")] E00TRISR206C = 206,
        [Description("Aquathlon - Olympic Individual")] E00AOI207C = 207,
        [Description("Aquathlon - Olympic Relay")] E00AOR208C = 208,
        [Description("Aquathlon - Middle Individual")] E00AMI209C = 209,
        [Description("Aquathlon - Middle Relay")] E00AMR210C = 210,
        [Description("Aquathlon - Sprint Individual")] E00ASI211C = 211,
        [Description("Aquathlon - Sprint Relay")] E00ASR212C = 212,

        [Description("Charity Swim - 1km")] E00S1KM301C = 301,
		[Description("Charity Swim - Over 1km")] E00S3KM302C = 302,
        */

        [Description("Pool Swim subs Sep to Dec")] SS05991 = 401,
		[Description("Pool Swim subs Jan to Apr")] SS05992 = 402,
        [Description("Pool Swim subs May to Aug")] SS05993 = 403,

        [Description("MSTC Membership Renewal")]
        MemberRenewal = 501,
        [Description("MSTC Membership Upgrade")]
        MemberUpgrade = 502,

        [Description("Training Credits")] TrainingCredits = 601,

        [Description("Open water swim signup")] OwsSignup = 701,

    }
}

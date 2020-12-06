using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Mstc.Core.Domain
{
	public enum TopicEnum
	{
		[Display(Name = "General")]
		General = 1,
		[Display(Name = "Membership")]
		Membership = 2,
		[Display(Name = "Open Water Swimming")]
		OWS = 3,
		[Display(Name = "Sponsorship")]
		Sponsorship = 4,
		[Display(Name = "Juniors")]
		Juniors = 5,
		[Display(Name = "Website")]
		Website = 6,
		[Display(Name = "Coaching")]
		Coaching = 7,
	}
}

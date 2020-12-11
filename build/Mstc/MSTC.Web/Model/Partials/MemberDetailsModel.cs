using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Mstc.Core.Domain;
using Mstc.Core.Validation;

namespace MSTC.Web.Model.Partials
{
    public class MemberDetailsModel
    {
		[Required, Display(Name = "Name*")]
		public string Name { get; set; }

		[Required(ErrorMessage = "Please enter your email address"), Display(Name = "Email*"), DataType(DataType.EmailAddress)]
		public string Email { get; set; }

		[Required, Display(Name = "Gender*")]
		public GenderEnum Gender { get; set; }

		[Required, Display(Name = "Date of birth*"), DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}"), DateOver16Validator, DataType(DataType.Date)]
		public DateTime DateOfBirth { get; set; }

		[Required, Display(Name = "House number and Street*")]
		public string Address1 { get; set; }

		[Required, Display(Name = "Town / City*")]
		public string City { get; set; }

		[Required, Display(Name = "Postcode*"), DataType(DataType.PostalCode)]
		public string Postcode { get; set; }

		[Required, Display(Name = "Phone number*"), DataType(DataType.PhoneNumber)]
		public string PhoneNumber { get; set; }

		[Display(Name = "BTF number")]
		public string BTFNumber { get; set; }

		[Required, Display(Name = "Medical conditions*")]
		public string MedicalConditions { get; set; }

		[Required, Display(Name = "Emergency contact name*")]
		public string EmergencyContactName { get; set; }

		[Required, Display(Name = "Emergency contact phone*")]
		public string EmergencyContactPhone { get; set; }

		[Display(Name = "Promote a service?")]
		public bool ShowService { get; set; }

		[Display(Name = "Service Website Address (Starting http or https)")]
		public string ServiceWebsite { get; set; }

		[Display(Name = "Service Name")]
		public string ServiceName { get; set; }

		[Display(Name = "Service Image")]
		public string ServiceImageId { get; set; }

		public HttpPostedFileBase ServiceImageFile { get; set; }

		[Display(Name = "Service Description"), MaxLength(200)]
		public string ServiceDescription { get; set; }
	}
}
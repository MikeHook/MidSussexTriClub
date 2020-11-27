using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Mstc.Core.Validation;

namespace Mstc.Core.Domain
{
	[Serializable]
	public class PersonalDetails
	{
		public PersonalDetails()
		{
			Genders = new List<string>() { "Male", "Female" };
		}

		public List<string> Genders { get; set; }

		[Required, Display(Name = "First name*")]
		public string FirstName { get; set; }

		[Required, Display(Name = "Last name*")]
		public string LastName { get; set; }

		[Required(ErrorMessage = "Please enter your email address"), Display(Name = "Email*"), DataType(DataType.EmailAddress)]
		public string Email { get; set; }

		[Required, Display(Name = "Password*"), DataType(DataType.Password), MinLength(8)]
		public string Password { get; set; }

		[Required, Display(Name = "Gender*")]	
		public string Gender { get; set; }

		[Required, Display(Name = "Date of birth*"), DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}"), DateOver16Validator, DataType(DataType.Date)]
		public DateTime? DateOfBirth { get; set; }

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

        public string DirectDebitMandateId { get; set; }
	}
}
using System.Collections.Generic;
using System.Configuration;
using System.Net.Mail;
using Mstc.Core.configuration;
using Mstc.Core.Domain;

namespace Mstc.Core.Providers
{
	public class EmailProvider
	{
		public EmailProvider()
		{

		}

		public const string MembersEmail = "members@midsussextriclub.com";
		public const string OwsEmail = "openwaterswim@midsussextriclub.com";
        public const string SponsorsEmail = "sponsorship@midsussextriclub.com";
		public const string JuniorsEmail = "juniors@midsussextriclub.com";
		public const string SupportEmail = "support@midsussextriclub.com";
		public const string CoachingEmail = "coaching@midsussextriclub.com";


		public Dictionary<TopicEnum, string> EmailLookup = new Dictionary<TopicEnum, string>()
		{
			{TopicEnum.Membership, MembersEmail},
			{TopicEnum.OWS, OwsEmail},
            {TopicEnum.Sponsorship, SponsorsEmail},
			{TopicEnum.Juniors, JuniorsEmail},
			{TopicEnum.Website, SupportEmail},
			{TopicEnum.Coaching, CoachingEmail},
		};

		public void SendEmail(string toAddress, string fromAddress, string subject, string htmlContent)
		{
			string environment = ConfigurationManager.AppSettings["environment"];
			if (environment != "Production")
			{
				toAddress = SupportEmail;
			}

			MailMessage objMail = new MailMessage();

			objMail.To.Add(toAddress);
			objMail.From = new MailAddress(fromAddress);
			objMail.Subject = subject;
			objMail.IsBodyHtml = true;
			objMail.Body = htmlContent;

		    string gmailUserName = ConfigurationManager.AppSettings["gmailUserName"];
		    if (string.IsNullOrWhiteSpace(gmailUserName) == false)
		    {
		        var GmailSmtpClient = new GmailSmtpClient();
		        GmailSmtpClient.Send(objMail);
		    }
		}
	}
}
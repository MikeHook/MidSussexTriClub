//------------------------------------------------------------------------------
// <auto-generated>
//   This code was generated by a tool.
//
//    Umbraco.ModelsBuilder v3.0.10.102
//
//   Changes to this file will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Web;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.ModelsBuilder;
using Umbraco.ModelsBuilder.Umbraco;

namespace Mstc.Core.ContentModels
{
	/// <summary>Event</summary>
	[PublishedContentModel("event")]
	public partial class Event : PublishedContentModel, IContentBase
	{
#pragma warning disable 0109 // new is redundant
		public new const string ModelTypeAlias = "event";
		public new const PublishedItemType ModelItemType = PublishedItemType.Content;
#pragma warning restore 0109

		public Event(IPublishedContent content)
			: base(content)
		{ }

#pragma warning disable 0109 // new is redundant
		public new static PublishedContentType GetModelContentType()
		{
			return PublishedContentType.Get(ModelItemType, ModelTypeAlias);
		}
#pragma warning restore 0109

		public static PublishedPropertyType GetModelPropertyType<TValue>(Expression<Func<Event, TValue>> selector)
		{
			return PublishedContentModelUtility.GetModelPropertyType(GetModelContentType(), selector);
		}

		///<summary>
		/// Cost
		///</summary>
		[ImplementPropertyType("cost")]
		public int Cost
		{
			get { return this.GetPropertyValue<int>("cost"); }
		}

		///<summary>
		/// Covid Health Declaration
		///</summary>
		[ImplementPropertyType("covidHealthDeclaration")]
		public IPublishedContent CovidHealthDeclaration
		{
			get { return this.GetPropertyValue<IPublishedContent>("covidHealthDeclaration"); }
		}

		///<summary>
		/// End Date
		///</summary>
		[ImplementPropertyType("endDate")]
		public DateTime EndDate
		{
			get { return this.GetPropertyValue<DateTime>("endDate"); }
		}

		///<summary>
		/// Event Type
		///</summary>
		[ImplementPropertyType("eventType")]
		public string EventType
		{
			get { return this.GetPropertyValue<string>("eventType"); }
		}

		///<summary>
		/// Indemnity Waiver
		///</summary>
		[ImplementPropertyType("indemnityWaiver")]
		public IPublishedContent IndemnityWaiver
		{
			get { return this.GetPropertyValue<IPublishedContent>("indemnityWaiver"); }
		}

		///<summary>
		/// Is Guest Event
		///</summary>
		[ImplementPropertyType("isGuestEvent")]
		public bool IsGuestEvent
		{
			get { return this.GetPropertyValue<bool>("isGuestEvent"); }
		}

		///<summary>
		/// Maximum Participants
		///</summary>
		[ImplementPropertyType("maximumParticipants")]
		public int MaximumParticipants
		{
			get { return this.GetPropertyValue<int>("maximumParticipants"); }
		}

		///<summary>
		/// Race Distances
		///</summary>
		[ImplementPropertyType("raceDistances")]
		public IEnumerable<string> RaceDistances
		{
			get { return this.GetPropertyValue<IEnumerable<string>>("raceDistances"); }
		}

		///<summary>
		/// Recurring
		///</summary>
		[ImplementPropertyType("recurring")]
		public bool Recurring
		{
			get { return this.GetPropertyValue<bool>("recurring"); }
		}

		///<summary>
		/// Recurring Frequency
		///</summary>
		[ImplementPropertyType("recurringFrequency")]
		public int RecurringFrequency
		{
			get { return this.GetPropertyValue<int>("recurringFrequency"); }
		}

		///<summary>
		/// Requires BTF License
		///</summary>
		[ImplementPropertyType("requiresBTFLicense")]
		public bool RequiresBtflicense
		{
			get { return this.GetPropertyValue<bool>("requiresBTFLicense"); }
		}

		///<summary>
		/// Start Date
		///</summary>
		[ImplementPropertyType("startDate")]
		public DateTime StartDate
		{
			get { return this.GetPropertyValue<DateTime>("startDate"); }
		}

		///<summary>
		/// Subscription Cost: Cost if the member has a subscription
		///</summary>
		[ImplementPropertyType("subsCost")]
		public int SubsCost
		{
			get { return this.GetPropertyValue<int>("subsCost"); }
		}

		///<summary>
		/// Hide in Nav
		///</summary>
		[ImplementPropertyType("umbracoNavihide")]
		public bool UmbracoNavihide
		{
			get { return this.GetPropertyValue<bool>("umbracoNavihide"); }
		}

		///<summary>
		/// Content
		///</summary>
		[ImplementPropertyType("bodyText")]
		public IHtmlString BodyText
		{
			get { return Mstc.Core.ContentModels.ContentBase.GetBodyText(this); }
		}

		///<summary>
		/// Page Title: The title of the page, this is also the first text in a google search result. The ideal length is between 40 and 60 characters
		///</summary>
		[ImplementPropertyType("pageTitle")]
		public string PageTitle
		{
			get { return Mstc.Core.ContentModels.ContentBase.GetPageTitle(this); }
		}
	}
}

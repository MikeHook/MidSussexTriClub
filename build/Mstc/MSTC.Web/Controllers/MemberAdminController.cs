using System;
using System.Collections.Generic;
using Mstc.Core.Dto;
using Mstc.Core.Providers;
using Umbraco.Web.WebApi;

namespace MSTC.Web.Controllers
{
	[MemberAuthorize(AllowGroup = "MemberAdmin")]
	public class MemberAdminController : UmbracoApiController
    {
		MemberProvider _memberProvider;

		public MemberAdminController()
		{
			_memberProvider = new MemberProvider(Services);
		}

        public IEnumerable<MemberDetailsDto> Get()
        {
			try
			{
				return _memberProvider.GetAllMemberDetails();					
			}
			catch (Exception ex)
			{
				Logger.Error(typeof(MemberAdminController), string.Format("Exception getting MemberAdmin members: {0}", ex.ToString()), ex);
				return new List<MemberDetailsDto>();
			}
        }

		
	}
}

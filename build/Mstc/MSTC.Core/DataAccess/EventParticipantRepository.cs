using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Mstc.Core.Domain;

namespace Mstc.Core.DataAccess
{
    public interface IEventParticipantRepository
    {
        IEnumerable<EventParticipant> GetByEventSlot(IDbConnection connection, int eventSlotId);
        IEnumerable<EventParticipant> GetAll(IDbConnection connection);
        EventParticipant Create(EventParticipant eventParticipant);    
        void Delete(int id);
    }

    public class EventParticipantRepository : IEventParticipantRepository
    {
        private readonly IDataConnection _dataConnection;

        public EventParticipantRepository(IDataConnection dataConnection)
        {
            _dataConnection = dataConnection;
        }

        string _baseQuery = @"SELECT ep.[Id]
                                    ,ep.[EventSlotId]
                                    ,ep.[MemberId]
                                    ,ep.[AmountPaid]
                                    ,ep.[RaceDistance]
		                            ,uNode.[text] as [Name]
		                            ,m.[Email]
		                            ,cData.dataNvarchar as [Phone]
                                  FROM [dbo].[EventParticipant] ep
                                  INNER JOIN [dbo].[cmsMember] m on ep.MemberId = m.nodeId
                                  INNER JOIN [dbo].[umbracoNode] uNode on uNode.Id = m.nodeId 
                                  INNER JOIN [dbo].[cmsPropertyData] cData on cData.contentNodeId = m.nodeId AND cData.propertytypeid = 119 ";

        public IEnumerable<EventParticipant> GetByEventSlot(IDbConnection connection, int eventSlotId)
        {
            string query = $"{_baseQuery} Where ep.EventSlotId = @EventSlotId";

            return connection.Query<EventParticipant>(query, new { EventSlotId = eventSlotId });
        }

        public IEnumerable<EventParticipant> GetAll(IDbConnection connection)
        {
            return connection.Query<EventParticipant>(_baseQuery);         
        }

        public EventParticipant Create(EventParticipant eventParticipant)
        {
            string query = @"INSERT INTO [dbo].[EventParticipant]            
                                           ([EventSlotId]                                       
                                           ,[MemberId]
                                           ,[AmountPaid]
                                           ,[RaceDistance])
                                        OUTPUT Inserted.Id
                                        VALUES
                                           (@EventSlotId
                                           ,@MemberId
                                           ,@AmountPaid
                                           ,@RaceDistance)";


            using (IDbConnection connection = _dataConnection.SqlConnection)
            {
                eventParticipant.Id = connection.Query<int>(query, eventParticipant).Single();
            }

            return eventParticipant;
        }

        public void Delete(int id)
        {
            string query = @"DELETE FROM [dbo].[EventParticipant] WHERE Id = @Id";
            using (IDbConnection connection = _dataConnection.SqlConnection)
            {
                connection.Execute(query, new { Id = id });
            }
        }        
    }
}

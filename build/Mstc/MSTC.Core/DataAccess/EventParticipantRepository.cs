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

        string _baseQuery = @"SELECT  [Id]
                                      ,[EventSlotId]
                                      ,[MemberId]
                                      ,[AmountPaid]
                                  FROM [dbo].[EventParticipant] ";

        public IEnumerable<EventParticipant> GetByEventSlot(IDbConnection connection, int eventSlotId)
        {
            string query = $"{_baseQuery} Where EventSlotId = @EventSlotId";

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
                                           ,[AmountPaid])
                                        OUTPUT Inserted.Id
                                        VALUES
                                           (@EventSlotId
                                           ,@MemberId
                                           ,@AmountPaid)";


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

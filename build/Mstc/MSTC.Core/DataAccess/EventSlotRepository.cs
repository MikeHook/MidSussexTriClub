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
    public interface IEventSlotRepository
    {
        EventSlot Create(EventSlot eventSlot);
        IEnumerable<EventSlot> GetAll(bool futureEventsOnly);
    }

    public class EventSlotRepository : IEventSlotRepository
    {
        string _baseGet = @"SELECT [Id]
                                      ,[EventTypeId]
                                      ,[Date]
                                      ,[Cost]
                                      ,[MaxParticipants]                                    
                                  FROM [dbo].[EventSlot] ";

        private readonly IDataConnection _dataConnection;

        public EventSlotRepository(IDataConnection dataConnection)
        {
            _dataConnection = dataConnection;
        }

        public EventSlot Create(EventSlot eventSlot)
        {
            string query = @"INSERT INTO [dbo].[EventSlot]            
                                           ([EventTypeId]
                                           ,[Date]
                                           ,[Cost]
                                           ,[MaxParticipants])
                                        OUTPUT Inserted.Id
                                        VALUES
                                           (@EventTypeId
                                           ,@Date
                                           ,@Cost
                                           ,@MaxParticipants)";


            using (IDbConnection connection = _dataConnection.SqlConnection)
            {
                eventSlot.Id = connection.Query<int>(query, eventSlot).Single();
            }

            return eventSlot;
        }

        public IEnumerable<EventSlot> GetAll(bool futureEventsOnly)
        {
            string query = $"{_baseGet} {(futureEventsOnly ? "Where [Date] > @now" : "")} order By Date";

            using (IDbConnection connection = _dataConnection.SqlConnection)
            {
                return connection.Query<EventSlot>(query, new { now = DateTime.Now });
            }
        }

    }
}

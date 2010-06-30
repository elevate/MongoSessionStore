using System;
using System.Configuration;
using MongoDB;
using MongoDB.Configuration;
using MongoDB.Connections;

namespace MongoSessionStore
{
    public class SessionStore
    {
        private string applicationName;
        private string connectionString;

        public SessionStore(string applicationName)
        {
            connectionString = (string)ConfigurationManager.AppSettings["mongoserver"];
            this.applicationName = applicationName;
        }

        public void Insert(Session session)
        {
            Document newSession = new Document() { { "SessionId",session.SessionID }, {"ApplicationName",session.ApplicationName},{"Created",session.Created},
            {"Expires",session.Expires},{"LockDate",session.LockDate},{"LockId",session.LockID},{"Timeout",session.Timeout},{"Locked",session.Locked},
            {"SessionItems",session.SessionItems},{"SessionItemsCount",session.SessionItemsCount},{"Flags",session.Flags}};

            using (var mongo = new Mongo(connectionString))
            {
                mongo.Connect();
                mongo[applicationName]["sessions"].Insert(newSession);
            }

        }

        public Session Get(string id, string applicationName)
        {
            Document selector = new Document() { { "SessionId", id }, { "ApplicationName", applicationName } };
            Session session;

            Document sessionDoc;

            using (var mongo = new Mongo(connectionString))
            {
                mongo.Connect();
                sessionDoc = mongo[applicationName]["sessions"].FindOne(selector);
            }

            if (sessionDoc == null)
            {
                session = null;
            }
            else
            {
                session = new Session(sessionDoc);
            }



            return session;
        }

        public void UpdateSession(string id, int timeout, Binary sessionItems, string applicationName, int sessionItemsCount, object lockId)
        {

            Document selector = new Document() { { "SessionId", id }, { "ApplicationName", applicationName }, { "LockId", lockId } };
            Document session = new Document() { { "$set", new Document() { { "Expires", DateTime.Now.AddMinutes((double)timeout) }, { "Timeout", timeout }, { "Locked", false }, { "SessionItems", sessionItems }, { "SessionItemsCount", sessionItemsCount } } } };
            using (var mongo = new Mongo(connectionString))
            {
                mongo.Connect();
                mongo[applicationName]["sessions"].Update(session, selector, 0, false);
            }

        }

        public void UpdateSessionExpiration(string id, string applicationName, double timeout)
        {

            Document selector = new Document() { { "SessionId", id }, { "ApplicationName", applicationName } };
            Document sessionUpdate = new Document() { { "$set", new Document() { { "Expires", DateTime.Now.AddMinutes(timeout) } } } };
            using (var mongo = new Mongo(connectionString))
            {
                mongo.Connect();
                mongo[applicationName]["sessions"].Update(sessionUpdate, selector, 0, false);
            }

        }

        public void EvictSession(Session session)
        {
            Document selector = new Document() { { "SessionId", session.SessionID }, { "ApplicationName", session.ApplicationName }, { "LockId", session.LockID } };

            using (var mongo = new Mongo(connectionString))
            {
                mongo.Connect();
                mongo[applicationName]["sessions"].Remove(selector);
            }


        }

        public void EvictSession(string id, string applicationName, object lockId)
        {
            Document selector = new Document() { { "SessionId", id }, { "ApplicationName", applicationName }, { "LockId", lockId } };

            using (var mongo = new Mongo(connectionString))
            {
                mongo.Connect();
                mongo[applicationName]["sessions"].Remove(selector);
            }


        }

        public void EvictExpiredSession(string id, string applicationName)
        {
            Document selector = new Document() { { "SessionId", id }, { "ApplicationName", applicationName },
            {"Expires",new Document(){{"$lt",DateTime.Now}} }};

            using (var mongo = new Mongo(connectionString))
            {
                mongo.Connect();
                mongo[applicationName]["sessions"].Remove(selector);
            }


        }

        public void LockSession(Session session)
        {
            Document selector = new Document() { { "SessionId", session.SessionID }, { "ApplicationName", session.ApplicationName } };
            Document sessionLock = new Document() { { "$set", new Document() {{"LockDate", DateTime.Now }, 
            {"LockId", session.LockID }, {"Locked", true }, {"Flags",0} } } };

            using (var mongo = new Mongo(connectionString))
            {
                mongo.Connect();
                mongo[applicationName]["sessions"].Update(sessionLock, selector, 0, false);
            }


        }

        public void ReleaseLock(string id, string applicationName, object lockId, double timeout)
        {
            Document selector = new Document() { { "SessionId", id }, { "ApplicationName", applicationName }, { "LockId", lockId } };
            Document sessionLock = new Document() { { "$set", new Document() { { "Expires", DateTime.Now.AddMinutes(timeout) }, { "Locked", false } } } };

            using (var mongo = new Mongo(connectionString))
            {
                mongo.Connect();
                mongo[applicationName]["sessions"].Update(sessionLock, selector, 0, false);
            }

        }
    }
}

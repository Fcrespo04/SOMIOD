using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using MiddleWare.Models;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Net;

namespace MiddleWare.Helpers
{
    public static class BD_Access
    {
        // Connection string do Web.config
        private static readonly string connectionString = ConfigurationManager.ConnectionStrings["SomiodConnStr"].ConnectionString;

        // ==================================================================================
        //                                 APPLICATION 
        // ==================================================================================

        public static bool CreateApplication(Application app)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    app.Name = GetUniqueName(conn, "application", app.Name, null);

                    if (string.IsNullOrEmpty(app.CreationDate))
                        app.CreationDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

                    var cmd = new SqlCommand("INSERT INTO application ([resource-name], [creation-datetime]) VALUES (@name, @date)", conn);
                    cmd.Parameters.AddWithValue("@name", app.Name);
                    cmd.Parameters.AddWithValue("@date", DateTime.Parse(app.CreationDate));

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception) { return false; }
        }

        public static Application GetApplication(string name)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT * FROM application WHERE [resource-name]=@name", conn);
                cmd.Parameters.AddWithValue("@name", name);

                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return new Application
                    {
                        Id = (int)r["id"],
                        Name = (string)r["resource-name"],
                        CreationDate = ((DateTime)r["creation-datetime"]).ToString("yyyy-MM-ddTHH:mm:ss")
                    };
                }
            }
        }

        public static Application UpdateApplication(string oldName, string newName)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("UPDATE application SET [resource-name]=@new WHERE [resource-name]=@old", conn);
                cmd.Parameters.AddWithValue("@new", newName);
                cmd.Parameters.AddWithValue("@old", oldName);

                if (cmd.ExecuteNonQuery() > 0) return GetApplication(newName);
                return null;
            }
        }

        public static bool DeleteApplication(string name)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("DELETE FROM application WHERE [resource-name]=@name", conn);
                cmd.Parameters.AddWithValue("@name", name);
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        // ==================================================================================
        //                                 CONTAINER
        // ==================================================================================

        public static bool CreateContainer(string appName, Container container)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    int? parentId = GetResourceId(conn, "application", appName, null);
                    if (parentId == null) return false;

                    container.Name = GetUniqueName(conn, "container", container.Name, parentId);
                    if (string.IsNullOrEmpty(container.CreationDate))
                        container.CreationDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

                    var cmd = new SqlCommand("INSERT INTO container ([resource-name], [parent], [creation-datetime]) VALUES (@name, @parent, @date)", conn);
                    cmd.Parameters.AddWithValue("@name", container.Name);
                    cmd.Parameters.AddWithValue("@parent", parentId);
                    cmd.Parameters.AddWithValue("@date", DateTime.Parse(container.CreationDate));

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception) { return false; }
        }

        public static Container GetContainer(string appName, string contName)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var sql = "SELECT c.* FROM container c JOIN application a ON c.parent=a.id WHERE a.[resource-name]=@app AND c.[resource-name]=@cont";
                var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@app", appName);
                cmd.Parameters.AddWithValue("@cont", contName);

                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return new Container
                    {
                        Id = (int)r["id"],
                        Name = (string)r["resource-name"],
                        ParentId = (int)r["parent"],
                        CreationDate = ((DateTime)r["creation-datetime"]).ToString("yyyy-MM-ddTHH:mm:ss")
                    };
                }
            }
        }

        public static Container UpdateContainer(string appName, string oldName, string newName)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand(
                    "UPDATE container SET [resource-name]=@new " +
                    "WHERE [resource-name]=@old AND [parent]=(SELECT id FROM application WHERE [resource-name]=@app)", conn);
                cmd.Parameters.AddWithValue("@new", newName);
                cmd.Parameters.AddWithValue("@old", oldName);
                cmd.Parameters.AddWithValue("@app", appName);

                if (cmd.ExecuteNonQuery() > 0) return GetContainer(appName, newName);
                return null;
            }
        }

        public static bool DeleteContainer(string appName, string contName)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand(
                    "DELETE FROM container WHERE [resource-name]=@name AND [parent]=(SELECT id FROM application WHERE [resource-name]=@app)", conn);
                cmd.Parameters.AddWithValue("@name", contName);
                cmd.Parameters.AddWithValue("@app", appName);
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        // ==================================================================================
        //                              CONTENT-INSTANCE 
        // ==================================================================================

        public static bool CreateContentInstance(string appName, string contName, ContentInstance ci)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    int? appId = GetResourceId(conn, "application", appName, null);
                    if (appId == null) return false;
                    int? contId = GetResourceId(conn, "container", contName, appId);
                    if (contId == null) return false;

                    ci.Name = GetUniqueName(conn, "content-instance", ci.Name, contId);
                    if (string.IsNullOrEmpty(ci.CreationDate))
                        ci.CreationDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

                    var cmd = new SqlCommand(
                        "INSERT INTO [content-instance] ([resource-name], [content], [content-type], [parent], [creation-datetime]) " +
                        "VALUES (@name, @content, @ctype, @parent, @date)", conn);
                    cmd.Parameters.AddWithValue("@name", ci.Name);
                    cmd.Parameters.AddWithValue("@content", ci.Content);
                    cmd.Parameters.AddWithValue("@ctype", ci.ContentType);
                    cmd.Parameters.AddWithValue("@parent", contId);
                    cmd.Parameters.AddWithValue("@date", DateTime.Parse(ci.CreationDate));

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception) { return false; }
        }

        public static ContentInstance GetContentInstance(string appName, string contName, string ciName)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var sql = "SELECT ci.* FROM [content-instance] ci " +
                          "JOIN container c ON ci.parent=c.id " +
                          "JOIN application a ON c.parent=a.id " +
                          "WHERE a.[resource-name]=@app AND c.[resource-name]=@cont AND ci.[resource-name]=@ci";

                var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@app", appName);
                cmd.Parameters.AddWithValue("@cont", contName);
                cmd.Parameters.AddWithValue("@ci", ciName);

                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return new ContentInstance
                    {
                        Id = (int)r["id"],
                        Name = (string)r["resource-name"],
                        Content = (string)r["content"],
                        ContentType = (string)r["content-type"],
                        ParentId = (int)r["parent"],
                        CreationDate = ((DateTime)r["creation-datetime"]).ToString("yyyy-MM-ddTHH:mm:ss")
                    };
                }
            }
        }

        public static bool DeleteContentInstance(string appName, string contName, string ciName)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand(
                    "DELETE FROM [content-instance] WHERE [resource-name]=@name AND [parent]=(" +
                    "SELECT id FROM container WHERE [resource-name]=@cont AND [parent]=(" +
                    "SELECT id FROM application WHERE [resource-name]=@app))", conn);

                cmd.Parameters.AddWithValue("@name", ciName);
                cmd.Parameters.AddWithValue("@cont", contName);
                cmd.Parameters.AddWithValue("@app", appName);

                return cmd.ExecuteNonQuery() > 0;
            }
        }

        // ==================================================================================
        //                                SUBSCRIPTION 
        // ==================================================================================

        public static bool CreateSubscription(string appName, string contName, Subscription sub)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    int? appId = GetResourceId(conn, "application", appName, null);
                    if (appId == null) return false;
                    int? contId = GetResourceId(conn, "container", contName, appId);
                    if (contId == null) return false;

                    sub.Name = GetUniqueName(conn, "subscription", sub.Name, contId);
                    if (string.IsNullOrEmpty(sub.CreationDate))
                        sub.CreationDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

                    var cmd = new SqlCommand(
                        "INSERT INTO subscription ([resource-name], [evt], [endpoint], [parent], [creation-datetime]) " +
                        "VALUES (@name, @evt, @endpoint, @parent, @date)", conn);
                    cmd.Parameters.AddWithValue("@name", sub.Name);
                    cmd.Parameters.AddWithValue("@evt", sub.Event);
                    cmd.Parameters.AddWithValue("@endpoint", sub.Endpoint);
                    cmd.Parameters.AddWithValue("@parent", contId);
                    cmd.Parameters.AddWithValue("@date", DateTime.Parse(sub.CreationDate));

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception) { return false; }
        }

        public static Subscription GetSubscription(string appName, string contName, string subName)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var sql = "SELECT s.* FROM subscription s " +
                          "JOIN container c ON s.parent=c.id " +
                          "JOIN application a ON c.parent=a.id " +
                          "WHERE a.[resource-name]=@app AND c.[resource-name]=@cont AND s.[resource-name]=@sub";

                var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@app", appName);
                cmd.Parameters.AddWithValue("@cont", contName);
                cmd.Parameters.AddWithValue("@sub", subName);

                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return new Subscription
                    {
                        Id = (int)r["id"],
                        Name = (string)r["resource-name"],
                        Event = (int)r["evt"],
                        Endpoint = (string)r["endpoint"],
                        ParentId = (int)r["parent"],
                        CreationDate = ((DateTime)r["creation-datetime"]).ToString("yyyy-MM-ddTHH:mm:ss")
                    };
                }
            }
        }

        public static bool DeleteSubscription(string appName, string contName, string subName)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand(
                    "DELETE FROM subscription WHERE [resource-name]=@name AND [parent]=(" +
                    "SELECT id FROM container WHERE [resource-name]=@cont AND [parent]=(" +
                    "SELECT id FROM application WHERE [resource-name]=@app))", conn);

                cmd.Parameters.AddWithValue("@name", subName);
                cmd.Parameters.AddWithValue("@cont", contName);
                cmd.Parameters.AddWithValue("@app", appName);

                return cmd.ExecuteNonQuery() > 0;
            }
        }

        // ==================================================================================
        //                                DISCOVERY
        // ==================================================================================

        public static List<string> DiscoverApplications()
        {
            var list = new List<string>();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT [resource-name] FROM application", conn);
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) list.Add($"/api/somiod/{r["resource-name"]}");
            }
            return list;
        }

        public static List<string> DiscoverContainers(string appName = null)
        {
            var list = new List<string>();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT a.[resource-name] as app, c.[resource-name] as cont FROM container c JOIN application a ON c.parent=a.id";
                if (appName != null) sql += " WHERE a.[resource-name]=@app";

                var cmd = new SqlCommand(sql, conn);
                if (appName != null) cmd.Parameters.AddWithValue("@app", appName);

                using (var r = cmd.ExecuteReader())
                    while (r.Read()) list.Add($"/api/somiod/{r["app"]}/{r["cont"]}");
            }
            return list;
        }

        public static List<string> DiscoverContentInstances(string appName = null, string contName = null)
        {
            var list = new List<string>();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT a.[resource-name] as app, c.[resource-name] as cont, ci.[resource-name] as ci " +
                             "FROM [content-instance] ci JOIN container c ON ci.parent=c.id JOIN application a ON c.parent=a.id";

                if (appName != null && contName != null) sql += " WHERE a.[resource-name]=@app AND c.[resource-name]=@cont";
                else if (appName != null) sql += " WHERE a.[resource-name]=@app";

                var cmd = new SqlCommand(sql, conn);
                if (appName != null) cmd.Parameters.AddWithValue("@app", appName);
                if (contName != null) cmd.Parameters.AddWithValue("@cont", contName);

                using (var r = cmd.ExecuteReader())
                    while (r.Read()) list.Add($"/api/somiod/{r["app"]}/{r["cont"]}/{r["ci"]}");
            }
            return list;
        }

        public static List<string> DiscoverSubscriptions(string appName = null, string contName = null)
        {
            var list = new List<string>();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT a.[resource-name] as app, c.[resource-name] as cont, s.[resource-name] as sub " +
                             "FROM subscription s JOIN container c ON s.parent=c.id JOIN application a ON c.parent=a.id";

                if (appName != null && contName != null) sql += " WHERE a.[resource-name]=@app AND c.[resource-name]=@cont";
                else if (appName != null) sql += " WHERE a.[resource-name]=@app";

                var cmd = new SqlCommand(sql, conn);
                if (appName != null) cmd.Parameters.AddWithValue("@app", appName);
                if (contName != null) cmd.Parameters.AddWithValue("@cont", contName);

                using (var r = cmd.ExecuteReader())
                    while (r.Read()) list.Add($"/api/somiod/{r["app"]}/{r["cont"]}/subs/{r["sub"]}");
            }
            return list;
        }

        // ==================================================================================
        //                       NOTIFICAÇÕES (MQTT - O MOTOR)
        // ==================================================================================

        public static void SendNotifications(int containerId, int evtType, object resourceData)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var sql = "SELECT endpoint FROM subscription WHERE parent=@pid AND (evt=@type OR evt=0)";
                var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@pid", containerId);
                cmd.Parameters.AddWithValue("@type", evtType);

                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        string endpointUrl = (string)r["endpoint"];

                        if (endpointUrl.ToLower().StartsWith("mqtt://"))
                        {
                            try
                            {
                                string clean = endpointUrl.Substring(7);
                                int barra = clean.IndexOf('/');

                                if (barra > 0)
                                {
                                    string ipString = clean.Substring(0, barra);
                                    string topico = clean.Substring(barra + 1);

                                    // FIX CRÍTICO: Se a BD diz "localhost", forçamos "127.0.0.1" para a API IPv4
                                    if (ipString.ToLower() == "localhost") ipString = "127.0.0.1";

                                    string payload = (resourceData is ContentInstance ci) ? ci.Content : "Evento " + evtType;

                                    MqttClient client = new MqttClient(IPAddress.Parse(ipString));
                                    client.Connect(Guid.NewGuid().ToString());

                                    if (client.IsConnected)
                                    {
                                        // FIX CRÍTICO: QoS 0 para rapidez
                                        client.Publish(topico,
                                            System.Text.Encoding.UTF8.GetBytes(payload),
                                            MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE,
                                            false);

                                        // FIX CRÍTICO: Sleep para dar tempo à mensagem de sair
                                        System.Threading.Thread.Sleep(500);

                                        client.Disconnect();
                                    }
                                }
                            }
                            catch (Exception) { /* Ignora erros de envio */ }
                        }
                    }
                }
            }
        }

        // ==================================================================================
        //                                HELPERS PRIVADOS
        // ==================================================================================

        public static int? GetResourceId(SqlConnection conn, string table, string name, int? parentId)
        {
            string sql = parentId == null
                ? $"SELECT id FROM [{table}] WHERE [resource-name]=@name"
                : $"SELECT id FROM [{table}] WHERE [resource-name]=@name AND parent=@pid";

            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@name", name);
                if (parentId != null) cmd.Parameters.AddWithValue("@pid", parentId);
                var res = cmd.ExecuteScalar();
                return res != null ? (int?)res : null;
            }
        }

        public static int? GetResourceId(string table, string name, int? parentId)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                return GetResourceId(conn, table, name, parentId);
            }
        }

        private static string GetUniqueName(SqlConnection conn, string tableName, string baseName, int? parentId)
        {
            string checkSql = parentId == null
                ? $"SELECT COUNT(1) FROM [{tableName}] WHERE [resource-name]=@name"
                : $"SELECT COUNT(1) FROM [{tableName}] WHERE [resource-name]=@name AND parent=@pid";

            using (var cmd = new SqlCommand(checkSql, conn))
            {
                cmd.Parameters.AddWithValue("@name", baseName);
                if (parentId != null) cmd.Parameters.AddWithValue("@pid", parentId);
                int count = (int)cmd.ExecuteScalar();
                if (count == 0) return baseName;
            }

            string idSql = $"SELECT IDENT_CURRENT('{tableName}')";
            using (var cmd = new SqlCommand(idSql, conn))
            {
                object result = cmd.ExecuteScalar();
                int nextId = (result != DBNull.Value) ? Convert.ToInt32(result) + 1 : 1;
                return $"{baseName}_{nextId}";
            }
        }
    }
}
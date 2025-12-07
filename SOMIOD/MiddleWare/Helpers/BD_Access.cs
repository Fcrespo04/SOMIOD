using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using MiddleWare.Models;

namespace MiddleWare.Helpers
{
    public static class BD_Access
    {
        // Connection string to access DB
        private static readonly string connectionString = Properties.Settings.Default.ConnStr;

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
                    // Unique name check
                    app.Name = GetUniqueName(conn, "application", app.Name, null);

                    // if CreationDate not provided, set to now
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
                // Nota: Update direto pode falhar se newName já existir. 
                // Se quiseres renomeação automática no update, terias de usar GetUniqueName aqui também.
                var cmd = new SqlCommand("UPDATE application SET [resource-name]=@new WHERE [resource-name]=@old", conn);
                cmd.Parameters.AddWithValue("@new", newName);
                cmd.Parameters.AddWithValue("@old", oldName);

                if (cmd.ExecuteNonQuery() > 0)
                {
                    return GetApplication(newName);
                }
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

        public static List<string> DiscoverApplications()
        {
            var list = new List<string>();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT [resource-name] FROM application", conn);
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read()) list.Add($"/api/somiod/{r["resource-name"]}");
                }
            }
            return list;
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

        public static List<string> DiscoverContainers(string appName)
        {
            var list = new List<string>();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var sql = "SELECT c.[resource-name] FROM container c JOIN application a ON c.parent=a.id WHERE a.[resource-name]=@app";
                var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@app", appName);
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read()) list.Add($"/api/somiod/{appName}/{r["resource-name"]}");
                }
            }
            return list;
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
                    // Obter IDs dos pais
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

        public static List<string> DiscoverContentInstances(string appName, string contName = null)
        {
            var list = new List<string>();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql;
                SqlCommand cmd;

                if (contName == null) // Discovery na App (Recursivo)
                {
                    sql = "SELECT c.[resource-name] as cont, ci.[resource-name] as ci FROM [content-instance] ci " +
                          "JOIN container c ON ci.parent=c.id JOIN application a ON c.parent=a.id WHERE a.[resource-name]=@app";
                    cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@app", appName);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) list.Add($"/api/somiod/{appName}/{r["cont"]}/{r["ci"]}");
                }
                else // Discovery no Container
                {
                    sql = "SELECT ci.[resource-name] as ci FROM [content-instance] ci " +
                          "JOIN container c ON ci.parent=c.id JOIN application a ON c.parent=a.id " +
                          "WHERE a.[resource-name]=@app AND c.[resource-name]=@cont";
                    cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@app", appName);
                    cmd.Parameters.AddWithValue("@cont", contName);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) list.Add($"/api/somiod/{appName}/{contName}/{r["ci"]}");
                }
            }
            return list;
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

        public static List<string> DiscoverSubscriptions(string appName, string contName = null)
        {
            var list = new List<string>();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql;
                SqlCommand cmd;

                if (contName == null) // Discovery na App
                {
                    sql = "SELECT c.[resource-name] as cont, s.[resource-name] as sub FROM subscription s " +
                          "JOIN container c ON s.parent=c.id JOIN application a ON c.parent=a.id WHERE a.[resource-name]=@app";
                    cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@app", appName);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) list.Add($"/api/somiod/{appName}/{r["cont"]}/subs/{r["sub"]}");
                }
                else // Discovery no Container
                {
                    sql = "SELECT s.[resource-name] as sub FROM subscription s " +
                          "JOIN container c ON s.parent=c.id JOIN application a ON c.parent=a.id " +
                          "WHERE a.[resource-name]=@app AND c.[resource-name]=@cont";
                    cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@app", appName);
                    cmd.Parameters.AddWithValue("@cont", contName);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) list.Add($"/api/somiod/{appName}/{contName}/subs/{r["sub"]}");
                }
            }
            return list;
        }

        // ==================================================================================
        //                                  HELPERS 
        // ==================================================================================

        // Method to get resource ID by name and optional parent ID
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

        // Method used to get resource ID without existing connection
        public static int? GetResourceId(string table, string name, int? parentId)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                return GetResourceId(conn, table, name, parentId);
            }
        }

        // Method to generate a unique resource name within its parent
        private static string GetUniqueName(SqlConnection conn, string tableName, string baseName, int? parentId)
        {
            // verify if the baseName already exists
            string checkSql = parentId == null
                ? $"SELECT COUNT(1) FROM [{tableName}] WHERE [resource-name]=@name"
                : $"SELECT COUNT(1) FROM [{tableName}] WHERE [resource-name]=@name AND parent=@pid";

            using (var cmd = new SqlCommand(checkSql, conn))
            {
                cmd.Parameters.AddWithValue("@name", baseName);
                if (parentId != null) cmd.Parameters.AddWithValue("@pid", parentId);

                int count = (int)cmd.ExecuteScalar();

                // If not exists, return baseName directly
                if (count == 0) return baseName;
            }

            // If exists, get the next ID using IDENT_CURRENT
            string idSql = $"SELECT IDENT_CURRENT('{tableName}')";

            using (var cmd = new SqlCommand(idSql, conn))
            {
                object result = cmd.ExecuteScalar();

                int nextId = (result != DBNull.Value) ? Convert.ToInt32(result) + 1 : 1;

                // Return the new unique name with the next ID appended as suffix
                return $"{baseName}_{nextId}";
            }
        }

        // method to get subscription endpoints for notifications
        public static List<string> GetSubscriptionEndpoints(string appName, string contName, int evtType)
        {
            var list = new List<string>();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                // Busca o ID do container primeiro
                int? appId = GetResourceId(conn, "application", appName, null);
                if (appId == null) return list;
                int? contId = GetResourceId(conn, "container", contName, appId);
                if (contId == null) return list;

                // Busca endpoints interessados
                // evt = 1 (Create), 2 (Delete). Se a subscrição tiver evt=0 (ambos), também apanha.
                var sql = "SELECT endpoint FROM subscription WHERE parent=@pid AND (evt=@type OR evt=0)";
                var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@pid", contId);
                cmd.Parameters.AddWithValue("@type", evtType);

                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read()) list.Add((string)r["endpoint"]);
                }
            }
            return list;
        }

        // Metohod to send notifications to subscribers
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
                        string endpoint = (string)r["endpoint"];
                        // TODO: Implementar lógica de envio (MQTT/HTTP)
                        System.Diagnostics.Debug.WriteLine($"[NOTIFY] Enviar {resourceData} para {endpoint} (Evento {evtType})");
                    }
                }
            }
        }
    }
}
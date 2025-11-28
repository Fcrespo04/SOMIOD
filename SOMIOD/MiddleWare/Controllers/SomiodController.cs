using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Web.Http;
using Newtonsoft.Json.Linq;

namespace MiddleWare.Controllers
{
    [RoutePrefix("api/somiod")]
    public class SomiodController : ApiController
    {
        private readonly string connectionString =
            ConfigurationManager.ConnectionStrings["SomiodConnStr"].ConnectionString;

        // =====================================================================
        // APPLICATION
        // =====================================================================

        [HttpPost]
        [Route("")]
        public IHttpActionResult CreateApplication([FromBody] JObject body)
        {
            // 1. Validar res-type
            string resType = body?["res-type"]?.ToString();
            if (resType != "application") return BadRequest("Invalid res-type. Expected 'application'.");

            string baseName = body?["resource-name"]?.ToString() ?? "app";

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // 2. Lógica de Unicidade (Renomeação Automática)
                    string finalName = GetUniqueAppName(conn, baseName);

                    var cmd = new SqlCommand("INSERT INTO application ([resource-name], [creation-datetime]) VALUES (@name, @date)", conn);
                    cmd.Parameters.AddWithValue("@name", finalName);
                    cmd.Parameters.AddWithValue("@date", DateTime.Now);
                    cmd.ExecuteNonQuery();

                    return Created($"/api/somiod/{finalName}", FetchApplication(conn, finalName));
                }
            }
            catch (Exception ex) { return InternalServerError(ex); }
        }

        [HttpGet]
        [Route("{appName}")]
        public IHttpActionResult GetApplication(string appName)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Discovery Logic
                    if (Request.Headers.Contains("somiod-discovery"))
                    {
                        var type = Request.Headers.GetValues("somiod-discovery").FirstOrDefault();
                        switch (type?.ToLower())
                        {
                            case "application": return Ok(new List<string> { $"/api/somiod/{appName}" });
                            case "container": return Ok(DiscoverContainers(conn, appName));
                            case "content-instance": return Ok(DiscoverContentInstances(conn, appName));
                            case "subscription": return Ok(DiscoverSubscriptions(conn, appName));
                            default: return BadRequest("Invalid discovery type");
                        }
                    }

                    var obj = FetchApplication(conn, appName);
                    return obj != null ? Ok(obj) : (IHttpActionResult)NotFound();
                }
            }
            catch (Exception ex) { return InternalServerError(ex); }
        }

        [HttpPut]
        [Route("{appName}")]
        public IHttpActionResult UpdateApplication(string appName, [FromBody] JObject body)
        {
            string newName = body?["resource-name"]?.ToString();
            if (string.IsNullOrWhiteSpace(newName)) return BadRequest("Missing resource-name");

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    // Nota: Aqui não implementei renomeação automática pois é um update explícito,
                    // mas podes adicionar se quiseres.
                    var cmd = new SqlCommand("UPDATE application SET [resource-name]=@new WHERE [resource-name]=@old", conn);
                    cmd.Parameters.AddWithValue("@new", newName);
                    cmd.Parameters.AddWithValue("@old", appName);

                    int rows = cmd.ExecuteNonQuery();
                    return rows > 0 ? Ok(FetchApplication(conn, newName)) : (IHttpActionResult)NotFound();
                }
            }
            catch (SqlException ex) when (ex.Number == 2627) { return Conflict(); }
            catch (Exception ex) { return InternalServerError(ex); }
        }

        [HttpDelete]
        [Route("{appName}")]
        public IHttpActionResult DeleteApplication(string appName)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand("DELETE FROM application WHERE [resource-name]=@name", conn);
                    cmd.Parameters.AddWithValue("@name", appName);
                    int rows = cmd.ExecuteNonQuery();
                    return rows > 0 ? StatusCode(HttpStatusCode.NoContent) : (IHttpActionResult)NotFound();
                }
            }
            catch (Exception ex) { return InternalServerError(ex); }
        }

        // =====================================================================
        // CONTAINER
        // =====================================================================

        [HttpPost]
        [Route("{appName}")]
        public IHttpActionResult CreateContainer(string appName, [FromBody] JObject body)
        {
            string resType = body?["res-type"]?.ToString();
            if (resType != "container") return BadRequest("Invalid res-type. Expected 'container'.");

            string baseName = body?["resource-name"]?.ToString() ?? "container";

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    int? parentId = GetAppId(conn, appName);
                    if (parentId == null) return NotFound();

                    string finalName = GetUniqueChildName(conn, "container", baseName, parentId.Value);

                    var cmd = new SqlCommand(
                        "INSERT INTO container ([resource-name], [parent], [creation-datetime]) VALUES (@name, @parent, @date)", conn);
                    cmd.Parameters.AddWithValue("@name", finalName);
                    cmd.Parameters.AddWithValue("@parent", parentId);
                    cmd.Parameters.AddWithValue("@date", DateTime.Now);
                    cmd.ExecuteNonQuery();

                    return Created($"/api/somiod/{appName}/{finalName}", FetchContainer(conn, appName, finalName));
                }
            }
            catch (Exception ex) { return InternalServerError(ex); }
        }

        [HttpGet]
        [Route("{appName}/{contName}")]
        public IHttpActionResult GetContainer(string appName, string contName)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    if (Request.Headers.Contains("somiod-discovery"))
                    {
                        var type = Request.Headers.GetValues("somiod-discovery").FirstOrDefault();
                        switch (type?.ToLower())
                        {
                            case "container": return Ok(new List<string> { $"/api/somiod/{appName}/{contName}" });
                            case "content-instance": return Ok(DiscoverContentInstances(conn, appName, contName));
                            case "subscription": return Ok(DiscoverSubscriptions(conn, appName, contName));
                            default: return BadRequest("Invalid discovery type");
                        }
                    }

                    var obj = FetchContainer(conn, appName, contName);
                    return obj != null ? Ok(obj) : (IHttpActionResult)NotFound();
                }
            }
            catch (Exception ex) { return InternalServerError(ex); }
        }

        [HttpDelete]
        [Route("{appName}/{contName}")]
        public IHttpActionResult DeleteContainer(string appName, string contName)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand(
                        "DELETE FROM container WHERE [resource-name]=@name AND [parent]=(SELECT id FROM application WHERE [resource-name]=@app)", conn);
                    cmd.Parameters.AddWithValue("@name", contName);
                    cmd.Parameters.AddWithValue("@app", appName);
                    int rows = cmd.ExecuteNonQuery();
                    return rows > 0 ? StatusCode(HttpStatusCode.NoContent) : (IHttpActionResult)NotFound();
                }
            }
            catch (Exception ex) { return InternalServerError(ex); }
        }

        // =====================================================================
        // CONTENT-INSTANCE
        // =====================================================================

        [HttpPost]
        [Route("{appName}/{contName}")]
        public IHttpActionResult CreateContentInstance(string appName, string contName, [FromBody] JObject body)
        {
            // O mesmo endpoint serve para Content-Instance e Subscription. Distinguir pelo res-type.
            string resType = body?["res-type"]?.ToString();

            if (resType == "subscription")
                return CreateSubscriptionInternal(appName, contName, body); // Redireciona para lógica de Subscrição

            if (resType != "content-instance")
                return BadRequest("Invalid res-type. Expected 'content-instance' or 'subscription'.");

            string baseName = body?["resource-name"]?.ToString() ?? "data";
            string contentType = body?["content-type"]?.ToString();
            string content = body?["content"]?.ToString();

            if (string.IsNullOrWhiteSpace(contentType) || string.IsNullOrWhiteSpace(content))
                return BadRequest("Missing content-type or content");

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    int? parentId = GetContainerId(conn, appName, contName);
                    if (parentId == null) return NotFound();

                    string finalName = GetUniqueChildName(conn, "content-instance", baseName, parentId.Value);

                    var cmd = new SqlCommand(
                        "INSERT INTO [content-instance] ([resource-name], [content-type], [content], [parent], [creation-datetime]) " +
                        "VALUES (@name, @ctype, @content, @parent, @date)", conn);
                    cmd.Parameters.AddWithValue("@name", finalName);
                    cmd.Parameters.AddWithValue("@ctype", contentType);
                    cmd.Parameters.AddWithValue("@content", content);
                    cmd.Parameters.AddWithValue("@parent", parentId);
                    cmd.Parameters.AddWithValue("@date", DateTime.Now);
                    cmd.ExecuteNonQuery();

                    var obj = FetchContentInstance(conn, appName, contName, finalName);

                    // !!! NOTIFICAÇÃO (Evento 1 = Criação) !!!
                    SendNotifications(conn, parentId.Value, 1, obj);

                    return Created($"/api/somiod/{appName}/{contName}/{finalName}", obj);
                }
            }
            catch (Exception ex) { return InternalServerError(ex); }
        }

        [HttpGet]
        [Route("{appName}/{contName}/{ciName}")]
        public IHttpActionResult GetContentInstance(string appName, string contName, string ciName)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    var obj = FetchContentInstance(conn, appName, contName, ciName);
                    return obj != null ? Ok(obj) : (IHttpActionResult)NotFound();
                }
            }
            catch (Exception ex) { return InternalServerError(ex); }
        }

        [HttpDelete]
        [Route("{appName}/{contName}/{ciName}")]
        public IHttpActionResult DeleteContentInstance(string appName, string contName, string ciName)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    int? parentId = GetContainerId(conn, appName, contName);
                    if (parentId == null) return NotFound();

                    // Obter objeto antes de apagar para enviar na notificação
                    var objToDelete = FetchContentInstance(conn, appName, contName, ciName);
                    if (objToDelete == null) return NotFound();

                    var cmd = new SqlCommand(
                        "DELETE FROM [content-instance] WHERE [resource-name]=@name AND [parent]=@pid", conn);
                    cmd.Parameters.AddWithValue("@name", ciName);
                    cmd.Parameters.AddWithValue("@pid", parentId);
                    int rows = cmd.ExecuteNonQuery();

                    if (rows > 0)
                    {
                        // !!! NOTIFICAÇÃO (Evento 2 = Eliminação) !!!
                        SendNotifications(conn, parentId.Value, 2, objToDelete);
                        return StatusCode(HttpStatusCode.NoContent);
                    }
                    return NotFound();
                }
            }
            catch (Exception ex) { return InternalServerError(ex); }
        }

        // =====================================================================
        // SUBSCRIPTION
        // =====================================================================

        // Método interno chamado pelo POST do Container
        private IHttpActionResult CreateSubscriptionInternal(string appName, string contName, JObject body)
        {
            string baseName = body?["resource-name"]?.ToString() ?? "sub";
            int evt = ParseEvt(body?["evt"]?.ToString());
            string endpoint = body?["endpoint"]?.ToString();

            if (string.IsNullOrWhiteSpace(endpoint)) return BadRequest("Missing endpoint");

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    int? parentId = GetContainerId(conn, appName, contName);
                    if (parentId == null) return NotFound();

                    string finalName = GetUniqueChildName(conn, "subscription", baseName, parentId.Value);

                    var cmd = new SqlCommand(
                        "INSERT INTO subscription ([resource-name], [evt], [endpoint], [parent], [creation-datetime]) " +
                        "VALUES (@name, @evt, @endpoint, @parent, @date)", conn);
                    cmd.Parameters.AddWithValue("@name", finalName);
                    cmd.Parameters.AddWithValue("@evt", evt);
                    cmd.Parameters.AddWithValue("@endpoint", endpoint);
                    cmd.Parameters.AddWithValue("@parent", parentId);
                    cmd.Parameters.AddWithValue("@date", DateTime.Now);
                    cmd.ExecuteNonQuery();

                    return Created($"/api/somiod/{appName}/{contName}/subs/{finalName}",
                        FetchSubscription(conn, appName, contName, finalName));
                }
            }
            catch (Exception ex) { return InternalServerError(ex); }
        }

        // Rota com /subs/ para GET e DELETE
        [HttpGet]
        [Route("{appName}/{contName}/subs/{subName}")]
        public IHttpActionResult GetSubscription(string appName, string contName, string subName)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    var obj = FetchSubscription(conn, appName, contName, subName);
                    return obj != null ? Ok(obj) : (IHttpActionResult)NotFound();
                }
            }
            catch (Exception ex) { return InternalServerError(ex); }
        }

        [HttpDelete]
        [Route("{appName}/{contName}/subs/{subName}")]
        public IHttpActionResult DeleteSubscription(string appName, string contName, string subName)
        {
            try
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
                    int rows = cmd.ExecuteNonQuery();
                    return rows > 0 ? StatusCode(HttpStatusCode.NoContent) : (IHttpActionResult)NotFound();
                }
            }
            catch (Exception ex) { return InternalServerError(ex); }
        }

        // =====================================================================
        // DISCOVERY RAÍZ
        // =====================================================================

        [HttpGet]
        [Route("")]
        public IHttpActionResult DiscoverRoot()
        {
            if (!Request.Headers.Contains("somiod-discovery"))
                return Ok(new List<string>());

            var type = Request.Headers.GetValues("somiod-discovery").FirstOrDefault();

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    switch (type?.ToLower())
                    {
                        case "application": return Ok(DiscoverApplications(conn));
                        // Segundo a tabela do enunciado, descobrir container na raiz não faz muito sentido, 
                        // mas se quiseres implementar seria "todos os containers de todas as apps".
                        default: return BadRequest("Invalid discovery type for root");
                    }
                }
            }
            catch (Exception ex) { return InternalServerError(ex); }
        }

        // =====================================================================
        // HELPERS DE LÓGICA E DADOS
        // =====================================================================

        // Notificações
        private void SendNotifications(SqlConnection conn, int containerId, int eventType, object resourceData)
        {
            // 1. Buscar Subscrições interessadas no evento (1=Create, 2=Delete)
            var subs = new List<string>();
            var sql = "SELECT endpoint FROM subscription WHERE parent = @pid AND (evt = @type OR evt = 0)"; // assumindo 0 = ambos, ou ajusta conforme tua logica

            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@pid", containerId);
                cmd.Parameters.AddWithValue("@type", eventType);
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read()) subs.Add((string)r["endpoint"]);
                }
            }

            // 2. Enviar (Placeholder - Implementar Cliente HTTP/MQTT aqui)
            foreach (var endpoint in subs)
            {
                // TODO: Identificar protocolo (http:// ou mqtt://) e enviar 'resourceData' e 'eventType'
                System.Diagnostics.Debug.WriteLine($"NOTIFICAR: {endpoint} sobre Evento {eventType}");
                // Exemplo: MqttClient.Publish(topic, xmlData)...
            }
        }

        // Unicidade de Nomes (Renomeação Automática)
        private string GetUniqueAppName(SqlConnection conn, string baseName)
        {
            string newName = baseName;
            int counter = 1;
            while (true)
            {
                var cmd = new SqlCommand("SELECT COUNT(1) FROM application WHERE [resource-name]=@name", conn);
                cmd.Parameters.AddWithValue("@name", newName);
                if ((int)cmd.ExecuteScalar() == 0) return newName;

                newName = baseName + counter;
                counter++;
            }
        }

        private string GetUniqueChildName(SqlConnection conn, string tableName, string baseName, int parentId)
        {
            string newName = baseName;
            int counter = 1;
            // Validação de segurança básica para nome da tabela (evitar injeção, embora seja interno)
            if (tableName != "container" && tableName != "content-instance" && tableName != "subscription")
                throw new Exception("Invalid table");

            while (true)
            {
                var cmd = new SqlCommand($"SELECT COUNT(1) FROM [{tableName}] WHERE [resource-name]=@name AND parent=@pid", conn);
                cmd.Parameters.AddWithValue("@name", newName);
                cmd.Parameters.AddWithValue("@pid", parentId);
                if ((int)cmd.ExecuteScalar() == 0) return newName;

                newName = baseName + counter;
                counter++;
            }
        }

        // Helpers de ID
        private int? GetAppId(SqlConnection conn, string appName)
        {
            var cmd = new SqlCommand("SELECT id FROM application WHERE [resource-name]=@name", conn);
            cmd.Parameters.AddWithValue("@name", appName);
            var res = cmd.ExecuteScalar();
            return res != null ? (int?)res : null;
        }

        private int? GetContainerId(SqlConnection conn, string appName, string contName)
        {
            var cmd = new SqlCommand(
                "SELECT c.id FROM container c JOIN application a ON c.parent=a.id WHERE a.[resource-name]=@app AND c.[resource-name]=@cont", conn);
            cmd.Parameters.AddWithValue("@app", appName);
            cmd.Parameters.AddWithValue("@cont", contName);
            var res = cmd.ExecuteScalar();
            return res != null ? (int?)res : null;
        }

        private int ParseEvt(string evtStr)
        {
            if (int.TryParse(evtStr, out var evt) && (evt == 1 || evt == 2)) return evt;
            return 1;
        }

        // Helpers de Fetch (Retornam Objetos Anónimos para JSON/XML)
        // Nota: Garante que os nomes das propriedades no objeto anónimo batem certo com o XML Serializer se usares objetos tipados,
        // ou usa os Models que criámos anteriormente em vez de objetos anónimos aqui.
        // Abaixo uso a estrutura dinâmica baseada nos teus Models.

        private object FetchApplication(SqlConnection conn, string appName)
        {
            // Recomendo usar a classe Model Application aqui em vez de anónimo para garantir XML correto
            // Exemplo simplificado:
            var cmd = new SqlCommand("SELECT * FROM application WHERE [resource-name]=@name", conn);
            cmd.Parameters.AddWithValue("@name", appName);
            using (var r = cmd.ExecuteReader())
            {
                if (!r.Read()) return null;
                return new Models.Application
                {
                    Id = (int)r["id"],
                    Name = (string)r["resource-name"],
                    CreationDate = ((DateTime)r["creation-datetime"]).ToString("yyyy-MM-ddTHH:mm:ss")
                };
            }
        }

        // ... (Repetir lógica de Fetch usando os Models Container, ContentInstance, Subscription criados antes)
        // Vou abreviar os restantes Fetchs assumindo que segues o padrão acima.
        private object FetchContainer(SqlConnection conn, string appName, string contName) { /* Lógica similar ao FetchApplication */ return null; }
        private object FetchContentInstance(SqlConnection conn, string appName, string contName, string ciName) { /* ... */ return null; }
        private object FetchSubscription(SqlConnection conn, string appName, string contName, string subName) { /* ... */ return null; }

        // Helpers de Discovery (Listas de Strings)
        private List<string> DiscoverApplications(SqlConnection conn)
        {
            var list = new List<string>();
            using (var cmd = new SqlCommand("SELECT [resource-name] FROM application", conn))
            using (var r = cmd.ExecuteReader())
                while (r.Read()) list.Add($"/api/somiod/{r["resource-name"]}");
            return list;
        }

        private List<string> DiscoverContainers(SqlConnection conn, string appName = null)
        {
            var list = new List<string>();
            var sql = "SELECT a.[resource-name] as app, c.[resource-name] as cont FROM container c JOIN application a ON c.parent=a.id " +
                      (appName != null ? "WHERE a.[resource-name]=@app" : "");
            using (var cmd = new SqlCommand(sql, conn))
            {
                if (appName != null) cmd.Parameters.AddWithValue("@app", appName);
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) list.Add($"/api/somiod/{r["app"]}/{r["cont"]}");
            }
            return list;
        }

        private List<string> DiscoverContentInstances(SqlConnection conn, string appName = null, string contName = null)
        {
            // Lógica similar para content-instance
            return new List<string>(); // Placeholder
        }

        private List<string> DiscoverSubscriptions(SqlConnection conn, string appName = null, string contName = null)
        {
            // Lógica similar para subscription (com /subs/)
            return new List<string>(); // Placeholder
        }
    }
}
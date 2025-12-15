using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using Newtonsoft.Json.Linq;
using MiddleWare.Models;
using MiddleWare.Helpers;
using System.Data.SqlClient;

namespace MiddleWare.Controllers
{
    [RoutePrefix("api/somiod")]
    public class ContainerController : ApiController
    {
        // =====================================================================
        // GET CONTAINER ou DISCOVERY (de filhos) (GET api/somiod/{appName}/{contName})
        // =====================================================================
        [HttpGet]
        [Route("{appName}/{contName}")]
        public IHttpActionResult GetContainer(string appName, string contName)
        {
            if (Request.Headers.Contains("somiod-discovery"))
            {
                return ProcessContainerDiscovery(appName, contName);
            }
            return GetContainerDetails(appName, contName);
        }

        private IHttpActionResult ProcessContainerDiscovery(string appName, string contName)
        {
            var existingContainer = BD_Access.GetContainer(appName, contName);
            if (existingContainer == null) return NotFound();

            var type = Request.Headers.GetValues("somiod-discovery").FirstOrDefault()?.ToLower();
            switch (type)
            {
                case "container":
                    return Ok(new List<string> { $"/api/somiod/{appName}/{contName}" });
                case "content-instance":
                    return Ok(BD_Access.DiscoverContentInstances(appName, contName));
                case "subscription":
                    return Ok(BD_Access.DiscoverSubscriptions(appName, contName));
                default:
                    return BadRequest("Invalid discovery type.");
            }
        }

        private IHttpActionResult GetContainerDetails(string appName, string contName)
        {
            var cont = BD_Access.GetContainer(appName, contName);
            if (cont == null) return NotFound();
            return Ok(cont);
        }

        // =====================================================================
        // UPDATE CONTAINER (PUT api/somiod/{appName}/{contName})
        // =====================================================================
        [HttpPut]
        [Route("{appName}/{contName}")]
        public IHttpActionResult UpdateContainer(string appName, string contName, [FromBody] JObject body)
        {
            string resType = body?["res-type"]?.ToString();
            if (!string.IsNullOrEmpty(resType) && resType != "container")
                return Content(HttpStatusCode.BadRequest, "Invalid res-type. Expected 'container'.");

            string newName = body?["resource-name"]?.ToString();
            if (string.IsNullOrWhiteSpace(newName)) return BadRequest("Missing resource-name");

            try
            {
                var updatedContainer = BD_Access.UpdateContainer(appName, contName, newName);
                if (updatedContainer != null) return Ok(updatedContainer);
                return NotFound();
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627 || ex.Number == 2601)
                    return Content(HttpStatusCode.Conflict, $"The name '{newName}' is already used.");
                return InternalServerError(ex);
            }
            catch (Exception ex) { return InternalServerError(ex); }
        }

        // =====================================================================
        // DELETE CONTAINER (DELETE api/somiod/{appName}/{contName})
        // =====================================================================
        [HttpDelete]
        [Route("{appName}/{contName}")]
        public IHttpActionResult DeleteContainer(string appName, string contName)
        {
            try
            {
                var container = BD_Access.GetContainer(appName, contName);
                if (container == null) return NotFound();

                var deletedContentInstances = BD_Access.DiscoverContentInstances(appName, contName);
                var deletedSubscriptions = BD_Access.DiscoverSubscriptions(appName, contName);

                if (BD_Access.DeleteContainer(appName, contName))
                {
                    var report = new
                    {
                        message = $"Container '{contName}' and children deleted.",
                        deleted_container = contName,
                        deleted_children = new
                        {
                            content_instances = deletedContentInstances,
                            subscriptions = deletedSubscriptions
                        }
                    };
                    return Ok(report);
                }
                return NotFound();
            }
            catch (Exception ex) { return InternalServerError(ex); }
        }

        [HttpPost]
        [Route("{appName}/{contName}")]
        public IHttpActionResult CreateChild(string appName, string contName, [FromBody] JObject body)
        {
            string resType = body?["res-type"]?.ToString();

            try
            {
                // --- SE FOR UM COMANDO (Content-Instance) ---
                if (resType == "content-instance")
                {
                    string content = body["content"]?.ToString(); // ABRIR ou FECHAR

                    var newContent = new ContentInstance
                    {
                        Name = body["resource-name"]?.ToString() ?? "data",
                        Content = content,
                        ContentType = "application/json"
                    };

                    // 1. Grava na BD usando o BD_Access antigo (Seguro)
                    if (BD_Access.CreateContentInstance(appName, contName, newContent))
                    {
                        // 2. MAGIA AQUI: Em vez de usar BD_Access.SendNotification...
                        // Vamos nós mesmos buscar a lista de quem quer saber e enviamos!

                        // Busca endpoints interessados no evento 1 (Creation)
                        var endpoints = BD_Access.GetSubscriptionEndpoints(appName, contName, 1);

                        string topico = $"{appName}/{contName}"; // ex: prisao/cela1

                        foreach (var ep in endpoints)
                        {
                            // Usa o nosso novo Helper para enviar "ABRIR" ou "FECHAR"
                            MqttHelper.PublishToBroker(ep, topico, content);
                        }

                        return Ok(newContent);
                    }
                }
                // --- SE FOR UMA SUBSCRIÇÃO ---
                else if (resType == "subscription")
                {
                    // Lógica normal de criar subscrição
                    var newSub = new Subscription
                    {
                        Name = body["resource-name"]?.ToString() ?? "sub",
                        Endpoint = body["endpoint"]?.ToString(),
                        Event = int.Parse(body["evt"]?.ToString())
                    };

                    if (BD_Access.CreateSubscription(appName, contName, newSub))
                        return Ok(newSub);
                }

                return BadRequest("Erro ou tipo desconhecido");
            }
            catch (Exception ex) { return InternalServerError(ex); }
        }
    }
}

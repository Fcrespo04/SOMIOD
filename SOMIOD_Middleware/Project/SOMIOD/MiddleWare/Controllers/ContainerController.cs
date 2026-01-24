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

        /// <summary>
        /// Get container properties or discover its child resources (content-instances or subscriptions).
        /// </summary>
        /// <param name="appName">Parent application name.</param>
        /// <param name="contName">Target container name.</param>
        /// <response code="200">Returns container properties or a list of children paths.</response>
        /// <response code="404">Container not found.</response>
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

        /// <summary>
        /// Update a container's properties (e.g., rename it).
        /// </summary>
        /// <param name="appName">Parent application name.</param>
        /// <param name="contName">Current container name.</param>
        /// <param name="body">JSON object containing the new 'resource-name'.</param>
        /// <response code="200">OK: Returns the updated container object.</response>
        /// <response code="400">Bad Request: Missing new resource-name.</response>
        /// <response code="404">Not Found: Container or Application not found.</response>
        /// <response code="409">Conflict: New name already in use.</response>
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
        /// <summary>
        /// Delete a container and all its child resources (Content-Instances and Subscriptions).
        /// </summary>
        /// <param name="appName">Parent application name.</param>
        /// <param name="contName">Container name to delete.</param>
        /// <response code="204">No Content: Resource successfully deleted.</response>
        /// <response code="404">Not Found: Container does not exist.</response>
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

        /// <summary>
        /// Create a child resource (Content-Instance or Subscription) inside a container.
        /// </summary>
        /// <remarks>Supports 'content-instance' and 'subscription' resource types.</remarks>
        /// <param name="appName">Parent application name.</param>
        /// <param name="contName">Target container name.</param>
        /// <param name="body">JSON object with resource properties.</param>
        /// <response code="201">Resource created successfully and notifications triggered.</response>
        /// <response code="400">Invalid data or unsupported content-type.</response>
        [HttpPost]
        [Route("{appName}/{contName}")]
        public IHttpActionResult CreateChild(string appName, string contName, [FromBody] JObject body)
        {
            string resType = body?["res-type"]?.ToString();

            try
            {
                if (resType == "content-instance")
                {
                    string content = body["content"]?.ToString();
                    // Validar Content-Type
                    string contentType = body["content-type"]?.ToString()?.ToLower();

                    if (contentType != "application/json" &&
                        contentType != "application/xml" &&
                        contentType != "text/plain")
                    {
                        return BadRequest("Invalid content-type. Supported: 'application/json', 'application/xml', 'text/plain'.");
                    }

                    var newContent = new ContentInstance
                    {
                        Name = body["resource-name"]?.ToString() ?? "data",
                        Content = content,
                        ContentType = contentType
                    };

                    //Grava na BD
                    if (BD_Access.CreateContentInstance(appName, contName, newContent))
                    {
                        // Busca endpoints interessados no evento 1 (Creation)
                        var endpoints = BD_Access.GetSubscriptionEndpoints(appName, contName, 1);

                        // Tópico COMPLETO (Necessário para o Monitor ouvir)
                        string topico = $"{appName}/{contName}";

                        // Mensagem em XML (Necessário para passar no XSD do Monitor)
                        string xmlMessage = "<notification>" +
                                                "<event>CREATE</event>" +
                                                $"<resource>{content}</resource>" +
                                            "</notification>";

                        foreach (var ep in endpoints)
                        {
                            string endpointLower = ep.ToLower().Trim();

                            // Envio MQTT
                            if (endpointLower.StartsWith("mqtt://"))
                            {
                                MqttHelper.PublishToBroker(ep, topico, xmlMessage);
                            }
                            // Envio HTTP
                            else if (endpointLower.StartsWith("http://") || endpointLower.StartsWith("https://"))
                            {
                                _ = HttpHelper.SendPostRequest(ep, xmlMessage);
                            }
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
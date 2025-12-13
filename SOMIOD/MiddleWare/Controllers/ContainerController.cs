using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using Newtonsoft.Json.Linq;
using MiddleWare.Models;
using MiddleWare.Helpers;

namespace MiddleWare.Controllers
{
    [RoutePrefix("api/somiod")]
    public class ContainerController : ApiController
    {
        // =====================================================================
        // GET CONTAINER (Inclui suporte a Discovery)
        // Rota: GET api/somiod/{appName}/{contName}
        // =====================================================================
        [HttpGet]
        [Route("{appName}/{contName}")]
        public IHttpActionResult GetContainer(string appName, string contName)
        {
            // Lógica de Discovery (Headers)
            if (Request.Headers.Contains("somiod-discovery"))
            {
                var type = Request.Headers.GetValues("somiod-discovery").FirstOrDefault()?.ToLower();
                switch (type)
                {
                    case "container": return Ok(new List<string> { $"/api/somiod/{appName}/{contName}" });
                    case "content-instance": return Ok(BD_Access.DiscoverContentInstances(appName, contName));
                    case "subscription": return Ok(BD_Access.DiscoverSubscriptions(appName, contName));
                    default: return BadRequest("Invalid discovery type");
                }
            }

            // Lógica Normal (Retornar dados do Container)
            var cont = BD_Access.GetContainer(appName, contName);
            return cont != null ? Ok(cont) : (IHttpActionResult)NotFound();
        }

        // =====================================================================
        // DELETE CONTAINER
        // Rota: DELETE api/somiod/{appName}/{contName}
        // =====================================================================
        [HttpDelete]
        [Route("{appName}/{contName}")]
        public IHttpActionResult DeleteContainer(string appName, string contName)
        {
            if (BD_Access.DeleteContainer(appName, contName)) return Ok();
            return NotFound();
        }

        // =====================================================================
        // UPDATE CONTAINER (Opcional)
        // =====================================================================
        [HttpPut]
        [Route("{appName}/{contName}")]
        public IHttpActionResult UpdateContainer(string appName, string contName, [FromBody] JObject body)
        {
            return StatusCode(HttpStatusCode.NotImplemented);
        }

        // =====================================================================
        // CREATE CHILD (Content-Instance ou Subscription)
        // Rota: POST api/somiod/{appName}/{contName}
        // =====================================================================
        [HttpPost]
        [Route("{appName}/{contName}")]
        public IHttpActionResult CreateChild(string appName, string contName, [FromBody] JObject body)
        {
            string resType = body?["res-type"]?.ToString();

            try
            {
                // 1. CRIAR CONTENT-INSTANCE (DADOS + MQTT)
                if (resType == "content-instance")
                {
                    string content = body["content"]?.ToString();
                    if (string.IsNullOrEmpty(content)) return BadRequest("Missing content");

                    var newContent = new ContentInstance
                    {
                        Name = body["resource-name"]?.ToString() ?? "data",
                        Content = content,
                        ContentType = body["content-type"]?.ToString() ?? "application/xml"
                    };

                    // Gravar na Base de Dados
                    if (BD_Access.CreateContentInstance(appName, contName, newContent))
                    {
                        var createdContent = BD_Access.GetContentInstance(appName, contName, newContent.Name);

                        // --- ENVIAR NOTIFICAÇÃO MQTT ---
                        if (createdContent != null)
                        {
                            BD_Access.SendNotifications(createdContent.ParentId, 1, createdContent);
                        }

                        return Created($"/api/somiod/{appName}/{contName}/{createdContent.Name}", createdContent);
                    }
                }
                // 2. CRIAR SUBSCRIPTION (SUBSCRIÇÃO)
                else if (resType == "subscription")
                {
                    var newSub = new Subscription
                    {
                        Name = body["resource-name"]?.ToString() ?? "sub",
                        Endpoint = body["endpoint"]?.ToString(),
                        Event = int.Parse(body["evt"]?.ToString())
                    };

                    if (BD_Access.CreateSubscription(appName, contName, newSub))
                    {
                        var createdSub = BD_Access.GetSubscription(appName, contName, newSub.Name);
                        return Created($"/api/somiod/{appName}/{contName}/subs/{createdSub.Name}", createdSub);
                    }
                }

                return BadRequest("Tipo inválido ou falha na BD.");
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
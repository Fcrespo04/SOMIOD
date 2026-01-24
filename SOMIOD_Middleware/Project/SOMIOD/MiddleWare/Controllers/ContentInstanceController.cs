using MiddleWare.Helpers; // Onde está a tua BD_Access
using MiddleWare.Models;  // Onde estão os teus Models
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;

namespace MiddleWare.Controllers
{
    [RoutePrefix("api/somiod")]
    public class ContentInstanceController : ApiController
    {
        // =====================================================================
        // GET CONTENT-INSTANCE
        // Rota: GET api/somiod/{appName}/{contName}/{ciName}
        // =====================================================================

        /// <summary>
        /// Retrieve a specific Content-Instance or perform discovery on it.
        /// </summary>
        /// <remarks>
        /// If the 'somiod-discovery' header is present (value: 'content-instance'), it returns the path to this instance.
        /// Otherwise, it returns the full details of the content instance.
        /// </remarks>
        /// <param name="appName">The name of the parent application.</param>
        /// <param name="contName">The name of the parent container.</param>
        /// <param name="ciName">The unique name of the content instance.</param>
        /// <response code="200">OK: Returns the Content-Instance object or a discovery list.</response>
        /// <response code="404">Not Found: The specified resource does not exist.</response>
        /// <response code="400">Bad Request: Invalid discovery type (Content-Instance is a leaf node).</response>
        [HttpGet]
        [Route("{appName}/{contName}/{ciName}")]
        public IHttpActionResult GetContentInstance(string appName, string contName, string ciName)
        {
            if (Request.Headers.Contains("somiod-discovery"))
            {
                return ProcessDiscovery(appName, contName, ciName);
            }
            return GetDetails(appName, contName, ciName);
        }

        private IHttpActionResult ProcessDiscovery(string appName, string contName, string ciName)
        {
            // 1. Verificar Existência (Evita rotas fantasmas)
            var existingCI = BD_Access.GetContentInstance(appName, contName, ciName);
            if (existingCI == null) return NotFound();

            var type = Request.Headers.GetValues("somiod-discovery").FirstOrDefault()?.ToLower();

            // Um Content-Instance é uma folha, só se descobre a si próprio
            if (type == "content-instance")
            {
                return Ok(new List<string> { $"/api/somiod/{appName}/{contName}/{ciName}" });
            }

            return BadRequest("Invalid discovery type. Content-Instance is a leaf node.");
        }

        private IHttpActionResult GetDetails(string appName, string contName, string ciName)
        {
            var ci = BD_Access.GetContentInstance(appName, contName, ciName);
            if (ci == null) return NotFound();
            return Ok(ci);
        }

        // =====================================================================
        // DELETE CONTENT-INSTANCE
        // Rota: DELETE api/somiod/{appName}/{contName}/{ciName}
        // =====================================================================

        /// <summary>
        /// Delete a Content-Instance and trigger deletion notifications (Event 2).
        /// </summary>
        /// <remarks>
        /// This operation removes the data record from the database and sends an XML notification 
        /// (event type DELETE) to all subscribed endpoints (MQTT and HTTP).
        /// </remarks>
        /// <param name="appName">Application name.</param>
        /// <param name="contName">Container name.</param>
        /// <param name="ciName">Content-Instance name to delete.</param>
        /// <response code="200">OK: Resource deleted successfully and notifications sent.</response>
        /// <response code="404">Not Found: Resource does not exist.</response>
        /// <response code="500">Internal Server Error: Database or Notification failure.</response>
        [HttpDelete]
        [Route("{appName}/{contName}/{ciName}")]
        public IHttpActionResult DeleteContentInstance(string appName, string contName, string ciName)
        {
            try
            {
                // OBTER DADOS ANTES DE APAGAR
                // Precisamos do conteúdo para incluir na notificação XML
                var ciToDelete = BD_Access.GetContentInstance(appName, contName, ciName);
                
                if (ciToDelete == null) return NotFound();

                // APAGAR DA BASE DE DADOS
                if (BD_Access.DeleteContentInstance(appName, contName, ciName))
                {
                    // ===============================================================
                    // DISPARAR NOTIFICAÇÃO DE ELIMINAÇÃO (Event 2)
                    // ===============================================================

                    // Buscar endpoints interessados no evento 2 (Deletion)
                    // O método GetSubscriptionEndpoints já filtra por (evt=2 OR evt=0)
                    var endpoints = BD_Access.GetSubscriptionEndpoints(appName, contName, 2);

                    // Definir o Tópico/Canal (Formato: api/somiod/app/container)
                    string topico = $"api/somiod/{appName}/{contName}";

                    // Construir o XML de Notificação
                    // Nota: O evento agora é "DELETE" e incluímos o conteúdo que foi apagado
                    string xmlMessage = "<notification>" +
                                            "<event>DELETE</event>" +
                                            $"<resource>{ciToDelete.Content}</resource>" +
                                        "</notification>";

                    // Enviar para todos os subscritores (Híbrido: MQTT + HTTP)
                    foreach (var ep in endpoints)
                    {
                        string endpointLower = ep.ToLower().Trim();

                        // Envio via MQTT
                        if (endpointLower.StartsWith("mqtt://"))
                        {
                            MqttHelper.PublishToBroker(ep, topico, xmlMessage);
                        }
                        // Envio via HTTP (Webhook)
                        else if (endpointLower.StartsWith("http://") || endpointLower.StartsWith("https://"))
                        {
                            // Fire-and-forget
                            _ = HttpHelper.SendPostRequest(ep, xmlMessage);
                        }
                    }
                    return StatusCode(HttpStatusCode.OK);
                }
                return NotFound();
            }
            catch (Exception ex) { return InternalServerError(ex); }
        }
    }
}
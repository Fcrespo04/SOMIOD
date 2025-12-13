using MiddleWare.Helpers;
using MiddleWare.Models;
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

        // =====================================================================
        // DELETE CONTENT-INSTANCE
        // Rota: DELETE api/somiod/{appName}/{contName}/{ciName}
        // =====================================================================
        [HttpDelete]
        [Route("{appName}/{contName}/{ciName}")]
        public IHttpActionResult DeleteContentInstance(string appName, string contName, string ciName)
        {
            try
            {
                // 1. Obter o objeto ANTES de apagar (para saber o ParentId e notificar)
                var ciToDelete = BD_Access.GetContentInstance(appName, contName, ciName);

                if (ciToDelete == null) return NotFound();

                // 2. Apagar da BD
                if (BD_Access.DeleteContentInstance(appName, contName, ciName))
                {
                    // 3. Notificar MQTT (Evento 2 = Eliminação)
                    // Usamos o ParentId para saber em que tópico publicar
                    BD_Access.SendNotifications(ciToDelete.ParentId, 2, ciToDelete);

                    return StatusCode(HttpStatusCode.NoContent);
                }

                return NotFound();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // =====================================================================
        // MÉTODOS AUXILIARES (PRIVADOS)
        // =====================================================================

        private IHttpActionResult ProcessDiscovery(string appName, string contName, string ciName)
        {
            var existingCI = BD_Access.GetContentInstance(appName, contName, ciName);
            if (existingCI == null) return NotFound();

            var type = Request.Headers.GetValues("somiod-discovery").FirstOrDefault()?.ToLower();

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
    }
}
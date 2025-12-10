using MiddleWare.Helpers; // Onde está a tua BD_Access
using MiddleWare.Models;  // Onde estão os teus Models
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace MiddleWare.Controllers
{
    [RoutePrefix("api/somiod")]
    public class SubscriptionController : ApiController
    {
        // =====================================================================
        // GET SUBSCRIPTION
        // Rota: GET api/somiod/{appName}/{contName}/subs/{subName}
        // Nota: O recurso usa o nó virtual '/subs/' para ser distinguido
        // =====================================================================

        [HttpGet]
        [Route("{appName}/{contName}/subs/{subName}")]
        public IHttpActionResult GetSubscription(string appName, string contName, string subName)
        {
            if (Request.Headers.Contains("somiod-discovery"))
            {
                return ProcessDiscovery(appName, contName, subName);
            }
            return GetDetails(appName, contName, subName);
        }

        private IHttpActionResult ProcessDiscovery(string appName, string contName, string subName)
        {
            // 1. Verificar Existência
            var existingSub = BD_Access.GetSubscription(appName, contName, subName);
            if (existingSub == null) return NotFound();

            var type = Request.Headers.GetValues("somiod-discovery").FirstOrDefault()?.ToLower();

            // Uma Subscription é uma folha, só se descobre a si própria
            if (type == "subscription")
            {
                return Ok(new List<string> { $"/api/somiod/{appName}/{contName}/subs/{subName}" });
            }

            return BadRequest("Invalid discovery type. Subscription is a leaf node.");
        }

        private IHttpActionResult GetDetails(string appName, string contName, string subName)
        {
            var sub = BD_Access.GetSubscription(appName, contName, subName);
            if (sub == null) return NotFound();
            return Ok(sub);
        }

        // =====================================================================
        // DELETE SUBSCRIPTION
        // Rota: DELETE api/somiod/{appName}/{contName}/subs/{subName}
        // =====================================================================
        [HttpDelete]
        [Route("{appName}/{contName}/subs/{subName}")]
        public IHttpActionResult DeleteSubscription(string appName, string contName, string subName)
        {
            try
            {
                // Tenta apagar na BD
                if (BD_Access.DeleteSubscription(appName, contName, subName))
                {
                    return StatusCode(System.Net.HttpStatusCode.NoContent);
                }

                return NotFound();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
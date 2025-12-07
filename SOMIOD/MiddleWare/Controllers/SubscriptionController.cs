using System;
using System.Web.Http;
using MiddleWare.Helpers; // Onde está a tua BD_Access
using MiddleWare.Models;  // Onde estão os teus Models

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
            try
            {
                // Busca a subscrição usando a classe auxiliar
                var sub = BD_Access.GetSubscription(appName, contName, subName);

                if (sub == null) return NotFound();

                return Ok(sub);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
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
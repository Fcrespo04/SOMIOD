using System;
using System.Web.Http;
using MiddleWare.Helpers; // Onde está a tua BD_Access
using MiddleWare.Models;  // Onde estão os teus Models

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
            try
            {
                // Busca os dados usando a classe auxiliar
                var ci = BD_Access.GetContentInstance(appName, contName, ciName);

                if (ci == null) return NotFound();

                return Ok(ci);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
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
                // 1. Obter o objeto ANTES de apagar
                var ciToDelete = BD_Access.GetContentInstance(appName, contName, ciName);

                if (ciToDelete == null) return NotFound();

                // 2. Efetuar a eliminação na BD
                if (BD_Access.DeleteContentInstance(appName, contName, ciName))
                {
                    // 3. Disparar Notificação de Eliminação (Evento 2 = Deletion)
                    // Precisamos do ID do Container pai para encontrar os subscritores
                    // (Estou a assumir que tens um método GetResourceId ou similar na BD_Access, 
                    // ou podes usar o ParentId que vem no objeto ciToDelete se o carregares)

                    int? appId = BD_Access.GetResourceId("application", appName, null);
                    if (appId != null)
                    {
                        int? containerId = BD_Access.GetResourceId("container", contName, appId);

                        if (containerId != null)
                        {
                            // Envia notificação de tipo 2 (Delete)
                            BD_Access.SendNotifications(containerId.Value, 2, ciToDelete);
                        }
                    }

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
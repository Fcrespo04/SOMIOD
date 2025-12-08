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
                // 1. Obter o objeto ANTES de apagar (Contém o ParentId que é o ID do Container)
                var ciToDelete = BD_Access.GetContentInstance(appName, contName, ciName);

                if (ciToDelete == null) return NotFound();

                // 2. Efetuar a eliminação na BD
                if (BD_Access.DeleteContentInstance(appName, contName, ciName))
                {
                    // 3. Disparar Notificação de Eliminação (Evento 2 = Deletion)
                    // O ciToDelete.ParentId é o ID do Container Pai
                    int containerId = ciToDelete.ParentId;

                    // Envia notificação de tipo 2 (Delete) usando o ID que já temos
                    // NOTA: Certifique-se que o BD_Access.SendNotifications aceita o ID do Container.
                    BD_Access.SendNotifications(containerId, 2, ciToDelete);

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
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
            try
            {
                // Se existir o Header de somiod-discovery, o seu valor é analisado
                if (Request.Headers.Contains("somiod-discovery"))
                {
                    var type = Request.Headers.GetValues("somiod-discovery").FirstOrDefault();
                    switch (type?.ToLower())
                    {
                        case "container":
                            // Retorna o próprio container
                            return Ok(new List<string> { $"/api/somiod/{appName}/{contName}" });

                        case "content-instance":
                            // Retorna lista de dados dentro deste container
                            return Ok(BD_Access.DiscoverContentInstances(appName, contName));

                        case "subscription":
                            // Retorna lista de subscrições dentro deste container
                            return Ok(BD_Access.DiscoverSubscriptions(appName, contName));

                        default:
                            return BadRequest("Invalid discovery type");
                    }
                }

                // Caso não exista o header é um get normal
                var container = BD_Access.GetContainer(appName, contName);
                if (container == null) return NotFound();

                return Ok(container);
            }
            catch (Exception ex) { return InternalServerError(ex); }
        }

        // =====================================================================
        //      UPDATE CONTAINER (PUT api/somiod/{appName}/{contName})
        // =====================================================================
        [HttpPut]
        [Route("{appName}/{contName}")]
        public IHttpActionResult UpdateContainer(string appName, string contName, [FromBody] JObject body)
        {
            string resType = body?["res-type"]?.ToString();

            // Se o campo vier preenchido e não for "container", rejeita.
            if (!string.IsNullOrEmpty(resType) && resType != "container")
            {
                return Content(HttpStatusCode.BadRequest, "Invalid res-type. Expected 'container' for this operation.");
            }

            string newName = body?["resource-name"]?.ToString();
            if (string.IsNullOrWhiteSpace(newName)) return BadRequest("Missing resource-name");

            try
            {
                // Tenta atualizar através da BD_Access
                var updatedContainer = BD_Access.UpdateContainer(appName, contName, newName);

                if (updatedContainer != null)
                {
                    return Ok(updatedContainer);
                }

                return NotFound(); // Ou Conflict se o nome já existir
            }
            catch (SqlException ex)
            {
                // Os números 2627 e 2601 são códigos do SQL Server para violação de chave única (Unique Constraint)
                if (ex.Number == 2627 || ex.Number == 2601)
                {
                    // Retorna 409 Conflict (o standard para "já existe") com mensagem clara
                    return Content(HttpStatusCode.Conflict,
                        $"The name '{newName}' is already being used by a container inside this application.");
                }

                // Se for outro erro de SQL, devolve 500
                return InternalServerError(ex);
            }
            catch (Exception ex) { return InternalServerError(ex); }
        }

        // =====================================================================
        //       DELETE CONTAINER  (DELETE api/somiod/{appName}/{contName})
        // =====================================================================
        [HttpDelete]
        [Route("{appName}/{contName}")]
        public IHttpActionResult DeleteContainer(string appName, string contName)
        {
            try
            {
                // Verificar se o container existe antes de tentar apagar
                var container = BD_Access.GetContainer(appName, contName);
                if (container == null) return NotFound();

                // Recolher a lista de filhos antes de fazer o delete para depois exibir
                var deletedContentInstances = BD_Access.DiscoverContentInstances(appName, contName);
                var deletedSubscriptions = BD_Access.DiscoverSubscriptions(appName, contName);

                // elimina o cointainer
                if (BD_Access.DeleteContainer(appName, contName))
                {
                    // prepara o objeto de resposta com todo o conteudo apagado
                    var report = new
                    {
                        message = $"The Container '{contName}' and all his childern were sucessfully deleted.",
                        deleted_container = contName,
                        deleted_children = new
                        {
                            content_instances_count = deletedContentInstances.Count,
                            content_instances = deletedContentInstances, // Lista de URLs/Nomes
                            subscriptions_count = deletedSubscriptions.Count,
                            subscriptions = deletedSubscriptions     // Lista de URLs/Nomes
                        }
                    };

                    return Ok(report);
                }

                return NotFound();
            }
            catch (Exception ex) { return InternalServerError(ex); }
        }

        // =====================================================================
        // CREATE CHILD (Content-Instance / Subscription) (POST api/somiod/{appName}/{contName})
        // =====================================================================
        [HttpPost]
        [Route("{appName}/{contName}")]
        public IHttpActionResult CreateChild(string appName, string contName, [FromBody] JObject body)
        {
            string resType = body?["res-type"]?.ToString();

            try
            {
                // CASO 1: Criar Content-Instance (Dados)
                if (resType == "content-instance")
                {
                    // Validações básicas
                    string content = body["content"]?.ToString();
                    string contentType = body["content-type"]?.ToString();
                    if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(contentType))
                        return BadRequest("Missing content or content-type");

                    var newContent = new ContentInstance
                    {
                        Name = body["resource-name"]?.ToString() ?? "data",
                        Content = content,
                        ContentType = contentType
                    };

                    // Tenta criar na BD
                    if (BD_Access.CreateContentInstance(appName, contName, newContent))
                    {
                        // Recuperar o objeto criado (com o nome final único)
                        var createdContent = BD_Access.GetContentInstance(appName, contName, newContent.Name);

                        // !!! DISPARAR NOTIFICAÇÃO (Evento 1 = Criação) !!!
                        // Assumimos que BD_Access tem um método para obter subscrições deste container
                        NotifySubscribers(appName, contName, 1, createdContent);

                        return Created($"/api/somiod/{appName}/{contName}/{createdContent.Name}", createdContent);
                    }
                }
                // CASO 2: Criar Subscription (Subscrição)
                else if (resType == "subscription")
                {
                    string endpoint = body["endpoint"]?.ToString();
                    string evtStr = body["evt"]?.ToString();

                    if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(evtStr))
                        return BadRequest("Missing endpoint or evt");

                    int evt = int.Parse(evtStr); // 1=Create, 2=Delete

                    var newSub = new Subscription
                    {
                        Name = body["resource-name"]?.ToString() ?? "sub",
                        Endpoint = endpoint,
                        Event = evt
                    };

                    if (BD_Access.CreateSubscription(appName, contName, newSub))
                    {
                        var createdSub = BD_Access.GetSubscription(appName, contName, newSub.Name);
                        // Subscrições têm uma rota especial virtual /subs/
                        return Created($"/api/somiod/{appName}/{contName}/subs/{createdSub.Name}", createdSub);
                    }
                }
                else
                {
                    return BadRequest("Invalid res-type. Expected 'content-instance' or 'subscription'.");
                }

                return NotFound(); // Container pai não encontrado
            }
            catch (Exception ex) { return InternalServerError(ex); }
        }

        // =====================================================================
        // HELPER PRIVADO DE NOTIFICAÇÕES
        // =====================================================================
        private void NotifySubscribers(string appName, string contName, int eventType, object resourceData)
        {
            // 1. Obter lista de endpoints interessados neste evento para este container
            // Precisas de um método na BD_Access que faça: SELECT endpoint FROM subscription WHERE parent=... AND (evt=type OR evt=0)
            List<string> endpoints = BD_Access.GetSubscriptionEndpoints(appName, contName, eventType);

            foreach (var endpoint in endpoints)
            {
                // Aqui implementas a lógica de envio (MQTT ou HTTP)
                // Exemplo simplificado de debug:
                System.Diagnostics.Debug.WriteLine($"[NOTIFICAÇÃO] Enviar para {endpoint}: Evento {eventType} no recurso {appName}/{contName}");

                // TODO: Implementar chamada real HTTP Client ou MQTT Client aqui
                // MqttHandler.Publish(endpoint, resourceData); 
            }
        }
    }
}
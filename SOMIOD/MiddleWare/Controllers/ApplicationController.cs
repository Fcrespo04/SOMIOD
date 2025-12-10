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
    public class ApplicationController : ApiController
    {
        // =====================================================================
        //             CREATE APPLICATION (POST api/somiod/)
        // =====================================================================
        [HttpPost]
        [Route("")]
        public IHttpActionResult CreateApplication([FromBody] JObject body)
        {
            // 1. Validar se o tipo é application
            string resType = body?["res-type"]?.ToString();
            if (resType != "application") return BadRequest("Invalid res-type. Expected 'application'.");

            // 2. Obter nome base (ou 'app' se vazio)
            string name = body?["resource-name"]?.ToString() ?? "app";

            var newApp = new Application { Name = name };

            // 3. Chamar BD_Access para criar (trata da unicidade do nome lá dentro)
            if (BD_Access.CreateApplication(newApp))
            {
                // Recuperar o objeto completo da BD para retornar (com data e nome final)
                var createdApp = BD_Access.GetApplication(newApp.Name);
                if (createdApp != null)
                {
                    return Created($"/api/somiod/{createdApp.Name}", createdApp);
                }
            }

            return InternalServerError();
        }

        // =====================================================================
        // DISCOVER ROOT (GET api/somiod/ -> Header: somiod-discovery: application)
        // =====================================================================
        [HttpGet]
        [Route("")]
        public IHttpActionResult DiscoverRoot()
        {
            // Validação: Raiz só suporta Discovery
            if (!Request.Headers.Contains("somiod-discovery"))
            {
                return BadRequest("Global GET is not supported. Please use the 'somiod-discovery' header.");
            }

            // Encaminha para função específica de Discovery
            return ProcessRootDiscovery();
        }

        // Função Privada: Lógica de Discovery da Raiz
        private IHttpActionResult ProcessRootDiscovery()
        {
            var type = Request.Headers.GetValues("somiod-discovery").FirstOrDefault()?.ToLower();
            switch (type)
            {
                case "application": return Ok(BD_Access.DiscoverApplications());
                case "container": return Ok(BD_Access.DiscoverContainers(null));
                case "content-instance": return Ok(BD_Access.DiscoverContentInstances(null, null));
                case "subscription": return Ok(BD_Access.DiscoverSubscriptions(null, null));
                default: return BadRequest("Invalid discovery type.");
            }
        }

        // =====================================================================
        // GET APPLICATION ou DISCOVERY de filhos (GET api/somiod/{appName}) 
        // =====================================================================
        [HttpGet]
        [Route("{appName}")]
        public IHttpActionResult GetApplication(string appName)
        {
            // Decisão: É Discovery ou Get Normal?
            if (Request.Headers.Contains("somiod-discovery"))
            {
                return ProcessApplicationDiscovery(appName);
            }
            return GetApplicationDetails(appName);
        }

        // Função Privada: Apenas Lógica de Discovery
        private IHttpActionResult ProcessApplicationDiscovery(string appName)
        {
            var existingApp = BD_Access.GetApplication(appName);
            if (existingApp == null)
            {
                return NotFound();
            }

            var type = Request.Headers.GetValues("somiod-discovery").FirstOrDefault()?.ToLower();
            switch (type)
            {
                case "application": return Ok(new List<string> { $"/api/somiod/{appName}" });
                case "container": return Ok(BD_Access.DiscoverContainers(appName));
                case "content-instance": return Ok(BD_Access.DiscoverContentInstances(appName, null));
                case "subscription": return Ok(BD_Access.DiscoverSubscriptions(appName, null));
                default: return BadRequest("Invalid discovery type.");
            }
        }

        // Função Privada: Apenas Lógica de Obter Recurso com GET normal
        private IHttpActionResult GetApplicationDetails(string appName)
        {
            var app = BD_Access.GetApplication(appName);
            if (app == null) return NotFound();
            return Ok(app);
        }


        // =====================================================================
        //          UPDATE APPLICATION (PUT api/somiod/{appName})
        // =====================================================================
        [HttpPut]
        [Route("{appName}")]
        public IHttpActionResult UpdateApplication(string appName, [FromBody] JObject body)
        {
            string newName = body?["resource-name"]?.ToString();
            if (string.IsNullOrWhiteSpace(newName)) return BadRequest("Missing resource-name");

            // Tenta atualizar. O método retorna o objeto atualizado ou null se falhar.
            var updatedApp = BD_Access.UpdateApplication(appName, newName);

            if (updatedApp != null)
            {
                return Ok(updatedApp);
            }

            // Se falhou, pode ser conflito de nomes ou app não encontrada.
            // Podes refinar isto se o BD_Access lançar exceções específicas.
            return NotFound();
        }

        // =====================================================================
        //         DELETE APPLICATION (DELETE api/somiod/{appName})
        // =====================================================================
        [HttpDelete]
        [Route("{appName}")]
        public IHttpActionResult DeleteApplication(string appName)
        {
            if (BD_Access.DeleteApplication(appName))
            {
                return StatusCode(HttpStatusCode.NoContent);
            }
            return NotFound();
        }

        // =====================================================================
        //  CREATE CONTAINER (Filho de Application) (POST api/somiod/{appName})
        // =====================================================================
        [HttpPost]
        [Route("{appName}")]
        public IHttpActionResult CreateContainer(string appName, [FromBody] JObject body)
        {
            // 1. Validar res-type
            string resType = body?["res-type"]?.ToString();
            if (resType != "container") return BadRequest("Invalid res-type. Expected 'container'.");

            string contName = body?["resource-name"]?.ToString() ?? "container";

            var newCont = new Container { Name = contName };

            // 2. Chamar BD_Access para criar associado à appName
            if (BD_Access.CreateContainer(appName, newCont))
            {
                // Buscar o objeto criado para retornar na resposta
                var createdCont = BD_Access.GetContainer(appName, newCont.Name);
                if (createdCont != null)
                {
                    return Created($"/api/somiod/{appName}/{createdCont.Name}", createdCont);
                }
            }

            return NotFound(); // Significa geralmente que a App pai não existe
        }
    }
}
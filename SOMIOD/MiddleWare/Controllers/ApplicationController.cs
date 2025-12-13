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
        // CREATE APPLICATION (POST api/somiod/)
        // =====================================================================
        [HttpPost]
        [Route("")]
        public IHttpActionResult CreateApplication([FromBody] JObject body)
        {
            string resType = body?["res-type"]?.ToString();
            if (resType != "application") return BadRequest("Invalid res-type. Expected 'application'.");

            string name = body?["resource-name"]?.ToString() ?? "app";
            var newApp = new Application { Name = name };

            if (BD_Access.CreateApplication(newApp))
            {
                var createdApp = BD_Access.GetApplication(newApp.Name);
                if (createdApp != null)
                {
                    return Created($"/api/somiod/{createdApp.Name}", createdApp);
                }
            }

            return InternalServerError();
        }

        // =====================================================================
        // DISCOVER ROOT (GET api/somiod/)
        // =====================================================================
        [HttpGet]
        [Route("")]
        public IHttpActionResult DiscoverRoot()
        {
            if (!Request.Headers.Contains("somiod-discovery"))
            {
                return BadRequest("Global GET is not supported. Use 'somiod-discovery' header.");
            }

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
        // GET APPLICATION ou DISCOVERY (GET api/somiod/{appName})
        // =====================================================================
        [HttpGet]
        [Route("{appName}")]
        public IHttpActionResult GetApplication(string appName)
        {
            // Lógica de Discovery
            if (Request.Headers.Contains("somiod-discovery"))
            {
                var existingApp = BD_Access.GetApplication(appName);
                if (existingApp == null) return NotFound();

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

            // Lógica Normal (Get Detalhes)
            var app = BD_Access.GetApplication(appName);
            return app != null ? Ok(app) : (IHttpActionResult)NotFound();
        }

        // =====================================================================
        // UPDATE APPLICATION (PUT api/somiod/{appName})
        // =====================================================================
        [HttpPut]
        [Route("{appName}")]
        public IHttpActionResult UpdateApplication(string appName, [FromBody] JObject body)
        {
            string newName = body?["resource-name"]?.ToString();
            if (string.IsNullOrWhiteSpace(newName)) return BadRequest("Missing resource-name");

            var updatedApp = BD_Access.UpdateApplication(appName, newName);
            if (updatedApp != null) return Ok(updatedApp);

            return NotFound();
        }

        // =====================================================================
        // DELETE APPLICATION (DELETE api/somiod/{appName})
        // =====================================================================
        [HttpDelete]
        [Route("{appName}")]
        public IHttpActionResult DeleteApplication(string appName)
        {
            if (BD_Access.DeleteApplication(appName)) return StatusCode(HttpStatusCode.NoContent);
            return NotFound();
        }

        // =====================================================================
        // CREATE CONTAINER (POST api/somiod/{appName})
        // =====================================================================
        [HttpPost]
        [Route("{appName}")]
        public IHttpActionResult CreateContainer(string appName, [FromBody] JObject body)
        {
            string resType = body?["res-type"]?.ToString();
            if (resType != "container") return BadRequest("Invalid res-type. Expected 'container'.");

            string contName = body?["resource-name"]?.ToString() ?? "container";
            var newCont = new Container { Name = contName };

            if (BD_Access.CreateContainer(appName, newCont))
            {
                var createdCont = BD_Access.GetContainer(appName, newCont.Name);
                if (createdCont != null)
                {
                    return Created($"/api/somiod/{appName}/{createdCont.Name}", createdCont);
                }
            }

            return NotFound();
        }
    }
}
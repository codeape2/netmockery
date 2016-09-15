using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace netmockery.Controllers
{
    public class EndpointsController : Controller
    {
        private EndpointCollection _endpointCollection;
        private ResponseRegistry _responseRegistry;

        public EndpointsController(EndpointCollection endpointCollection, ResponseRegistry responseRegistry)
        {
            _endpointCollection = endpointCollection;
            _responseRegistry = responseRegistry;
        }

        public ResponseRegistry ResponseRegistry => _responseRegistry;

        public ActionResult Index()
        {
            ViewData["responseRegistry"] = _responseRegistry;
            return View(_endpointCollection);
        }



        public ActionResult ReloadConfig()
        {
            Startup.ReloadConfig();
            return RedirectToAction("Index");
        }

        public ActionResult EndpointDetails(string name, int highlight = -1)
        {
            ViewData["responseRegistry"] = _responseRegistry;
            ViewData["highlight"] = highlight;
            return View(_endpointCollection.Get(name));
        }

        public ActionResult ViewRequestCreatorFile(string name, int requestCreatorId)
        {
            var endpoint = _endpointCollection.Get(name);
            var requestCreator = endpoint.Responses.ElementAt(requestCreatorId).Item2 as IResponseCreatorWithFilename;
            if (requestCreator != null)
            {
                return File(System.IO.File.OpenRead(requestCreator.Filename), "text/plain");
            }
            else
            {
                return NotFound();
            }
        }        
    }
}

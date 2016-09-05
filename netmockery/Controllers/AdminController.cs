using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace netmockery.Controllers
{
    public class AdminController : Controller
    {
        private EndpointCollection _endpointCollection;
        private ResponseRegistry _responseRegistry;

        public AdminController(EndpointCollection endpointCollection, ResponseRegistry responseRegistry)
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

        public ActionResult Requests()
        {
            ViewData["title"] = "(all endpoints)";
            return View(_responseRegistry.Responses.Take(100));
        }

        public ActionResult RequestsForEndpoint(string endpointName)
        {
            ViewData["title"] = $"for endpoint {endpointName}";
            return View("Requests", _responseRegistry.ForEndpoint(endpointName));
        }

        public ActionResult RequestsErrorsOnly()
        {
            ViewData["title"] = $"(errors only)";
            return View("Requests", from responseItem in _responseRegistry.Responses where responseItem.Error != null select responseItem);
        }


        public ActionResult DownloadResponse(int requestId)
        {
            return File(Encoding.UTF8.GetBytes(_responseRegistry.Get(requestId).ResponseBody), "text/plain", $"response_{requestId}.txt");
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

        public ActionResult EndpointRequests(string name)
        {
            ViewData["responseRegistry"] = _responseRegistry;
            return View(_endpointCollection.Get(name));
        }

        
    }
}

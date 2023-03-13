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
        private EndpointCollectionProvider _endpointCollectionProvider;
        private EndpointCollection _endpointCollection;
        private ResponseRegistry _responseRegistry;

        public EndpointsController(EndpointCollectionProvider endpointCollectionProvider, ResponseRegistry responseRegistry)
        {
            _endpointCollectionProvider = endpointCollectionProvider;
            _endpointCollection = _endpointCollectionProvider.EndpointCollection;
            _responseRegistry = responseRegistry;
        }

        public ResponseRegistry ResponseRegistry => _responseRegistry;

        public ActionResult Index()
        {
            ViewData["responseRegistry"] = _responseRegistry;
            return View(_endpointCollection);
        }

        public ActionResult Config()
        {
            ViewData["Now"] = DateTime.Now;
            ViewData["ReloadTimestamps"] = _endpointCollectionProvider.ReloadTimestamps;
            ViewData["SourceDirectory"] = _endpointCollection.SourceDirectory;
            ViewData["NetmockeryVersion"] = Program.NetmockeryVersion;
            ViewData["DefaultScriptReferences"] = 
                    from 
                        metadatareference in DynamicResponseCreatorBase.GetDefaultMetadataReferences()
                    select 
                        metadatareference.Display;
            return View();
        }

        public ActionResult EndpointNames()
        {
            return Json(from e in _endpointCollection.Endpoints select e.Name);
        }



        public ActionResult ReloadConfig()
        {
            Console.Error.WriteLine("Reloading config");

            _endpointCollectionProvider.Reload();
            _responseRegistry.Clear();

            Console.Error.WriteLine("Config reloaded");
            return RedirectToAction("Config");
        }

        public ActionResult EndpointDetails(string name, int highlight = -1)
        {
            ViewData["responseRegistry"] = _responseRegistry;
            ViewData["highlight"] = highlight;
            ViewData["name"] = name;
            return View(_endpointCollection.Get(name));
        }

        [HttpGet]
        public ActionResult AdjustParam(string name, int index)
        {
            ViewBag.CancelUrl = Url.Action("EndpointDetails", new { name = name });
            return View(_endpointCollection.Get(name).GetParameter(index));
        }

        [HttpPost]
        public ActionResult AdjustParam(string name, int index, string value)
        {            
            _endpointCollection.Get(name).GetParameter(index).Value = value;
            return RedirectToAction("EndpointDetails", new { name = name });
        }

        public ActionResult ResetParam(string name, int index)
        {
            _endpointCollection.Get(name).GetParameter(index).ResetToDefaultValue();
            return RedirectToAction("EndpointDetails", new { name = name });
        }

        public ActionResult EndpointJsonFile(string name)
        {
            var endpoint = _endpointCollection.Get(name);
            return Content(System.IO.File.ReadAllText(System.IO.Path.Combine(endpoint.Directory, "endpoint.json")));
        }

        public ActionResult ViewResponseCreatorFile(string name, int responseCreatorId)
        {
            var endpoint = _endpointCollection.Get(name);
            var responseCreator = endpoint.Responses.ElementAt(responseCreatorId).Item2;
            if (responseCreator is FileDynamicResponseCreator fileDynamicResponseCreator)
            {
                var sourceCode = fileDynamicResponseCreator.GetSourceCodeWithIncludesExecuted();
                var lines = sourceCode.Split('\n');
                var sourceWithLineNumber = 
                    from
                        i in Enumerable.Range(0, lines.Length)
                    select 
                        $"{i + 1}: {lines[i]}"; 
                return Content(string.Join("\n", sourceWithLineNumber), "text/plain");
            }
            else if (responseCreator is IResponseCreatorWithFilename responseCreatorWithFilename)
            {
                return File(System.IO.File.OpenRead(responseCreatorWithFilename.Filename), "text/plain");
            }
            else
            {
                return NotFound();
            }
        }        
    }
}

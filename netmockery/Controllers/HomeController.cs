using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using IO = System.IO;
using System.Linq;
using System.Threading.Tasks;
using Markdig;


namespace netmockery.Controllers
{
    public class HomeController : Controller
    {
        private EndpointCollection _endpointCollection;

        public HomeController(EndpointCollection endpointCollection)
        {
            _endpointCollection = endpointCollection;
        }

        public ActionResult Index()
        {
            var indexFile = IO.Path.Combine(_endpointCollection.SourceDirectory, "index.md");
            if (IO.File.Exists(indexFile))
            {
                return View("DisplayMarkdown", Markdown.ToHtml(IO.File.ReadAllText(indexFile)));
            }
            else
            {
                return RedirectToAction("Index", "Endpoints");
            }

        }
    }
}

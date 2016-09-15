using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using IO = System.IO;
using System.Linq;
using System.Threading.Tasks;
using HeyRed.MarkdownSharp;


namespace netmockery.Controllers
{
    public class HomeController : Controller
    {
        private string _directory;

        public HomeController(EndpointCollection endpointCollection)
        {
            _directory = endpointCollection.SourceDirectory;
        }

        public ActionResult Index()
        {
            var indexFile = IO.Path.Combine(Program.EndpointCollection.SourceDirectory, "index.md");
            if (IO.File.Exists(indexFile))
            {
                return View("DisplayMarkdown", new Markdown().Transform(IO.File.ReadAllText(indexFile)));
            }
            else
            {
                return RedirectToAction("Index", "Endpoints");
            }

        }
    }
}

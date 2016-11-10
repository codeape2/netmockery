using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HeyRed.MarkdownSharp;
using Microsoft.AspNetCore.Hosting;

namespace netmockery.Controllers
{
    public class DocumentationController : Controller
    {
        private IHostingEnvironment _env;

        public DocumentationController(IHostingEnvironment env)
        {
            _env = env;  
        }

        public ActionResult Index()
        {
            var text = new Markdown().Transform(System.IO.File.ReadAllText(System.IO.Path.Combine(_env.ContentRootPath, "documentation.md")));
            return View("DisplayMarkdown", text);
        }
    }
}

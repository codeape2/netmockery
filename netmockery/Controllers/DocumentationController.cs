using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HeyRed.MarkdownSharp;

namespace netmockery.Controllers
{
    public class DocumentationController : Controller
    {
        public ActionResult Index()
        {
            var markdown = new Markdown();
            var text = markdown.Transform(System.IO.File.ReadAllText("documentation.md"));
            return View(model: text);
        }
    }
}

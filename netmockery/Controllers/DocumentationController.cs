using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Markdig;
using Microsoft.AspNetCore.Hosting;
using Markdig.Extensions.AutoIdentifiers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Markdig.Renderers.Html;

namespace netmockery.Controllers
{
    public class TocItem
    {
        public string id;
        public string title;
        public int level;

        public List<TocItem> children = new List<TocItem>();
        public bool HasChildren => children.Count > 0;
    }


    public class DocumentationController : Controller
    {
        private IWebHostEnvironment _env;

        public DocumentationController(IWebHostEnvironment env)
        {
            _env = env;  
        }

        private string GetTitle(ContainerInline container)
        {
            var parts = new List<string>();
            foreach (var child in container)
            {
                if (child is LiteralInline)
                {
                    parts.Add((child as LiteralInline).Content.ToString());
                }

                if (child is CodeInline)
                {
                    parts.Add((child as CodeInline).Content.ToString());
                }
            }
            return string.Join("", parts);
        }


        public ActionResult Index()
        {
            var builder = new MarkdownPipelineBuilder();
            var toc = new List<TocItem>();

            builder.DocumentProcessed += (document) =>
            {
                var headers =
                    from lrd in document.GetLinkReferenceDefinitions(true).OfType<HeadingLinkReferenceDefinition>()
                    select new
                    {
                        level = lrd.Heading.Level,
                        title = GetTitle(lrd.Heading.Inline),
                        id = lrd.Heading.GetAttributes().Id
                    };

                TocItem current = null;
                foreach (var header in headers)
                {
                    if (current == null || current.level == header.level)
                    {
                        current = new TocItem { id = header.id, title = header.title, level = header.level };
                        toc.Add(current);
                        continue;
                    }
                    if (current.level + 1 == header.level)
                    {
                        current.children.Add(new TocItem { id = header.id, title = header.title, level = header.level });
                    }
                }

            };
            var pipeline = builder.UseAutoIdentifiers().Build();
            var text = Markdown.ToHtml(
                System.IO.File.ReadAllText(System.IO.Path.Combine(_env.ContentRootPath, "documentation.md")),
                pipeline
            );
            ViewData["toc"] = toc;
            return View("DisplayMarkdown", text);
        }
    }
}

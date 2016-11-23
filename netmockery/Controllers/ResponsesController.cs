using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace netmockery.Controllers
{
    public class ResponsesController : Controller
    {
        private ResponseRegistry _responseRegistry;
        const int PAGESIZE = 20;

        public ResponsesController(ResponseRegistry responseRegistry)
        {
            _responseRegistry = responseRegistry;
        }

        public ActionResult Index(int page = 1)
        {
            ViewData["title"] = "(all endpoints)";
            return PagedView(_responseRegistry.Responses, page);
        }

        public ActionResult ErrorsOnly(int page = 1)
        {
            ViewData["title"] = $"(errors only)";
            return PagedView(from responseItem in _responseRegistry.Responses where responseItem.Error != null select responseItem, page);
        }

        public ActionResult ForEndpoint(string endpointName, int page = 1)
        {
            ViewData["title"] = $"for endpoint {endpointName}";
            return PagedView(_responseRegistry.ForEndpoint(endpointName), page);
        }

        public ActionResult RequestDetails(int responseId)
        {
            return View(_responseRegistry.Get(responseId));
        }

        public ActionResult RequestBody(int responseId)
        {
            return Content(_responseRegistry.Get(responseId).RequestBody);
        }

        public ActionResult ResponseDetails(int responseId)
        {
            return Content(_responseRegistry.Get(responseId).ResponseBody);
        }

        /*
        public ActionResult DownloadRequest(int requestId)
        {
            return File(Encoding.UTF8.GetBytes(_responseRegistry.Get(requestId).RequestBody), "text/plain", $"request_{requestId}.txt");
        }

        public ActionResult DownloadResponse(int requestId)
        {
            return File(Encoding.UTF8.GetBytes(_responseRegistry.Get(requestId).ResponseBody), "text/plain", $"response_{requestId}.txt");
        }
        */

        private ActionResult PagedView(IEnumerable<ResponseRegistryItem> items, int page)
        {
            var pageIndex = page - 1;
            ViewData["page"] = page;
            ViewData["PAGESIZE"] = PAGESIZE;
            return View("Index", items.Skip(pageIndex * PAGESIZE).Take(PAGESIZE));
        }


    }
}

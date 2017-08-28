using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace netmockery.Controllers
{
    public class ResponseCreatorViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(Endpoint endpoint, ResponseCreator responseCreator)
        {
            ViewData["index"] = responseCreator.Index;
            ViewData["endpointName"] = endpoint.Name;

            if (responseCreator is FileResponse)
            {
                return View("FileResponse", responseCreator as FileResponse);
            }
            else if (responseCreator is FileDynamicResponseCreator)
            {
                return View("FileDynamicResponseCreator", responseCreator as FileDynamicResponseCreator);
            }
            else if (responseCreator is ForwardResponseCreator)
            {
                return View("ForwardResponseCreator", responseCreator as ForwardResponseCreator);
            }
            else if (responseCreator is LiteralResponse)
            {
                return View("LiteralResponse", responseCreator as LiteralResponse);
            }
            else
            {
                return View(responseCreator);
            }            
        }
    }
}

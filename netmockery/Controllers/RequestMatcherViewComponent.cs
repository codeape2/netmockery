using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace netmockery.Controllers
{
    public class RequestMatcherViewComponent : ViewComponent
    {        
        public IViewComponentResult Invoke(RequestMatcher requestMatcher)
        {
            if (requestMatcher is AnyMatcher)
            {
                return View("AnyMatcher", requestMatcher);
            }
            else if (requestMatcher is XPathMatcher)
            {
                return View("XPathMatcher", requestMatcher as XPathMatcher);
            }
            else if (requestMatcher is RegexMatcher)
            {
                return View("RegexMatcher", requestMatcher as RegexMatcher);
            }
            else
            {
                return View(requestMatcher);
            }
        }
    }
}

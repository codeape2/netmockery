using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace netmockery.Controllers
{
    public class ResponseTableViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(IEnumerable<ResponseRegistryItem> rows)
        {
            return View(rows);
        }
    }
}

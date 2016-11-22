﻿using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using System.Text;

namespace netmockery.Controllers
{
    public class WebTestRunner : TestRunner
    {
        StringBuilder response = new StringBuilder();

        public WebTestRunner(EndpointCollection endpointCollection) : base(endpointCollection)
        { 
        }

        public override string ToString() => response.ToString();

        public override void WriteBeginTest(int index, NetmockeryTestCase testcase)
        {
            response.Append($"{index} {testcase.Name} ");
        }

        public override void WriteError(string s)
        {
            response.AppendLine($"ERROR: {s}");
        }

        public override void WriteResponse(string s)
        {
            response.Append(s);
        }

        public override void WriteResult(NetmockeryTestCaseResult result)
        {
            response.AppendLine(result.ResultAsString);
        }

        public override void WriteSummary(int errors)
        {
            response.AppendLine($"Total: {Tests.Count()} Errors: {errors}");
        }
    }
    public class TestsController : Controller
    {
        private EndpointCollection endpoints;

        public TestsController(EndpointCollection endpoints)
        {
            this.endpoints = endpoints;
        }
        public ActionResult Index()
        {            
            return View(new WebTestRunner(endpoints));
        }

        public ActionResult RunAll()
        {
            var testRunner = new WebTestRunner(endpoints);
            testRunner.TestAll();
            return Content(testRunner.ToString());
        }

        public ActionResult Run(int index)
        {
            var testRunner = new WebTestRunner(endpoints);
            testRunner.ExecuteTestAndOutputResult(index);
            return Content(testRunner.ToString());
        }

        public ActionResult ViewResponse(int index)
        {
            var testRunner = new WebTestRunner(endpoints);
            testRunner.ShowResponse(index);
            return Content(testRunner.ToString());
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (! TestRunner.HasTestSuite(endpoints.SourceDirectory))
            {
                context.Result = View("NoTests", null);
            }
        }
    }
}

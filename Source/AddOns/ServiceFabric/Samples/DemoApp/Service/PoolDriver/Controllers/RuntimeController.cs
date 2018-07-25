using Microsoft.AspNetCore.Mvc;
using Microsoft.PSharp.ServiceFabric;
using Microsoft.ServiceFabric.Data;
using PoolServicesContract;
using Microsoft.ServiceFabric.Data.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;

namespace PoolDriver.Controllers
{
    [Route("debug/api/[controller]")]
    public class RuntimeController : Controller
    {
        private IReliableStateManager stateManager;
        private PoolDriverService service;

        public RuntimeController(IReliableStateManager stateManager, PoolDriverService service)
        {
            this.stateManager = stateManager;
            this.service = service;
        }

        // POST api/values
        [HttpPost]
        [Route("SimpleData")]
        public Task Post([FromBody]Config value)
        {
            return this.service.SubmitRequest(value);
        }
    }

    public class Config
    {
        public int TotalPools;
    }
}

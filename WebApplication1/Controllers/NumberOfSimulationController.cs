using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using EFOP;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NumberOfSimulationController : ControllerBase
    {

        //POST: api/NumberOfSimulation
        // megmondja hogy hány szimuláció van lefuttatva
        [HttpGet]
        public String Get()
        {
            return $"{{\"simulationNumber\":\"{SimulationHandler.GetSimulationCount() }\"}}"; // "{"simulationNumber":"3"}
        }
    }
}
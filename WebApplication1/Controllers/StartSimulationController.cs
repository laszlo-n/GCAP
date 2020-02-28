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
    public class StartSimulationController : ControllerBase
    {
        /// <summary>
        /// Starts a new simulation with 100 rounds.
        /// api/StartSimulation
        /// </summary>
        [HttpGet]
        public string GetSimulation()
        {
            try
            {
                SimulationHandler.StartNew(100);
                return "";
            }
            catch (ArgumentOutOfRangeException e)
            {
                return "{\"message\":\"There is no simulation\"}"; // "{"simulationNumber":"3"};
            }

        }

        /// <summary>
        /// Starts a new simulation with the given rounds.
        ///  GET /api/StartSimulation/round
        /// </summary>
        [HttpGet("{round}", Name = "GetRound")]
        public String GetRound(int round)
        {
            try
            {
                SimulationHandler.StartNew(round);
                return "";
            }
            catch (ArgumentOutOfRangeException e)
            {
                return "{\"message\":\"There is no simulation\"}"; // "{"simulationNumber":"3"};
            }

        }


     
        /*
        // POST /api/startsimulation
        [HttpPost]
        public String GetStartSimulation()
        {
            SimulationHandler.StartNew(100);
            return $"{{\"simulationNumber\":\"{SimulationHandler.GetSimulationCount() - 1}\"}}"; // "{"simulationNumber":"3"}
        }
        */

        //// POST /api/StartSimulation/round
        //[HttpPost]
        //public String Post(int round)
        //{
        //    SimulationHandler.StartNew(round);
        //    return "igen";
        //    //return $"{{\"simulationNumber\":\"{SimulationHandler.GetSimulationCount() - 1}\"}}"; // "{"simulationNumber":"3"}
        //}

    }
}
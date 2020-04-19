using System;
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
        /// <returns> A json object like this "{"simulationNumber":"3"}". 
        /// If the given value positive the simulation was successful, otherwise the simulation was unsuccessful.
        /// </returns>
        [HttpGet]
        public string GetSimulation()
        {
            try
            {
                Simulation s = SimulationHandler.StartNew(100);
                return $"{{\"simulationID\":\"{s.ID.ToString()}\"}}"; 
            }

            catch (ArgumentOutOfRangeException)
            {
                return $"{{\"simulationNumber\":\"-1\"}}";
            }

        }

        /// <summary>
        /// Starts a new simulation with the given rounds.
        /// GET /api/StartSimulation/round
        /// </summary>
        /// </summary>
        /// <returns> A json object like this "{"simulationNumber":"3"}". 
        /// If the given value positive the simulation was successful, otherwise the simulation was unsuccessful.
        /// </returns>
        [HttpGet("{round}")]
        public String GetRound(int round)
        {
            try
            {
                Simulation s = SimulationHandler.StartNew(round);
                return $"{{\"simulationID\":\"{s.ID.ToString()}\"}}";
            }
            catch (ArgumentOutOfRangeException)
            {
                return $"{{\"simulationID\":\"-1\"}}";
            }

        }

    }
}
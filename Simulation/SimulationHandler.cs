using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#pragma warning disable 1591
namespace EFOP
{
	public static class SimulationHandler
	{
		private static object idLockObject = new object();

		private static List<Simulation> simulations = new List<Simulation>();

		// caching simulations to be accessed faster by id
		private static Dictionary<int, Simulation> simulationsByID = new Dictionary<int, Simulation>();
		private static Random idGenerator = new Random();

		static SimulationHandler()
			=> Array.ForEach(
				Directory.GetDirectories(".").
					Select(e => Simulation.GetIdFromDataDirectory(e)).
					Where(e => e != -1).
					ToArray(),
				(id) => simulationsByID.Add(id, null)
			);

		public static Simulation GetSimulation(int index)
			=> simulations[index];

		public static int GetSimulationCount()
			=> simulations.Count;
		
		public static Simulation GetSimulationById(int simID)
			=> simulationsByID[simID];

		public static Simulation StartNew()
		{
			return SimulationHandler.StartNew(int.MaxValue);
		}

		public static Simulation StartNew(int rounds)
		{
			Simulation s;

			// lock id generation until the simulation is added to the sim-by-id cache
			// this is so we don't accidentally create duplicate ids if the program is refactored to run on multiple threads
			lock(idLockObject)
			{
				s = new Simulation(GenerateSimulationId());
				simulations.Add(s);
				simulationsByID.Add(s.ID, s);
			}
			
			s.RoundPassed += SimulationRoundPassed;
			s.Start(rounds);
			return s;
		}
		
		// TODO: remove this if not neccessary
		private static void SimulationRoundPassed(object sender, RoundPassedEventArgs e)
		{
			Console.WriteLine($"Round passed: {e.Round}");
		}

		private static int GenerateSimulationId()
		{
			int id = 0;
			do
			{
				id = idGenerator.Next();
			}
			while(simulationsByID.ContainsKey(id)); // this also contains archive simulations
			return id;
		}
	}
}
#pragma warning restore 1591

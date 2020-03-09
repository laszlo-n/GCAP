using System;

using EFOP.Archives;

using JSONSerializer;

namespace EFOP
{
    class Program
    {
        static void Main(string[] args)
        {
            //string json = "{\"key1\": \"value1\"  ,\"key2\": [\"val1\", 3, 5.7, {\"onlykey\":  \"onlyval\"}], \"no\": null, \"a key\": false}   ";
            //Console.WriteLine(new JSONObject(json).ToString());
			//Simulation s = SimulationHandler.StartNew(2000);
            //Console.WriteLine($"Started simulation with ID #{s.ID}.");

            Console.WriteLine("Loading simulation with ID: 1609954104...");
            SimulationArchive archive = new SimulationArchive(1609954104);
        }
    }
}

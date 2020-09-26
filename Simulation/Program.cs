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
            for (int i = 0; i < 10; i++)
            {
                SimulationHandler.StartNew(10000);
            }
            
            //Console.WriteLine("Loading simulation with ID: 575679314...");
            //SimulationArchive archive = new SimulationArchive(575679314);
        }
    }
}

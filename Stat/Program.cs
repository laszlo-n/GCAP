using System;

namespace Stat
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Szimuláció ID: ");
            int simID = int.Parse(Console.ReadLine());

            FamilyTreeBuilder.BuildTree(simID);
        }

        public static string GetSimDir(int uid)
        {
            return $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/GCAP/sim_{uid}";
        }
    }
}

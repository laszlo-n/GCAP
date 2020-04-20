using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Stat
{
    class Program
    {
        static Dictionary<int, string> directions = new Dictionary<int, string>
        {
            { 0, "B " },
            { 1, "BL" },
            { 2, "JF" },
            { 3, "J " },
            { 4, "JL" },
            { 5, "BL" }
        };

        static void Main(string[] args)
        {
            while(true)
            {
                Console.Write("> ");
                string[] command = Console.ReadLine().Split(' ');
                switch(command[0])
                {
                    case "load":
                        try
                        {
                            FamilyTreeBuilder.LoadTree(int.Parse(command[1]));
                            Console.WriteLine("Szimuláció betöltve");
                            command = new[] { "automaton", "by-family-size" };
                            goto case "automaton";
                        }
                        catch(Exception)
                        {
                            Console.WriteLine("Szimuláció betöltése sikertelen");
                        }
                        break;
                    case "unload":
                        FamilyTreeBuilder.Unload();
                        break;
                    case "exit":
                        return;
                    case "automaton":
                        int id;
                        if(int.TryParse(command[1], out id))
                        {
                            List<(int, string, int)> wiring = FamilyTreeBuilder.GetWiring(id);
                            foreach((int from, string through, int to) in wiring)
                            {
                                Console.WriteLine($"{directions[from]}--{through}-->{directions[to]}");
                            }
                        }
                        else
                        {
                            switch(command[1])
                            {
                                case "list":
                                    ReadOnlyCollection<FamilyTreeItem> families = FamilyTreeBuilder.Families;
                                    
                                    Action<FamilyTreeItem, int> listWithChildren = null;
                                    listWithChildren = (item, layer) =>
                                    {

                                        for(int i = 0; i < layer - 1; i ++)
                                        {
                                            Console.Write("| ");
                                        }
                                        if(layer != 0)
                                        {
                                            Console.Write("|-");
                                        }

                                        Console.WriteLine(item.UID);
                                        foreach(var child in item.Children)
                                        {
                                            listWithChildren(child, layer + 1);
                                        }
                                    };

                                    for(int i = 0; i < families.Count; i ++)
                                    {
                                        listWithChildren(families[i], 0);
                                    }
                                    break;
                                case "by-family-size":
                                    Dictionary<int, int> familyCounts = new Dictionary<int, int>();
                                    for(int i = 0; i < FamilyTreeBuilder.Families.Count; i ++)
                                    {
                                        int count = FamilyTreeBuilder.Families[i].RecursiveCount;
                                        if(familyCounts.ContainsKey(count))
                                        {
                                            familyCounts[count] ++;
                                        }
                                        else
                                        {
                                            familyCounts.Add(count, 1);
                                        }
                                    }

                                    List<(int, int)> tmpList = new List<(int, int)>();
                                    foreach(KeyValuePair<int, int> element in familyCounts)
                                    {
                                        tmpList.Add((element.Key, element.Value));
                                    }

                                    tmpList = tmpList.OrderByDescending(e => e.Item1).ToList();

                                    foreach((int size, int count) in tmpList)
                                    {
                                        Console.WriteLine($"{size} tagú család: {count} db");
                                    }

                                    Console.WriteLine("============");

                                    int[] categories = new int[8];
                                    for(int i = 0; i < categories.Length; i ++)
                                    {
                                        categories[i] = 0;
                                    }

                                    foreach((int size, int count) in tmpList)
                                    {
                                        if(size == 1)
                                        {
                                            categories[0] += count;
                                        }
                                        else if(size == 2)
                                        {
                                            categories[1] += count;
                                        }
                                        else if(size <= 4)
                                        {
                                            categories[2] += count;
                                        }
                                        else if(size <= 6)
                                        {
                                            categories[3] += count;
                                        }
                                        else if(size <= 12)
                                        {
                                            categories[4] += count;
                                        }
                                        else if(size <= 20)
                                        {
                                            categories[5] += count;
                                        }
                                        else if(size <= 30)
                                        {
                                            categories[6] += count;
                                        }
                                        else
                                        {
                                            categories[7] += count;
                                        }
                                    }

                                    for(int i = 0; i < categories.Length; i ++)
                                    {
                                        Console.Write($"{categories[i]}{(i == categories.Length - 1 ? "\n" : "\t" )}");
                                    }
                                    break;
                            }
                        }

                        break;
                    default:
                        Console.WriteLine("Imseretlen parancs");
                        break;
                }
            }
        }

        public static string GetSimDir(int uid)
        {
            return $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/GCAP/sim_{uid}";
        }
    }
}

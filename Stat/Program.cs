using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
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

        static Action<FamilyTreeItem, int> listWithChildren = null;

        static void Main(string[] args)
        {
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

            while(true)
            {
                Console.Write("> ");
                string[] command = Console.ReadLine().Split(' ');
                switch(command[0])
                {
                    case "load":
                        try
                        {
                            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                            sw.Start();
                            if(command.Length == 2)
                                FamilyTreeBuilder.LoadTree(int.Parse(command[1]));
                            else if(command[2] == "noorder")
                                FamilyTreeBuilder.LoadTree(int.Parse(command[1]), int.MaxValue, false);
                            sw.Stop();
                            Console.WriteLine($"Szimuláció betöltve, {sw.ElapsedMilliseconds} ms\n");
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
                    case "alldeath":
                        List<int> ids = new List<int>();
                        using(StreamReader be = new StreamReader("/home/sisisisi/Asztal/in.txt"))
                        {
                            while(!be.EndOfStream)
                            {
                                ids.Add(int.Parse(be.ReadLine()));
                            }
                        }

                        using(StreamWriter ki = new StreamWriter("/home/sisisisi/Asztal/out.txt"))
                        {
                            for(int i = 0; i < ids.Count; i ++)
                            {
                                FamilyTreeBuilder.LoadTree(ids[i]);
                                Console.WriteLine($"Betöltve: {i + 1}, {ids[i]}");
                                Queue<FamilyTreeItem> qu = new Queue<FamilyTreeItem>();
                                int al = 0, dd = 0;

                                for(int j = 0; j < FamilyTreeBuilder.Families.Count; j ++)
                                {
                                    qu.Enqueue(FamilyTreeBuilder.Families[j]);
                                }

                                while(qu.Count != 0)
                                {
                                    FamilyTreeItem automaton = qu.Dequeue();
                                    if(automaton.DeathRound == -1)
                                        al ++;
                                    else
                                        dd ++;

                                    for(int j = 0; j < automaton.Children.Count; j ++)
                                    {
                                        qu.Enqueue(automaton.Children[j]);
                                    }
                                }

                                ki.WriteLine($"{ids[i]}\t{al}\t{dd}");
                                Console.WriteLine($"Kész: {i + 1}");
                            }
                        }
                        break;
                    case "death":
                        Queue<FamilyTreeItem> q = new Queue<FamilyTreeItem>();
                        int alive = 0, dead = 0;

                        for(int i = 0; i < FamilyTreeBuilder.Families.Count; i ++)
                        {
                            q.Enqueue(FamilyTreeBuilder.Families[i]);
                        }

                        while(q.Count != 0)
                        {
                            FamilyTreeItem automaton = q.Dequeue();
                            if(automaton.DeathRound == -1)
                                alive ++;
                            else
                                dead ++;

                            for(int i = 0; i < automaton.Children.Count; i ++)
                            {
                                q.Enqueue(automaton.Children[i]);
                            }
                        }

                        Console.WriteLine($"Él: {alive}\nHalott: {dead}");

                        break;
                    case "automaton":
                        int id;
                        if(int.TryParse(command[1], out id))
                        {
                            FamilyTreeItem automaton = FamilyTreeBuilder.GetAutomaton(id);

                            if(command.Length > 2)
                            {
                                switch(command[2])
                                {
                                    case "image":
                                        Bitmap b = automaton.GenerateImage();
                                        string path = $"{Program.GetSimDir(FamilyTreeBuilder.CurrentSimID)}/img/{command[1]}.png";
                                        if(!System.IO.Directory.Exists(Path.GetDirectoryName(path)))
                                        {
                                            Directory.CreateDirectory(Path.GetDirectoryName(path));
                                        }
                                        
                                        b.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                                        Console.WriteLine($"Kép sikeresen elmentve:\n{path}");
                                        break;
                                    case "list":
                                        listWithChildren(automaton, 0);
                                        break;
                                }
                            }
                            else
                            {
                                // kezdőállapot
                                Console.WriteLine($"Kezdőállapot: {automaton.StartState}\n");
                                Console.WriteLine($"Születés: {automaton.SpawnRound}. kör");
                                Console.WriteLine($"Halál: {(automaton.DeathRound == -1 ? "-" : automaton.DeathRound.ToString() + ". kör")}\n");

                                // bekötés
                                Console.WriteLine("Bekötés:");
                                ReadOnlyDictionary<(int, string), int> wiring = automaton.wiring;
                                foreach(KeyValuePair<(int, string), int> change in wiring)
                                {
                                    Console.WriteLine($"{directions[change.Key.Item1]}--{change.Key.Item2}-->{directions[change.Value]}");
                                }
                                Console.WriteLine();

                                // reakció környezetekre
                                Console.WriteLine("Reakciók:");
                                Console.WriteLine($"Üres környezet: {automaton.ComputeResult("uuuuuu")}");
                                Console.WriteLine($"Fa balra      : {automaton.ComputeResult("tuuuuu")}");
                                Console.WriteLine($"Fa bal-fent   : {automaton.ComputeResult("utuuuu")}");
                                Console.WriteLine($"Fa jobb-fent  : {automaton.ComputeResult("uutuuu")}");
                                Console.WriteLine($"Fa jobbra     : {automaton.ComputeResult("uuutuu")}");
                                Console.WriteLine($"Fa jobb-lent  : {automaton.ComputeResult("uuuutu")}");
                                Console.WriteLine($"Fa bal-lent   : {automaton.ComputeResult("uuuuut")}");
                                Console.WriteLine($"Or. balra      : {automaton.ComputeResult("luuuuu")}");
                                Console.WriteLine($"Or. bal-fent   : {automaton.ComputeResult("uluuuu")}");
                                Console.WriteLine($"Or. jobb-fent  : {automaton.ComputeResult("uuluuu")}");
                                Console.WriteLine($"Or. jobbra     : {automaton.ComputeResult("uuuluu")}");
                                Console.WriteLine($"Or. jobb-lent  : {automaton.ComputeResult("uuuulu")}");
                                Console.WriteLine($"Or. bal-lent   : {automaton.ComputeResult("uuuuul")}");
                                Console.WriteLine($"Aut. balra      : {automaton.ComputeResult("auuuuu")}");
                                Console.WriteLine($"Aut. bal-fent   : {automaton.ComputeResult("uauuuu")}");
                                Console.WriteLine($"Aut. jobb-fent  : {automaton.ComputeResult("uuauuu")}");
                                Console.WriteLine($"Aut. jobbra     : {automaton.ComputeResult("uuuauu")}");
                                Console.WriteLine($"Aut. jobb-lent  : {automaton.ComputeResult("uuuuau")}");
                                Console.WriteLine($"Aut. bal-lent   : {automaton.ComputeResult("uuuuua")}");
                            }
                        }
                        else
                        {
                            switch(command[1])
                            {
                                case "list":
                                    ReadOnlyCollection<FamilyTreeItem> families = FamilyTreeBuilder.Families;

                                    for(int i = 0; i < families.Count; i ++) listWithChildren(families[i], 0);

                                    break;

                                case "by-family-size":
                                    var stats = AnalyzeSim();
                                    Console.Write(stats[0]);
                                    Console.WriteLine("--------------------------------");
                                    Console.Write(stats[1]);
                                    break;
                            }
                        }

                        break;
                    case "explore":
                        switch (command[1])
                        {
                            case "simulations":
                                var simlist = File.ReadAllLines($"in_simlist.txt");
                                var sw = new System.Diagnostics.Stopwatch();

                                using (var details = new StreamWriter($"out_details.txt", false, System.Text.Encoding.UTF8))
                                using (var summary = new StreamWriter($"out_summary.txt", false, System.Text.Encoding.UTF8))
                                {
                                    foreach (var sim in simlist)
                                    {
                                        Console.WriteLine($"A(z) {sim} számú szimuláció feldolgozása...");
                                        sw = System.Diagnostics.Stopwatch.StartNew();

                                        FamilyTreeBuilder.LoadTree(int.Parse(sim));
                                        var stats = AnalyzeSim();

                                        details.WriteLine($"A(z) {sim} számú szimuláció adatai:");
                                        details.WriteLine();
                                        details.WriteLine(stats[0]);
                                        details.Flush();

                                        summary.Write(stats[1]);
                                        summary.Flush();

                                        sw.Stop();
                                        Console.WriteLine($"A(z) {sim} számú szimuláció feldolgozása sikeres. Eltelt idő: {sw.ElapsedMilliseconds / 1000} mp.");
                                    }
                                }
                                break;
                            case "generations":
                                var simavgs200 = new List<List<double>>();
                                var simtops200 = new List<List<int>>();
                                var simnames200 = new List<string>();

                                var simavgs500 = new List<List<double>>();
                                var simtops500 = new List<List<int>>();
                                var simnames500 = new List<string>();

                                foreach (var simdir in GetSimDirs())
                                {
                                    var avglist = new List<double>();
                                    var toplist = new List<int>();

                                    var referredDirName = GetSimDirNames().Where(i => simdir.Contains(i)).FirstOrDefault();

                                    FamilyTreeBuilder.LoadTree(int.Parse(referredDirName.Substring(4)));
                                    var limit = FamilyTreeBuilder.RoundCount;

                                    for (var i = 0; i < limit; i++)
                                    {
                                        FamilyTreeBuilder.LoadTree(int.Parse(referredDirName.Substring(4)), i);

                                        Dictionary<int, int> familyCounts = new Dictionary<int, int>();
                                        for (int j = 0; j < FamilyTreeBuilder.Families.Count; j++)
                                        {
                                            int count = FamilyTreeBuilder.Families[j].RecursiveCount;
                                            if (familyCounts.ContainsKey(count)) familyCounts[count]++;
                                            else familyCounts.Add(count, 1);
                                        }

                                        List<(int, int)> tmpList = new List<(int, int)>();
                                        foreach (KeyValuePair<int, int> element in familyCounts)
                                            tmpList.Add((element.Key, element.Value));

                                        //tmpList = tmpList.OrderByDescending(e => e.Item1).ToList();

                                        avglist.Add(tmpList.Average(e => e.Item1));
                                        toplist.Add(tmpList.Max(e => e.Item1));
                                    }

                                    if (limit == 200)
                                    {
                                        simavgs200.Add(avglist);
                                        simtops200.Add(toplist);
                                        simnames200.Add(referredDirName);
                                    }
                                    else if (limit == 500)
                                    {
                                        simavgs500.Add(avglist);
                                        simtops500.Add(toplist);
                                        simnames500.Add(referredDirName);
                                    }
                                }

                                if (simnames200.Count != 0)
                                {
                                    Console.WriteLine("200-as körszámú szimulációk utódstatisztikái (átlagok):\n");

                                    // header
                                    Console.Write("Kör");
                                    foreach (var name in simnames200) Console.Write($"\t{name}");
                                    Console.WriteLine();

                                    for (var round = 0; round < 200; round++)
                                    {
                                        Console.Write($"{round + 1}. kör");
                                        foreach (var simavg in simavgs200)
                                        {
                                            Console.Write($"\t{simavg[round]}");
                                        }
                                        Console.WriteLine();
                                    }

                                    Console.WriteLine();

                                    Console.WriteLine("200-as körszámú szimulációk utódstatisztikái (maximumok):\n");

                                    // header
                                    Console.Write("Kör");
                                    foreach (var name in simnames200) Console.Write($"\t{name}");
                                    Console.WriteLine();

                                    for (var round = 0; round < 200; round++)
                                    {
                                        Console.Write($"{round + 1}. kör");
                                        foreach (var simtop in simtops200)
                                        {
                                            Console.Write($"\t{simtop[round]}");
                                        }
                                        Console.WriteLine();
                                    }
                                }

                                Console.WriteLine();

                                if (simnames500.Count != 0)
                                {
                                    Console.WriteLine("500-as körszámú szimulációk utódstatisztikái (átlagok):\n");

                                    // header
                                    Console.Write("Kör");
                                    foreach (var name in simnames500) Console.Write($"\t{name}");
                                    Console.WriteLine();

                                    for (var round = 0; round < 500; round++)
                                    {
                                        Console.Write($"{round + 1}. kör");
                                        foreach (var simavg in simavgs500)
                                        {
                                            Console.Write($"\t{simavg[round]}");
                                        }
                                        Console.WriteLine();
                                    }

                                    Console.WriteLine();

                                    Console.WriteLine("500-as körszámú szimulációk utódstatisztikái (maximumok):\n");

                                    // header
                                    Console.Write("Kör");
                                    foreach (var name in simnames500) Console.Write($"\t{name}");
                                    Console.WriteLine();

                                    for (var round = 0; round < 500; round++)
                                    {
                                        Console.Write($"{round + 1}. kör");
                                        foreach (var simtop in simtops500)
                                        {
                                            Console.Write($"\t{simtop[round]}");
                                        }
                                        Console.WriteLine();
                                    }
                                }
                                break;
                        }
                        break;
                    default:
                        Console.WriteLine("Ismeretlen parancs");
                        break;
                }
            }
        }

        public static string GetSimDir(int uid)
        {
            return $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/GCAP/sim_{uid}";
        }

        public static string[] GetSimDirs()
        {
            return Directory.GetDirectories($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/GCAP/");
        }

        public static string[] GetSimDirNames()
        {
            var dirinfo = new DirectoryInfo($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/GCAP/");
            return dirinfo.GetDirectories("*").Select(d => d.Name).ToArray();
        }

        public static string[] AnalyzeSim()
        {
            var details = new StringWriter();
            var summary = new StringWriter();

            Dictionary<int, int> familyCounts = new Dictionary<int, int>();
            for (int i = 0; i < FamilyTreeBuilder.Families.Count; i++)
            {
                int count = FamilyTreeBuilder.Families[i].RecursiveCount;
                if (familyCounts.ContainsKey(count)) familyCounts[count]++;
                else familyCounts.Add(count, 1);
            }

            List<(int, int)> tmpList = new List<(int, int)>();
            foreach (KeyValuePair<int, int> element in familyCounts)
            {
                tmpList.Add((element.Key, element.Value));
            }

            tmpList = tmpList.OrderByDescending(e => e.Item1).ToList();

            foreach ((int size, int count) in tmpList) details.WriteLine($"{size} tagú család: {count} db");

            int[] categories = new int[10];

            foreach ((int size, int count) in tmpList)
            {
                if (size == 1)
                {
                    categories[0] += count;
                }
                else if (size == 2)
                {
                    categories[1] += count;
                }
                else if (size <= 4)
                {
                    categories[2] += count;
                }
                else if (size <= 6)
                {
                    categories[3] += count;
                }
                else if (size <= 12)
                {
                    categories[4] += count;
                }
                else if (size <= 20)
                {
                    categories[5] += count;
                }
                else if (size <= 30)
                {
                    categories[6] += count;
                }
                else if (size <= 50)
                {
                    categories[7] += count;
                }
                else if (size <= 100)
                {
                    categories[8] += count;
                }
                else
                {
                    categories[9] += count;
                }
            }

            for (int i = 0; i < categories.Length; i++) summary.Write($"{categories[i]}{(i == categories.Length - 1 ? "\n" : "\t")}");

            return new string[] { details.ToString(), summary.ToString() };
        }
    }
}

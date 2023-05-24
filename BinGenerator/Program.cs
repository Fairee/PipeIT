using System;


namespace BinGenerator
{
    //BE CAREFUL - UNLIKE IN UNITY HERE THE LONGITUDE IS X AND LATITUDE IS Y - sorry, I've changed it midway, but the shapefile gives longitude first (but the rest of things usually use latitude first) :(
    //ALSO BEWARE THAT THERE IS NO MEMORY MANAGMENT HERE, IF THE FILE IS TOO BIG OR YOU MAKE DIVIDE THE SPACE INTO TOO MANY GRID CELLS YOULL GET OUT OF MEMORY ERROR
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 4)
            {
                Console.WriteLine("Please provide two int values first is the amount of meters of a zone into which the whole area should be diveded. the other is the amount of meters of areas into which the zones should be diveded.");
                Console.WriteLine("The number of areas has to dived the number of zones without leftover.");
                Console.WriteLine("And a name for the file that will be created");
                Console.WriteLine("Finally the full path to the .shp file");
                return;
            }

            int zone, area;

            if (int.TryParse(args[0], out zone) && int.TryParse(args[1], out area))
            {
                Generator gen = new Generator();
                gen.extractRawData(args[3]);
                gen.generateGrid(zone, area);
                string name = args[2] + ".bin";
                gen.CreateSearchFile(name);
            }
            else {
                Console.WriteLine("Invalid Input");
            }

        }
    }
}

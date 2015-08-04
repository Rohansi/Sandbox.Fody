using System;
using System.IO;

class Program
{
    static void Main()
    {
        using (var fs = new FileStream("file.txt", FileMode.Open))
        using (var reader = new StreamReader(fs))
        {
            Console.WriteLine(reader.ReadToEnd());
        }

        Console.WriteLine(typeof(Program));
        Console.ReadLine();
    }
}

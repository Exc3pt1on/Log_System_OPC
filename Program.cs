using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Opc.UaFx.Client;

namespace DataLogger
{
    internal class Program
    {

        static void Main(string[] args)
        {

            while (true)
            {
                try
                {
                    string path = NameFile();
                    Connect(path);
                }
                catch (FormatException)
                {
                    Console.WriteLine("Invalid path name...");
                }
                catch (Exception)
                {
                    Console.WriteLine("Connection lost, trying to reconnect...");
                    throw;
                }
            }
            
        }
        static string NameFile()
        {
            Console.WriteLine("Name the log file: ");
            string path = "Logs/" + Console.ReadLine() + ".csv";
            WriteCsvHeader(path);
            Console.WriteLine("Storing data in " + path);
            return path;
        }
        static void Connect(string path)
        {
            using (var client = new OpcClient("opc.tcp://localhost:4840/")) //"opc.tcp://localhost:4841"
            {
                client.Connect();
                while (true)
                {
                    var temperature = client.ReadNode("ns=2;s=Temperature");
                    var heat = client.ReadNode("ns=2;s=Heat");
                    var fan = client.ReadNode("ns=2;s=Fan");
                    Console.WriteLine("Current Temperature is {0}°C, Heat is {1}v, Fan is {2}v", temperature, heat, fan);



                    if (path == ".csv")
                    {
                        // Use "" in filename to not store to file
                        Thread.Sleep(2000);
                    }
                    else
                    {
                        LogValuesToCsv(path, temperature.ToString(), heat.ToString(), fan.ToString());
                        Thread.Sleep(500);
                    }

                }
            }
        }
        static void WriteCsvHeader(string filePath)
        {
            // Write header to CSV file if the file doesn't exist
            if (!File.Exists(filePath))
            {
                using (StreamWriter writer = new StreamWriter(filePath, false))
                {
                    writer.WriteLine("Timestamp;Temperature (°C);Heat (V);Fan (V)");
                }
            }
        }

        static void LogValuesToCsv(string filePath, object temperature, object heat, object fan)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");//.ff
            // Log values to CSV file
            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                writer.WriteLine($"{timestamp};{temperature};{heat};{fan}");
            }
        }

    }
}

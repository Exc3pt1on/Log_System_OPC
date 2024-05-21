using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IdentityModel.Protocols.WSTrust;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Opc.UaFx.Client;
using System.Configuration;
using System.Data;

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
                    var highLimitTemp = client.ReadNode("ns=2;s=HighLimitTemp");
                    var lowLimitTemp = client.ReadNode("ns=2;s=LowLimitTemp");
                    var highLimitFan = client.ReadNode("ns=2;s=HighLimitFan");
                    var lowLimitFan = client.ReadNode("ns=2;s=LowLimitFan");
                    var highLimitHeat = client.ReadNode("ns=2;s=HighLimitHeat");
                    var lowLimitHeat = client.ReadNode("ns=2;s=LowLimitHeat");
                    double tempD = 0, fanD = 0, heatD = 0, hTempD = 0, lTempD = 0, hFanD = 0, lFanD = 0, hHeatD = 0, lHeatD = 0;
                    Console.WriteLine($"Current Temperature is {temperature}°C, Heat is {heat}v, Fan is {fan}v");



                    if (path == "Logs/.csv")
                    {
                        // Use "" in filename to not store data
                        Thread.Sleep(2000);
                    }
                    else
                    {
                        LogValuesToCsv(path, temperature.ToString(), heat.ToString(), fan.ToString());

                        double.TryParse(temperature.ToString(), out tempD);
                        double.TryParse(fan.ToString(), out fanD);
                        double.TryParse(heat.ToString(), out heatD);

                        double.TryParse(highLimitTemp.ToString(), out hTempD);
                        double.TryParse(lowLimitTemp.ToString(), out lTempD);

                        double.TryParse(highLimitFan.ToString(), out hFanD);
                        double.TryParse(lowLimitFan.ToString(), out lFanD);

                        double.TryParse(highLimitHeat.ToString(), out hHeatD);
                        double.TryParse(lowLimitHeat.ToString(), out lHeatD);

                        WriteToDatabase("temp1", tempD, hTempD, lTempD);
                        WriteToDatabase("heater1", heatD, hHeatD, lHeatD);
                        WriteToDatabase("fan1", fanD, hFanD, lFanD);
                        Thread.Sleep(500);
                    }


                }
            }
        }

        static void WriteToDatabase(string name, double value, double highLim, double lowLim)
        {
            // Method for wrinting the sensor data to the database
            // Copied from Database Systems Assignment in IIA2017-1 24V Industrial Information Technology
            // A connection string is created in the 'App.config' file for easy implementation of database connectionstring

            string connectionString = ConfigurationManager.ConnectionStrings["ScadaDB"].ConnectionString;
            DateTime dateTime = DateTime.Now;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    using (SqlCommand command = new SqlCommand("InsertDeviceValue", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.Add("@DeviceName", SqlDbType.NChar).Value = name;
                        command.Parameters.Add("@DeviceValue", SqlDbType.Float).Value = value;
                        command.Parameters.Add("@DateTime", SqlDbType.DateTime).Value = dateTime;
                        command.Parameters.Add("@HighLimit", SqlDbType.Float).Value = highLim;
                        command.Parameters.Add("@LowLimit", SqlDbType.Float).Value = lowLim;
                        connection.Open();
                        command.ExecuteNonQuery();
                        connection.Close();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred: " + ex.Message);
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

        static void LogToDatabase()
        {

        }
    }
}

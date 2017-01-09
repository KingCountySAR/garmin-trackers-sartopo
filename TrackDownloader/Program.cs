using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Garmin.Device.Core;
using Newtonsoft.Json;

namespace TrackDownloader
{
  class Program
  {
    static void Main(string[] args)
    {

      string server = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:8080";

      string input = string.Empty;

      while (input.Length == 0)
      {
        GarminDevice baseStation = null;

        var list = GarminDevice.DiscoverDevices();
        list.ForEach(f => {
          var reader = new GarminReader(f);
          var info = reader.ReadInfo();
          Console.WriteLine(info.Id + ": " + info.Description);

          if (baseStation == null && info.SupportedProtocols != null && info.SupportedProtocols.Any(p => p.tag == (byte)'A' && p.data == 1100))
          {
            baseStation = f;
          }
          else
          {
            f.Dispose();
          }
        });


        if (baseStation != null)
        {
          var reader = new GarminReader(baseStation);

          using (var file = File.Open("track.log", FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
          {
            using (var writer = new StreamWriter(file))
            {
              writer.AutoFlush = true;

              HttpClient web = new HttpClient();

              while (true)
              {
                var packet = reader.WaitForPacket(3078);
                var d1100 = new D1100TrackedAsset(packet.data);

                ConsoleColor color = d1100.Color > 15 ? ConsoleColor.White : (ConsoleColor)d1100.Color;

                Console.SetCursorPosition(0, (d1100.Index - 202) * 2 + 2);
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(d1100.Identifier.PadRight(4));
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"  {d1100.Time:T}  Batt:{d1100.Battery}/4  Comm:{d1100.Comm}/5  GPS:{d1100.Gps}/3  ID:{d1100.CollarId / 256}-{d1100.CollarId % 256}");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"           {d1100.Position.Latitude:0.000000}, {d1100.Position.Longitude:0.000000}     {d1100.DogStatus.ToString().PadRight(10)}");
                Console.ForegroundColor = ConsoleColor.Gray;
                writer.WriteLine(JsonConvert.SerializeObject(d1100));
                Console.SetCursorPosition(0, 1);

                #pragma warning disable 4014
                Task.Run(async () =>
                {
                  try
                  {
                    await web.GetAsync($"{server}/rest/location/update/position?lat={d1100.Position.Latitude}&lng={d1100.Position.Longitude}&id=APRS:{d1100.Identifier}");
                  }
                  catch
                  {

                  }
                }).ConfigureAwait(false);
                #pragma warning restore 4014
              }
            }
          }
        }


        Console.WriteLine("Found " + list.Count + " devices. Enter to go again.");
        input = Console.ReadLine();

      }
    }
  }
}

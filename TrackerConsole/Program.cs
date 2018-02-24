using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Garmin.Device.Core;
using Newtonsoft.Json;

namespace TrackerConsole
{
  enum Args
  {
    Server = 0,
    Callsign,
    ReplayFile,
    ReplaySpeed
  }

  class Program
  {
    static string callsign = null;

    static void Main(string[] args)
    {
      ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11;


      var tracker = new Program(args.Length > (int)Args.Server ? args[(int)Args.Server].TrimEnd('/') : "http://localhost:8080");
      callsign = args.Length > (int)Args.Callsign ? args[(int)Args.Callsign] : "COLLAR";

      Console.Clear();
      if (args.Length <= (int)Args.ReplayFile)
      {
        tracker.Start();
      }
      else
      {
        int speedUp = 1;
        if (args.Length == (int)Args.ReplaySpeed + 1)
        {
          int.TryParse(args[(int)Args.ReplaySpeed], out speedUp);
        }
        tracker.Replay(args[(int)Args.ReplayFile], speedUp);
      }
    }

    StreamWriter _writer = null;
    readonly string _server;
    readonly HttpClient _web = new HttpClient();
    readonly string[] _errors = new string[5];
    int errorIndex = 0;
    Dictionary<string, D1100TrackedAsset> latest = new Dictionary<string, D1100TrackedAsset>();
    object syncLock = new object();

    public Program(string server)
    {
      _server = server;
    }

    public void Start()
    {
      string input = string.Empty;

      while (input.Length == 0)
      {
        GarminDevice baseStation = null;

        var list = GarminDevice.DiscoverDevices();
        list.ForEach(f =>
        {
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

          while (true)
          {
            try
            {
              var packet = reader.WaitForPacket(3078);
              Update(new D1100TrackedAsset(packet.data));
            }
            catch (Exception e)
            {
              LogException(e.Message);
            }
          }
        }

        Console.WriteLine("Found " + list.Count + " devices. Enter to go again.");
        input = Console.ReadLine();

      }
    }

    public void Replay(string filename, int speedUp)
    {
      string line;
      DateTimeOffset lastTime = DateTimeOffset.MinValue;
      using (var reader = new StreamReader(filename))
      {
        while ((line = reader.ReadLine()) != null)
        {
          var entry = JsonConvert.DeserializeObject<D1100TrackedAsset>(line);
          var delay = 0;
          if (lastTime != DateTimeOffset.MinValue)
          {
            delay = (int)((entry.Time - lastTime).TotalSeconds * 1000) / speedUp;
          }
          lastTime = entry.Time;
          if (delay > 0) Thread.Sleep(delay);
          Update(entry);
        }
      }
    }

    private void Update(D1100TrackedAsset entry)
    {
      if (_writer == null)
      {
        var filename = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"track-{DateTime.Now.ToString("yyyy-MM-dd-HHmm")}.log");
        _writer = new StreamWriter(File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite));
        _writer.AutoFlush = true;
      }

      _writer.WriteLine(JsonConvert.SerializeObject(entry));
      lock (syncLock)
      {
        if (latest.ContainsKey(entry.Identifier))
        {
          latest[entry.Identifier] = entry;
        }
        else
        {
          latest.Add(entry.Identifier, entry);
        }
      }
#pragma warning disable 4014
      Task.Run(async () =>
      {
        try
        {
          string identifier = entry.Identifier;
          if (!string.IsNullOrWhiteSpace(callsign)) identifier = callsign + "-" + identifier;

          await _web.GetAsync($"{_server}/rest/location/update/position?lat={entry.Position.Latitude}&lng={entry.Position.Longitude}&id=FLEET:{identifier}");
        }
        catch (HttpRequestException ex)

        {
          LogException(ex.InnerException.Message);
        }
        catch (Exception ex)
        {
          LogException(ex.Message);
        }
      }).ConfigureAwait(false);
#pragma warning restore 4014

      Print();
    }

    private void LogException(string message)
    {
      lock (syncLock)
      {
        _errors[errorIndex] = $"{DateTime.Now:T} - {message}";
        errorIndex = (errorIndex + 1) % _errors.Length;
      }
      Print();
    }

    private void Print()
    {
      int width = Console.WindowWidth - 1;
      lock (syncLock)
      {
        Console.SetCursorPosition(0, 0);
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("Garmin Alpha Track Download v" + typeof(Program).Assembly.GetName().Version);
        Console.WriteLine("Logging to " + _server);
        Console.WriteLine();
        foreach (var p in latest.Keys.OrderBy(f => f).Select(f => latest[f]))
        {
          Console.ForegroundColor = ConsoleColor.White;
          string id = p.Identifier.PadRight(4);
          Console.Write(id);
          Console.ForegroundColor = ConsoleColor.Gray;
          Console.WriteLine($"  {p.Time:T}  Batt:{p.Battery}/4  Comm:{p.Comm}/5  GPS:{p.Gps}/3  ID:{p.CollarId / 256}-{p.CollarId % 256}".PadRight(width - id.Length));
          Console.ForegroundColor = ConsoleColor.Green;
          Console.WriteLine($"           {p.Position.Latitude:0.000000}, {p.Position.Longitude:0.000000}     {p.DogStatus}".PadRight(width));
          Console.WriteLine(string.Empty.PadRight(width));
        }

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(string.Empty.PadRight(width));
        for (var i = 0; i < _errors.Length; i++)
        {
          foreach (var line in _errors[(i + errorIndex) % _errors.Length].Wrap(width))
          {
            Console.WriteLine(line.PadRight(width));
          }
        }
      }
    }
  }
}

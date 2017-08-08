using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using TrackerConsole;

namespace log_to_gpx
{
  class Team
  {
    public StringBuilder sb;
    public int count;
    public int misses;
    public DateTimeOffset lastPoint;
    public Team(D1100TrackedAsset packet)
    {
      sb = new StringBuilder($"<trk><name>{packet.Identifier}</name><trkseg>");
      lastPoint = packet.Time.AddSeconds(-30);
    }
  }

  class Program
  {
    static void Main(string[] args)
    {
      if (args.Length != 1)
      {
        Console.Error.WriteLine("log-to-gpx <filename.gpx>");
        return;
      }

      Dictionary<string, Team> tracks = new Dictionary<string, Team>();

      foreach (var line in File.ReadLines(args[0]))
      {
        var packet = JsonConvert.DeserializeObject<D1100TrackedAsset>(line);
        Team team;
        if (!tracks.TryGetValue(packet.Identifier, out team))
        {
          team = new Team(packet);
          tracks.Add(packet.Identifier, team);
        }

        if ((packet.Time - team.lastPoint).TotalSeconds > 45)
        {
          var missed = (int)((packet.Time - team.lastPoint).TotalSeconds / 30);
          team.misses += missed;
          if (missed > 5)
          {
            Console.WriteLine($"{packet.Identifier} missed {missed} packets before {packet.Time.TimeOfDay}");
          }
        }
        team.count++;
        team.lastPoint = packet.Time;

        team.sb.AppendLine($"<trkpt lat=\"{packet.Position.Latitude}\" lon=\"{packet.Position.Longitude}\"><time>{packet.Time.ToString("s")}Z</time></trkpt>");
      }

      using (var writer = new StreamWriter("output.gpx"))
      {
        writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        writer.WriteLine("<gpx xmlns:gpxx=\"http://www.garmin.com/xmlschemas/GpxExtensions/v3\" xmlns=\"http://www.topografix.com/GPX/1/1\" version=\"1.1\" creator=\"log-to-gpx\">");
        foreach (var pair in tracks)
        {
          writer.WriteLine(pair.Value.sb.ToString());
          writer.WriteLine("</trkseg></trk>");
          Console.WriteLine($"{pair.Key}: Missed {pair.Value.misses}, got {pair.Value.count}");
        }
        writer.WriteLine("</gpx>");
      }
    }
  }
}

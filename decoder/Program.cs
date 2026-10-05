using System.Text.Json;
using FortniteReplayReader;
using ReplayExport;

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: ReplayExport.ReplayReader <file.replay>");
    return 2;
}

var file = Path.GetFullPath(args[0]);
if (!File.Exists(file))
{
    Console.Error.WriteLine($"Replay not found: {file}");
    return 3;
}

try
{
    var reader = new TelemetryReader();
    var replay = reader.ReadReplay(file);
    var players = replay.PlayerData.Select(p => new {
        name = p.PlayerName ?? (p.IsBot && p.Id is int bot ? TelemetryReader.BotId(bot) : "Unknown"),
        playerId = p.PlayerId,
        kills = p.Kills,
        teamKills = p.TeamKills,
        placement = p.Placement,
        bot = p.IsBot,
        replayOwner = p.IsReplayOwner,
        platform = p.Platform
    }).ToArray();

    var events = replay.Eliminations.Select(e => new {
        time = e.Info.StartTime / 1000.0,
        killer = e.Eliminator,
        victim = e.Eliminated,
        knocked = e.Knocked,
        weapon = e.GunType,
        distance = e.Distance,
        x = e.EliminatorInfo.Location?.X,
        y = e.EliminatorInfo.Location?.Y,
        z = e.EliminatorInfo.Location?.Z
    }).OrderBy(e => e.time).ToArray();

    var positions = reader.Positions.Select(p => new {
        time = p.T, player = p.Player, x = p.X, y = p.Y, z = p.Z,
        yaw = p.Yaw, pitch = p.Pitch, vx = p.Vx, vy = p.Vy, vz = p.Vz,
        downed = p.Downed, inStorm = p.InStorm, targeting = p.Targeting,
        crouched = p.Crouched, sprinting = p.Sprinting, jumping = p.Jumping, skydiving = p.Skydiving
    }).ToArray();

    var health = reader.Health.Select(h => new { time = h.T, player = h.Player, health = h.Health, shield = h.Shield }).ToArray();
    var teams = reader.Teams.Select(x => new { time = x.T, player = x.Player, team = x.TeamIndex }).ToArray();
    var weapons = reader.Weapons.Select(x => new { time = x.T, player = x.Player, weapon = reader.ClassOf(x.WeaponGuid), guid = x.WeaponGuid }).ToArray();
    var zones = replay.MapData.SafeZones.Select(z => new {
        startShrink = z.StartShrinkTime,
        finishShrink = z.FinishShrinkTime,
        radius = z.Radius,
        nextRadius = z.NextRadius,
        x = z.NextCenter?.X,
        y = z.NextCenter?.Y
    }).ToArray();

    var result = new {
        meta = new {
            file = Path.GetFileName(file),
            branch = replay.Header.Branch,
            mode = replay.GameData.CurrentPlaylist,
            duration = replay.Header.LengthInMS / 1000.0,
            players = players.Length,
            placement = players.Where(p => p.placement > 0).OrderBy(p => p.placement).FirstOrDefault()?.placement ?? 0,
            totalKills = events.Length
        },
        players,
        events,
        positions,
        health,
        teams,
        weapons,
        safezones = zones
    };

    Console.Out.Write(JsonSerializer.Serialize(result));
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.ToString());
    return 10;
}

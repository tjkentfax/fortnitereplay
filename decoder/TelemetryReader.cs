using FortniteReplayReader;
using FortniteReplayReader.Models.NetFieldExports;
using Unreal.Core;
using Unreal.Core.Contracts;
using Unreal.Core.Models;
using Unreal.Core.Models.Enums;

namespace ReplayExport;

public record HealthRow(double T, uint Channel, string? Player, float Health, float Shield);
public record TeamRow(double T, string Player, int TeamIndex);
public record WeaponRow(double T, uint Channel, string? Player, uint WeaponGuid);
public record PositionRow(double T, uint Channel, string? Player, double X, double Y, double Z, float? Yaw, float? Pitch, double? Vx, double? Vy, double? Vz, bool? Downed, bool? InStorm, bool? Targeting, bool? Crouched, bool? Sprinting, bool? Jumping, bool? Skydiving);

public class TelemetryReader() : ReplayReader(null, ParseMode.Full)
{
    public List<HealthRow> Health { get; } = new();
    public List<PositionRow> Positions { get; } = new();
    public List<TeamRow> Teams { get; } = new();
    public List<WeaponRow> Weapons { get; } = new();
    private readonly Dictionary<uint, uint> _pawnToStateGuid = new();
    private readonly Dictionary<uint, string> _stateChannelToPlayer = new();
    private readonly Dictionary<uint, uint> _guidToChannel = new();
    private readonly Dictionary<uint, string> _guidToClass = new();
    private readonly Dictionary<uint, uint> _lastWeapon = new();
    private double _time;

    public string? ClassOf(uint guid) => _guidToClass.GetValueOrDefault(guid);
    public static string BotId(int statePlayerId) => $"BOT_{statePlayerId}";

    public override void ReadDemoFrameIntoPlaybackPackets(FArchive archive)
    {
        var start = archive.Position;
        if (archive.NetworkVersion >= NetworkVersionHistory.HISTORY_MULTIPLE_LEVELS) archive.ReadInt32();
        _time = archive.ReadSingle();
        archive.Seek(start);
        base.ReadDemoFrameIntoPlaybackPackets(archive);
    }

    protected override void OnChannelOpened(uint channelIndex, NetworkGUID? actor)
    {
        if (actor != null) _guidToChannel[actor.Value] = channelIndex;
        base.OnChannelOpened(channelIndex, actor);
    }

    protected override void OnExportRead(uint channelIndex, INetFieldExportGroup? exportGroup)
    {
        switch (exportGroup)
        {
            case FortPlayerState state:
                var id = state.bIsABot == true ? state.BotUniqueId : state.UniqueId ?? state.UniqueID;
                if (string.IsNullOrEmpty(id) && state.bIsABot == true && ((int?)state.PlayerId ?? state.PlayerID) is int botNumber)
                    id = BotId(botNumber);
                if (!string.IsNullOrEmpty(id)) _stateChannelToPlayer[channelIndex] = id;
                if (state.TeamIndex is int team && _stateChannelToPlayer.GetValueOrDefault(channelIndex) is string teamPlayer)
                    Teams.Add(new TeamRow(_time, teamPlayer, team));
                break;
            case PlayerPawn pawn:
                if (pawn.PlayerState.HasValue) _pawnToStateGuid[channelIndex] = pawn.PlayerState.Value;
                if (pawn.CurrentWeapon is uint weapon && _lastWeapon.GetValueOrDefault(channelIndex) != weapon)
                {
                    _lastWeapon[channelIndex] = weapon;
                    Weapons.Add(new WeaponRow(_time, channelIndex, PlayerForChannel(channelIndex), weapon));
                }
                if (pawn.ReplicatedMovement is FRepMovement m && m.Location != null)
                {
                    Positions.Add(new PositionRow(_time, channelIndex, PlayerForChannel(channelIndex), m.Location.X, m.Location.Y, m.Location.Z,
                        m.Rotation?.Yaw, m.Rotation?.Pitch, m.LinearVelocity?.X, m.LinearVelocity?.Y, m.LinearVelocity?.Z,
                        pawn.bIsDBNO, pawn.bIsInAnyStorm, pawn.bIsTargeting, pawn.bIsCrouched, pawn.bIsSprinting, pawn.bIsJumping, pawn.bIsSkydiving));
                }
                break;
            case HealthSet hs:
                Health.Add(new HealthRow(_time, channelIndex, PlayerForChannel(channelIndex), hs.HealthCurrentValue, hs.ShieldCurrentValue));
                break;
        }
        base.OnExportRead(channelIndex, exportGroup);
    }

    private string? PlayerForChannel(uint channel)
    {
        if (_pawnToStateGuid.TryGetValue(channel, out var stateGuid) && _guidToChannel.TryGetValue(stateGuid, out var stateChannel) && _stateChannelToPlayer.TryGetValue(stateChannel, out var player)) return player;
        return _stateChannelToPlayer.GetValueOrDefault(channel);
    }
}

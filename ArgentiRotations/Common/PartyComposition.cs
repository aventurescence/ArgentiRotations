using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.DalamudServices;

namespace ArgentiRotations.Common;

public class PartyComposition
{
    private static ulong _hostileTargetId = 0;
    private static IPlayerCharacter Player => ECommons.GameHelpers.Player.Object;

    private static IBattleChara? HostileTarget
    {
        get => Svc.Objects.SearchById(_hostileTargetId) as IBattleChara;
        set => _hostileTargetId = value?.GameObjectId ?? 0UL;
    }

    public static bool HasBuffs
    {
        get
        {
            StatusList();

            if (Buffs.Count == 0) return false;

            // Check if player has any buff from our list, and it's not about to expire
            var playerHasBuffs = Buffs.Where(buff => buff.Type == StatusType.Buff)
                .Any(buff => Player.HasStatus(false, buff.Ids) &&
                             !Player.WillStatusEnd(0, false, buff.Ids));

            // Check if target has any debuff from our list, and it's not about to expire
            var targetHasDebuffs = HostileTarget != null &&
                                   Buffs.Where(buff => buff.Type == StatusType.Debuff)
                                       .Any(buff => HostileTarget.HasStatus(false, buff.Ids) &&
                                                    !HostileTarget.WillStatusEnd(0, false, buff.Ids));
            return playerHasBuffs || targetHasDebuffs;
        }
    }

    public static List<StatusInfo> Buffs { get; } = [];

    public static void StatusList()
    {
        Buffs.Clear();
        var processedJobs = new HashSet<string>();

        if (CustomRotation.PartyComposition == null)
        {
            var abbr = Player.ClassJob.Value.Abbreviation.ToString();
            AddJobBuffs(abbr, processedJobs);
        }
        else
        {
            foreach (var job in CustomRotation.PartyComposition)
            {
                var abbr = job.Value.Abbreviation.ToString();
                AddJobBuffs(abbr, processedJobs);
            }
        }
    }

    private static readonly Dictionary<string, List<StatusInfo>> JobBuffs = new()
    {
        { "AST", [new StatusInfo("Divination", "AST", StatusType.Buff, StatusID.Divination)] },
        { "BRD", [
                new StatusInfo("Battle Voice", "BRD", StatusType.Buff, StatusID.BattleVoice),
                new StatusInfo("Radiant Finale", "BRD", StatusType.Buff, StatusID.RadiantFinale_2964,
                    StatusID.RadiantFinale)]
        },
        { "DNC", [new StatusInfo("Technical Finish", "DNC", StatusType.Buff, StatusID.TechnicalFinish)] },
        { "DRG", [new StatusInfo("Battle Litany", "DRG", StatusType.Buff, StatusID.BattleLitany)] },
        { "MNK", [new StatusInfo("Brotherhood", "MNK", StatusType.Buff, StatusID.Brotherhood)] },
        {
            "NIN", [
                new StatusInfo("Mug", "NIN", StatusType.Debuff, StatusID.Mug),
                new StatusInfo("Dokumori", "NIN", StatusType.Debuff, StatusID.Dokumori, StatusID.Dokumori_4303)
            ]
        },
        { "PCT", [new StatusInfo("Starry Muse", "PCT", StatusType.Buff, StatusID.StarryMuse)] },
        { "RPR", [new StatusInfo("Arcane Circle", "RPR", StatusType.Buff, StatusID.ArcaneCircle)] },
        {
            "RDM", [new StatusInfo("Embolden", "RDM", StatusType.Buff, StatusID.Embolden, StatusID.Embolden_1297)]
        },
        {
            "SCH",
            [
                new StatusInfo("Chain Stratagem", "SCH", StatusType.Debuff, StatusID.ChainStratagem,
                    StatusID.ChainStratagem_1406)
            ]
        },
        { "SMN", [new StatusInfo("Searing Light", "SMN", StatusType.Buff, StatusID.SearingLight)] }
    };

    private static void AddJobBuffs(string abbr, HashSet<string> processedJobs)
    {
        if (!processedJobs.Add(abbr)) return;

        if (JobBuffs.TryGetValue(abbr, out var buffs))
        {
            Buffs.AddRange(buffs);
        }
    }
}



public enum StatusType
{
    Buff,
    Debuff
}

public class StatusInfo(string name, string jobAbbr,StatusType type, params StatusID[] ids)
{
    public StatusID[] Ids { get; } = ids;
    public string Name { get; } = name;
    public string JobAbbr { get; } = jobAbbr;
    public StatusType Type { get; } = type;
}
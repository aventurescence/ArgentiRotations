using ECommons.ExcelServices;
namespace ArgentiRotations.Common;

public enum DancePartnerPrio : byte
{
    SAM = 1,
    PCT = 2,
    RPR = 3,
    VPR = 4,
    MNK = 5,
    NIN = 6,
    DRG = 7,
    BLM = 8,
    RDM = 9,
    SMN = 10,
    MCH = 11,
    BRD = 12,
    DNC = 13
}

public static class DancePartnerMapping
{
    private static readonly Dictionary<DancePartnerPrio, Job> JobMap = new()
    {
        { DancePartnerPrio.SAM, Job.SAM },
        { DancePartnerPrio.PCT, Job.PCT },
        { DancePartnerPrio.RPR, Job.RPR },
        { DancePartnerPrio.VPR, Job.VPR },
        { DancePartnerPrio.MNK, Job.MNK },
        { DancePartnerPrio.NIN, Job.NIN },
        { DancePartnerPrio.DRG, Job.DRG },
        { DancePartnerPrio.BLM, Job.BLM },
        { DancePartnerPrio.RDM, Job.RDM },
        { DancePartnerPrio.SMN, Job.SMN },
        { DancePartnerPrio.MCH, Job.MCH },
        { DancePartnerPrio.BRD, Job.BRD },
        { DancePartnerPrio.DNC, Job.DNC },
    };
}
using System.ComponentModel;
using ECommons.ExcelServices;

namespace ArgentiRotations.Common;

public enum DancePartnerPrio : byte
{

    [Description("Samurai")]SAM = Job.SAM,
    [Description("Pictomancer")]PCT = Job.PCT,
    [Description("Reaper")]RPR = Job.RPR,
    [Description("Viper")]VPR = Job.VPR,
    [Description("Monk")]MNK = Job.MNK,
    [Description("Ninja")]NIN = Job.NIN,
    [Description("Dragoon")]DRG = Job.DRG,
    [Description("Black Mage")]BLM = Job.BLM,
    [Description("Red Mage")]RDM = Job.RDM,
    [Description("Summoner")]SMN = Job.SMN,
    [Description("Machinist")]MCH = Job.MCH,
    [Description("Bard")]BRD = Job.BRD,
    [Description("Dancer")]DNC = Job.DNC
}
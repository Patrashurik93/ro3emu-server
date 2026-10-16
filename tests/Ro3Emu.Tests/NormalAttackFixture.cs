using Romsg;
using Ro3Emu.Bot;
using Ro3Emu.Server.Game;

namespace Ro3Emu.Tests;

/// <summary>Reward/PvP fixtures must not bypass the local normal-attack cadence by cancelling its action tail.</summary>
internal static class NormalAttackFixture
{
    public static async Task WaitBeforeNextAsync(BotClient bot)
    {
        var timeline = bot.Inbox.Snapshot()
            .Where(e => e.IsGame(CombatService.UseInSkill) || e.IsGame(CombatService.UseInSkillHigh))
            .Select(e => UseInSkillInfo.Parser.ParseFrom(e.Body))
            .LastOrDefault(t => t.SrcID == (long)bot.Self!.Uuid && t.SkillID == 1000001);
        if (timeline != null) await WaitUntilAsync(timeline);
    }

    private static async Task WaitUntilAsync(UseInSkillInfo timeline)
    {
        long remaining = timeline.StartTime + timeline.ChantMs + timeline.SkillMs - MonsterService.NowMs;
        if (remaining > 0) await Task.Delay(TimeSpan.FromMilliseconds(remaining + 25));
    }

    public static async Task WaitForTailAsync(BotClient bot, long castId)
    {
        var message = await bot.Inbox.WaitAsync(e =>
            (e.IsGame(CombatService.UseInSkill) || e.IsGame(CombatService.UseInSkillHigh))
            && UseInSkillInfo.Parser.ParseFrom(e.Body).SessionId == castId,
            TimeSpan.FromSeconds(10));
        await WaitUntilAsync(UseInSkillInfo.Parser.ParseFrom(message.Body));
    }
}

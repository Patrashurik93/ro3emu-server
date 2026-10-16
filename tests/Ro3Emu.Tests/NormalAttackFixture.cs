using Romsg;
using Ro3Emu.Bot;
using Ro3Emu.Server.Game;

namespace Ro3Emu.Tests;

/// <summary>Reward/PvP fixtures must not bypass the local normal-attack cadence by cancelling its action tail.</summary>
internal static class NormalAttackFixture
{
    public static async Task<CastSkillRsp> CastWhenReadyAsync(BotClient bot, long targetId)
    {
        long deadline = MonsterService.NowMs + 5000;
        while (true)
        {
            await WaitBeforeNextAsync(bot);
            await bot.SendGameAsync(CombatService.UseSkill, new CastSkillReq { SkillId = 1000001, TargetId = targetId });
            var message = await bot.Inbox.WaitAsync(e => e.IsGame(CombatService.UseSkill), TimeSpan.FromSeconds(10));
            var cast = CastSkillRsp.Parser.ParseFrom(message.Body);
            // A timed fixture can race an authoritative auto-repeat. Obey only the cooldown refusal;
            // all other errors still fail, and the cadence-specific tests never use this retry helper.
            if (cast.Result == ErrorCodes.SkillCooldown && MonsterService.NowMs < deadline)
            {
                await Task.Delay(100);
                continue;
            }
            Assert.Equal(0, cast.Result);
            return cast;
        }
    }

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
            && UseInSkillInfo.Parser.ParseFrom(e.Body) is var timeline
            && timeline.SrcID == (long)bot.Self!.Uuid && timeline.SessionId == castId,
            TimeSpan.FromSeconds(10));
        await WaitUntilAsync(UseInSkillInfo.Parser.ParseFrom(message.Body));
    }
}

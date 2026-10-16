using Microsoft.Extensions.DependencyInjection;
using Romsg;
using Ro3Emu.Server.Game;

namespace Ro3Emu.Tests;

public sealed partial class SkillCombatTests
{
    [ClientDataFact]
    public async Task NormalAttackInterruptDoesNotResetCadenceAndAttackResumesAfterIt()
    {
        await using var alice = await InWorld("Alice");
        var target = await Target("alice", stationary: true);
        var combat = app.Services.GetRequiredService<CombatService>();
        int waitMs = 0;
        await Loop.InvokeAsync(async () =>
        {
            var session = Session("alice");
            var request = new CastSkillReq { SkillId = 1000001, TargetId = target.Uid };
            await combat.CastAsync(session, request);
            Assert.NotNull(session.SkillCast);
            waitMs = session.SkillCast.ChantMs + session.SkillCast.ActionMs + 100;
            await combat.InterruptAsync(session);
            Assert.Null(session.SkillCast);
            await combat.CastAsync(session, request);
            Assert.Null(session.SkillCast);
        });
        Assert.Equal(0, (await Next(alice, CombatService.UseSkill, CastSkillRsp.Parser)).Result);
        Assert.Equal(ErrorCodes.SkillCooldown,
            (await Next(alice, CombatService.UseSkill, CastSkillRsp.Parser)).Result);
        await Task.Delay(waitMs);
        await alice.SendGameAsync(CombatService.UseSkill,
            new CastSkillReq { SkillId = 1000001, TargetId = target.Uid });
        Assert.Equal(0, (await Next(alice, CombatService.UseSkill, CastSkillRsp.Parser)).Result);
    }

    [ClientDataFact]
    public async Task UnlockAllIsRoleScopedReversibleAndKeepsStructuralGuards()
    {
        await using var alice = await InWorld("Alice");
        await using var bob = await InWorld("Bob");
        var functions = app.Services.GetRequiredService<FunctionService>();
        await Loop.InvokeAsync(() =>
        {
            Assert.False(functions.IsOpen(Session("alice"), 55));
            Assert.False(functions.IsOpen(Session("bob"), 55));
            return Task.CompletedTask;
        });
        await Gm("alice", "!unlockall on");
        await Loop.InvokeAsync(() =>
        {
            Assert.True(functions.GmFunctionsOpen(Session("alice")));
            Assert.True(functions.IsOpen(Session("alice"), 55));
            Assert.False(functions.IsOpen(Session("bob"), 55));
            Assert.False(functions.IsOpen(Session("alice"), 1003));
            return Task.CompletedTask;
        });
        await Gm("alice", "!unlockall status");
        await Gm("alice", "!unlockall off");
        await Loop.InvokeAsync(() =>
        {
            Assert.False(functions.GmFunctionsOpen(Session("alice")));
            Assert.False(functions.IsOpen(Session("alice"), 55));
            return Task.CompletedTask;
        });
    }
}

using System;

static class Program
{
    static int checks;
    static void Assert(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception(message);
    }
    static MatchRules Match(int count = 2, bool start = true)
    {
        var rules = new MatchRules();
        for (int i = 0; i < count; i++) rules.Register((ulong)i, (ulong)(100 + i), "Player " + i, i % 2, i);
        if (start) rules.Start(10, 300);
        return rules;
    }
    static void Main()
    {
        Assert(!MatchRules.IsEnemy(0,0) && !MatchRules.IsEnemy(1,1), "Friendly damage denied");
        Assert(MatchRules.IsEnemy(0,1) && MatchRules.IsEnemy(1,0), "Enemy teams accepted");
        Assert(!MatchRules.IsEnemy(-1,0) && !MatchRules.IsEnemy(0,2), "Invalid teams denied");
        var setup = Match(start:false);
        Assert(!setup.RecordDeath(0,101,0,3), "Preparation death ignored");
        setup.Resolve(1000,120);
        Assert(setup.Phase == MatchRules.Stage.Preparing, "Preparation never decides winner");

        foreach (int count in new[] {2,4})
        {
            var rules = Match(count);
            Assert(rules.EndsAt == 310, "Timer begins at readiness timestamp");
            Assert(rules.RecordDeath(0,101,20,3), "Enemy kill");
            Assert(!rules.RecordDeath(0,101,20,3), "Duplicate death ignored");
            Assert(rules.Players[0].deaths == 1 && rules.Players[1].kills == 1, "Single K/D attribution");
            Assert(rules.Players[0].respawnAt == 23, "Three second deadline");
            rules.Resolve(22,120);
            Assert(rules.Running, "Pending legal return prevents elimination");
            Assert(!rules.ConfirmReturn(0,1,22), "Cannot acknowledge unscheduled relocation");
            rules.Players[0].relocating = true;
            Assert(!rules.ConfirmReturn(0,1,22), "Return cannot happen before three seconds");
            Assert(!rules.ConfirmReturn(0,999,23), "Wrong life acknowledgement rejected");
            Assert(rules.ConfirmReturn(0,1,23), "Owner relocation acknowledged");
            Assert(!rules.ConfirmReturn(0,1,23), "Duplicate return ignored");
            Assert(rules.Players[0].alive && rules.Players[0].deaths == 1 && rules.Players[1].kills == 1, "Return preserves statistics");
            for (int life = 2; life <= 5; life++)
            {
                Assert(rules.RecordDeath(0,101,20+life*5,3), "Repeated death");
                rules.Players[0].relocating = true;
                Assert(rules.ConfirmReturn(0,(uint)life,23+life*5), "Repeated return");
            }
            Assert(rules.Players[0].deaths == 5 && rules.Players[1].kills == 5, "Stats survive five lives");
            Assert(rules.DestroyBase(0), "Base destroyed once");
            Assert(!rules.DestroyBase(0), "Duplicate destruction ignored");
            rules.Resolve(80,120);
            Assert(rules.Running, "Living team survives destroyed base");
        }
        var canceled = Match();
        canceled.RecordDeath(0,101,20,3);
        canceled.Players[0].relocating = true;
        canceled.DestroyBase(0);
        Assert(canceled.Players[0].respawnAt == -1 && !canceled.ConfirmReturn(0,1,23), "Destruction cancels pending and in-flight return");
        canceled.Resolve(23,120);
        Assert(canceled.Winner == 1, "Team eliminated after canceled return");

        var allDeadWithBases = Match(4);
        for (ulong i=0;i<4;i++) allDeadWithBases.RecordDeath(i,ulong.MaxValue,20,3);
        allDeadWithBases.Resolve(21,120);
        Assert(allDeadWithBases.Running, "Both teams can return despite all dead");
        Assert(allDeadWithBases.Players[0].kills == 0, "Environmental death grants no kill");
        allDeadWithBases.Resolve(310,120);
        Assert(allDeadWithBases.Winner == -1 && allDeadWithBases.DestroyedBases == 3, "All pending returns canceled atomically yields draw");

        // Same event batch must be commutative for base and lethal-hit ordering.
        foreach (bool reverse in new[] {false,true})
        {
            var simultaneous = Match();
            simultaneous.DestroyBase(reverse ? 1 : 0);
            simultaneous.DestroyBase(reverse ? 0 : 1);
            simultaneous.RecordDeath(reverse ? 1ul : 0ul,reverse ? 100ul : 101ul,20,3);
            simultaneous.RecordDeath(reverse ? 0ul : 1ul,reverse ? 101ul : 100ul,20,3);
            simultaneous.Resolve(20,120);
            Assert(simultaneous.Winner == -1, "Simultaneous elimination draw independent of order");
            Assert(simultaneous.Players[0].kills == 1 && simultaneous.Players[1].kills == 1, "Dead shooter's bullet retains attribution");
            Assert(!simultaneous.RecordDeath(0,101,21,3) && !simultaneous.DestroyBase(0), "Finished result/stats frozen");
            simultaneous.Disconnect(0);
            Assert(!simultaneous.Players[0].disconnected, "Post-result departure does not rewrite frozen statistics");
        }
        var overtime = Match(4);
        overtime.RecordDeath(0,101,309,3);
        overtime.RecordDeath(1,100,309,3);
        overtime.Resolve(310,120);
        Assert(overtime.Phase == MatchRules.Stage.Overtime && overtime.EndsAt == 430 && overtime.DestroyedBases == 3, "Atomic overtime transition");
        Assert(overtime.Players[0].respawnAt == -1 && overtime.Players[1].respawnAt == -1, "Overtime cancels both returns");
        overtime.Resolve(430,120);
        Assert(overtime.Winner == -1, "Both living teams at overtime expiry draw");
        var overtimeKill = Match(); overtimeKill.Resolve(310,120);
        overtimeKill.RecordDeath(0,101,311,3); overtimeKill.Resolve(311,120);
        Assert(overtimeKill.Winner == 1 && overtimeKill.Players[0].respawnAt == -1, "Overtime last kill ends match immediately without return");

        var boundary = Match();
        boundary.RecordDeath(0,101,307,3); boundary.Players[0].relocating = true;
        Assert(!boundary.ConfirmReturn(0,1,310), "Late acknowledgement cannot return at overtime boundary");
        boundary.Resolve(310,120);
        Assert(boundary.Winner == 1 && boundary.DestroyedBases == 3, "Expired return loses without destruction-order advantage");

        var disconnected = Match();
        disconnected.RecordDeath(0,101,20,3);
        disconnected.Disconnect(0);
        disconnected.Resolve(21,120);
        Assert(disconnected.Winner == 1 && disconnected.Players[0].respawnAt == -1, "Disconnected player cannot reserve respawn");
        Assert(disconnected.Players[0].disconnected && disconnected.Players[0].deaths == 1, "Disconnected stats retained");
        var bothLeft = Match(); bothLeft.Disconnect(0); bothLeft.Disconnect(1); bothLeft.Resolve(20,120);
        Assert(bothLeft.Winner == -1, "Both disconnected in event batch draw on server");
        var friendlyCredit = Match(); friendlyCredit.RecordDeath(0,100,20,3);
        Assert(friendlyCredit.Players[0].kills == 0, "Invalid self killer gets no kill");
        var baseOnly = Match(); baseOnly.DestroyBase(0);
        Assert(baseOnly.Players[0].deaths == 0 && baseOnly.Players[1].kills == 0 && baseOnly.Players[0].alive, "Base destruction is neither player death nor kill");
        Console.WriteLine($"PASS: {checks} match rule assertions.");
    }
}

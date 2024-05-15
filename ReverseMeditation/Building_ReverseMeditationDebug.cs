using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;
using RimWorld;
using Verse.AI;
using Unity.Jobs;
using RimWorld.Planet;

namespace ReverseMeditation {
    [DefOf]
    public static class RMDefOf {
        static RMDefOf() {
            DefOfHelper.EnsureInitializedInCtor(typeof(RMDefOf));
        }
        [MayRequireRoyalty]
        public static JobDef ReverseMeditate;
    }
    public class Building_ReverseMeditationDebug : Building {
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn) {
            foreach (FloatMenuOption floatMenuOption in base.GetFloatMenuOptions(selPawn)) {
                yield return floatMenuOption;
            }
            var targ = new LocalTargetInfo(this);
            if (targ.IsValid && selPawn.CanReach(this, PathEndMode.OnCell, selPawn.NormalMaxDanger())) {
                JobDef def1 = JobDefOf.Meditate;
                var focus = MeditationUtility.BestFocusAt(targ, selPawn);
                var job1 = JobMaker.MakeJob(def1, targ, null, focus);
                job1.ignoreJoyTimeAssignment = true;
                yield return new FloatMenuOption(
                    "meditate",
                    delegate () {
                        selPawn.jobs.TryTakeOrderedJob(job1, new JobTag?(JobTag.Misc), false);
                    });

                JobDef def2 = RMDefOf.ReverseMeditate;
                var job2 = JobMaker.MakeJob(def2, targ, null, focus);
                job2.ignoreJoyTimeAssignment = true;
                yield return new FloatMenuOption(
                    "reverse meditate",
                    delegate () {
                        selPawn.jobs.TryTakeOrderedJob(job2, new JobTag?(JobTag.Misc), false);
                    });
            }
        }
    }
}

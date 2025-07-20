using System.Collections.Generic;

using Verse;
using RimWorld;
using Verse.AI;
using UnityEngine;
using Verse.Sound;

namespace ReverseMeditation {
    public class JobDriver_ReverseMeditate : JobDriver_Meditate {
        private const int JobCheckIntervalTicks = 18000;
        private Mote psyfocusMote;
        private bool FromBed {
            get {
                return job.GetTarget(TargetIndex.B).IsValid;
            }
        }

        //ベースからコピペ
        protected override IEnumerable<Toil> MakeNewToils() {
            Toil meditate = ToilMaker.MakeToil("MakeNewToils");
            meditate.socialMode = RandomSocialMode.Off;
            if (FromBed) {
                this.KeepLyingDown(TargetIndex.B);
                meditate = Toils_LayDown.LayDown(TargetIndex.B, job.GetTarget(TargetIndex.B).Thing is Building_Bed, false, false, true, PawnPosture.LayingOnGroundNormal, false);
            } else {
                yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
                meditate.initAction = delegate () {
                    LocalTargetInfo target = job.GetTarget(TargetIndex.C);
                    if (target.IsValid) {
                        faceDir = target.Cell - pawn.Position;
                        return;
                    }
                    faceDir = (job.def.faceDir.IsValid ? job.def.faceDir : Rot4.Random).FacingCell;
                };
                if (Focus != null) {
                    meditate.FailOnDespawnedOrNull(TargetIndex.C);
                    meditate.FailOnForbidden(TargetIndex.A);
                    if (pawn.HasPsylink && Focus.Thing != null) {
                        meditate.FailOn(() => Focus.Thing.GetStatValueForPawn(StatDefOf.MeditationFocusStrength, pawn, true) < float.Epsilon);
                    }
                }
                meditate.handlingFacing = true;
            }
            meditate.defaultCompleteMode = ToilCompleteMode.Delay;
            //meditate.defaultDuration = job.def.joyDuration;
            meditate.defaultDuration = 18000;
            meditate.FailOn(() => !MeditationUtility.CanMeditateNow(pawn) || !MeditationUtility.SafeEnvironmentalConditions(pawn, TargetLocA, Map));
            meditate.AddPreTickAction(delegate {
                var assignment = pawn.GetTimeAssignment();
                bool flag = assignment == TimeAssignmentDefOf.Meditate|| assignment== TimeAssignmentDefOf.Anything;
                if (job.ignoreJoyTimeAssignment) {
                    Pawn_PsychicEntropyTracker psychicEntropy = pawn.psychicEntropy;
                    bool flag2 = !flag && job.wasOnMeditationTimeAssignment;
                    if (pawn.IsHashIntervalTick(JobCheckIntervalTicks) && psychicEntropy != null && !flag && psychicEntropy.CurrentPsyfocus <= Mathf.Max(psychicEntropy.TargetPsyfocus - 0.05f, 0f)) {
                        pawn.jobs.CheckForJobOverride(0f);
                        return;
                    }
                    if (flag2 && psychicEntropy.TargetPsyfocus > psychicEntropy.CurrentPsyfocus) {
                        base.EndJobWith(JobCondition.InterruptForced);
                        return;
                    }
                    job.psyfocusTargetLast = psychicEntropy.TargetPsyfocus;
                    job.wasOnMeditationTimeAssignment = flag;
                } else if (pawn.needs.joy.CurLevelPercentage >= 1f) {
                    EndJobWith(JobCondition.InterruptForced);
                    return;
                }
                if (faceDir.IsValid && !FromBed) {
                    pawn.rotationTracker.FaceCell(pawn.Position + faceDir);
                }
                ReverseMeditationTick();
                if (ModsConfig.RoyaltyActive && MeditationFocusDefOf.Natural.CanPawnUse(pawn)) {
                    int num = GenRadial.NumCellsInRadius(MeditationUtility.FocusObjectSearchRadius);
                    for (int i = 0; i < num; i++) {
                        IntVec3 c = pawn.Position + GenRadial.RadialPattern[i];
                        if (c.InBounds(pawn.Map)) {
                            Plant plant = c.GetPlant(pawn.Map);
                            if (plant != null && plant.def == ThingDefOf.Plant_TreeAnima) {
                                CompSpawnSubplant compSpawnSubplant = plant.TryGetComp<CompSpawnSubplant>();
                                if (compSpawnSubplant != null) {
                                    compSpawnSubplant.AddProgress(JobDriver_Meditate.AnimaTreeSubplantProgressPerTick, false);
                                }
                            }
                        }
                    }
                }
            });
            yield return meditate;
            yield break;
        }

        //ベースのMeditationTick()から大部分をコピペ
        private void ReverseMeditationTick() {
            pawn.skills.Learn(SkillDefOf.Intellectual, 0.018000001f, false, false);
#if V15
            pawn.GainComfortFromCellIfPossible(false);
#else
            pawn.GainComfortFromCellIfPossible(1,false);
#endif
            /*
            if (pawn.needs.joy != null) {
                JoyUtility.JoyTickCheckEnd(pawn, JoyTickFullJoyAction.None, 1f, null);
            }
            */
            if (pawn.IsHashIntervalTick(100)) {
                FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.Meditating, 0.42f);
            }
            if (ModsConfig.RoyaltyActive && pawn.psychicEntropy != null) {
                pawn.psychicEntropy.Notify_Meditated();
                if (pawn.HasPsylink && pawn.psychicEntropy.PsychicSensitivity > 1E-45f) {
                    float yOffset = (float)(pawn.Position.x % 2 + pawn.Position.z % 2) / 10f;
                    if (psyfocusMote == null || psyfocusMote.Destroyed) {
                        psyfocusMote = MoteMaker.MakeAttachedOverlay(pawn, ThingDefOf.Mote_PsyfocusPulse, Vector3.zero, 1f, -1f);
                        psyfocusMote.yOffset = yOffset;
                    }
                    psyfocusMote.Maintain();
                    if (sustainer == null || sustainer.Ended) {
                        sustainer = SoundDefOf.MeditationGainPsyfocus.TrySpawnSustainer(SoundInfo.InMap(pawn, MaintenanceType.PerTick));
                    }
                    sustainer.Maintain();
                    //pawn.psychicEntropy.GainPsyfocus(Focus.Thing);
                    var focusGain = MeditationUtility.PsyfocusGainPerTick(pawn, Focus.Thing);
                    pawn.psychicEntropy.OffsetPsyfocusDirectly(-focusGain);
                }
            }
        }
    }
}

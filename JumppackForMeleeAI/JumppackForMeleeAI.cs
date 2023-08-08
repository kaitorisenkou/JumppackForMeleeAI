using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using RimWorld;
using HarmonyLib;
using System.Text.RegularExpressions;
using Verse.AI;
using static UnityEngine.GraphicsBuffer;

namespace JumppackForMeleeAI {
    [StaticConstructorOnStartup]
    public class JumppackForMeleeAI {
        static JumppackForMeleeAI() {
            /*
            Log.Message("[JumppackForMeleeAI]Now Active");
            var harmony = new Harmony("kaitorisenkou.JumppackForMeleeAI");
            ManualPatch(harmony);
            //harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.Message("[JumppackForMeleeAI]Harmony patch complete!");
            */
        }
        /*
        static void ManualPatch(Harmony harmony) {
            Type FollowAndMeleeAttack_innerType = AccessTools.TypeByName("Verse.AI.Toils_Combat+<>c__DisplayClass6_0");
            if (FollowAndMeleeAttack_innerType != null) {
                MethodInfo m = AccessTools.Method(FollowAndMeleeAttack_innerType, "<FollowAndMeleeAttack>b__0");
                if (m != null) {
                    harmony.Patch(m, null, null,
                        new HarmonyMethod(AccessTools.Method(typeof(FollowAndMeleeAttack_Patch), "Transpiler", new Type[] { typeof(IEnumerable<CodeInstruction>), typeof(ILGenerator) })));
                    Log.Message("[JumppackForMeleeAI]Patched: <FollowAndMeleeAttack>b__0");
                }
            }
        }
        */
    }
    /*
    public static class FollowAndMeleeAttack_Patch {
        public static int patchCount = 0;
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
            var instructionList = instructions.ToList();
            FieldInfo fieldInfo = AccessTools.Field(typeof(Pawn_PathFollower), "get_Destination");
            for (int i = 0; i < instructionList.Count; i++) {
                if (instructionList[i].opcode == OpCodes.Callvirt && (MethodInfo)instructionList[i].operand == fieldInfo) {
                    Label label = generator.DefineLabel();
                    instructionList[i -3].labels.Add(label);
                    instructionList.InsertRange(i -3, new CodeInstruction[] {
                        new CodeInstruction(OpCodes.Ldloc_0),
                        new CodeInstruction(OpCodes.Ldloc_S,instructionList[i-2].operand),
                        new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(FollowAndMeleeAttack_Patch),nameof(GetJunpPack))),
                        new CodeInstruction(OpCodes.Brfalse_S,label),
                        new CodeInstruction(OpCodes.Ret)
                    });
                    break;
                }
            }
            return instructionList;
        }

        public static bool GetJunpPack(Pawn actor, LocalTargetInfo target) {
            Log.Message("[JfMA]GetJunpPack");
            if (actor.CanReachImmediate(target, PathEndMode.Touch) || actor.IsColonistPlayerControlled) {
                Log.Message("[JfMA]reached or player");
                return false;
            }

            IEnumerable<Verb> jumpVerbs = 
                actor.VerbTracker.AllVerbs
                .Concat(actor.equipment.AllEquipmentVerbs)
                .Concat(actor.apparel.AllApparelVerbs)
                .Where(t => t is Verb_Jump);
            if (jumpVerbs.EnumerableNullOrEmpty<Verb>()) {
                Log.Message("[JfMA]no jump verb");
                return false;
            }

            //Verb jumpVerb = jumpVerbs.First();
            foreach(var jumpVerb in jumpVerbs) {
                if (jumpVerb.ValidateTarget(target)) {
                    Job job = JobMaker.MakeJob(JobDefOf.UseVerbOnThingStatic, target);
                    job.verbToUse = jumpVerb;
                    actor.jobs.TryTakeOrderedJob(job, new JobTag?(JobTag.Misc), false);
                    Log.Message("[JfMA]return true");
                    return true;
                }
            }
            Log.Message("[JfMA]other problems");
            return false;
        }
        public static bool RetTest() {
            return false;
        }
    }
    */
}

using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace DigForHoe
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "felih.DigForHoe";
        public const string NAME = "DigForHoe";
        public const string VERSION = "1.0.0";

        internal static ManualLogSource Log;
        internal static ConfigEntry<bool> DetailedLog;

        private void Awake()
        {
            Log = Logger;
            DetailedLog = Config.Bind("9 - Debug", "DetailedLog", false, "Writes details to the BepInEx log.");
            new Harmony(GUID).PatchAll(typeof(Plugin).Assembly);
            Log.LogInfo(NAME + " " + VERSION + " loaded");
        }

        internal static void Debug(string msg)
        {
            if (DetailedLog.Value) Log.LogInfo(msg);
        }
    }

    // Adds a "Dig" option to the hoe. It is a copy of the hoe's "Raise ground" piece that carries the
    // pickaxe's dig instead, so it lowers the ground exactly like a pickaxe hit, wherever you aim.
    [HarmonyPatch]
    internal static class DigPiece
    {
        internal const string PieceName = "DigForHoe_dig";
        private static GameObject _holder;   // inactive parent, keeps our template from running
        private static GameObject _piece;
        private static int _digHash;          // hash of the pickaxe's dig prefab
        private static string _digName;

        [HarmonyPatch(typeof(ObjectDB), "Awake")]
        [HarmonyPostfix]
        private static void OnObjectDBAwake(ObjectDB __instance) { Setup(__instance); }

        [HarmonyPatch(typeof(ObjectDB), "CopyOtherDB")]
        [HarmonyPostfix]
        private static void OnCopyOtherDB(ObjectDB __instance) { Setup(__instance); }

        private static void Setup(ObjectDB db)
        {
            GameObject hoe = db.GetItemPrefab("Hoe");
            GameObject pick = db.GetItemPrefab("PickaxeAntler");
            if (hoe == null || pick == null) return;
            PieceTable table = hoe.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces;
            GameObject dig = pick.GetComponent<ItemDrop>().m_itemData.m_shared.m_spawnOnHitTerrain;
            if (table == null || dig == null) return;
            TerrainOp digOp = dig.GetComponentInChildren<TerrainOp>();
            if (digOp == null) { Plugin.Log.LogWarning("Pickaxe dig has no TerrainOp: " + dig.name); return; }

            if (table.m_pieces.Exists(p => p != null && p.name == PieceName)) return;

            if (_piece == null)
            {
                GameObject raise = FindRaise(table.m_pieces);
                if (raise == null) { Plugin.Log.LogWarning("Hoe 'raise ground' piece not found"); return; }

                _holder = new GameObject("DigForHoe_templates");
                _holder.SetActive(false);
                Object.DontDestroyOnLoad(_holder);
                _piece = Object.Instantiate(raise, _holder.transform);
                _piece.name = PieceName;

                ZNetView nv = _piece.GetComponent<ZNetView>();
                if (nv != null) Object.DestroyImmediate(nv);

                TerrainOp op = _piece.GetComponent<TerrainOp>();
                op.m_settings = digOp.m_settings;
                op.m_onPlacedEffect = digOp.m_onPlacedEffect;
                op.m_spawnOnPlaced = digOp.m_spawnOnPlaced;
                op.m_chanceToSpawn = digOp.m_chanceToSpawn;
                op.m_maxSpawned = digOp.m_maxSpawned;
                op.m_spawnAtMaxLevelDepth = digOp.m_spawnAtMaxLevelDepth;

                Piece piece = _piece.GetComponent<Piece>();
                piece.m_name = "Dig";
                piece.m_description = "Lowers the ground like a pickaxe hit, where you aim.";
                piece.m_resources = new Piece.Requirement[0];
                Sprite icon = pick.GetComponent<ItemDrop>().m_itemData.GetIcon();
                if (icon != null) piece.m_icon = icon;

                _digName = Utils.GetPrefabName(digOp.gameObject.name);
                _digHash = _digName.GetStableHashCode();
                Plugin.Debug("[setup] dig piece built from " + raise.name + ", uses " + _digName
                    + " (raise " + digOp.m_settings.m_raise + " delta " + digOp.m_settings.m_raiseDelta
                    + " radius " + digOp.m_settings.m_raiseRadius + ")");
            }

            int at = table.m_pieces.FindIndex(p => p != null && p.GetComponent<TerrainOp>() != null
                && p.GetComponent<TerrainOp>().m_settings.m_raise && p.GetComponent<TerrainOp>().m_settings.m_raiseDelta > 0f);
            if (at >= 0) table.m_pieces.Insert(at + 1, _piece);
            else table.m_pieces.Add(_piece);
            Plugin.Debug("[setup] added Dig to the hoe");
        }

        private static GameObject FindRaise(List<GameObject> pieces)
        {
            foreach (GameObject p in pieces)
            {
                if (p == null) continue;
                TerrainOp op = p.GetComponent<TerrainOp>();
                if (op != null && op.m_settings.m_raise && op.m_settings.m_raiseDelta > 0f) return p;
            }
            return null;
        }

        // The build menu only lists pieces the character has learned, and a new piece is never learned.
        // Always show Dig in the hoe menu, right after "Raise ground".
        [HarmonyPatch(typeof(PieceTable), "UpdateAvailable")]
        [HarmonyPostfix]
        private static void ShowInMenu(PieceTable __instance)
        {
            if (_piece == null || !__instance.m_pieces.Contains(_piece)) return;
            Piece dig = _piece.GetComponent<Piece>();
            Traverse t = Traverse.Create(__instance);
            HashSet<Piece> available = t.Field<HashSet<Piece>>("m_availablePieces").Value;
            if (available == null || available.Contains(dig)) return;
            HashSet<Piece> enabled = t.Field<HashSet<Piece>>("m_enabledPieces").Value;
            if (enabled != null) enabled.Add(dig);
            available.Add(dig);
            List<List<Piece>> byCat = t.Field<List<List<Piece>>>("m_availablePiecesByCategory").Value;
            if (byCat == null) return;
            int cat = (int)dig.m_category;
            for (int i = 0; i < byCat.Count; i++)
            {
                if (cat != 100 && i != cat) continue;
                List<Piece> list = byCat[i];
                int at = list.FindIndex(p => p != null && p.gameObject.name == "raise_v2");
                if (at >= 0) list.Insert(at + 1, dig); else list.Add(dig);
            }
        }

        // Terrain changes travel over the network by prefab name only. Send the pickaxe's dig name, so
        // whoever owns that patch of ground applies a normal pickaxe dig, even without this mod.
        [HarmonyPatch(typeof(TerrainOp.Settings), "Serialize")]
        [HarmonyPrefix]
        private static bool Serialize(ZPackage pkg, GameObject prefab)
        {
            if (_digHash == 0 || prefab == null || Utils.GetPrefabName(prefab.name) != PieceName) return true;
            pkg.Write(_digHash);
            Plugin.Debug("[dig] at " + prefab.transform.position);
            return false;
        }
    }
}

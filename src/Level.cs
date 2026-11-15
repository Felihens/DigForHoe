using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace DigForHoe
{
    // Level ground that really levels. The game levels to the height of the spot you click (or, holding the
    // alternative place key, of your feet), so on uneven ground every stroke levels to a different height. Here
    // the hoe's level ground (and anything else of the hoe that levels, like paved road) picks one height:
    //  1. the ground already levelled right around the stroke (the height most of it shares), so each stroke
    //     carries on the flat you are making;
    //  2. else the height of your last stroke nearby, a moment ago;
    //  3. else the height of your feet (as holding the alternative place key always does).
    // The ghost shows at that height, a label under the crosshair says how much it raises or digs, and a stroke
    // that would raise or dig more than MaxChange anywhere under it is refused. The game's own edge smoothing is
    // kept, so the ground still blends into what is around it.
    [HarmonyPatch]
    internal static class Level
    {
        private static readonly System.Reflection.FieldInfo GhostI = AccessTools.Field(typeof(Player), "m_placementGhost");
        private static readonly System.Reflection.FieldInfo PiecesI = AccessTools.Field(typeof(Player), "m_buildPieces");
        private static readonly System.Reflection.FieldInfo StatusI = AccessTools.Field(typeof(Player), "m_placementStatus");
        private static readonly System.Reflection.FieldInfo ModifiedI = AccessTools.Field(typeof(TerrainComp), "m_modifiedHeight");
        private static readonly System.Reflection.FieldInfo LevelDeltaI = AccessTools.Field(typeof(TerrainComp), "m_levelDelta");
        private static readonly System.Reflection.FieldInfo HmapI = AccessTools.Field(typeof(TerrainComp), "m_hmap");
        private static readonly System.Reflection.FieldInfo WidthI = AccessTools.Field(typeof(TerrainComp), "m_width");

        private static GameObject GhostF(Player p) { return GhostI.GetValue(p) as GameObject; }
        private static PieceTable PiecesF(Player p) { return PiecesI.GetValue(p) as PieceTable; }
        private static bool StatusValid(Player p) { return StatusI.GetValue(p).ToString() == "Valid"; }
        private static void SetInvalid(Player p) { StatusI.SetValue(p, System.Enum.Parse(StatusI.FieldType, "Invalid")); }
        private static bool[] ModifiedF(TerrainComp t) { return ModifiedI.GetValue(t) as bool[]; }
        private static float[] LevelDeltaF(TerrainComp t) { return LevelDeltaI.GetValue(t) as float[]; }
        private static Heightmap HmapF(TerrainComp t) { return HmapI.GetValue(t) as Heightmap; }
        private static int WidthF(TerrainComp t) { return (int)WidthI.GetValue(t); }
        private static readonly System.Reflection.MethodInfo GhostValidM = AccessTools.Method(typeof(Player), "SetPlacementGhostValid");

        private const float SameFlat = 0.15f;      // heights this close count as the same flat
        private const float LastStrokeRange = 12f, LastStrokeSeconds = 90f;

        // what the label shows
        private static bool _active, _tooMuch;
        private static float _change, _worst;
        private static string _from = "";

        private static float _lastH, _lastAt = -999f;
        private static Vector3 _lastPos;
        private static float _pendingH;
        private static bool _pending;

        private static bool IsLevelOp(GameObject prefab, out TerrainOp op)
        {
            op = prefab != null ? prefab.GetComponent<TerrainOp>() : null;
            return op != null && op.m_settings.m_level && !op.m_settings.m_raise;
        }

        private static bool Ground(Vector3 at, out float h)
        {
            if (Heightmap.GetHeight(at, out h)) return true;
            h = at.y;
            return false;
        }

        // the ground at this point was levelled before (by any level stroke, yours or not)
        private static bool Levelled(Vector3 at)
        {
            TerrainComp tc = TerrainComp.FindTerrainCompiler(at);
            if (tc == null) return false;
            Heightmap hm = HmapF(tc);
            bool[] mod = ModifiedF(tc);
            float[] lvl = LevelDeltaF(tc);
            if (hm == null || mod == null || lvl == null) return false;
            int x, y;
            hm.WorldToVertex(at, out x, out y);
            int w = WidthF(tc) + 1;
            if (x < 0 || y < 0 || x >= w || y >= w) return false;
            int i = y * w + x;
            return i < mod.Length && mod[i] && Mathf.Abs(lvl[i]) > 0.01f;
        }

        // the height most of the levelled ground in a ring around the stroke shares; false if there is none
        private static bool FlatAround(Vector3 center, float radius, out float height, out int count)
        {
            height = 0f;
            count = 0;
            List<float> hs = new List<float>();
            for (float r = radius + 0.75f; r <= radius + 2.6f; r += 0.9f)
            {
                int n = Mathf.Max(12, Mathf.RoundToInt(r * 6f));
                for (int k = 0; k < n; k++)
                {
                    float a = k * Mathf.PI * 2f / n;
                    Vector3 p = center + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                    float h;
                    if (!Ground(p, out h) || !Levelled(p)) continue;
                    // flat there too: its neighbours half a metre away are at the same height
                    float hx, hz;
                    Ground(p + new Vector3(0.5f, 0f, 0f), out hx);
                    Ground(p + new Vector3(0f, 0f, 0.5f), out hz);
                    if (Mathf.Abs(hx - h) > 0.1f || Mathf.Abs(hz - h) > 0.1f) continue;
                    hs.Add(h);
                }
            }
            if (hs.Count < 3) return false;
            hs.Sort();
            // the biggest group of heights within SameFlat of each other
            int best = 0, bestAt = 0;
            for (int i = 0, j = 0; i < hs.Count; i++)
            {
                while (hs[i] - hs[j] > SameFlat) j++;
                if (i - j + 1 > best) { best = i - j + 1; bestAt = j; }
            }
            if (best < 3) return false;
            height = hs[bestAt + best / 2];
            count = best;
            return true;
        }

        [HarmonyPatch(typeof(Player), "UpdatePlacementGhost"), HarmonyPostfix]
        private static void Ghost(Player __instance)
        {
            _active = false;
            _pending = false;
            if (!Plugin.SmartLevel.Value || __instance != Player.m_localPlayer) return;
            GameObject ghost = GhostF(__instance);
            PieceTable table = PiecesF(__instance);
            TerrainOp op;
            if (ghost == null || !ghost.activeSelf || table == null || !IsLevelOp(table.GetSelectedPrefab(), out op)) return;
            if (!StatusValid(__instance)) return;

            Vector3 at = ghost.transform.position;
            float radius = Mathf.Max(0.5f, op.m_settings.m_levelRadius);
            float h;
            int count;
            bool alt = ZInput.GetButton("AltPlace") || ZInput.GetButton("JoyAltPlace");
            if (!alt && FlatAround(at, radius, out h, out count)) _from = "the level ground beside it";
            else if (!alt && Time.time - _lastAt < LastStrokeSeconds && Vector3.Distance(Flat(at), Flat(_lastPos)) < LastStrokeRange) { h = _lastH; _from = "your last stroke"; }
            else
            {
                float feet;
                Ground(__instance.transform.position, out feet);
                h = feet;
                _from = "your feet";
            }

            // how much the stroke raises or digs anywhere under it
            float ground;
            Ground(at, out ground);
            _change = h - ground;
            _worst = 0f;
            for (float x = -radius; x <= radius; x += 0.5f)
                for (float z = -radius; z <= radius; z += 0.5f)
                {
                    if (!op.m_settings.m_square && x * x + z * z > radius * radius) continue;
                    float g;
                    if (!Ground(at + new Vector3(x, 0f, z), out g)) continue;
                    if (Mathf.Abs(h - g) > Mathf.Abs(_worst)) _worst = h - g;
                }
            _tooMuch = Mathf.Abs(_worst) > Plugin.MaxChange.Value;
            _active = true;

            at.y = h - op.m_settings.m_levelOffset;
            ghost.transform.position = at;
            if (_tooMuch)
            {
                SetInvalid(__instance);
                if (GhostValidM != null) GhostValidM.Invoke(__instance, new object[] { false });
                return;
            }
            _pending = true;
            _pendingH = h;
        }

        private static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        [HarmonyPatch(typeof(Player), "TryPlacePiece"), HarmonyPrefix]
        private static bool Refuse(Player __instance)
        {
            if (!_active || !_tooMuch || __instance != Player.m_localPlayer) return true;
            __instance.Message(MessageHud.MessageType.Center, "Too far to " + (_worst > 0f ? "raise" : "dig") + " here (" + Mathf.Abs(_worst).ToString("0.0") + " m): level closer to this height first");
            return false;
        }

        [HarmonyPatch(typeof(Player), "TryPlacePiece"), HarmonyPostfix]
        private static void Placed(Player __instance, bool __result)
        {
            if (!__result || !_pending || __instance != Player.m_localPlayer) return;
            GameObject ghost = GhostF(__instance);
            _lastH = _pendingH;
            _lastAt = Time.time;
            _lastPos = ghost != null ? ghost.transform.position : __instance.transform.position;
            Plugin.Debug("[level] stroke at " + _lastH.ToString("0.00") + " (from " + _from + "), change " + _change.ToString("0.00") + " m, worst " + _worst.ToString("0.00") + " m");
        }

        // The game smooths after it levels, over a wider circle, and that leaves the middle wavy. Once the stroke is
        // done, the middle (85% of the level circle) is levelled again to the stroke's height: perfectly flat there,
        // and the edge still blends into the ground around it. This runs where the ground is worked out (the game
        // that owns that patch), for any level stroke.
        private static readonly System.Reflection.MethodInfo LevelM = AccessTools.Method(typeof(TerrainComp), "LevelTerrain");
        internal const float FlatMiddle = 0.85f;

        [HarmonyPatch(typeof(TerrainComp), "InternalDoOperation"), HarmonyPostfix]
        private static void FlatCentre(TerrainComp __instance, Vector3 pos, TerrainOp.Settings modifier)
        {
            if (!Plugin.SmartLevel.Value || LevelM == null || modifier == null || !modifier.m_level || modifier.m_raise) return;
            LevelM.Invoke(__instance, new object[] { pos + Vector3.up * modifier.m_levelOffset, modifier.m_levelRadius * FlatMiddle, modifier.m_square });
        }

        private static GUIStyle _label;

        internal static void OnGUI()
        {
            if (!_active || !Plugin.SmartLevel.Value || Event.current.type != EventType.Repaint) return;
            if (Player.m_localPlayer == null || !Player.m_localPlayer.InPlaceMode() || Hud.IsUserHidden()) return;
            float s = Screen.height / 1080f;
            if (_label == null) _label = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, fontStyle = FontStyle.Bold };
            _label.fontSize = Mathf.RoundToInt(18 * s);
            string text = _tooMuch
                ? "Too far to " + (_worst > 0f ? "raise" : "dig") + " here (" + Mathf.Abs(_worst).ToString("0.0") + " m)"
                : "Level " + (_change >= 0f ? "+" : "") + _change.ToString("0.0") + " m  ·  height of " + _from;
            Rect r = new Rect(Screen.width / 2f - 300f * s, Screen.height / 2f + 40f * s, 600f * s, 30f * s);
            Color keep = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.8f);
            _label.normal.textColor = GUI.color;
            GUI.Label(new Rect(r.x + 1.5f * s, r.y + 1.5f * s, r.width, r.height), text, _label);
            GUI.color = Color.white;
            _label.normal.textColor = _tooMuch ? new Color(1f, 0.45f, 0.35f) : new Color(1f, 0.92f, 0.7f);
            GUI.Label(r, text, _label);
            GUI.color = keep;
        }
    }
}

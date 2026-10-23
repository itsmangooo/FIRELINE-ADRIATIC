using System;
using System.Linq;
using Fireline.Simulation;
using UnityEngine;

namespace Fireline.Client
{
    /// <summary>Read-only M02 engineering overlay, not M05A gameplay UI or M04 fire VFX; every colored cell comes from the replica.</summary>
    public sealed class FireStateView : MonoBehaviour
    {
        private UnityEngine.UI.Text title, detail;
        private UnityEngine.UI.RawImage grid;
        private Texture2D texture;
        private GameObject canvas;
        private static readonly Color32[] Colors = { new Color32(48, 89, 60, 255), new Color32(222, 187, 60, 255),
            new Color32(239, 83, 40, 255), new Color32(161, 86, 42, 255), new Color32(58, 153, 192, 255), new Color32(48, 48, 53, 255) };

        /// <summary>Create a separate screen overlay over the preserved calibration scene, with phase legend and authoritative status.</summary>
        private void Awake()
        {
            canvas = new GameObject("M02 Engineering State Display", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; canvas.GetComponent<Canvas>().sortingOrder = 100;
            var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
            var panel = new GameObject("Read-only State Panel", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            panel.transform.SetParent(canvas.transform, false); var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0); rect.anchorMax = new Vector2(0.73f, 1); rect.offsetMin = new Vector2(14, 14); rect.offsetMax = new Vector2(-14, -14);
            panel.GetComponent<UnityEngine.UI.Image>().color = new Color(0.025f, 0.045f, 0.065f, 0.96f);
            title = Label(panel.transform, "M02 / AUTHORITATIVE FIRE REPLICA", 25, new Vector2(20, 620), new Vector2(850, 55));
            detail = Label(panel.transform, "Waiting for server baseline...", 17, new Vector2(20, 520), new Vector2(850, 95));
            var picture = new GameObject("Server Cell Phases", typeof(RectTransform), typeof(UnityEngine.UI.RawImage));
            picture.transform.SetParent(panel.transform, false); var area = picture.GetComponent<RectTransform>();
            area.anchorMin = area.anchorMax = Vector2.zero; area.pivot = Vector2.zero; area.anchoredPosition = new Vector2(20, 185); area.sizeDelta = new Vector2(840, 320);
            grid = picture.GetComponent<UnityEngine.UI.RawImage>(); grid.color = Color.clear;
            Label(panel.transform, "NORTH +Z ↑     EAST +X →     Cell colors are server burn phases\nGreen: unburned  |  Yellow: heating  |  Orange: burning\nBrown: smoldering  |  Blue: extinguished  |  Dark gray: charred\n\nSynthetic SI test grid • read-only diagnostic • no client fire simulation", 17, new Vector2(20, 25), new Vector2(850, 145));
        }

        /// <summary>Create a noninteractive text label with a built-in font and fixed engineering-panel bounds.</summary>
        private static UnityEngine.UI.Text Label(Transform parent, string value, int size, Vector2 position, Vector2 bounds)
        {
            var obj = new GameObject("Diagnostic Label", typeof(RectTransform), typeof(UnityEngine.UI.Text)); obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = position; rect.sizeDelta = bounds;
            var label = obj.GetComponent<UnityEngine.UI.Text>(); label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = value; label.fontSize = size; label.color = new Color(0.9f, 0.95f, 0.98f); label.raycastTarget = false; return label;
        }

        /// <summary>Render canonically ordered chunks side by side from received phases and show server tick/digest/wind/resource totals.</summary>
        public void Show(FireSnapshot state, int networkId, int baselines, int deltas, int repairs)
        {
            int count = Math.Max(1, state.Chunks.Length), width = state.Size * count;
            if (texture == null || texture.width != width || texture.height != state.Size)
            { if (texture != null) Destroy(texture); texture = new Texture2D(width, state.Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point }; grid.texture = texture; }
            for (int n = 0; n < state.Chunks.Length; n++)
                for (int i = 0; i < state.Chunks[n].Cells.Length; i++) texture.SetPixel(n * state.Size + i % state.Size, i / state.Size, Colors[(int)state.Chunks[n].Cells[i].Phase]);
            texture.Apply(false); grid.color = Color.white;
            float displayWidth = Math.Min(840, 320 * count); grid.rectTransform.sizeDelta = new Vector2(displayWidth, displayWidth / count);
            int charred = state.Chunks.Sum(c => c.Cells.Count(v => v.Vegetation == VegetationPhase.Charred));
            title.text = $"M02 / READ-ONLY REPLICA / CLIENT {networkId}";
            detail.text = $"SERVER TICK {state.Tick}  |  DIGEST {state.Checksum}\nBaseline {baselines}  Delta {deltas}  Repairs {repairs}  |  Charred vegetation {charred}\nWind E/N {state.Wind.EastMps:F1}/{state.Wind.NorthMps:F1} m/s (rev {state.Wind.Revision})  |  Water {state.AppliedWaterKg:F0} kg";
        }

        /// <summary>Make stale state explicit rather than suggesting the client still receives live authority.</summary>
        public void Disconnected(string reason) => title.text = "DISCONNECTED / LAST VALID STATE / " + reason;

        /// <summary>Release only the generated display texture/canvas, preserving the original client scene and shared assets.</summary>
        private void OnDestroy() { if (texture != null) Destroy(texture); if (canvas != null) Destroy(canvas); }
    }
}

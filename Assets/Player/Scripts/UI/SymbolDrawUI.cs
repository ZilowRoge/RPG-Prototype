using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Player.UI {
    public class SymbolDrawUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler {
        public RawImage drawImage;
        public int textureSize = 1024;
        public int brushRadius = 2;
        public Color drawColor = Color.white;

        private Texture2D drawTexture;
        private bool isDrawing = false;
        private Vector2? lastPixelPos = null;

        private int minX, maxX, minY, maxY;

        void Start() {
            drawTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            drawTexture.filterMode = FilterMode.Point;
            drawImage.texture = drawTexture;
            ClearTexture();
        }

        public void OnPointerDown(PointerEventData eventData) {
            isDrawing = true;
            lastPixelPos = null;
            ResetBoundingBox();
            DrawFromPointer(eventData);
        }

        public void OnDrag(PointerEventData eventData) {
            if (isDrawing) {
                DrawFromPointer(eventData);
            }
        }

        public void OnPointerUp(PointerEventData eventData) {
            isDrawing = false;
            lastPixelPos = null;
        }

        private void DrawFromPointer(PointerEventData data) {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                drawImage.rectTransform, data.position, null, out Vector2 local)) {
                Rect rect = drawImage.rectTransform.rect;

                float xNorm = (local.x - rect.xMin) / rect.width;
                float yNorm = (local.y - rect.yMin) / rect.height;

                int px = Mathf.Clamp((int)(xNorm * textureSize), 0, textureSize - 1);
                int py = Mathf.Clamp((int)(yNorm * textureSize), 0, textureSize - 1);

                Vector2 currentPos = new Vector2(px, py);

                if (lastPixelPos.HasValue) {
                    DrawLine((int)lastPixelPos.Value.x, (int)lastPixelPos.Value.y, px, py, drawColor);
                } else {
                    DrawBrush(px, py, brushRadius, drawColor);
                }

                drawTexture.Apply();
                lastPixelPos = currentPos;
            }
        }

        private void DrawLine(int x0, int y0, int x1, int y1, Color color) {
            int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true) {
                DrawBrush(x0, y0, brushRadius, color);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }

        private void DrawBrush(int cx, int cy, int radius, Color color) {
            int sqrRadius = radius * radius;
            for (int dx = -radius; dx <= radius; dx++) {
                for (int dy = -radius; dy <= radius; dy++) {
                    int x = cx + dx;
                    int y = cy + dy;

                    if (x >= 0 && x < textureSize && y >= 0 && y < textureSize) {
                        if (dx * dx + dy * dy <= sqrRadius) {
                            drawTexture.SetPixel(x, y, color);

                            if (x < minX) minX = x;
                            if (x > maxX) maxX = x;
                            if (y < minY) minY = y;
                            if (y > maxY) maxY = y;
                        }
                    }
                }
            }
        }

        public void ClearTexture() {
            Color clearColor = new Color(0, 0, 0, 0);
            Color[] clear = new Color[textureSize * textureSize];
            for (int i = 0; i < clear.Length; i++) clear[i] = clearColor;
            drawTexture.SetPixels(clear);
            drawTexture.Apply();
            ResetBoundingBox();
        }

        private void ResetBoundingBox() {
            minX = textureSize;
            maxX = 0;
            minY = textureSize;
            maxY = 0;
        }

        public Texture2D GetDrawnTexture() {
            return drawTexture;
        }

        public Texture2D GetNormalizedTexture64() {
            int cropW = maxX - minX + 1;
            int cropH = maxY - minY + 1;

            if (cropW <= 0 || cropH <= 0)
                return new Texture2D(64, 64);

            Texture2D cropped = new Texture2D(cropW, cropH);
            cropped.SetPixels(drawTexture.GetPixels(minX, minY, cropW, cropH));
            cropped.Apply();

            Texture2D resized = new Texture2D(64, 64);
            for (int y = 0; y < 64; y++) {
                for (int x = 0; x < 64; x++) {
                    float u = (float)x / 63f;
                    float v = (float)y / 63f;
                    resized.SetPixel(x, y, cropped.GetPixelBilinear(u, v));
                }
            }

            resized.Apply();
            return resized;
        }
    }
}

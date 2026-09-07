using UnityEngine;
using UnityEngine.UIElements;

namespace ThreeInARow.Presentation
{
    public sealed partial class ThreeInARowApp
    {
        private void SizeEncounterBoard()
        {
            if (_board == null || _safeArea == null || _board.panel == null) return;
            var reserved = 0f;
            foreach (var child in _safeArea.Children())
            {
                if (child == _board || child.resolvedStyle.position == Position.Absolute) continue;
                reserved += child.layout.height + child.resolvedStyle.marginTop + child.resolvedStyle.marginBottom;
            }
            var side = Mathf.Min(720f, _safeArea.contentRect.width,
                Mathf.Max(0f, _safeArea.contentRect.height - reserved - 8f));
            if (float.IsNaN(side) || side < 1f) return;
            _board.style.width = side;
            _board.style.height = side;
        }

        private void AddDungeonBackdrop()
        {
            var backdrop = new DungeonOrnament(true) { name = "dungeon-backdrop" };
            backdrop.style.position = Position.Absolute;
            backdrop.style.left = 0;
            backdrop.style.right = 0;
            backdrop.style.top = 0;
            backdrop.style.bottom = 0;
            _root.Add(backdrop);
        }

        private void StyleGameButton(Button button, bool primary)
        {
            var resting = primary ? Hex("#234542") : Hex("#14282D");
            var edge = primary ? Gold : Hex("#4A696A");
            button.AddToClassList("game-button");
            if (_reducedMotion) button.AddToClassList("reduced-motion");
            button.style.backgroundImage = StyleKeyword.None;
            button.style.backgroundColor = resting;
            SetBorder(button, edge, 1);
            button.style.borderBottomWidth = 3;
            button.style.borderTopLeftRadius = 3;
            button.style.borderTopRightRadius = 3;
            button.style.borderBottomLeftRadius = 3;
            button.style.borderBottomRightRadius = 3;
            button.RegisterCallback<PointerDownEvent>(_ =>
            {
                if (button.enabledInHierarchy) button.style.backgroundColor = Hex("#39625A");
            });
            button.RegisterCallback<PointerUpEvent>(_ => button.style.backgroundColor = resting);
            button.RegisterCallback<PointerCancelEvent>(_ => button.style.backgroundColor = resting);
            button.RegisterCallback<PointerLeaveEvent>(_ => button.style.backgroundColor = resting);
        }

        // Procedural crystal silhouettes and etched lines need no external art or texture downloads.
        private sealed class DungeonOrnament : VisualElement
        {
            private readonly bool _backdrop;
            public DungeonOrnament(bool backdrop)
            {
                _backdrop = backdrop;
                pickingMode = PickingMode.Ignore;
                generateVisualContent += Draw;
            }

            private void Draw(MeshGenerationContext context)
            {
                var painter = context.painter2D;
                var w = contentRect.width;
                var h = contentRect.height;
                if (w <= 0 || h <= 0) return;
                if (_backdrop)
                {
                    painter.fillColor = Hex("#10232A");
                    for (var index = 0; index < 7; index++)
                    {
                        var x = index * w / 6f;
                        var rise = h * (index % 2 == 0 ? 0.23f : 0.15f);
                        painter.BeginPath();
                        painter.MoveTo(new Vector2(x - w * 0.1f, h));
                        painter.LineTo(new Vector2(x - w * 0.035f, h - rise));
                        painter.LineTo(new Vector2(x + w * 0.025f, h - rise * 1.18f));
                        painter.LineTo(new Vector2(x + w * 0.1f, h));
                        painter.ClosePath();
                        painter.Fill();
                    }
                    painter.strokeColor = Hex("#1D363C");
                    painter.lineWidth = 1;
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(8, h * 0.77f));
                    painter.LineTo(new Vector2(8, 34));
                    painter.LineTo(new Vector2(34, 8));
                    painter.LineTo(new Vector2(w - 34, 8));
                    painter.LineTo(new Vector2(w - 8, 34));
                    painter.LineTo(new Vector2(w - 8, h * 0.77f));
                    painter.Stroke();
                }
                else
                {
                    painter.strokeColor = Gold;
                    painter.lineWidth = 1.5f;
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(0, h / 2));
                    painter.LineTo(new Vector2(w / 2 - 20, h / 2));
                    painter.MoveTo(new Vector2(w / 2 + 20, h / 2));
                    painter.LineTo(new Vector2(w, h / 2));
                    painter.MoveTo(new Vector2(w / 2, 4));
                    painter.LineTo(new Vector2(w / 2 + 12, h / 2));
                    painter.LineTo(new Vector2(w / 2, h - 4));
                    painter.LineTo(new Vector2(w / 2 - 12, h / 2));
                    painter.ClosePath();
                    painter.Stroke();
                }
            }
        }
    }
}

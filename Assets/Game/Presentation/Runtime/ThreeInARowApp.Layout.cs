using System;
using ThreeInARow.Application;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThreeInARow.Presentation
{
    public sealed partial class ThreeInARowApp
    {
        private VisualElement _contentHost;
        private int _codexSection;
        private int _expeditionSection;

        private VisualElement ContentHost => _contentHost ?? _safeArea;

        private VisualElement CreateScrollBody(string name)
        {
            var scroll = new ScrollView(ScrollViewMode.Vertical) { name = name };
            scroll.style.flexGrow = 1;
            scroll.style.flexShrink = 1;
            scroll.style.width = Length.Percent(100);
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            _safeArea.Add(scroll);

            var body = new VisualElement { name = name + "-content" };
            body.style.width = Length.Percent(100);
            body.style.flexShrink = 0;
            scroll.Add(body);
            _contentHost = body;
            return body;
        }

        private void AddScreenHeading(string eyebrow, string heading, string introduction = null)
        {
            if (!string.IsNullOrEmpty(eyebrow))
            {
                var eyebrowLabel = LabelText(eyebrow.ToUpperInvariant(), 15, Cyan, TextAnchor.MiddleCenter);
                eyebrowLabel.style.letterSpacing = 2;
                eyebrowLabel.style.marginTop = 4;
                ContentHost.Add(eyebrowLabel);
            }

            var title = Title(heading, 38, Gold);
            title.style.marginTop = 2;
            title.style.marginBottom = 6;
            ContentHost.Add(title);

            if (string.IsNullOrEmpty(introduction)) return;
            var intro = LabelText(introduction, 18, Muted, TextAnchor.MiddleCenter);
            intro.style.whiteSpace = WhiteSpace.Normal;
            intro.style.marginBottom = 10;
            ContentHost.Add(intro);
        }

        private VisualElement TabStrip(string[] labels, int selected, Action<int> select)
        {
            var strip = new VisualElement();
            strip.name = "section-tabs";
            strip.style.marginTop = 8;
            strip.style.marginBottom = 10;
            var columns = labels.Length > 3 ? 2 : labels.Length;
            VisualElement currentRow = null;
            for (var index = 0; index < labels.Length; index++)
            {
                if (index % columns == 0)
                {
                    currentRow = Row();
                    currentRow.name = "section-tab-row";
                    strip.Add(currentRow);
                }
                var captured = index;
                var button = SmallButton(labels[index], () => select(captured));
                button.style.flexGrow = 1;
                button.style.flexBasis = 0;
                button.style.minWidth = 0;
                button.style.marginTop = 4;
                button.style.marginBottom = 4;
                button.style.backgroundColor = index == selected ? Hex("#29524E") : Hex("#10252D");
                button.style.color = index == selected ? Gold : TextColor;
                currentRow.Add(button);
            }
            return strip;
        }

        private Button NavigationTile(string heading, string detail, Action action)
        {
            var button = new Button(action);
            button.style.flexGrow = 1;
            button.style.flexBasis = 0;
            button.style.minWidth = 0;
            button.style.minHeight = 94;
            button.style.height = StyleKeyword.Auto;
            button.style.marginLeft = 5;
            button.style.marginRight = 5;
            button.style.marginTop = 5;
            button.style.marginBottom = 5;
            button.style.paddingLeft = 14;
            button.style.paddingRight = 14;
            button.style.paddingTop = 12;
            button.style.paddingBottom = 12;
            button.style.flexDirection = FlexDirection.Column;
            button.style.alignItems = Align.FlexStart;
            StyleGameButton(button, false);

            var title = LabelText(heading, 19, Gold);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.whiteSpace = WhiteSpace.Normal;
            button.Add(title);
            var description = LabelText(detail, 15, Muted);
            description.style.whiteSpace = WhiteSpace.Normal;
            description.style.marginTop = 3;
            button.Add(description);
            return button;
        }

        private VisualElement Footer(params VisualElement[] actions)
        {
            var footer = new VisualElement { name = "screen-footer" };
            footer.style.flexShrink = 0;
            footer.style.width = Length.Percent(100);
            footer.style.paddingTop = 6;
            foreach (var action in actions) footer.Add(action);
            _safeArea.Add(footer);
            return footer;
        }

        private Button RunMenuButton()
        {
            var button = SmallButton("← МЕНЮ", ShowSaveAndExitConfirmation);
            button.tooltip = "Сохранить забег и вернуться в главное меню";
            return button;
        }

        private void ShowSaveAndExitConfirmation()
        {
            if (_inputLocked || _director.Screen == RunScreen.SkillWindow)
            {
                ShowModal("ХОД ЕЩЁ ВЫПОЛНЯЕТСЯ",
                    "Дождитесь завершения анимации и ответа врага, затем откройте меню снова.");
                return;
            }

            if (!FlushPulseClockBeforeInput()) return;
            _director.SaveCurrentCheckpoint();

            ShowModal("ВЕРНУТЬСЯ В ГЛАВНОЕ МЕНЮ?",
                "Последняя безопасная точка забега сохранена. Его можно будет продолжить с главного экрана.", modal =>
                {
                    modal.Add(ActionButton("СОХРАНИТЬ И ВЫЙТИ", () =>
                    {
                        _director.ReturnToTitle(false);
                        BuildTitle();
                    }, true));
                });
        }
    }
}

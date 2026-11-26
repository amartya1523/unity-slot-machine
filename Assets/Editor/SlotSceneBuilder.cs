using System.Collections.Generic;
using System.IO;
using Underpin.SlotMachine.Audio;
using Underpin.SlotMachine.Data;
using Underpin.SlotMachine.Machine;
using Underpin.SlotMachine.Reels;
using Underpin.SlotMachine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Underpin.SlotMachine.EditorTools
{
    /// <summary>
    /// Builds the whole game scene, its prefabs, data assets and animation clips from code.
    ///
    /// Why a builder instead of only a hand-made scene: the layout is reproducible, every
    /// reference is wired the same way each time, and the reel windows line up exactly with
    /// the pixel coordinates measured from the provided cabinet art. Run it from
    /// "Underpin > Rebuild Slot Machine Scene". It is safe to run again.
    /// </summary>
    public static class SlotSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/SlotMachine.unity";

        private const string SpriteDir = "Assets/UI/Sprites";
        private const string SoundDir = "Assets/Sounds";
        private const string DataDir = "Assets/Data";
        private const string AnimDir = "Assets/Animations";
        private const string PrefabDir = "Assets/Prefabs";

        // Cabinet art is 816 x 624. These are the transparent reel windows measured from it.
        private const float CabinetWidth = 816f;
        private const float CabinetHeight = 624f;
        private static readonly float[] ReelWindowX = { 229f, 359f, 489f };
        private const float ReelWindowY = 246f;
        private const float ReelWindowWidth = 108f;
        private const float ReelWindowHeight = 210f;
        private const float SymbolSize = 96f;
        private const float SymbolPitch = 104f;
        private const int CellsPerReel = 5;

        private static readonly Color Gold = new Color(1f, 0.84f, 0.32f);
        private static readonly Color SoftWhite = new Color(0.95f, 0.95f, 1f);
        private static readonly Color LabelBlue = new Color(0.72f, 0.76f, 1f);

        private static Font font;

        [MenuItem("Underpin/Rebuild Slot Machine Scene", priority = 0)]
        public static void BuildScene()
        {
            SpriteImportProcessor.EnsureSpritesImported();
            foreach (string dir in new[] { DataDir, DataDir + "/Symbols", AnimDir, PrefabDir, "Assets/Scenes" })
            {
                Directory.CreateDirectory(dir);
            }

            AssetDatabase.Refresh();
            font = LoadFont();

            // ---------- Data ----------
            SymbolDefinition seven = CreateSymbol("seven", "Seven", "symbol_seven", 2, 50, SymbolRole.Jackpot, 0);
            SymbolDefinition bar = CreateSymbol("bar", "BAR", "symbol_bar", 4, 25, SymbolRole.Standard, 0);
            SymbolDefinition bell = CreateSymbol("bell", "Bell", "symbol_bell", 6, 10, SymbolRole.FreeSpins, 5);
            SymbolDefinition cherry = CreateSymbol("cherry", "Cherry", "symbol_cherry", 8, 4, SymbolRole.Standard, 0);
            var symbols = new List<SymbolDefinition> { seven, bar, bell, cherry };

            var paytable = LoadOrCreate<PaytableDefinition>($"{DataDir}/Paytable.asset");
            paytable.Configure(symbols, new[] { 10, 50, 100 }, 1000, 1000, 0.05f, 20);
            EditorUtility.SetDirty(paytable);

            var reelProfile = LoadOrCreate<ReelAnimationProfile>($"{AnimDir}/ReelAnimationProfile.asset");
            AnimationClip titleGlow = CreatePulseClip($"{AnimDir}/WinTitleGlow.anim", 1.1f, 0.6f);
            AnimationClip spinIdle = CreatePulseClip($"{AnimDir}/SpinButtonIdle.anim", 1.04f, 1.4f);
            AssetDatabase.SaveAssets();

            // ---------- Scene ----------
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();
            CreateEventSystem();
            Transform canvas = CreateCanvas();

            Image background = AddImage(Ui("Background", canvas), LoadSprite("Panels/background_gradient"));
            Stretch(background.rectTransform);

            // Machine root uses the cabinet's own pixel space (816 x 624), scaled up as one block.
            RectTransform machine = RectOf(Ui("SlotMachine", canvas));
            Center(machine, new Vector2(-260f, -10f), new Vector2(CabinetWidth, CabinetHeight));
            machine.localScale = Vector3.one * 1.55f;

            Stretch(AddImage(Ui("ReelGlass", machine), LoadSprite("Machine/reel_glass")).rectTransform);

            // Reels: build the first one, save it as a prefab, then instance it twice.
            ReelView firstReel = CreateReel(machine, reelProfile);
            PlaceTopLeft(RectOf(firstReel.gameObject), ReelWindowX[0], ReelWindowY, ReelWindowWidth, ReelWindowHeight);
            GameObject reelPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(
                firstReel.gameObject, $"{PrefabDir}/Reel.prefab", InteractionMode.AutomatedAction);

            var reels = new List<ReelView> { firstReel };
            for (int i = 1; i < ReelWindowX.Length; i++)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(reelPrefab, machine);
                instance.name = $"Reel {i + 1}";
                PlaceTopLeft(RectOf(instance), ReelWindowX[i], ReelWindowY, ReelWindowWidth, ReelWindowHeight);
                reels.Add(instance.GetComponent<ReelView>());
            }

            firstReel.gameObject.name = "Reel 1";

            float windowsWidth = ReelWindowX[2] + ReelWindowWidth - ReelWindowX[0];
            Image payline = AddImage(Ui("Payline", machine), null);
            payline.color = new Color(1f, 0.2f, 0.2f, 0.55f);
            PlaceTopLeft(payline.rectTransform, ReelWindowX[0], ReelWindowY + ReelWindowHeight / 2f - 1.5f, windowsWidth, 3f);

            Image glow = AddImage(Ui("WinGlow", machine), null);
            PlaceTopLeft(glow.rectTransform, ReelWindowX[0], ReelWindowY, windowsWidth, ReelWindowHeight);
            var winFlash = glow.gameObject.AddComponent<WinFlashView>();
            Set(winFlash, "glow", glow);

            Stretch(AddImage(Ui("CabinetFrame", machine), LoadSprite("Machine/cabinet_frame")).rectTransform);

            LeverView lever = CreateLever(machine);

            Text cabinetWin = AddText(Ui("CabinetWinText", machine), "WIN 0", 26, Gold, TextAnchor.MiddleCenter);
            PlaceTopLeft(cabinetWin.rectTransform, 240f, 562f, 345f, 50f);

            // ---------- Right-hand HUD ----------
            RectTransform hudRoot = RectOf(Ui("HUD", canvas));
            Stretch(hudRoot);
            HudView hud = hudRoot.gameObject.AddComponent<HudView>();

            Text credits = CreateInfoBox(hudRoot, "CreditsBox", "CREDITS", new Vector2(640f, 300f));
            Text jackpot = CreateInfoBox(hudRoot, "JackpotBox", "PROGRESSIVE JACKPOT", new Vector2(640f, 130f));
            jackpot.color = new Color(1f, 0.45f, 0.35f);
            Text betValue = CreateInfoBox(hudRoot, "BetBox", "BET", new Vector2(640f, -40f));

            Button betDown = CreateSpriteButton(hudRoot, "BetDown", "arrow_left", new Vector2(380f, -40f), new Vector2(84f, 84f));
            Button betUp = CreateSpriteButton(hudRoot, "BetUp", "arrow_right", new Vector2(900f, -40f), new Vector2(84f, 84f));

            Button spin = CreateBoxButton(hudRoot, "SpinButton", "SPIN", 64, new Vector2(640f, -215f), new Vector2(420f, 150f));
            AddLoopingAnimation(spin.gameObject, spinIdle);
            Button paytableButton = CreateBoxButton(hudRoot, "PaytableButton", "PAYTABLE", 26, new Vector2(640f, -345f), new Vector2(240f, 80f));

            RectTransform freeSpinsBadge = RectOf(Ui("FreeSpinsBadge", hudRoot));
            Center(freeSpinsBadge, new Vector2(640f, 450f), new Vector2(380f, 100f));
            AddImage(freeSpinsBadge.gameObject, LoadSprite("Panels/info_box")).color = new Color(1f, 0.85f, 0.4f);
            Text freeSpinsText = AddText(Ui("Text", freeSpinsBadge), "FREE SPINS: 0", 36, Color.white, TextAnchor.MiddleCenter);
            Stretch(freeSpinsText.rectTransform);

            Text message = AddText(Ui("Message", hudRoot), "", 28, SoftWhite, TextAnchor.MiddleCenter);
            Center(message.rectTransform, new Vector2(640f, -450f), new Vector2(560f, 90f));

            Text hint = AddText(Ui("Hint", hudRoot), "SPACE / lever / SPIN to play", 20, LabelBlue, TextAnchor.MiddleCenter, FontStyle.Normal);
            Center(hint.rectTransform, new Vector2(640f, -510f), new Vector2(560f, 40f));

            Set(hud, "creditsText", credits);
            Set(hud, "betText", betValue);
            Set(hud, "jackpotText", jackpot);
            Set(hud, "lastWinText", cabinetWin);
            Set(hud, "messageText", message);
            Set(hud, "freeSpinsBadge", freeSpinsBadge.gameObject);
            Set(hud, "freeSpinsText", freeSpinsText);

            // ---------- QA panel ----------
            QaPanelView qa = CreateQaPanel(canvas, symbols);

            // ---------- Popups (last, so they draw on top) ----------
            PaytableView paytableView = CreatePaytablePopup(canvas, symbols);
            ConfirmDialogView confirm = CreateConfirmPopup(canvas);
            WinPopupView winPopup = CreateWinPopup(canvas, titleGlow);

            SavePrefab(paytableView.gameObject, "PaytablePopup");
            SavePrefab(confirm.gameObject, "ConfirmDialog");
            SavePrefab(winPopup.gameObject, "WinPopup");

            // ---------- Audio ----------
            SfxPlayer sfx = CreateAudio();

            // ---------- Controller ----------
            var controllerGo = new GameObject("SlotMachineController");
            var controller = controllerGo.AddComponent<SlotMachineController>();
            Set(controller, "paytable", paytable);
            SetArray(controller, "reels", reels.ConvertAll(r => (Object)r));
            Set(controller, "lever", lever);
            Set(controller, "winFlash", winFlash);
            Set(controller, "hud", hud);
            Set(controller, "winPopup", winPopup);
            Set(controller, "confirmDialog", confirm);
            Set(controller, "paytableView", paytableView);
            Set(controller, "qaPanel", qa);
            Set(controller, "spinButton", spin);
            Set(controller, "betUpButton", betUp);
            Set(controller, "betDownButton", betDown);
            Set(controller, "paytableButton", paytableButton);
            Set(controller, "sfx", sfx);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[SlotSceneBuilder] Scene, prefabs and data assets rebuilt.");
        }

        // =====================================================================
        // Data + animation assets
        // =====================================================================

        private static SymbolDefinition CreateSymbol(string id, string displayName, string spriteName,
            int weight, int multiplier, SymbolRole role, int freeSpins)
        {
            var symbol = LoadOrCreate<SymbolDefinition>($"{DataDir}/Symbols/{displayName}.asset");
            symbol.Configure(id, displayName, LoadSprite($"Symbols/{spriteName}"), weight, multiplier, role, freeSpins);
            EditorUtility.SetDirty(symbol);
            return symbol;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        /// <summary>A looping legacy clip that gently scales the object up and back down.</summary>
        private static AnimationClip CreatePulseClip(string path, float peakScale, float period)
        {
            var clip = new AnimationClip { legacy = true, wrapMode = WrapMode.Loop };
            var curve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(period * 0.5f, peakScale),
                new Keyframe(period, 1f));
            for (int i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            }

            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalScale.x"), curve);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalScale.y"), curve);

            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        private static void AddLoopingAnimation(GameObject target, AnimationClip clip)
        {
            var animation = target.AddComponent<Animation>();
            animation.clip = clip;
            animation.AddClip(clip, clip.name);
            animation.playAutomatically = true;
            animation.wrapMode = WrapMode.Loop;
        }

        // =====================================================================
        // Scene scaffolding
        // =====================================================================

        private static void CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.04f, 0.12f);
            cam.orthographic = true;
            go.AddComponent<AudioListener>();
        }

        private static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        private static Transform CreateCanvas()
        {
            var go = new GameObject("Canvas", typeof(RectTransform));
            go.layer = 5;
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return go.transform;
        }

        private static ReelView CreateReel(Transform parent, ReelAnimationProfile profile)
        {
            GameObject reelGo = Ui("Reel", parent);
            reelGo.AddComponent<RectMask2D>();
            var reel = reelGo.AddComponent<ReelView>();

            var cells = new List<Object>();
            for (int i = 0; i < CellsPerReel; i++)
            {
                Image cell = AddImage(Ui($"Cell {i}", reelGo.transform), LoadSprite("Symbols/symbol_cherry"));
                cell.preserveAspect = true;
                RectTransform rt = cell.rectTransform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(SymbolSize, SymbolSize);
                rt.anchoredPosition = new Vector2(0f, (i - CellsPerReel / 2) * SymbolPitch);
                cells.Add(cell);
            }

            SetArray(reel, "cells", cells);
            Set(reel, "profile", profile);
            SetFloat(reel, "symbolPitch", SymbolPitch);
            return reel;
        }

        private static LeverView CreateLever(Transform machine)
        {
            Image leverImage = AddImage(Ui("Lever", machine), LoadSprite("Machine/lever_up"));
            Stretch(leverImage.rectTransform);
            var lever = leverImage.gameObject.AddComponent<LeverView>();

            // Clickable area just around the lever knob and stick (measured from the art).
            Image hit = AddImage(Ui("LeverHitArea", machine), null, true);
            hit.color = Color.clear;
            PlaceTopLeft(hit.rectTransform, 668f, 290f, 102f, 280f);
            var button = hit.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;

            Set(lever, "leverImage", leverImage);
            Set(lever, "leverUp", LoadSprite("Machine/lever_up"));
            Set(lever, "leverDown", LoadSprite("Machine/lever_down"));
            Set(lever, "hitArea", button);
            return lever;
        }

        private static SfxPlayer CreateAudio()
        {
            var go = new GameObject("Audio");
            var sfx = go.AddComponent<SfxPlayer>();
            var oneShot = go.AddComponent<AudioSource>();
            oneShot.playOnAwake = false;
            var loop = go.AddComponent<AudioSource>();
            loop.playOnAwake = false;
            loop.volume = 0.5f;

            Set(sfx, "oneShotSource", oneShot);
            Set(sfx, "loopSource", loop);
            Set(sfx, "leverPull", Clip("lever_pull"));
            Set(sfx, "spinLoop", Clip("reel_spin_loop"));
            Set(sfx, "reelStop", Clip("reel_stop"));
            Set(sfx, "buttonClick", Clip("button_click"));
            Set(sfx, "winSmall", Clip("win_small"));
            Set(sfx, "winJackpot", Clip("win_jackpot"));
            Set(sfx, "coinTick", Clip("coin_tick"));
            Set(sfx, "freeSpins", Clip("free_spins"));
            return sfx;
        }

        // =====================================================================
        // HUD widgets
        // =====================================================================

        /// <summary>A labelled value box (info_box art). Returns the value text.</summary>
        private static Text CreateInfoBox(Transform parent, string name, string label, Vector2 position)
        {
            RectTransform box = RectOf(Ui(name, parent));
            Center(box, position, new Vector2(420f, 150f));
            AddImage(box.gameObject, LoadSprite("Panels/info_box"));

            Text labelText = AddText(Ui("Label", box), label, 22, LabelBlue, TextAnchor.MiddleCenter);
            PlaceAnchored(labelText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(400f, 34f));

            Text value = AddText(Ui("Value", box), "0", 52, Gold, TextAnchor.MiddleCenter);
            PlaceAnchored(value.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -14f), new Vector2(400f, 70f));
            return value;
        }

        private static Button CreateBoxButton(Transform parent, string name, string label, int fontSize,
            Vector2 position, Vector2 size)
        {
            RectTransform rt = RectOf(Ui(name, parent));
            Center(rt, position, size);
            Image image = AddImage(rt.gameObject, LoadSprite("Panels/info_box"), true);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colours = button.colors;
            colours.highlightedColor = new Color(1f, 0.92f, 0.7f);
            colours.pressedColor = new Color(0.75f, 0.65f, 0.45f);
            colours.disabledColor = new Color(0.45f, 0.45f, 0.5f, 0.8f);
            button.colors = colours;

            Text text = AddText(Ui("Label", rt), label, fontSize, Gold, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            return button;
        }

        /// <summary>Button using the 4-state sprite sheets (normal / highlighted / pressed / disabled).</summary>
        private static Button CreateSpriteButton(Transform parent, string name, string spriteKey,
            Vector2 position, Vector2 size)
        {
            RectTransform rt = RectOf(Ui(name, parent));
            Center(rt, position, size);
            Image image = AddImage(rt.gameObject, LoadSprite($"Buttons/btn_{spriteKey}_normal"), true);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            SpriteState states = button.spriteState;
            states.highlightedSprite = LoadSprite($"Buttons/btn_{spriteKey}_highlighted");
            states.pressedSprite = LoadSprite($"Buttons/btn_{spriteKey}_pressed");
            states.selectedSprite = LoadSprite($"Buttons/btn_{spriteKey}_normal");
            states.disabledSprite = TrySprite($"Buttons/btn_{spriteKey}_disabled") ?? LoadSprite($"Buttons/btn_{spriteKey}_normal");
            button.spriteState = states;
            return button;
        }

        private static QaPanelView CreateQaPanel(Transform canvas, List<SymbolDefinition> symbols)
        {
            RectTransform root = RectOf(Ui("QaPanel", canvas));
            Stretch(root);
            var qa = root.gameObject.AddComponent<QaPanelView>();

            RectTransform toggle = RectOf(Ui("QaToggle", root));
            PlaceAnchored(toggle, new Vector2(0f, 0f), new Vector2(60f, 40f), new Vector2(80f, 44f));
            Image toggleBg = AddImage(toggle.gameObject, null, true);
            toggleBg.color = new Color(1f, 1f, 1f, 0.12f);
            var toggleButton = toggle.gameObject.AddComponent<Button>();
            toggleButton.targetGraphic = toggleBg;
            Stretch(AddText(Ui("Label", toggle), "QA", 22, new Color(1f, 1f, 1f, 0.6f), TextAnchor.MiddleCenter).rectTransform);

            RectTransform panel = RectOf(Ui("QaButtons", root));
            PlaceAnchored(panel, new Vector2(0f, 0f), new Vector2(180f, 230f), new Vector2(340f, 300f));
            Image panelBg = AddImage(panel.gameObject, LoadSprite("Panels/info_box"), true);
            panelBg.type = Image.Type.Simple;

            Text title = AddText(Ui("Title", panel), "QA: force next spin", 20, LabelBlue, TextAnchor.MiddleCenter);
            PlaceAnchored(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(320f, 30f));

            var buttons = new List<Object>();
            var forced = new List<Object>();
            for (int i = 0; i < symbols.Count; i++)
            {
                SymbolDefinition symbol = symbols[i];
                string label = symbol.Role == SymbolRole.Jackpot ? "3 x 7  (jackpot)"
                    : symbol.Role == SymbolRole.FreeSpins ? $"3 x {symbol.DisplayName}  (free spins)"
                    : $"3 x {symbol.DisplayName}";

                RectTransform b = RectOf(Ui($"Force {symbol.DisplayName}", panel));
                PlaceAnchored(b, new Vector2(0.5f, 1f), new Vector2(0f, -78f - i * 54f), new Vector2(290f, 46f));
                Image bg = AddImage(b.gameObject, null, true);
                bg.color = new Color(0.2f, 0.2f, 0.45f, 0.95f);
                var button = b.gameObject.AddComponent<Button>();
                button.targetGraphic = bg;
                Stretch(AddText(Ui("Label", b), label, 20, SoftWhite, TextAnchor.MiddleCenter).rectTransform);
                buttons.Add(button);
                forced.Add(symbol);
            }

            Set(qa, "toggleButton", toggleButton);
            Set(qa, "panelRoot", panel.gameObject);
            SetArray(qa, "forceButtons", buttons);
            SetArray(qa, "forceSymbols", forced);
            return qa;
        }

        // =====================================================================
        // Popups
        // =====================================================================

        /// <summary>Full-screen dimmer + centred popup panel. Returns the panel.</summary>
        private static RectTransform CreatePopupShell(Transform canvas, string name, Vector2 panelSize, out GameObject root)
        {
            root = Ui(name, canvas);
            Stretch(RectOf(root));
            var group = root.AddComponent<CanvasGroup>();
            group.alpha = 0f; // hidden until opened at runtime
            group.blocksRaycasts = false;
            group.interactable = false;
            Image dimmer = AddImage(root, null, true);
            dimmer.color = new Color(0f, 0f, 0.05f, 0.65f);

            RectTransform panel = RectOf(Ui("Panel", root.transform));
            Center(panel, Vector2.zero, panelSize);
            AddImage(panel.gameObject, LoadSprite("Panels/popup_panel"), true);
            return panel;
        }

        private static Button AddCloseButton(RectTransform panel)
        {
            Button close = CreateSpriteButton(panel, "CloseButton", "close", Vector2.zero, new Vector2(90f, 90f));
            PlaceAnchored(RectOf(close.gameObject), new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(90f, 90f));
            return close;
        }

        private static WinPopupView CreateWinPopup(Transform canvas, AnimationClip titleGlow)
        {
            RectTransform panel = CreatePopupShell(canvas, "WinPopup", new Vector2(900f, 424f), out GameObject root);
            var view = root.AddComponent<WinPopupView>();

            Text title = AddText(Ui("Title", panel), "WIN!", 84, Gold, TextAnchor.MiddleCenter);
            Center(title.rectTransform, new Vector2(0f, 120f), new Vector2(800f, 110f));
            var animation = title.gameObject.AddComponent<Animation>();
            animation.clip = titleGlow;
            animation.AddClip(titleGlow, titleGlow.name);
            animation.playAutomatically = false;
            animation.wrapMode = WrapMode.Loop;

            Image icon = AddImage(Ui("SymbolIcon", panel), LoadSprite("Symbols/symbol_seven"));
            icon.preserveAspect = true;
            Center(icon.rectTransform, new Vector2(-300f, 5f), new Vector2(120f, 120f));

            Text amount = AddText(Ui("Amount", panel), "0", 96, Color.white, TextAnchor.MiddleCenter);
            Center(amount.rectTransform, new Vector2(40f, 5f), new Vector2(560f, 120f));

            Text detail = AddText(Ui("Detail", panel), "", 28, LabelBlue, TextAnchor.UpperCenter, FontStyle.Normal);
            Center(detail.rectTransform, new Vector2(0f, -125f), new Vector2(800f, 110f));

            Button close = AddCloseButton(panel);

            Set(view, "panel", panel);
            Set(view, "titleText", title);
            Set(view, "amountText", amount);
            Set(view, "detailText", detail);
            Set(view, "symbolIcon", icon);
            Set(view, "closeButton", close);
            Set(view, "titleAnimation", animation);
            return view;
        }

        private static ConfirmDialogView CreateConfirmPopup(Transform canvas)
        {
            RectTransform panel = CreatePopupShell(canvas, "ConfirmDialog", new Vector2(820f, 386f), out GameObject root);
            var view = root.AddComponent<ConfirmDialogView>();

            Text message = AddText(Ui("Message", panel), "", 40, Color.white, TextAnchor.MiddleCenter);
            Center(message.rectTransform, new Vector2(0f, 60f), new Vector2(720f, 160f));

            Button yes = CreateStateButton(panel, "YesButton", "yes", new Vector2(-170f, -110f));
            Button no = CreateStateButton(panel, "NoButton", "no", new Vector2(170f, -110f));

            Set(view, "panel", panel);
            Set(view, "messageText", message);
            Set(view, "yesButton", yes);
            Set(view, "noButton", no);
            return view;
        }

        /// <summary>YES / NO art comes in 3 states (normal / highlighted / pressed).</summary>
        private static Button CreateStateButton(Transform parent, string name, string key, Vector2 position)
        {
            Button button = CreateSpriteButton(parent, name, key, position, new Vector2(260f, 100f));
            return button;
        }

        private static PaytableView CreatePaytablePopup(Transform canvas, List<SymbolDefinition> symbols)
        {
            RectTransform panel = CreatePopupShell(canvas, "PaytablePopup", new Vector2(1100f, 560f), out GameObject root);
            var view = root.AddComponent<PaytableView>();

            Text title = AddText(Ui("Title", panel), "PAYTABLE", 60, Gold, TextAnchor.MiddleCenter);
            Center(title.rectTransform, new Vector2(0f, 205f), new Vector2(800f, 80f));

            for (int i = 0; i < symbols.Count; i++)
            {
                SymbolDefinition symbol = symbols[i];
                float y = 115f - i * 78f;
                for (int k = 0; k < 3; k++)
                {
                    Image icon = AddImage(Ui($"{symbol.DisplayName} icon {k}", panel), symbol.Sprite);
                    icon.preserveAspect = true;
                    Center(icon.rectTransform, new Vector2(-330f + k * 72f, y), new Vector2(64f, 64f));
                }

                string extra = symbol.Role == SymbolRole.Jackpot ? "  + PROGRESSIVE JACKPOT"
                    : symbol.Role == SymbolRole.FreeSpins ? $"  + {symbol.FreeSpinsAwarded} FREE SPINS"
                    : "";
                Text pays = AddText(Ui($"{symbol.DisplayName} pays", panel),
                    $"pays {symbol.ThreeOfAKindMultiplier} x bet{extra}", 34, Color.white, TextAnchor.MiddleLeft);
                Center(pays.rectTransform, new Vector2(170f, y), new Vector2(620f, 64f));
            }

            Text rules = AddText(Ui("Rules", panel),
                "You win when all 3 reels show the same symbol on the red centre line.\n" +
                "5% of every paid bet grows the jackpot. Free spins replay your bet for free.",
                22, LabelBlue, TextAnchor.MiddleCenter, FontStyle.Normal);
            Center(rules.rectTransform, new Vector2(0f, -205f), new Vector2(1000f, 70f));

            Button close = AddCloseButton(panel);
            Set(view, "panel", panel);
            Set(view, "closeButton", close);
            return view;
        }

        private static void SavePrefab(GameObject go, string name)
        {
            PrefabUtility.SaveAsPrefabAssetAndConnect(go, $"{PrefabDir}/{name}.prefab", InteractionMode.AutomatedAction);
        }

        // =====================================================================
        // Small helpers
        // =====================================================================

        private static GameObject Ui(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
            go.transform.SetParent(parent, false);
            return go;
        }

        private static RectTransform RectOf(GameObject go) => (RectTransform)go.transform;

        private static Image AddImage(GameObject go, Sprite sprite, bool raycast = false)
        {
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = raycast;
            return image;
        }

        private static Text AddText(GameObject go, string text, int size, Color colour, TextAnchor alignment,
            FontStyle style = FontStyle.Bold)
        {
            var t = go.AddComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = colour;
            t.alignment = alignment;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0.1f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);
            return t;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void Center(RectTransform rt, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        private static void PlaceAnchored(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        /// <summary>Places a rect using top-left pixel coordinates of its parent (like an image editor).</summary>
        private static void PlaceTopLeft(RectTransform rt, float x, float y, float width, float height)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(width, height);
        }

        private static Sprite LoadSprite(string relativePath)
        {
            Sprite sprite = TrySprite(relativePath);
            if (sprite == null)
            {
                Debug.LogError($"[SlotSceneBuilder] Missing sprite {relativePath}");
            }

            return sprite;
        }

        private static Sprite TrySprite(string relativePath)
            => AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteDir}/{relativePath}.png");

        private static AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>($"{SoundDir}/{name}.wav");

        private static Font LoadFont()
        {
            // Unity 2022.2+ ships LegacyRuntime.ttf; older versions ship Arial.ttf.
            var legacy = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return legacy != null ? legacy : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static SerializedProperty Find(SerializedObject so, string field)
        {
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
            {
                throw new System.ArgumentException($"Field '{field}' not found on {so.targetObject.GetType().Name}");
            }

            return property;
        }

        private static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            Find(so, field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            Find(so, field).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(Object target, string field, IList<Object> values)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = Find(so, field);
            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

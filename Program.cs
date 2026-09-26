// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace havWndSwitcher
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            AppPaths.Ensure(); // Create config dir
            ConfigService.Load(AppPaths.ConfigFile);
            var resolvedLanguage = Localization.ResolveLanguage(AppPaths.LangRoot, ConfigService.CurrentConfig.Language);
            if (!string.Equals(resolvedLanguage, ConfigService.CurrentConfig.Language, StringComparison.OrdinalIgnoreCase))
            {
                ConfigService.CurrentConfig.Language = resolvedLanguage;
                ConfigService.Save(AppPaths.ConfigFile);
            }
            Localization.Initialize(AppPaths.LangRoot, resolvedLanguage);
            Application.Run(new SwitcherApp()); // No UI window
        }
    }

    static class Localization
    {
        private static Dictionary<string, string> _languages = new();

        public static void Initialize(string langRoot, string language)
        {
            using var english = typeof(Localization).Assembly.GetManifestResourceStream("languages.en.json");
            _languages = english is null
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(english) ?? new Dictionary<string, string>();
            MergeLanguageFile(Path.Combine(langRoot, "en.json"), _languages);

            if (!string.Equals(language, "en", StringComparison.OrdinalIgnoreCase))
            {
                MergeLanguageFile(Path.Combine(langRoot, $"{language}.json"), _languages);
            }
        }

        public static string Entry(string key, params object[] args)
        {
            if (!_languages.TryGetValue(key, out var value))
            {
                value = key;
            }

            return args.Length == 0 ? value : string.Format(value, args);
        }

        public static string ResolveLanguage(string langRoot, string? requested)
        {
            var available = GetAvailableLanguages(langRoot).Select(x => x.Code).ToList();

            if (!string.IsNullOrWhiteSpace(requested))
            {
                var normalized = requested!.Trim();
                if (available.Contains(normalized, StringComparer.OrdinalIgnoreCase))
                {
                    return normalized;
                }

                var dashIndex = normalized.IndexOf('-');
                if (dashIndex > 0)
                {
                    var shortCode = normalized.Substring(0, dashIndex);
                    if (available.Contains(shortCode, StringComparer.OrdinalIgnoreCase))
                    {
                        return shortCode;
                    }
                }
            }

            var cultureCode = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (available.Contains(cultureCode, StringComparer.OrdinalIgnoreCase))
            {
                return cultureCode;
            }

            return "en";
        }

        public static List<(string Code, string Name)> GetAvailableLanguages(string langRoot)
        {
            if (!Directory.Exists(langRoot))
            {
                return [("en", "English")];
            }

            var list = new List<(string Code, string Name)>();
            foreach (var file in Directory.GetFiles(langRoot, "*.json"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    list.Add((name, ReadLanguageName(file, name)));
                }
            }

            if (!list.Any(item => string.Equals(item.Code, "en", StringComparison.OrdinalIgnoreCase)))
            {
                list.Add(("en", "English"));
            }

            list.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Code, b.Code));
            return list;
        }

        private static Dictionary<string, string>? LoadLanguageFile(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            }
            catch
            {
            }

            return null;
        }

        private static void MergeLanguageFile(string path, Dictionary<string, string> target)
        {
            var data = LoadLanguageFile(path);
            if (data is null)
            {
                return;
            }

            foreach (var kvp in data)
            {
                if (kvp.Value is not null)
                {
                    target[kvp.Key] = kvp.Value;
                }
            }
        }

        private static string ReadLanguageName(string path, string fallback)
        {
            var data = LoadLanguageFile(path);
            if (data is not null && data.TryGetValue("LanguageName", out var name) && !string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            return fallback;
        }
    }

    // Location: %AppData%\havWndSwitcher\config.json
    class SwitcherConfig
    {
        public string Language { get; set; } = "en";
#if DEBUG
        public bool DebugOverlay { get; set; } = false;
#endif
        public bool PreferPerMonitor { get; set; } = true;
        public bool SkipFullscreen { get; set; } = true;
        public bool SuspendWhenFullscreen { get; set; } = true;
        public bool BoostNewWindows { get; set; } = true;
        public bool TaskbarWindowsOnly { get; set; } = true;
        public bool PreferMainWindowPerProcess { get; set; } = true;
        public bool IncludeMinimizedWindows { get; set; } = false;
        public int BoostWindowAgeSeconds { get; set; } = 5;
        public int NextWindowKey { get; set; } = (int)Keys.OemPeriod;
        public int PreviousWindowKey { get; set; } = (int)Keys.Oemcomma;
        public uint NextWindowModifiers { get; set; } = 0;
        public uint PreviousWindowModifiers { get; set; } = 0;
        public int FullscreenOverrideKey { get; set; } = (int)Keys.ShiftKey;
    }

    static class ConfigService
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };

        public static SwitcherConfig CurrentConfig { get; private set; } = new();

        public static void Load(string path)
        {
            if (!File.Exists(path))
            {
                // Write default config on first run
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, JsonSerializer.Serialize(CurrentConfig, _jsonSerializerOptions));
                return;
            }

            var json = File.ReadAllText(path);
            CurrentConfig = JsonSerializer.Deserialize<SwitcherConfig>(json) ?? new();
        }

        public static void Save(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(CurrentConfig, _jsonSerializerOptions));
        }

        public static void ResetToDefaults()
        {
            CurrentConfig = new SwitcherConfig();
        }
    }

    static class AppPaths
    {
        public static readonly string AppRoot = AppContext.BaseDirectory;
        public static readonly string ConfigRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "havWndSwitcher");

        public static string ConfigFile => Path.Combine(ConfigRoot, "config.json");
        public static string LangRoot => Path.Combine(AppRoot, "languages");

        public static void Ensure()
        {
            Directory.CreateDirectory(ConfigRoot);
        }
    }

#if DEBUG
    class DebugOverlay : Form
    {
        private readonly Label _label = new();

        public DebugOverlay()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.Black;
            ForeColor = Color.Lime;
            Opacity = 0.75;
            Padding = new Padding(8);
            AutoSize = true;

            _label.AutoSize = true;
            Controls.Add(_label);

            StartPosition = FormStartPosition.Manual;
            Location = new Point(10, 10);
        }

        public void UpdateText(string text)
        {
            _label.Text = text;
        }
    }
#endif

    class MessageWindow : NativeWindow, IDisposable
    {
        private readonly SwitcherApp _app;

        private const int WM_HOTKEY = 0x0312;

        public MessageWindow(SwitcherApp app)
        {
            _app = app;
            CreateHandle(new CreateParams()); // Invisible, no taskbar entry
        }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == WM_HOTKEY)
            {
                _app.OnHotkey(message.WParam.ToInt32());
            }
            base.WndProc(ref message);
        }

        public void Dispose()
        {
            DestroyHandle();
            GC.SuppressFinalize(this);
        }
    }

    class SettingsDialog : Form
    {
        private sealed class LanguageOption(string code, string label)
        {
            public string Code { get; } = code;
            public string Label { get; } = label;
            public override string ToString() => Label;
        }

        private sealed class ModifierOption(string label, uint modifier)
        {
            public string Label { get; } = label;
            public uint Modifier { get; } = modifier;
            public override string ToString() => Label;
        }

        private readonly TextBox _nextWindowKeyBox = new();
        private readonly TextBox _previousWindowKeyBox = new();
        private readonly CheckedListBox _nextWindowMods = new();
        private readonly CheckedListBox _previousWindowMods = new();
        private readonly TextBox _overrideKeyBox = new();
        private readonly Button _nextWindowCapture = new();
        private readonly Button _previousWindowCapture = new();
        private readonly Button _overrideCapture = new();
        private readonly NumericUpDown _freshSeconds = new();
        private readonly ComboBox _language = new();
        private readonly Label _hotkeyError = new() { ForeColor = Color.Firebrick };

        private TextBox? _captureTarget;
        private Action<Keys>? _captureSetter;
        private bool _suppressModifierEvents;

        public Keys NextWindowKey => _nextWindowKey;
        public Keys PreviousWindowKey => _previousWindowKey;
        public uint NextWindowModifiers => GetModifiers(_nextWindowMods);
        public uint PreviousWindowModifiers => GetModifiers(_previousWindowMods);
        public Keys FullscreenOverrideKey => _overrideKey;
        public int BoostWindowAgeSeconds => (int)_freshSeconds.Value;
        public string Language => (_language.SelectedItem as LanguageOption)?.Code ?? "en";

        private Keys _nextWindowKey;
        private Keys _previousWindowKey;
        private Keys _overrideKey;

        public SettingsDialog(Keys nextWindow, Keys previousWindow, uint nextWindowMods, uint previousWindowMods, Keys fullscreenOverrideKey, int boostWindowAgeSeconds, string language)
        {
            Text = Localization.Entry("Dialog_Settings_Title");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;
            KeyDown += OnDialogKeyDown;
            FormClosing += OnDialogFormClosing;

            var nextWindowLabel = new Label { Text = Localization.Entry("Dialog_NextWindow"), AutoSize = true };
            var nextWindowModsLabel = new Label { Text = Localization.Entry("Dialog_Modifiers"), AutoSize = true };
            var previousWindowLabel = new Label { Text = Localization.Entry("Dialog_PreviousWindow"), AutoSize = true };
            var previousWindowModsLabel = new Label { Text = Localization.Entry("Dialog_Modifiers"), AutoSize = true };
            var overrideLabel = new Label { Text = Localization.Entry("Dialog_FullscreenOverride"), AutoSize = true };
            var freshLabel = new Label { Text = Localization.Entry("Dialog_BoostWindowAge"), AutoSize = true };
            var languageLabel = new Label { Text = Localization.Entry("Dialog_Language"), AutoSize = true };

            _nextWindowKey = nextWindow;
            _previousWindowKey = previousWindow;
            _overrideKey = fullscreenOverrideKey;

            const int leftMargin = 12;
            const int rowGap = 12;
            const int labelToControlOffset = 4;
            const int labelToButtonOffset = 6;
            const int bottomMargin = 12;
            var captureLeft = 350;
            var labelRight = new[]
            {
                nextWindowLabel,
                nextWindowModsLabel,
                previousWindowLabel,
                previousWindowModsLabel,
                overrideLabel
            }.Max(label => leftMargin + TextRenderer.MeasureText(label.Text, label.Font).Width);
            var fieldLeft = Math.Max(100, labelRight + 8);
            var fieldWidth = Math.Max(120, captureLeft - fieldLeft - 10);

            _nextWindowCapture.Click += (_, __) => BeginCapture(_nextWindowKeyBox, value => _nextWindowKey = value);
            _previousWindowCapture.Click += (_, __) => BeginCapture(_previousWindowKeyBox, value => _previousWindowKey = value);
            _overrideCapture.Click += (_, __) => BeginCapture(_overrideKeyBox, value => _overrideKey = value);

            _freshSeconds.Width = 80;
            _freshSeconds.Minimum = 1;
            _freshSeconds.Maximum = 60;
            _freshSeconds.Value = Math.Max(1, Math.Min(60, boostWindowAgeSeconds));

            _language.Width = fieldWidth;
            _language.DropDownStyle = ComboBoxStyle.DropDownList;
            _language.Items.AddRange(BuildLanguageOptions().Cast<object>().ToArray());
            var selectedLang = string.IsNullOrWhiteSpace(language) ? "en" : language;
            _language.SelectedItem = FindLanguageOption(selectedLang) ?? _language.Items.Cast<LanguageOption>().FirstOrDefault();

            _nextWindowMods.Width = fieldWidth;
            _previousWindowMods.Width = fieldWidth;
            _nextWindowMods.CheckOnClick = true;
            _previousWindowMods.CheckOnClick = true;
            var modifierOptions = new[]
            {
                new ModifierOption(Localization.Entry("Modifier_AltGr"), SwitcherApp.MOD_ALT | SwitcherApp.MOD_CONTROL),
                new ModifierOption(Localization.Entry("Modifier_Alt"), SwitcherApp.MOD_ALT),
                new ModifierOption(Localization.Entry("Modifier_Ctrl"), SwitcherApp.MOD_CONTROL),
                new ModifierOption(Localization.Entry("Modifier_Shift"), SwitcherApp.MOD_SHIFT),
                new ModifierOption(Localization.Entry("Modifier_Win"), SwitcherApp.MOD_WIN)
            };
            _nextWindowMods.Items.AddRange(modifierOptions.Cast<object>().ToArray());
            _previousWindowMods.Items.AddRange(modifierOptions.Select(option =>
                new ModifierOption(option.Label, option.Modifier)).Cast<object>().ToArray());
            _nextWindowMods.ItemCheck += (_, e) => HandleModifierItemCheck(_nextWindowMods, e);
            _previousWindowMods.ItemCheck += (_, e) => HandleModifierItemCheck(_previousWindowMods, e);
            _nextWindowMods.IntegralHeight = false;
            _previousWindowMods.IntegralHeight = false;
            _nextWindowMods.Height = (_nextWindowMods.ItemHeight * _nextWindowMods.Items.Count) + 4;
            _previousWindowMods.Height = (_previousWindowMods.ItemHeight * _previousWindowMods.Items.Count) + 4;
            ApplyModifiers(_nextWindowMods, nextWindowMods);
            ApplyModifiers(_previousWindowMods, previousWindowMods);

            var y = 16;
            nextWindowLabel.Location = new Point(leftMargin, y);
            ConfigureKeyBox(_nextWindowKeyBox, new Point(fieldLeft, y - labelToControlOffset), fieldWidth, _nextWindowKey);
            ConfigureCaptureButton(_nextWindowCapture, new Point(captureLeft, y - labelToButtonOffset));
            y = (y - labelToControlOffset) + _nextWindowKeyBox.Height + rowGap;

            nextWindowModsLabel.Location = new Point(leftMargin, y);
            _nextWindowMods.Location = new Point(fieldLeft, y - labelToControlOffset);
            y = (y - labelToControlOffset) + _nextWindowMods.Height + rowGap;

            previousWindowLabel.Location = new Point(leftMargin, y);
            ConfigureKeyBox(_previousWindowKeyBox, new Point(fieldLeft, y - labelToControlOffset), fieldWidth, _previousWindowKey);
            ConfigureCaptureButton(_previousWindowCapture, new Point(captureLeft, y - labelToButtonOffset));
            y = (y - labelToControlOffset) + _previousWindowKeyBox.Height + rowGap;

            previousWindowModsLabel.Location = new Point(leftMargin, y);
            _previousWindowMods.Location = new Point(fieldLeft, y - labelToControlOffset);
            y = (y - labelToControlOffset) + _previousWindowMods.Height + rowGap;

            overrideLabel.Location = new Point(leftMargin, y);
            ConfigureKeyBox(_overrideKeyBox, new Point(fieldLeft, y - labelToControlOffset), fieldWidth, _overrideKey);
            ConfigureCaptureButton(_overrideCapture, new Point(captureLeft, y - labelToButtonOffset));
            y = (y - labelToControlOffset) + _overrideKeyBox.Height + rowGap;

            freshLabel.Location = new Point(leftMargin, y);
            _freshSeconds.Location = new Point(260, y - labelToControlOffset);
            y = (y - labelToControlOffset) + _freshSeconds.Height + rowGap;

            languageLabel.Location = new Point(leftMargin, y);
            _language.Location = new Point(fieldLeft, y - labelToControlOffset);
            y = (y - labelToControlOffset) + _language.Height + rowGap;

            const int errorWidth = 416;
            _hotkeyError.Location = new Point(leftMargin, y);
            _hotkeyError.Size = TextRenderer.MeasureText(Localization.Entry("Hotkeys_F12NotAllowed"),
                _hotkeyError.Font, new Size(errorWidth, int.MaxValue), TextFormatFlags.WordBreak);
            _hotkeyError.Width = errorWidth;
            y += _hotkeyError.Height + rowGap;

            var reset = new Button { Text = Localization.Entry("Dialog_Reset"), Location = new Point(leftMargin, y), Width = 110 };
            var ok = new Button { Text = Localization.Entry("Dialog_OK"), DialogResult = DialogResult.OK, Location = new Point(265, y) };
            var cancel = new Button { Text = Localization.Entry("Dialog_Cancel"), DialogResult = DialogResult.Cancel, Location = new Point(345, y) };
            ClientSize = new Size(440, y + reset.Height + bottomMargin);
            reset.Click += (_, __) => ResetKeyBindings();

            AcceptButton = ok;
            CancelButton = cancel;

            Controls.AddRange([
                nextWindowLabel, _nextWindowKeyBox, _nextWindowCapture,
                nextWindowModsLabel, _nextWindowMods,
                previousWindowLabel, _previousWindowKeyBox, _previousWindowCapture,
                previousWindowModsLabel, _previousWindowMods,
                overrideLabel, _overrideKeyBox, _overrideCapture,
                freshLabel, _freshSeconds,
                languageLabel, _language,
                _hotkeyError, reset, ok, cancel
            ]);
        }

        private static void ConfigureKeyBox(TextBox box, Point location, int width, Keys key)
        {
            box.Location = location;
            box.Width = width;
            box.ReadOnly = true;
            box.Text = HotkeyText.FormatKey(key);
            box.TabStop = true;
        }

        private static void ConfigureCaptureButton(Button button, Point location)
        {
            button.Text = Localization.Entry("Dialog_Change");
            button.Location = location;
            button.Size = new Size(75, 23);
        }


        private IEnumerable<LanguageOption> BuildLanguageOptions()
        {
            foreach (var item in Localization.GetAvailableLanguages(AppPaths.LangRoot))
            {
                yield return new LanguageOption(item.Code, item.Name);
            }
        }

        private LanguageOption? FindLanguageOption(string code)
        {
            foreach (LanguageOption option in _language.Items)
            {
                if (string.Equals(option.Code, code, StringComparison.OrdinalIgnoreCase))
                {
                    return option;
                }
            }

            var fallback = new LanguageOption(code, GetLanguageLabel(code));
            _language.Items.Add(fallback);
            return fallback;
        }

        private static string GetLanguageLabel(string code)
        {
            return code;
        }

        private void ResetKeyBindings()
        {
            _nextWindowKey = Keys.OemPeriod;
            _previousWindowKey = Keys.Oemcomma;
            _overrideKey = Keys.ShiftKey;

            _nextWindowKeyBox.Text = HotkeyText.FormatKey(_nextWindowKey);
            _previousWindowKeyBox.Text = HotkeyText.FormatKey(_previousWindowKey);
            _overrideKeyBox.Text = HotkeyText.FormatKey(_overrideKey);

            ApplyModifiers(_nextWindowMods, SwitcherApp.MOD_NONE);
            ApplyModifiers(_previousWindowMods, SwitcherApp.MOD_NONE);

            _freshSeconds.Value = 5;
            _hotkeyError.Text = string.Empty;
        }

        private void BeginCapture(TextBox box, Action<Keys> setter)
        {
            _captureTarget = box;
            _captureSetter = setter;
            box.Text = Localization.Entry("Dialog_PressAKey");
            Focus();
        }

        private void OnDialogKeyDown(object? sender, KeyEventArgs e)
        {
            if (_captureTarget is null || _captureSetter is null)
            {
                return;
            }

            e.SuppressKeyPress = true;
            e.Handled = true;

            if (e.KeyCode == Keys.F12 && _captureTarget != _overrideKeyBox)
            {
                _hotkeyError.Text = Localization.Entry("Hotkeys_F12NotAllowed");
                return;
            }

            _captureSetter(e.KeyCode);
            _captureTarget.Text = HotkeyText.FormatKey(e.KeyCode);
            _captureTarget = null;
            _captureSetter = null;

            _hotkeyError.Text = string.Empty;
        }

        private void OnDialogFormClosing(object? sender, FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.OK &&
                (_nextWindowKey == Keys.F12 || _previousWindowKey == Keys.F12))
            {
                _hotkeyError.Text = Localization.Entry("Hotkeys_F12NotAllowed");
                e.Cancel = true;
            }
        }

        private static uint GetModifiers(CheckedListBox list)
        {
            uint mods = 0;
            foreach (var item in list.CheckedItems)
            {
                if (item is ModifierOption option)
                {
                    mods |= option.Modifier;
                }
            }
            return mods;
        }

        private static void ApplyModifiers(CheckedListBox list, uint mods)
        {
            var remaining = mods;
            for (int index = 0; index < list.Items.Count; ++index)
            {
                if (list.Items[index] is ModifierOption option &&
                    (remaining & option.Modifier) == option.Modifier)
                {
                    list.SetItemChecked(index, true);
                    remaining &= ~option.Modifier;
                }
                else
                {
                    list.SetItemChecked(index, false);
                }
            }
        }

        private void HandleModifierItemCheck(CheckedListBox list, ItemCheckEventArgs e)
        {
            if (_suppressModifierEvents)
            {
                return;
            }

            if (list.Items[e.Index] is not ModifierOption option)
            {
                return;
            }

            var altGrMask = SwitcherApp.MOD_ALT | SwitcherApp.MOD_CONTROL;
            if (option.Modifier == altGrMask && e.NewValue == CheckState.Checked)
            {
                SetModifierChecked(list, SwitcherApp.MOD_ALT, false);
                SetModifierChecked(list, SwitcherApp.MOD_CONTROL, false);
            }
            else if ((option.Modifier == SwitcherApp.MOD_ALT || option.Modifier == SwitcherApp.MOD_CONTROL) &&
                     e.NewValue == CheckState.Checked)
            {
                SetModifierChecked(list, altGrMask, false);
            }
        }

        private void SetModifierChecked(CheckedListBox list, uint modifier, bool check)
        {
            _suppressModifierEvents = true;
            try
            {
                for (int index = 0; index < list.Items.Count; ++index)
                {
                    if (list.Items[index] is ModifierOption option && option.Modifier == modifier)
                    {
                        list.SetItemChecked(index, check);
                    }
                }
            }
            finally
            {
                _suppressModifierEvents = false;
            }
        }
    }

    static class HotkeyText
    {
        public static string FormatHotkey(uint modifiers, int keyCode)
        {
            var modifierText = FormatModifiers(modifiers);
            var keyText = FormatKey((Keys)keyCode);
            if (string.IsNullOrEmpty(modifierText))
            {
                return keyText;
            }
            return $"{modifierText}+{keyText}";
        }

        public static string FormatModifiers(uint modifiers)
        {
            var parts = new List<string>();
            if ((modifiers & SwitcherApp.MOD_CONTROL) != 0)
            {
                parts.Add(Localization.Entry("Modifier_Ctrl"));
            }
            if ((modifiers & SwitcherApp.MOD_ALT) != 0)
            {
                parts.Add(Localization.Entry("Modifier_Alt"));
            }
            if ((modifiers & SwitcherApp.MOD_SHIFT) != 0)
            {
                parts.Add(Localization.Entry("Modifier_Shift"));
            }
            if ((modifiers & SwitcherApp.MOD_WIN) != 0)
            {
                parts.Add(Localization.Entry("Modifier_Win"));
            }

            return string.Join("+", parts);
        }

        public static string FormatKey(Keys key)
        {
            if (key is >= Keys.D0 and <= Keys.D9)
            {
                var digit = (int)key - (int)Keys.D0;
                return Localization.Entry($"Key_Digit{digit}");
            }

            if (key is >= Keys.NumPad0 and <= Keys.NumPad9)
            {
                var digit = (int)key - (int)Keys.NumPad0;
                return Localization.Entry($"Key_Numpad{digit}");
            }

            return key switch
            {
                Keys.Return => Localization.Entry("Key_Enter"),
                Keys.Escape => Localization.Entry("Key_Esc"),
                Keys.Back => Localization.Entry("Key_Backspace"),
                Keys.Tab => Localization.Entry("Key_Tab"),
                Keys.Space => Localization.Entry("Key_Space"),
                Keys.Insert => Localization.Entry("Key_Insert"),
                Keys.Delete => Localization.Entry("Key_Delete"),
                Keys.Home => Localization.Entry("Key_Home"),
                Keys.End => Localization.Entry("Key_End"),
                Keys.PageUp => Localization.Entry("Key_PageUp"),
                Keys.PageDown => Localization.Entry("Key_PageDown"),
                Keys.Left => Localization.Entry("Key_Left"),
                Keys.Right => Localization.Entry("Key_Right"),
                Keys.Up => Localization.Entry("Key_Up"),
                Keys.Down => Localization.Entry("Key_Down"),
                Keys.PrintScreen => Localization.Entry("Key_PrintScreen"),
                Keys.Pause => Localization.Entry("Key_Pause"),
                Keys.CapsLock => Localization.Entry("Key_CapsLock"),
                Keys.NumLock => Localization.Entry("Key_NumLock"),
                Keys.Scroll => Localization.Entry("Key_ScrollLock"),
                Keys.LWin => Localization.Entry("Key_LeftWin"),
                Keys.RWin => Localization.Entry("Key_RightWin"),
                Keys.ShiftKey => Localization.Entry("Key_Shift"),
                Keys.ControlKey => Localization.Entry("Key_Ctrl"),
                Keys.Menu => Localization.Entry("Key_Alt"),
                Keys.LMenu => Localization.Entry("Key_LeftAlt"),
                Keys.RMenu => Localization.Entry("Key_RightAlt"),
                Keys.Apps => Localization.Entry("Key_Menu"),
                Keys.Add => Localization.Entry("Key_NumpadAdd"),
                Keys.Subtract => Localization.Entry("Key_NumpadSubtract"),
                Keys.Multiply => Localization.Entry("Key_NumpadMultiply"),
                Keys.Divide => Localization.Entry("Key_NumpadDivide"),
                Keys.Decimal => Localization.Entry("Key_NumpadDecimal"),
                Keys.Oemcomma => Localization.Entry("Key_Comma"),
                Keys.OemPeriod => Localization.Entry("Key_Period"),
                Keys.OemMinus => Localization.Entry("Key_Minus"),
                Keys.Oemplus => Localization.Entry("Key_Equals"),
                Keys.OemQuestion => Localization.Entry("Key_Slash"),
                Keys.OemSemicolon => Localization.Entry("Key_Semicolon"),
                Keys.OemQuotes => Localization.Entry("Key_Apostrophe"),
                Keys.OemOpenBrackets => Localization.Entry("Key_LeftBracket"),
                Keys.OemCloseBrackets => Localization.Entry("Key_RightBracket"),
                Keys.OemPipe or Keys.OemBackslash or Keys.Oem102 => Localization.Entry("Key_Backslash"),
                Keys.Oemtilde => Localization.Entry("Key_Grave"),
                _ => key.ToString()
            };
        }
    }

    class SwitcherApp : ApplicationContext
    {
        static class NativeHotkeys
        {
            [DllImport("user32.dll")]
            public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, int vk);

            [DllImport("user32.dll")]
            public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        }

        static class NativeWindows
        {
            public const uint MONITOR_DEFAULTTONEAREST = 2;

            public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

            [DllImport("user32.dll")]
            public static extern IntPtr GetForegroundWindow();

            [DllImport("user32.dll")]
            public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

            [DllImport("user32.dll")]
            public static extern IntPtr GetWindow(IntPtr hWnd, uint cmd);

            [DllImport("user32.dll")]
            public static extern bool IsWindowVisible(IntPtr hWnd);

            [DllImport("user32.dll")]
            public static extern bool IsIconic(IntPtr hWnd);

            [DllImport("user32.dll")]
            public static extern bool SetForegroundWindow(IntPtr hWnd);

            [DllImport("user32.dll")]
            public static extern IntPtr MonitorFromWindow(IntPtr hWnd, uint flags);

            [DllImport("user32.dll")]
            public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

            [DllImport("user32.dll")]
            public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);

            [DllImport("user32.dll")]
            public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO mi);

            [DllImport("user32.dll")]
            public static extern short GetAsyncKeyState(int vKey);

            [DllImport("dwmapi.dll")]
            public static extern int DwmGetWindowAttribute(IntPtr hWnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

            [DllImport("user32.dll")]
            public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

            public struct RECT
            {
                public int Left;
                public int Top;
                public int Right;
                public int Bottom;
            }

            public struct MONITORINFO
            {
                public uint cbSize;
                public RECT rcMonitor;
                public RECT rcWork;
                public uint dwFlags;
            }
        }

        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        const int GWL_STYLE = -16;
        const int GWL_EXSTYLE = -20;
        const int WS_EX_TOOLWINDOW = 0x00000080;
        const int WS_EX_APPWINDOW = 0x00040000;
        const int WS_CAPTION = 0x00C00000;
        const int WS_THICKFRAME = 0x00040000;
        const int WS_MAXIMIZE = 0x01000000;
        const int WS_CHILD = 0x40000000;
        const int SW_RESTORE = 9;

        const int HOTKEY_NEXT_WINDOW = 1;
        const int HOTKEY_PREVIOUS_WINDOW = 2;
        const int HOTKEY_NEXT_WINDOW_OVERRIDE = 3;
        const int HOTKEY_PREVIOUS_WINDOW_OVERRIDE = 4;

        // Modifiers
        public const uint MOD_NONE = 0x0000;
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;

        const uint GW_OWNER = 4;

        private readonly NotifyIcon _tray;
        private readonly MessageWindow _msgWindow;
#if DEBUG
        private DebugOverlay? _overlay;
        private ToolStripMenuItem? _toggleDebugOverlay;
#endif
        private ToolStripMenuItem? _togglePreferPerMonitor;
        private ToolStripMenuItem? _toggleSkipFullscreen;
        private ToolStripMenuItem? _toggleSuspendWhenFullscreen;
        private ToolStripMenuItem? _toggleBoostNewWindows;
        private ToolStripMenuItem? _toggleTaskbarWindowsOnly;
        private ToolStripMenuItem? _togglePreferMainWindowPerProcess;
        private ToolStripMenuItem? _toggleIncludeMinimized;

        // Boost target process
        private static uint _boostTargetPid = 0;

        // First-seen timestamps
        private static readonly Dictionary<IntPtr, DateTime> _windowSeen = [];
        private static readonly DateTime _seededAt = DateTime.MinValue;

        private static readonly Dictionary<IntPtr, bool> _lastFullscreen = [];

        private static readonly TimeSpan _cycleWindow = TimeSpan.FromMilliseconds(1500);
        private List<IntPtr>? _cycleWindows;
        private DateTime _cycleExpiresAt = DateTime.MinValue;

        // Configurable freshness window
        private TimeSpan _boostWindowAge => TimeSpan.FromSeconds(ConfigService.CurrentConfig.BoostWindowAgeSeconds);

        public SwitcherApp()
        {
            _msgWindow = new MessageWindow(this);
            _tray = BuildTrayIcon();

            AppPaths.Ensure();
            ConfigService.Load(AppPaths.ConfigFile);
            var resolvedLanguage = Localization.ResolveLanguage(AppPaths.LangRoot, ConfigService.CurrentConfig.Language);
            if (!string.Equals(resolvedLanguage, ConfigService.CurrentConfig.Language, StringComparison.OrdinalIgnoreCase))
            {
                ConfigService.CurrentConfig.Language = resolvedLanguage;
                ConfigService.Save(AppPaths.ConfigFile);
            }
            Localization.Initialize(AppPaths.LangRoot, resolvedLanguage);
            RebuildTrayMenu();
            SeedWindowSeen();

#if DEBUG
            if (ConfigService.CurrentConfig.DebugOverlay)
            {
                _overlay = new DebugOverlay();
                _overlay.Show();
            }
#endif

            if (!RegisterHotkeys())
            {
                ShowTrayWarning(Localization.Entry("Hotkeys_Registration_Failed_All"));
            }
            else
            {
                ShowTrayInfo(Localization.Entry("Hotkeys_Registration_Success"));
            }
        }

        public void OnHotkey(int id)
        {
            switch (id)
            {
                case HOTKEY_NEXT_WINDOW:
                case HOTKEY_NEXT_WINDOW_OVERRIDE:
                    CycleNextWindow();
                    break;

                case HOTKEY_PREVIOUS_WINDOW:
                case HOTKEY_PREVIOUS_WINDOW_OVERRIDE:
                    CyclePreviousWindow();
                    break;
            }
        }

        protected override void ExitThreadCore()
        {
            UnregisterHotkeys();

            _tray.Visible = false;
            _tray.Dispose();
            _msgWindow.DestroyHandle();

            base.ExitThreadCore();
        }

        private bool RegisterHotkeys()
        {
            uint nextWindowMods = ConfigService.CurrentConfig.NextWindowModifiers;
            uint previousWindowMods = ConfigService.CurrentConfig.PreviousWindowModifiers;
            bool nextWindow = ConfigService.CurrentConfig.NextWindowKey != (int)Keys.F12 &&
                NativeHotkeys.RegisterHotKey(_msgWindow.Handle, HOTKEY_NEXT_WINDOW, nextWindowMods, ConfigService.CurrentConfig.NextWindowKey);
            bool previousWindow = ConfigService.CurrentConfig.PreviousWindowKey != (int)Keys.F12 &&
                NativeHotkeys.RegisterHotKey(_msgWindow.Handle, HOTKEY_PREVIOUS_WINDOW, previousWindowMods, ConfigService.CurrentConfig.PreviousWindowKey);
            RegisterOverrideHotkeys(nextWindowMods, previousWindowMods);
            if (!nextWindow || !previousWindow)
            {
                var missing = new List<string>();
                if (!nextWindow)
                {
                    missing.Add(Localization.Entry("Dialog_NextWindow"));
                }
                if (!previousWindow)
                {
                    missing.Add(Localization.Entry("Dialog_PreviousWindow"));
                }
                ShowTrayWarning(Localization.Entry("Hotkeys_Registration_Failed", string.Join(", ", missing)));
            }
            return nextWindow && previousWindow;
        }

        private void RegisterOverrideHotkeys(uint nextWindowMods, uint previousWindowMods)
        {
            uint overrideModifier = GetOverrideModifier();
            if (overrideModifier == MOD_NONE)
            {
                return;
            }

            uint nextWindowOverrideMods = nextWindowMods | overrideModifier;
            if (nextWindowOverrideMods != nextWindowMods && ConfigService.CurrentConfig.NextWindowKey != (int)Keys.F12)
            {
                NativeHotkeys.RegisterHotKey(_msgWindow.Handle, HOTKEY_NEXT_WINDOW_OVERRIDE, nextWindowOverrideMods, ConfigService.CurrentConfig.NextWindowKey);
            }

            uint previousWindowOverrideMods = previousWindowMods | overrideModifier;
            if (previousWindowOverrideMods != previousWindowMods && ConfigService.CurrentConfig.PreviousWindowKey != (int)Keys.F12)
            {
                NativeHotkeys.RegisterHotKey(_msgWindow.Handle, HOTKEY_PREVIOUS_WINDOW_OVERRIDE, previousWindowOverrideMods, ConfigService.CurrentConfig.PreviousWindowKey);
            }
        }

        private void UnregisterHotkeys()
        {
            NativeHotkeys.UnregisterHotKey(_msgWindow.Handle, HOTKEY_NEXT_WINDOW);
            NativeHotkeys.UnregisterHotKey(_msgWindow.Handle, HOTKEY_PREVIOUS_WINDOW);
            NativeHotkeys.UnregisterHotKey(_msgWindow.Handle, HOTKEY_NEXT_WINDOW_OVERRIDE);
            NativeHotkeys.UnregisterHotKey(_msgWindow.Handle, HOTKEY_PREVIOUS_WINDOW_OVERRIDE);
        }

        private void ShowTrayInfo(string message)
        {
            _tray.BalloonTipTitle = Localization.Entry("App_Title");
            _tray.BalloonTipText = message;
            _tray.ShowBalloonTip(1500);
        }

        private void ShowTrayWarning(string message)
        {
            _tray.BalloonTipTitle = Localization.Entry("App_Warning_Title");
            _tray.BalloonTipText = message;
            _tray.ShowBalloonTip(3000);
        }

        private void DebugNote(string rule, IntPtr hWnd)
        {
#if DEBUG
            if (_overlay is null)
            {
                return;
            }

            NativeWindows.GetWindowThreadProcessId(hWnd, out uint pid);
            var monitor = NativeWindows.MonitorFromWindow(hWnd, NativeWindows.MONITOR_DEFAULTTONEAREST);
            var processName = GetProcessName(pid);

            _overlay.UpdateText(
                $"{Localization.Entry("Overlay_Rule")}: {rule}\n" +
                $"{Localization.Entry("Overlay_HWND")}: 0x{hWnd.ToInt64():X}\n" +
                $"{Localization.Entry("Overlay_PID")}: {pid}\n" +
                $"{Localization.Entry("Overlay_Process")}: {processName}\n" +
                $"{Localization.Entry("Overlay_Monitor")}: {monitor}\n" +
                $"{Localization.Entry("Overlay_Time")}: {DateTime.Now:T}"
            );
#else
            _ = rule;
            _ = hWnd;
#endif
        }

        private NotifyIcon BuildTrayIcon()
        {
            return new NotifyIcon
            {
                Text = BuildTrayTooltip(),
                Icon = LoadEmbeddedIconOrDefault("havWndSwitcher.ico"),
                Visible = true,
                ContextMenuStrip = BuildTrayMenu()
            };
        }

        private ContextMenuStrip BuildTrayMenu()
        {
            var menu = new ContextMenuStrip();

            menu.Items.Add(Localization.Entry("Menu_About"), null, (_, __) =>
            {
                ShowAboutDialog();
            });

            menu.Items.Add(Localization.Entry("Menu_Settings"), null, (_, __) =>
            {
                ShowSettingsDialog();
            });

            menu.Items.Add(new ToolStripSeparator());

#if DEBUG
            _toggleDebugOverlay = AddToggleMenuItem(menu, Localization.Entry("Menu_DebugOverlay"), ConfigService.CurrentConfig.DebugOverlay, enabled =>
            {
                ConfigService.CurrentConfig.DebugOverlay = enabled;
                ConfigService.Save(AppPaths.ConfigFile);
                SetDebugOverlayEnabled(enabled);
            });
#endif

            _togglePreferPerMonitor = AddToggleMenuItem(menu, Localization.Entry("Menu_PreferPerMonitor"), ConfigService.CurrentConfig.PreferPerMonitor, enabled =>
            {
                ConfigService.CurrentConfig.PreferPerMonitor = enabled;
                ConfigService.Save(AppPaths.ConfigFile);
            });

            _toggleSkipFullscreen = AddToggleMenuItem(menu, Localization.Entry("Menu_SkipFullscreen"), ConfigService.CurrentConfig.SkipFullscreen, enabled =>
            {
                ConfigService.CurrentConfig.SkipFullscreen = enabled;
                ConfigService.Save(AppPaths.ConfigFile);
            });

            _toggleSuspendWhenFullscreen = AddToggleMenuItem(menu, Localization.Entry("Menu_SuspendWhenFullscreen"), ConfigService.CurrentConfig.SuspendWhenFullscreen, enabled =>
            {
                ConfigService.CurrentConfig.SuspendWhenFullscreen = enabled;
                ConfigService.Save(AppPaths.ConfigFile);
            });

            _toggleBoostNewWindows = AddToggleMenuItem(menu, Localization.Entry("Menu_BoostNew"), ConfigService.CurrentConfig.BoostNewWindows, enabled =>
            {
                ConfigService.CurrentConfig.BoostNewWindows = enabled;
                ConfigService.Save(AppPaths.ConfigFile);
            });

            _toggleTaskbarWindowsOnly = AddToggleMenuItem(menu, Localization.Entry("Menu_TaskbarOnly"), ConfigService.CurrentConfig.TaskbarWindowsOnly, enabled =>
            {
                ConfigService.CurrentConfig.TaskbarWindowsOnly = enabled;
                ConfigService.Save(AppPaths.ConfigFile);
            });

            _togglePreferMainWindowPerProcess = AddToggleMenuItem(menu, Localization.Entry("Menu_PreferMainWindow"), ConfigService.CurrentConfig.PreferMainWindowPerProcess, enabled =>
            {
                ConfigService.CurrentConfig.PreferMainWindowPerProcess = enabled;
                ConfigService.Save(AppPaths.ConfigFile);
            });

            _toggleIncludeMinimized = AddToggleMenuItem(menu, Localization.Entry("Menu_IncludeMinimized"), ConfigService.CurrentConfig.IncludeMinimizedWindows, enabled =>
            {
                ConfigService.CurrentConfig.IncludeMinimizedWindows = enabled;
                ConfigService.Save(AppPaths.ConfigFile);
            });

            menu.Items.Add(new ToolStripSeparator());

            menu.Items.Add(Localization.Entry("Menu_ResetDefaults"), null, (_, __) =>
            {
                ResetConfigToDefaults();
            });

            menu.Items.Add(Localization.Entry("Menu_RestartHotkeys"), null, (_, __) =>
            {
                UnregisterHotkeys();

                if (!RegisterHotkeys())
                {
                    ShowTrayWarning(Localization.Entry("Hotkeys_Restart_Failed"));
                }
                else
                {
                    ShowTrayInfo(Localization.Entry("Hotkeys_Restarted"));
                }
            });

            menu.Items.Add(Localization.Entry("Menu_OpenConfigFolder"), null, (_, __) =>
            {
                System.Diagnostics.Process.Start("explorer.exe", AppPaths.ConfigRoot);
            });

            menu.Items.Add(Localization.Entry("Menu_ReloadConfig"), null, (_, __) =>
            {
                ConfigService.Load(AppPaths.ConfigFile);
                var resolvedLanguage = Localization.ResolveLanguage(AppPaths.LangRoot, ConfigService.CurrentConfig.Language);
                if (!string.Equals(resolvedLanguage, ConfigService.CurrentConfig.Language, StringComparison.OrdinalIgnoreCase))
                {
                    ConfigService.CurrentConfig.Language = resolvedLanguage;
                    ConfigService.Save(AppPaths.ConfigFile);
                }
                Localization.Initialize(AppPaths.LangRoot, resolvedLanguage);
                RebuildTrayMenu();
            });

#if DEBUG
            menu.Items.Add(Localization.Entry("Menu_ExportDiagnostics"), null, (_, __) =>
            {
                ExportDiagnostics();
            });
#endif

            menu.Items.Add(new ToolStripSeparator());

            menu.Items.Add(Localization.Entry("Menu_Exit"), null, (_, __) =>
            {
                ExitThread();
            });

            return menu;
        }

        private static ToolStripMenuItem AddToggleMenuItem(ContextMenuStrip menu, string label, bool initialState, Action<bool> onToggle)
        {
            var item = new ToolStripMenuItem(label)
            {
                CheckOnClick = true,
                Checked = initialState
            };

            item.CheckedChanged += (_, __) => onToggle(item.Checked);
            menu.Items.Add(item);
            return item;
        }

        private void ShowAboutDialog()
        {
            var assemblyName = Assembly.GetExecutingAssembly().GetName();
            var version = assemblyName.Version?.ToString() ?? Localization.Entry("Value_Unknown");
            MessageBox.Show(
                $"{Localization.Entry("App_Title")}\n{Localization.Entry("About_Version")} {version}\nCopyright © 2025-2026 René Nicolaus\n\n{Localization.Entry("About_Body")}",
                Localization.Entry("Menu_About"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private void SetDebugOverlayEnabled(bool enabled)
        {
#if DEBUG
            if (enabled)
            {
                if (_overlay is null)
                {
                    _overlay = new DebugOverlay();
                }

                if (!_overlay.Visible)
                {
                    _overlay.Show();
                }
            }
            else
            {
                if (_overlay is not null)
                {
                    _overlay.Close();
                    _overlay.Dispose();
                    _overlay = null;
                }
            }
#else
            _ = enabled;
#endif
        }

        private void ResetConfigToDefaults()
        {
            ConfigService.ResetToDefaults();
            ConfigService.Save(AppPaths.ConfigFile);
            Localization.Initialize(AppPaths.LangRoot, ConfigService.CurrentConfig.Language);

#if DEBUG
            SetDebugOverlayEnabled(ConfigService.CurrentConfig.DebugOverlay);
#endif

            _cycleWindows = null;
            _cycleExpiresAt = DateTime.MinValue;
            UpdateToggleMenuChecks();

            UnregisterHotkeys();
            if (!RegisterHotkeys())
            {
                ShowTrayWarning(Localization.Entry("Hotkeys_Update_Failed"));
            }
            else
            {
                ShowTrayInfo(Localization.Entry("Settings_Reset"));
            }

            RebuildTrayMenu();
        }

        private void UpdateToggleMenuChecks()
        {
#if DEBUG
            if (_toggleDebugOverlay is not null)
            {
                _toggleDebugOverlay.Checked = ConfigService.CurrentConfig.DebugOverlay;
            }
#endif
            if (_togglePreferPerMonitor is not null)
            {
                _togglePreferPerMonitor.Checked = ConfigService.CurrentConfig.PreferPerMonitor;
            }
            if (_toggleSkipFullscreen is not null)
            {
                _toggleSkipFullscreen.Checked = ConfigService.CurrentConfig.SkipFullscreen;
            }
            if (_toggleSuspendWhenFullscreen is not null)
            {
                _toggleSuspendWhenFullscreen.Checked = ConfigService.CurrentConfig.SuspendWhenFullscreen;
            }
            if (_toggleBoostNewWindows is not null)
            {
                _toggleBoostNewWindows.Checked = ConfigService.CurrentConfig.BoostNewWindows;
            }
            if (_toggleTaskbarWindowsOnly is not null)
            {
                _toggleTaskbarWindowsOnly.Checked = ConfigService.CurrentConfig.TaskbarWindowsOnly;
            }
            if (_togglePreferMainWindowPerProcess is not null)
            {
                _togglePreferMainWindowPerProcess.Checked = ConfigService.CurrentConfig.PreferMainWindowPerProcess;
            }
            if (_toggleIncludeMinimized is not null)
            {
                _toggleIncludeMinimized.Checked = ConfigService.CurrentConfig.IncludeMinimizedWindows;
            }
        }

        private void RebuildTrayMenu()
        {
            if (_tray is null)
            {
                return;
            }
            var oldMenu = _tray.ContextMenuStrip;
            _tray.ContextMenuStrip = BuildTrayMenu();
            _tray.Text = BuildTrayTooltip();
            oldMenu?.Dispose();
        }

        private string BuildTrayTooltip()
        {
            var nextHotkey = HotkeyText.FormatHotkey(ConfigService.CurrentConfig.NextWindowModifiers, ConfigService.CurrentConfig.NextWindowKey);
            var previousHotkey = HotkeyText.FormatHotkey(ConfigService.CurrentConfig.PreviousWindowModifiers, ConfigService.CurrentConfig.PreviousWindowKey);
            var nextLine = $"{Localization.Entry("Tray_Next")}: {nextHotkey}";
            var previousLine = $"{Localization.Entry("Tray_Previous")}: {previousHotkey}";
            return FitNotifyIconText(Localization.Entry("App_Title"), nextLine, previousLine);
        }

        private static string FitNotifyIconText(string title, string nextLine, string previousLine)
        {
            const int maxLength = 63;
            var full = $"{title}\r\n{nextLine}\r\n{previousLine}";
            if (full.Length <= maxLength)
            {
                return full;
            }

            var withoutTitle = $"{nextLine}\r\n{previousLine}";
            if (withoutTitle.Length <= maxLength)
            {
                return withoutTitle;
            }

            var keysOnly = $"{StripTrayLabel(nextLine)}\r\n{StripTrayLabel(previousLine)}";
            if (keysOnly.Length <= maxLength)
            {
                return keysOnly;
            }

            return keysOnly[..maxLength];
        }

        private static string StripTrayLabel(string line)
        {
            const string separator = ": ";
            var index = line.IndexOf(separator, StringComparison.Ordinal);
            if (index < 0)
            {
                return line;
            }
            return line[(index + separator.Length)..];
        }

        private void ShowSettingsDialog()
        {
            UnregisterHotkeys();
            ShowTrayInfo(Localization.Entry("Hotkeys_Temporarily_Disabled"));
            using var dialog = new SettingsDialog(
                (Keys)ConfigService.CurrentConfig.NextWindowKey,
                (Keys)ConfigService.CurrentConfig.PreviousWindowKey,
                ConfigService.CurrentConfig.NextWindowModifiers,
                ConfigService.CurrentConfig.PreviousWindowModifiers,
                (Keys)ConfigService.CurrentConfig.FullscreenOverrideKey,
                ConfigService.CurrentConfig.BoostWindowAgeSeconds,
                ConfigService.CurrentConfig.Language);

            if (dialog.ShowDialog() != DialogResult.OK)
            {
                if (!RegisterHotkeys())
                {
                    ShowTrayWarning(Localization.Entry("Hotkeys_Update_Failed"));
                }
                return;
            }

            if (dialog.NextWindowKey == dialog.PreviousWindowKey &&
                dialog.NextWindowModifiers == dialog.PreviousWindowModifiers)
            {
                ShowTrayWarning(Localization.Entry("Hotkeys_Duplicate"));
                if (!RegisterHotkeys())
                {
                    ShowTrayWarning(Localization.Entry("Hotkeys_Update_Failed"));
                }
                return;
            }

            ConfigService.CurrentConfig.NextWindowKey = (int)dialog.NextWindowKey;
            ConfigService.CurrentConfig.PreviousWindowKey = (int)dialog.PreviousWindowKey;
            ConfigService.CurrentConfig.NextWindowModifiers = dialog.NextWindowModifiers;
            ConfigService.CurrentConfig.PreviousWindowModifiers = dialog.PreviousWindowModifiers;
            ConfigService.CurrentConfig.FullscreenOverrideKey = (int)dialog.FullscreenOverrideKey;
            ConfigService.CurrentConfig.BoostWindowAgeSeconds = dialog.BoostWindowAgeSeconds;
            ConfigService.CurrentConfig.Language = dialog.Language;
            ConfigService.Save(AppPaths.ConfigFile);
            Localization.Initialize(AppPaths.LangRoot, ConfigService.CurrentConfig.Language);

            UnregisterHotkeys();
            if (!RegisterHotkeys())
            {
                ShowTrayWarning(Localization.Entry("Hotkeys_Update_Failed"));
            }
            else
            {
                ShowTrayInfo(Localization.Entry("Hotkeys_Updated"));
            }

            RebuildTrayMenu();
        }

        private static Icon LoadEmbeddedIconOrDefault(string fileName)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = Array.Find(assembly.GetManifestResourceNames(), x => x.EndsWith($".{fileName}", StringComparison.OrdinalIgnoreCase));
                if (resourceName is null)
                {
                    return SystemIcons.Application;
                }
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream is null)
                {
                    return SystemIcons.Application;
                }
                return new Icon(stream);
            }
            catch
            {
                return SystemIcons.Application;
            }
        }

        static string DiagnosticsFile => Path.Combine(AppPaths.ConfigRoot, $"diag_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

        private void ExportDiagnostics()
        {
            var lines = new List<string>
            {
              Localization.Entry("Diagnostics_Title"),
              $"{Localization.Entry("Diagnostics_Time")}: {DateTime.Now}",
              ""
            };

            foreach (var w in EnumerateWindowsNextWindow())
            {
                NativeWindows.GetWindowThreadProcessId(w, out uint pid);
                var monitor = NativeWindows.MonitorFromWindow(w, NativeWindows.MONITOR_DEFAULTTONEAREST);

                bool valid = IsValidWindow(w);
                var processName = GetProcessName(pid);

                lines.Add(
                    $"HWND=0x{w.ToInt64():X}  PID={pid}  Monitor={monitor}  " +
                    $"Process={processName}  Valid={valid}"
                );
            }

            File.WriteAllLines(DiagnosticsFile, lines);

            ShowTrayInfo(Localization.Entry("Diagnostics_Exported"));
        }

        private void SetForeground(IntPtr hWnd)
        {
            if (ConfigService.CurrentConfig.IncludeMinimizedWindows && NativeWindows.IsIconic(hWnd))
            {
                NativeWindows.ShowWindow(hWnd, SW_RESTORE);
            }
            NativeWindows.SetForegroundWindow(hWnd);

            // Boost target process
            NativeWindows.GetWindowThreadProcessId(hWnd, out uint pid);
            _boostTargetPid = pid;
        }

        private IEnumerable<IntPtr> EnumerateWindowsNextWindow()
        {
            return EnumerateWindowsFromForeground(nextWindow: true);
        }

        private IEnumerable<IntPtr> EnumerateWindowsPreviousWindow()
        {
            return EnumerateWindowsFromForeground(nextWindow: false);
        }

        private IEnumerable<IntPtr> EnumerateWindowsFromForeground(bool nextWindow)
        {
            var start = NativeWindows.GetForegroundWindow();
            if (start == IntPtr.Zero)
            {
                yield break;
            }

            var windows = GetCycleWindows(start);
            if (windows.Count <= 1)
            {
                yield break;
            }

            int startIndex = windows.IndexOf(start);
            if (startIndex < 0)
            {
                yield break;
            }

            for (int offset = 1; offset < windows.Count; ++offset)
            {
                int index = nextWindow
                    ? (startIndex + offset) % windows.Count
                    : (startIndex - offset + windows.Count) % windows.Count;

                var window = windows[index];
                TrackWindowSeen(window);
                yield return window;
            }
        }

        private List<IntPtr> GetCycleWindows(IntPtr start)
        {
            var now = DateTime.UtcNow;
            if (_cycleWindows is null || now > _cycleExpiresAt || !_cycleWindows.Contains(start))
            {
                _cycleWindows = EnumerateWindowsZOrder();
            }

            _cycleExpiresAt = now + _cycleWindow;
            if (ConfigService.CurrentConfig.PreferMainWindowPerProcess)
            {
                return FilterToMainWindows(_cycleWindows, start);
            }

            return _cycleWindows;
        }

        private List<IntPtr> FilterToMainWindows(List<IntPtr> windows, IntPtr start)
        {
            var candidates = new List<IntPtr>();
            var candidatesSet = new HashSet<IntPtr>();

            foreach (var window in windows)
            {
                if (window == start || IsValidWindow(window))
                {
                    candidates.Add(window);
                    candidatesSet.Add(window);
                }
            }

            NativeWindows.GetWindowThreadProcessId(start, out uint startPid);

            var mainByPid = new Dictionary<uint, IntPtr>();
            foreach (var window in candidates)
            {
                NativeWindows.GetWindowThreadProcessId(window, out uint pid);
                if (mainByPid.ContainsKey(pid))
                {
                    continue;
                }

                IntPtr main;
                try
                {
                    main = System.Diagnostics.Process.GetProcessById((int)pid).MainWindowHandle;
                }
                catch
                {
                    main = IntPtr.Zero;
                }

                if (main != IntPtr.Zero && candidatesSet.Contains(main))
                {
                    mainByPid[pid] = main;
                }
                else
                {
                    mainByPid[pid] = IntPtr.Zero;
                }
            }

            var result = new List<IntPtr>();
            var seen = new HashSet<uint>();

            foreach (var window in candidates)
            {
                if (window == start)
                {
                    result.Add(window);
                    continue;
                }

                NativeWindows.GetWindowThreadProcessId(window, out uint pid);

                if (pid == startPid)
                {
                    if (mainByPid.TryGetValue(pid, out var main) && main != IntPtr.Zero && window == main)
                    {
                        result.Add(window);
                    }
                    continue;
                }

                if (seen.Contains(pid))
                {
                    continue;
                }

                if (mainByPid.TryGetValue(pid, out var mainWindow) && mainWindow != IntPtr.Zero)
                {
                    if (window == mainWindow)
                    {
                        result.Add(window);
                        seen.Add(pid);
                    }
                    continue;
                }

                result.Add(window);
                seen.Add(pid);
            }

            return result;
        }

        private static List<IntPtr> EnumerateWindowsZOrder()
        {
            var windows = new List<IntPtr>();
            NativeWindows.EnumWindows((hWnd, _) =>
            {
                windows.Add(hWnd);
                return true;
            }, IntPtr.Zero);

            return windows;
        }

        private static void TrackWindowSeen(IntPtr hWnd)
        {
            if (!_windowSeen.ContainsKey(hWnd))
            {
                _windowSeen[hWnd] = DateTime.UtcNow;
            }
        }

        private static bool IsTaskbarWindow(IntPtr hWnd)
        {
            int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);

            // Skip tool windows
            if ((exStyle & WS_EX_TOOLWINDOW) != 0)
            {
                return false;
            }

            int style = GetWindowLong(hWnd, GWL_STYLE);

            // Skip child windows
            if ((style & WS_CHILD) != 0)
            {
                return false;
            }

            // If it declares itself as an app window -> good
            if ((exStyle & WS_EX_APPWINDOW) != 0)
            {
                return true;
            }

            // Exclude owned windows (popups / dialogs often have owners)
            if (NativeWindows.GetWindow(hWnd, GW_OWNER) != IntPtr.Zero)
            {
                return false;
            }

            return true;
        }

        private bool IsValidWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
            {
                return false;
            }

#if DEBUG
            if (_overlay is not null && _overlay.IsHandleCreated && hWnd == _overlay.Handle)
            {
                return false;
            }
#endif

            if (!NativeWindows.IsWindowVisible(hWnd))
            {
                return false;
            }

            if (NativeWindows.IsIconic(hWnd))
            {
                return ConfigService.CurrentConfig.IncludeMinimizedWindows;
            }

            if (ConfigService.CurrentConfig.TaskbarWindowsOnly &&
                !IsTaskbarWindow(hWnd))
            {
                return false;
            }

            return true;
        }

        private static string GetProcessName(uint pid)
        {
            if (pid == 0)
            {
                return Localization.Entry("Value_Unknown");
            }

            try
            {
                return System.Diagnostics.Process.GetProcessById((int)pid).ProcessName;
            }
            catch
            {
                return Localization.Entry("Value_Unknown");
            }
        }

        private static bool IsFullscreenInternal(IntPtr hWnd, IntPtr monitor)
        {
            var rect = new NativeWindows.RECT();
            const int dwmExtendedFrameBounds = 9;
            int dwmResult = NativeWindows.DwmGetWindowAttribute(
                hWnd,
                dwmExtendedFrameBounds,
                out rect,
                Marshal.SizeOf<NativeWindows.RECT>());
            if (dwmResult != 0 && !NativeWindows.GetWindowRect(hWnd, out rect))
            {
                return false;
            }

            var monitorInfo = new NativeWindows.MONITORINFO();
            monitorInfo.cbSize = (uint)Marshal.SizeOf(monitorInfo);

            if (!NativeWindows.GetMonitorInfo(monitor, ref monitorInfo))
            {
                return false;
            }

            if (IsBorderlessMaximized(hWnd))
            {
                return true;
            }

            return
                rect.Left <= monitorInfo.rcMonitor.Left &&
                rect.Top <= monitorInfo.rcMonitor.Top &&
                rect.Right >= monitorInfo.rcMonitor.Right &&
                rect.Bottom >= monitorInfo.rcMonitor.Bottom;
        }

        private static bool IsBorderlessMaximized(IntPtr hWnd)
        {
            int style = GetWindowLong(hWnd, GWL_STYLE);
            bool maximized = (style & WS_MAXIMIZE) != 0;
            bool hasFrame = (style & (WS_CAPTION | WS_THICKFRAME)) != 0;
            return maximized && !hasFrame;
        }

        private static bool IsFullscreenCached(IntPtr hWnd, IntPtr monitor)
        {
            if (!NativeWindows.IsIconic(hWnd))
            {
                bool isFullscreen = IsFullscreenInternal(hWnd, monitor);
                _lastFullscreen[hWnd] = isFullscreen;
                return isFullscreen;
            }

            return _lastFullscreen.TryGetValue(hWnd, out bool cached) && cached;
        }

        private static bool IsOverrideKeyDown()
        {
            int virtualKey = ConfigService.CurrentConfig.FullscreenOverrideKey;
            return (NativeWindows.GetAsyncKeyState(virtualKey) & 0x8000) != 0;
        }

        private static bool ShouldSkipFullscreen()
        {
            return ConfigService.CurrentConfig.SkipFullscreen && !IsOverrideKeyDown();
        }

        private static uint GetOverrideModifier()
        {
            return (Keys)ConfigService.CurrentConfig.FullscreenOverrideKey switch
            {
                Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey => MOD_SHIFT,
                Keys.ControlKey or Keys.LControlKey or Keys.RControlKey => MOD_CONTROL,
                Keys.Menu or Keys.LMenu or Keys.RMenu => MOD_ALT,
                Keys.LWin or Keys.RWin => MOD_WIN,
                _ => MOD_NONE
            };
        }

        private bool IsFreshBoostWindow(IntPtr hWnd)
        {
            NativeWindows.GetWindowThreadProcessId(hWnd, out uint pid);
            if (pid != _boostTargetPid)
            {
                return false;
            }

            if (!_windowSeen.TryGetValue(hWnd, out var firstSeen))
            {
                return false;
            }

            if (firstSeen == _seededAt)
            {
                return false;
            }

            if (DateTime.UtcNow - firstSeen > _boostWindowAge)
            {
                return false;
            }

            if (!IsValidWindow(hWnd))
            {
                return false;
            }

            return true;
        }

        private static void SeedWindowSeen()
        {
            foreach (var window in EnumerateWindowsZOrder())
            {
                if (!_windowSeen.ContainsKey(window))
                {
                    _windowSeen[window] = _seededAt;
                }
            }
        }

        private bool IsBoostTargetWindow(IntPtr hWnd)
        {
            if (!IsValidWindow(hWnd))
            {
                return false;
            }

            NativeWindows.GetWindowThreadProcessId(hWnd, out uint pid);
            return pid == _boostTargetPid;
        }

        private bool TryFindFreshBoostWindow(IEnumerable<IntPtr> windows, out IntPtr target)
        {
            foreach (var window in windows)
            {
                if (IsFreshBoostWindow(window))
                {
                    target = window;
                    return true;
                }
            }

            target = IntPtr.Zero;
            return false;
        }

        private bool TryFindBoostTargetWindow(IEnumerable<IntPtr> windows, out IntPtr target)
        {
            foreach (var window in windows)
            {
                if (IsBoostTargetWindow(window))
                {
                    target = window;
                    return true;
                }
            }

            target = IntPtr.Zero;
            return false;
        }

        private bool TryFindNextOnSameMonitor(IntPtr currentMonitor, out IntPtr target)
        {
            foreach (var window in EnumerateWindowsNextWindow())
            {
                if (!IsValidWindow(window))
                {
                    continue;
                }

                var monitor = NativeWindows.MonitorFromWindow(window, NativeWindows.MONITOR_DEFAULTTONEAREST);
                if (monitor != currentMonitor)
                {
                    continue;
                }

                if (ShouldSkipFullscreen() && IsFullscreenCached(window, monitor))
                {
                    continue;
                }

                target = window;
                return true;
            }

            target = IntPtr.Zero;
            return false;
        }

        private bool TryFindPrevOnSameMonitor(IntPtr currentMonitor, out IntPtr target)
        {
            foreach (var window in EnumerateWindowsPreviousWindow())
            {
                if (!IsValidWindow(window))
                {
                    continue;
                }

                var monitor = NativeWindows.MonitorFromWindow(window, NativeWindows.MONITOR_DEFAULTTONEAREST);
                if (monitor != currentMonitor)
                {
                    continue;
                }

                if (ShouldSkipFullscreen() && IsFullscreenCached(window, monitor))
                {
                    continue;
                }

                target = window;
                return true;
            }

            target = IntPtr.Zero;
            return false;
        }

        private bool TryFindNextGlobal(IntPtr currentMonitor, out IntPtr target)
        {
            foreach (var window in EnumerateWindowsNextWindow())
            {
                if (!IsValidWindow(window))
                {
                    continue;
                }

                var monitor = NativeWindows.MonitorFromWindow(window, NativeWindows.MONITOR_DEFAULTTONEAREST);
                if (monitor == currentMonitor)
                {
                    continue;
                }

                if (ShouldSkipFullscreen() && IsFullscreenCached(window, monitor))
                {
                    continue;
                }

                target = window;
                return true;
            }

            target = IntPtr.Zero;
            return false;
        }

        private bool TryFindPrevGlobal(IntPtr currentMonitor, out IntPtr target)
        {
            foreach (var window in EnumerateWindowsPreviousWindow())
            {
                if (!IsValidWindow(window))
                {
                    continue;
                }

                var monitor = NativeWindows.MonitorFromWindow(window, NativeWindows.MONITOR_DEFAULTTONEAREST);
                if (monitor == currentMonitor)
                {
                    continue;
                }

                if (ShouldSkipFullscreen() && IsFullscreenCached(window, monitor))
                {
                    continue;
                }

                target = window;
                return true;
            }

            target = IntPtr.Zero;
            return false;
        }

        private void WrapNextWindow()
        {
            foreach (var window in EnumerateWindowsNextWindow())
            {
                if (!IsValidWindow(window))
                {
                    continue;
                }

                var monitor = NativeWindows.MonitorFromWindow(window, NativeWindows.MONITOR_DEFAULTTONEAREST);
                if (ShouldSkipFullscreen() && IsFullscreenCached(window, monitor))
                {
                    continue;
                }

                DebugNote(Localization.Entry("Rule_Wrap_NextWindow"), window);
                SetForeground(window);
                return;
            }
        }

        private void WrapPreviousWindow()
        {
            foreach (var window in EnumerateWindowsPreviousWindow())
            {
                if (!IsValidWindow(window))
                {
                    continue;
                }

                var monitor = NativeWindows.MonitorFromWindow(window, NativeWindows.MONITOR_DEFAULTTONEAREST);
                if (ShouldSkipFullscreen() && IsFullscreenCached(window, monitor))
                {
                    continue;
                }

                DebugNote(Localization.Entry("Rule_Wrap_PreviousWindow"), window);
                SetForeground(window);
                return;
            }
        }

        private void CycleNextWindow()
        {
            var currentWindow = NativeWindows.GetForegroundWindow();
            if (currentWindow == IntPtr.Zero)
            {
                return;
            }

            var currentMonitor = NativeWindows.MonitorFromWindow(currentWindow, NativeWindows.MONITOR_DEFAULTTONEAREST);

            if (ConfigService.CurrentConfig.SuspendWhenFullscreen &&
                IsFullscreenCached(currentWindow, currentMonitor) &&
                !IsOverrideKeyDown())
            {
                return;
            }

            // 1. Boost: fresh boost-target window wins immediately
            if (ConfigService.CurrentConfig.BoostNewWindows)
            {
                if (TryFindFreshBoostWindow(EnumerateWindowsNextWindow(), out var freshBoost))
                {
                    DebugNote(Localization.Entry("Rule_Fresh_Boost"), freshBoost);
                    SetForeground(freshBoost);
                    return;
                }
            }

            // 2. Per-monitor preferred switching
            if (ConfigService.CurrentConfig.PreferPerMonitor &&
                TryFindNextOnSameMonitor(currentMonitor, out var localTarget))
            {
                DebugNote(Localization.Entry("Rule_Same_Monitor"), localTarget);
                SetForeground(localTarget);
                return;
            }

            // 3. Other boost-target windows
            if (TryFindBoostTargetWindow(EnumerateWindowsNextWindow(), out var boostTarget))
            {
                DebugNote(Localization.Entry("Rule_Boost_Window"), boostTarget);
                SetForeground(boostTarget);
                return;
            }

            // 4. Global fall-through
            if (ConfigService.CurrentConfig.PreferPerMonitor &&
                TryFindNextGlobal(currentMonitor, out var globalTarget))
            {
                DebugNote(Localization.Entry("Rule_Global"), globalTarget);
                SetForeground(globalTarget);
                return;
            }

            // 5. Wrap-around
            WrapNextWindow();
        }

        private void CyclePreviousWindow()
        {
            var currentWindow = NativeWindows.GetForegroundWindow();
            if (currentWindow == IntPtr.Zero)
            {
                return;
            }

            var currentMonitor = NativeWindows.MonitorFromWindow(currentWindow, NativeWindows.MONITOR_DEFAULTTONEAREST);

            if (ConfigService.CurrentConfig.SuspendWhenFullscreen &&
                IsFullscreenCached(currentWindow, currentMonitor) &&
                !IsOverrideKeyDown())
            {
                return;
            }

            // 1. Boost: fresh boost-target window wins immediately
            if (ConfigService.CurrentConfig.BoostNewWindows)
            {
                if (TryFindFreshBoostWindow(EnumerateWindowsPreviousWindow(), out var freshBoost))
                {
                    DebugNote(Localization.Entry("Rule_Fresh_Boost"), freshBoost);
                    SetForeground(freshBoost);
                    return;
                }
            }

            // 2. Per-monitor preferred switching
            if (ConfigService.CurrentConfig.PreferPerMonitor &&
                TryFindPrevOnSameMonitor(currentMonitor, out var localTarget))
            {
                DebugNote(Localization.Entry("Rule_Same_Monitor"), localTarget);
                SetForeground(localTarget);
                return;
            }

            // 3. Other boost-target windows
            if (TryFindBoostTargetWindow(EnumerateWindowsPreviousWindow(), out var boostTarget))
            {
                DebugNote(Localization.Entry("Rule_Boost_Window"), boostTarget);
                SetForeground(boostTarget);
                return;
            }

            // 4. Global fall-through
            if (ConfigService.CurrentConfig.PreferPerMonitor &&
                TryFindPrevGlobal(currentMonitor, out var globalTarget))
            {
                DebugNote(Localization.Entry("Rule_Global"), globalTarget);
                SetForeground(globalTarget);
                return;
            }

            // 5. Wrap-around
            WrapPreviousWindow();
        }
    }
}

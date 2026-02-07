namespace Death_and_Access;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

public class SpecificTextAnnouncer
{
    private readonly ConditionalWeakTable<object, LastTextHolder> _lastByComponent = new();
    private string _lastMoneyNotifyText;
    private bool _moneyNotifyVisible;
    private int _lastMoneyNotifyTick;
    private bool _suppressMoneyNotifications;
    private readonly ConditionalWeakTable<object, FocusState> _focusByRoot = new();
    private readonly ConditionalWeakTable<object, CoinState> _coinStates = new();
    private ScreenreaderProvider _screenreader;
    private Type _tmpTextType;
    private Type _uiTextType;
    private Type _inputType;
    private Type _keyCodeType;
    private Type _unityObjectType;
    private Type _canvasGroupType;
    private Type _selectableType;
    private Type _elevatorManagerType;
    private Type _shopType;
    private Type _eventSystemType;
    private Type _paperworkType;
    private Type _audioSourceType;
    private Type _saveManagerType;
    private Type _articyGlobalVariablesType;
    private Type _deskLampType;
    private Type _moneyNotificationType;
    private Type _resourcesType;
    private MethodInfo _findObjectsOfTypeMethod;
    private MethodInfo _findObjectsOfTypeAllMethod;
    private object _lastSelectedGameObject;
    private object _lastHoveredGameObject;
    private LampState _deskLampState;
    private string _lastSceneName;
    private string _lastShopHoverText;
    private string _lastMouseHoverText;
    private readonly Dictionary<string, object> _keyCodes = new(StringComparer.OrdinalIgnoreCase);

    public void Initialize(ScreenreaderProvider screenreader)
    {
        _screenreader = screenreader;
    }

    public void Update()
    {
        if (_screenreader == null || !_screenreader.Enabled)
            return;

        EnsureTypes();

        if (!TryGetCurrentSceneName(out var sceneName))
            return;

        var sceneChanged = !string.Equals(sceneName, _lastSceneName, StringComparison.Ordinal);
        _lastSceneName = sceneName;
        _suppressMoneyNotifications = string.Equals(sceneName, "Elevator", StringComparison.OrdinalIgnoreCase);

        if (IsIntroOrComicScene(sceneName))
        {
            AnnounceScreenByInstance("IntroController");
            AnnounceScreenByInstance("SkipIntro");
            var forceComicRead = IsComicScene(sceneName) && IsPromptShortcutPressed();
            AnnounceAllTextComponents(sceneChanged || forceComicRead, includeInactive: true, ignoreVisibility: true);
            AnnounceDialogueScreen();
            AnnounceCurrentSelection();
            AnnounceCurrentHover();
            return;
        }

        if (string.Equals(sceneName, "PostGame", StringComparison.OrdinalIgnoreCase))
        {
            AnnouncePostGame();
            return;
        }

        AnnounceDialogueScreen();
        AnnounceHUD();
        var suppressHover = false;
        var suppressSelection = false;
        AnnouncePaperwork(ref suppressHover, ref suppressSelection);
        AnnounceLetterOfFate();
        AnnounceConfirmScreens();
        AnnounceGallery();
        AnnouncePhone(ref suppressHover, ref suppressSelection);
        AnnounceVoteCounter();
        AnnounceDecisionCoinFlipResult();
        AnnounceChaosGlobeScore(ref suppressHover, ref suppressSelection);
        AnnounceLampPowerChange();
        if (!suppressSelection && !_screenreader.IsBusy)
            AnnounceCurrentSelection();
        if (!suppressHover)
            AnnounceCurrentHover();
        if (!suppressHover)
            AnnounceMouseHover();

    }

    public void Cleanup()
    {
        _screenreader = null;
    }

    private void EnsureTypes()
    {
        _tmpTextType ??= TypeResolver.Get("TMPro.TMP_Text");
        _uiTextType ??= TypeResolver.Get("UnityEngine.UI.Text");
        _inputType ??= TypeResolver.Get("UnityEngine.Input");
        _keyCodeType ??= TypeResolver.Get("UnityEngine.KeyCode");
        _unityObjectType ??= TypeResolver.Get("UnityEngine.Object");
        _canvasGroupType ??= TypeResolver.Get("UnityEngine.CanvasGroup");
        _selectableType ??= TypeResolver.Get("UnityEngine.UI.Selectable");
        _elevatorManagerType ??= TypeResolver.Get("ElevatorManager");
        _shopType ??= TypeResolver.Get("Shop");
        _eventSystemType ??= TypeResolver.Get("UnityEngine.EventSystems.EventSystem");
        _paperworkType ??= TypeResolver.Get("Paperwork");
        _audioSourceType ??= TypeResolver.Get("UnityEngine.AudioSource");
        _saveManagerType ??= TypeResolver.Get("SaveManager");
        _articyGlobalVariablesType ??= TypeResolver.Get("Articy.Project_Of_Death.GlobalVariables.ArticyGlobalVariables");
        _deskLampType ??= TypeResolver.Get("DeskLamp");
        _moneyNotificationType ??= TypeResolver.Get("MoneyNotification");
        _resourcesType ??= TypeResolver.Get("UnityEngine.Resources");
        _findObjectsOfTypeMethod ??= _unityObjectType?.GetMethod(
            "FindObjectsOfType",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(Type) },
            null);
        _findObjectsOfTypeAllMethod ??= _resourcesType?.GetMethod(
            "FindObjectsOfTypeAll",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(Type) },
            null);
    }

    private void AnnounceDialogueScreen()
    {
        var instance = GetStaticInstance("DialogueScreen");
        if (instance == null)
            return;

        if (IsVoicePlaying(instance))
            return;

        ProcessRoot(instance, IsDialogueFocused, _ =>
        {
            var state = _focusByRoot.GetOrCreateValue(instance);
            var prompt = GetDialoguePromptText(instance);

            if (IsPromptShortcutPressed())
            {
                var replay = !string.IsNullOrWhiteSpace(prompt) ? prompt : state.LastDialogPrompt;
                if (!string.IsNullOrWhiteSpace(replay))
                {
                    _screenreader?.SuppressHoverFor(2000);
                    AnnounceContent(replay, priority: true);
                }
            }

            state.LastDialogPrompt = prompt;
        });
    }

    private void AnnounceHUD()
    {
        var instance = GetStaticInstance("HUDManager");
        if (instance == null)
            return;

        ProcessRoot(instance, IsHudFocused, force =>
        {
            var hover = GetFieldValue(instance, "TextHover");
            var hoverShop = GetFieldValue(instance, "TextHoverShop");
            var money = GetFieldValue(instance, "TextMoney");

            var hoverText = GetTextValue(hover);
            var hoverShopText = GetTextValue(hoverShop);

            if (!((IsShopActive() || IsElevatorSceneActive()) && IsLikelyValueText(hoverText)))
                AnnounceComponent(hover, null, force);

            if (!((IsShopActive() || IsElevatorSceneActive()) && IsLikelyValueText(hoverShopText)))
                AnnounceShopHover(hoverShop, force);

            if (IsShopActive() && !string.IsNullOrWhiteSpace(hoverShopText))
                return;

            var moneyText = GetTextValue(money);
            if (!((IsShopActive() || IsElevatorSceneActive()) && IsLikelyValueText(moneyText)))
                AnnounceComponent(money, null, force);
        });
    }

    private void AnnounceShopHover(object hoverComponent, bool force)
    {
        if (hoverComponent == null)
            return;

        if (!IsComponentInActiveScene(hoverComponent))
            return;

        if (IsShopActive())
        {
            var combined = BuildShopHoverText(hoverComponent);
            if (!string.IsNullOrWhiteSpace(combined))
            {
                var normalized = NormalizeShopHoverText(combined);
                if (string.IsNullOrWhiteSpace(normalized))
                    return;

                if (string.Equals(_lastShopHoverText, normalized, StringComparison.Ordinal))
                    return;

                _lastShopHoverText = normalized;
                AnnounceContent(normalized, priority: false);
                return;
            }
            _lastShopHoverText = null;
        }

        var text = GetTextValue(hoverComponent);
        if (string.Equals(text, "shop item template", StringComparison.OrdinalIgnoreCase))
        {
            var shopName = GetShopDisplayName();
            if (!string.IsNullOrWhiteSpace(shopName))
            {
                AnnounceContent(shopName, priority: false);
                return;
            }
        }

        AnnounceComponent(hoverComponent, null, force);
    }

    private string BuildShopHoverText(object hoverComponent)
    {
        var description = GetTextValue(hoverComponent);
        description = TextSanitizer.StripRichTextTags(description)?.Trim();

        var shopName = GetShopItemNameText();
        var shopPrice = GetShopItemPriceText();

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(shopName))
            parts.Add(shopName);
        if (!string.IsNullOrWhiteSpace(description))
            parts.Add(description);
        if (!string.IsNullOrWhiteSpace(shopPrice))
            parts.Add(shopPrice);

        return parts.Count == 0 ? null : string.Join(". ", parts);
    }

    private string NormalizeShopHoverText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var trimmed = text.Trim();
        var builder = new System.Text.StringBuilder(trimmed.Length);
        var lastWasSpace = false;
        foreach (var ch in trimmed)
        {
            if (char.IsWhiteSpace(ch))
            {
                if (lastWasSpace)
                    continue;
                builder.Append(' ');
                lastWasSpace = true;
            }
            else
            {
                builder.Append(ch);
                lastWasSpace = false;
            }
        }

        return builder.ToString();
    }

    private string GetShopItemNameText()
    {
        if (_shopType == null)
            return null;

        try
        {
            var instanceField = _shopType.GetField("instance", BindingFlags.Public | BindingFlags.Static);
            var instance = instanceField?.GetValue(null);
            if (instance == null)
                return null;

            var textNameField = _shopType.GetField("TextName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var textName = textNameField?.GetValue(instance);
            return GetTextValue(textName);
        }
        catch
        {
            return null;
        }
    }

    private string GetShopItemPriceText()
    {
        if (_shopType == null)
            return null;

        try
        {
            var instanceField = _shopType.GetField("instance", BindingFlags.Public | BindingFlags.Static);
            var instance = instanceField?.GetValue(null);
            if (instance == null)
                return null;

            var textPriceField = _shopType.GetField("TextPrice", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var textPrice = textPriceField?.GetValue(instance);
            var raw = GetTextValue(textPrice);
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            return "Price " + raw.Trim();
        }
        catch
        {
            return null;
        }
    }

    private bool IsShopActive()
    {
        if (_shopType == null)
            return false;

        try
        {
            var instanceField = _shopType.GetField("instance", BindingFlags.Public | BindingFlags.Static);
            var instance = instanceField?.GetValue(null);
            if (instance == null)
                return false;

            if (!ReflectionUtils.TryGetProperty(instance.GetType(), instance, "gameObject", out var go))
                return true;

            return go == null || IsGameObjectActive(go);
        }
        catch
        {
            return false;
        }
    }

    private bool IsElevatorSceneActive()
    {
        var elevatorManager = GetStaticInstance("ElevatorManager");
        if (elevatorManager == null)
            return false;

        try
        {
            var method = elevatorManager.GetType().GetMethod("GetCurrentScene", BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
                return false;

            var scene = method.Invoke(elevatorManager, null);
            return scene != null && string.Equals(scene.ToString(), "Elevator", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private bool IsLikelyValueText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var trimmed = text.Trim();
        var digits = 0;
        var nonDigits = 0;
        foreach (var ch in trimmed)
        {
            if (char.IsDigit(ch))
            {
                digits++;
                continue;
            }

            if (ch == '.' || ch == ',' || ch == '-' || ch == '$' || ch == '+')
                continue;

            if (char.IsWhiteSpace(ch))
                continue;

            nonDigits++;
        }

        if (nonDigits > 0)
            return false;

        return digits > 0;
    }

    private string GetShopDisplayName()
    {
        if (_shopType == null)
            return null;

        try
        {
            var instanceField = _shopType.GetField("instance", BindingFlags.Public | BindingFlags.Static);
            var instance = instanceField?.GetValue(null);
            if (instance == null)
                return null;

            var textNameField = _shopType.GetField("TextName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var textName = textNameField?.GetValue(instance);
            return GetTextValue(textName);
        }
        catch
        {
            return null;
        }
    }

    private void AnnouncePaperwork(ref bool suppressHover, ref bool suppressSelection)
    {
        var focusedPaperworks = new List<object>();
        foreach (var paperwork in FindObjectsOfType("Paperwork"))
        {
            if (paperwork == null)
                continue;

            if (IsPaperworkFocusedForAnnouncement(paperwork))
                focusedPaperworks.Add(paperwork);
            else
            {
                var state = _focusByRoot.GetOrCreateValue(paperwork);
                state.IsFocused = false;
                state.LastLampResult = null;
            }
        }

        if (focusedPaperworks.Count == 0)
            return;

        suppressHover = true;
        suppressSelection = true;

        object primary = null;
        foreach (var paperwork in focusedPaperworks)
        {
            var state = _focusByRoot.GetOrCreateValue(paperwork);
            if (!state.IsFocused)
            {
                primary = paperwork;
                break;
            }
        }

        primary ??= focusedPaperworks[0];

        foreach (var paperwork in focusedPaperworks)
        {
            var state = _focusByRoot.GetOrCreateValue(paperwork);
            var isNewFocus = !state.IsFocused;
            state.IsFocused = true;
            if (!ReferenceEquals(paperwork, primary))
                continue;

            var profileText = BuildPaperworkProfileTextFromAssignedProfile(paperwork);
            var markStatus = BuildPaperworkMarkStatusText(paperwork);
            var lampResult = BuildLampResultText(paperwork);
            var combinedText = CombineProfileAndLamp(profileText, markStatus, lampResult);
            if (!string.IsNullOrWhiteSpace(combinedText)
                && (isNewFocus || !string.Equals(state.LastProfileText, combinedText, StringComparison.Ordinal)))
            {
                AnnounceContent(combinedText, priority: true);
                state.LastProfileText = combinedText;
                state.LastLampResult = lampResult;
            }
            else if (!string.IsNullOrWhiteSpace(combinedText) && state.LastProfileText == null)
            {
                AnnounceContent(combinedText, priority: true);
                state.LastProfileText = combinedText;
                state.LastLampResult = lampResult;
            }
        }
    }

    private void AnnounceLetterOfFate()
    {
        var instance = GetStaticInstance("LetterOfFate");
        if (instance == null)
            return;

        ProcessRoot(instance, IsLetterFocused, force =>
        {
            var letterText = BuildLetterText(instance);
            if (!string.IsNullOrWhiteSpace(letterText))
            {
                var state = _focusByRoot.GetOrCreateValue(instance);
                if (force || !string.Equals(state.LastLetterText, letterText, StringComparison.Ordinal))
                {
                    AnnounceContent(letterText, priority: true);
                    state.LastLetterText = letterText;
                }
                return;
            }

            AnnounceTextComponents(instance, null, force);
        });
    }

    private void AnnounceConfirmScreens()
    {
        AnnounceScreenByInstanceSelectionFirst("MarkConfirm");
        AnnounceScreenByInstanceSelectionFirst("FaxConfirm");
        AnnounceScreenByInstanceSelectionFirst("BuyConfirm");
        AnnounceScreenByInstanceSelectionFirst("RestartConfirm");
        AnnounceScreenByInstanceSelectionFirst("EndDayConfirm");
    }

    private void AnnounceGallery()
    {
        AnnounceScreenByInstance("GalleryScreen");
    }

    private void AnnouncePhone(ref bool suppressHover, ref bool suppressSelection)
    {
        var instance = GetStaticInstance("Phone");
        if (instance == null)
            return;

        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "TextClock"
        };

        var focused = IsPhoneFocused(instance);
        var state = _focusByRoot.GetOrCreateValue(instance);
        if (focused)
        {
            suppressHover = true;
            suppressSelection = true;
            var newsText = BuildPhoneNewsText(instance);
            if (!state.IsFocused || (!string.IsNullOrWhiteSpace(newsText)
                && !string.Equals(state.LastPhoneText, newsText, StringComparison.Ordinal)))
            {
                state.IsFocused = true;
                if (!string.IsNullOrWhiteSpace(newsText))
                {
                    AnnounceContent(newsText, priority: true);
                    state.LastPhoneText = newsText;
                    return;
                }
                AnnounceTextComponents(instance, excluded, force: true);
                return;
            }

            AnnounceTextComponents(instance, excluded, force: false);
            return;
        }

        state.IsFocused = false;
    }

    private void AnnounceVoteCounter()
    {
        var instance = GetStaticInstance("VoteCounter");
        if (instance == null)
            return;

        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "TextTimer"
        };

        ProcessRoot(instance, IsVoteCounterFocused, force => AnnounceTextComponents(instance, excluded, force));
    }

    private void AnnounceDecisionCoinFlipResult()
    {
        var coin = GetStaticInstance("DecisionCoin");
        if (coin == null)
            return;

        if (!IsRootActive(coin))
            return;

        var state = _coinStates.GetOrCreateValue(coin);
        var coinType = coin.GetType();
        var hasSpinning = TryGetBoolField(coinType, coin, "bIsSpinning", out var spinning);
        if (!hasSpinning)
            spinning = false;

        var renderer = GetFieldValue(coin, "CoinRenderer");
        var sprite = GetSprite(renderer);

        if (!state.Initialized)
        {
            state.Initialized = true;
            state.WasSpinning = spinning;
            state.LastSprite = sprite;
            return;
        }

        if (state.WasSpinning && !spinning)
        {
            var label = ResolveDecisionCoinLabel(coin, sprite);
            if (!string.IsNullOrWhiteSpace(label)
                && !string.Equals(label, state.LastResult, StringComparison.Ordinal))
            {
                AnnounceContent(label, priority: false);
                state.LastResult = label;
            }
        }

        state.WasSpinning = spinning;
        state.LastSprite = sprite;
    }

    private void AnnounceChaosGlobeScore(ref bool suppressHover, ref bool suppressSelection)
    {
        var globe = GetStaticInstance("ChaosGlobe");
        if (globe == null)
            return;

        var state = _focusByRoot.GetOrCreateValue(globe);
        var focused = IsChaosGlobeFocused(globe);
        if (focused)
        {
            suppressHover = true;
            suppressSelection = true;
            var scoreText = BuildChaosGlobeScoreText();
            if (!state.IsFocused || (!string.IsNullOrWhiteSpace(scoreText)
                && !string.Equals(state.LastChaosGlobeScore, scoreText, StringComparison.Ordinal)))
            {
                state.IsFocused = true;
                if (!string.IsNullOrWhiteSpace(scoreText))
                {
                    AnnounceContent(scoreText, priority: false);
                    state.LastChaosGlobeScore = scoreText;
                }
            }
        }
        else
        {
            state.IsFocused = false;
            state.LastChaosGlobeScore = null;
        }
    }

    private void AnnounceLampPowerChange()
    {
        if (_deskLampType == null)
            return;

        var lamp = GetStaticInstance(_deskLampType);
        if (lamp == null)
            return;

        var light = GetFieldValue(lamp, "Light");
        if (light == null)
            return;

        if (!ReflectionUtils.TryGetProperty(light.GetType(), light, "gameObject", out var lightObject) || lightObject == null)
            return;

        var isOn = IsGameObjectActive(lightObject);
        if (_deskLampState == null)
        {
            _deskLampState = new LampState { IsOn = isOn, Initialized = true };
            return;
        }

        if (_deskLampState.IsOn == isOn)
            return;

        _deskLampState.IsOn = isOn;
        AnnounceContent(isOn ? "On" : "Off", priority: false);
    }

    private string ResolveDecisionCoinLabel(object coin, object sprite)
    {
        if (coin == null || sprite == null)
            return null;

        var skull = GetFieldValue(coin, "SpriteSkull");
        if (skull != null && ReferenceEquals(skull, sprite))
            return "Skull";

        var ankh = GetFieldValue(coin, "SpriteAnkh");
        if (ankh != null && ReferenceEquals(ankh, sprite))
            return "Ankh";

        var spriteName = GetObjectName(sprite);
        if (string.IsNullOrWhiteSpace(spriteName))
            return null;

        if (skull != null && string.Equals(GetObjectName(skull), spriteName, StringComparison.Ordinal))
            return "Skull";

        if (ankh != null && string.Equals(GetObjectName(ankh), spriteName, StringComparison.Ordinal))
            return "Ankh";

        return spriteName;
    }

    private void AnnouncePostGame()
    {
        var instance = GetStaticInstance("MortimerPostGame");
        if (instance == null)
            return;

        ProcessRoot(instance, IsRootActive, force => AnnounceTextComponents(instance, null, force));
        AnnounceCurrentSelection();
        AnnounceCurrentHover();
    }

    private string BuildPaperworkProfileTextFromAssignedProfile(object paperwork)
    {
        if (paperwork == null)
            return null;

        var parts = new List<string>();
        AddAssignedProfileValue(parts, "Name", GetAssignedProfileField(paperwork, "profile_name"));
        AddAssignedProfileValue(parts, "Age", GetAssignedProfileField(paperwork, "profile_age_value"));
        AddAssignedProfileValue(parts, "Position", GetAssignedProfileField(paperwork, "profile_job"));
        AddAssignedProfileValue(parts, null, GetAssignedProfileField(paperwork, "profile_bio"));

        if (parts.Count == 0)
            return null;

        return string.Join(". ", parts);
    }

    private void AddAssignedProfileValue(List<string> parts, string label, string value)
    {
        if (parts == null || string.IsNullOrWhiteSpace(value))
            return;

        if (IsPlaceholderValue(value))
            return;

        if (!string.IsNullOrWhiteSpace(label))
            parts.Add(label + " " + value.Trim());
        else
            parts.Add(value.Trim());
    }

    private string BuildChaosGlobeScoreText()
    {
        if (_articyGlobalVariablesType == null)
            return null;

        var defaults = GetStaticMemberValue(_articyGlobalVariablesType, "Default");
        if (defaults == null)
            return null;

        var rep = GetMemberValue(defaults, "rep");
        if (rep == null)
            return null;

        var worstValue = GetMemberValue(rep, "worst_parameter_value");
        var worstName = GetMemberValue(rep, "worst_parameter_name") as string;
        var ecologyValue = GetMemberValue(rep, "ecology");
        var peaceValue = GetMemberValue(rep, "peace");
        var prosperityValue = GetMemberValue(rep, "prosperity");
        var healthValue = GetMemberValue(rep, "health");

        var formattedValue = FormatNumber(worstValue);
        var ecologyText = FormatNumber(ecologyValue);
        var peaceText = FormatNumber(peaceValue);
        var prosperityText = FormatNumber(prosperityValue);
        var healthText = FormatNumber(healthValue);

        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(formattedValue))
            parts.Add("Snowglobe score " + formattedValue);
        if (!string.IsNullOrWhiteSpace(ecologyText))
            parts.Add("Ecology " + ecologyText);
        if (!string.IsNullOrWhiteSpace(peaceText))
            parts.Add("Peace " + peaceText);
        if (!string.IsNullOrWhiteSpace(prosperityText))
            parts.Add("Prosperity " + prosperityText);
        if (!string.IsNullOrWhiteSpace(healthText))
            parts.Add("Health " + healthText);

        if (!string.IsNullOrWhiteSpace(worstName) && !string.IsNullOrWhiteSpace(formattedValue))
            parts.Add("Worst parameter " + worstName.Trim() + " " + formattedValue);

        if (parts.Count == 0)
            return null;

        return string.Join(". ", parts) + ".";
    }

    private string BuildPaperworkMarkStatusText(object paperwork)
    {
        if (paperwork == null)
            return null;

        var markStatus = GetMemberValue(paperwork, "MarkStatus");
        var markStatusText = markStatus?.ToString();
        if (string.IsNullOrWhiteSpace(markStatusText)
            || string.Equals(markStatusText, "Unmarked", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return string.Equals(markStatusText, "Live", StringComparison.OrdinalIgnoreCase)
            ? "Marked live"
            : "Marked die";
    }

    private string BuildLampResultText(object paperwork)
    {
        if (paperwork == null)
            return null;

        if (!IsLampOn())
            return null;

        var markStatus = GetMemberValue(paperwork, "MarkStatus");
        var markStatusText = markStatus?.ToString();
        if (string.IsNullOrWhiteSpace(markStatusText)
            || string.Equals(markStatusText, "Unmarked", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var profile = GetProfileFromPaperwork(paperwork);
        if (profile == null)
            return null;

        var profileId = GetMemberValue(profile, "Id");
        var hasSinBulb = GetInventoryFlag("sin_bulb");
        var revealStats = ShouldRevealLampStats(markStatusText, profileId, hasSinBulb);

        var parts = new List<string>();
        if (!revealStats)
            return null;

        var template = GetMemberValue(profile, "Template");
        if (template == null)
            return null;

        var data = string.Equals(markStatusText, "Live", StringComparison.OrdinalIgnoreCase)
            ? GetMemberValue(template, "profile_spare_data")
            : GetMemberValue(template, "profile_death_data");

        if (data == null)
            return null;

        AppendLampValue(parts, "Ecology", GetMemberValue(data, GetLampValueField(markStatusText, "ecology")));
        AppendLampValue(parts, "Prosperity", GetMemberValue(data, GetLampValueField(markStatusText, "prosperity")));
        AppendLampValue(parts, "Health", GetMemberValue(data, GetLampValueField(markStatusText, "healthcare")));
        AppendLampValue(parts, "Peace", GetMemberValue(data, GetLampValueField(markStatusText, "peace")));

        if (parts.Count == 0)
            return null;

        return string.Join(". ", parts) + ".";
    }

    private object GetProfileFromPaperwork(object paperwork)
    {
        if (paperwork == null)
            return null;

        var method = paperwork.GetType().GetMethod("GetProfile", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (method != null)
        {
            try
            {
                return method.Invoke(paperwork, null);
            }
            catch
            {
                return null;
            }
        }

        return GetMemberValue(paperwork, "AssignedProfile");
    }

    private bool ShouldRevealLampStats(string markStatus, object profileId, bool hasSinBulb)
    {
        var state = GetCurrentCarryoverState();
        if (state == null || profileId == null)
            return false;

        var doomed = InvokeBool(state, "HasProfileBeenDoomedBefore", profileId);
        var spared = InvokeBool(state, "HasProfileBeenSparedBefore", profileId);

        if (string.Equals(markStatus, "Die", StringComparison.OrdinalIgnoreCase))
            return doomed || (spared && hasSinBulb);

        if (string.Equals(markStatus, "Live", StringComparison.OrdinalIgnoreCase))
            return spared || (doomed && hasSinBulb);

        return false;
    }

    private object GetCurrentCarryoverState()
    {
        if (_saveManagerType == null)
            return null;

        try
        {
            var instanceField = _saveManagerType.GetField("instance", BindingFlags.Public | BindingFlags.Static);
            var instance = instanceField?.GetValue(null);
            if (instance == null)
                return null;

            var method = _saveManagerType.GetMethod("GetCurrentCarryoverPlayerState", BindingFlags.Instance | BindingFlags.Public);
            return method?.Invoke(instance, null);
        }
        catch
        {
            return null;
        }
    }

    private bool InvokeBool(object instance, string methodName, object argument)
    {
        if (instance == null || string.IsNullOrWhiteSpace(methodName))
            return false;

        try
        {
            var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
                return false;

            var result = method.Invoke(instance, new[] { argument });
            return result is bool value && value;
        }
        catch
        {
            return false;
        }
    }

    private bool GetInventoryFlag(string fieldName)
    {
        if (_articyGlobalVariablesType == null || string.IsNullOrWhiteSpace(fieldName))
            return false;

        var defaults = GetStaticMemberValue(_articyGlobalVariablesType, "Default");
        if (defaults == null)
            return false;

        var inventory = GetMemberValue(defaults, "inventory");
        if (inventory == null)
            return false;

        return ReflectionUtils.TryGetBoolProperty(inventory.GetType(), inventory, fieldName, out var value) && value;
    }

    private string GetLampValueField(string markStatus, string stat)
    {
        if (string.Equals(markStatus, "Live", StringComparison.OrdinalIgnoreCase))
            return "profile_spare_" + stat + "_value";

        return "profile_death_" + stat + "_value";
    }

    private void AppendLampValue(List<string> parts, string label, object rawValue)
    {
        if (parts == null || string.IsNullOrWhiteSpace(label))
            return;

        if (!TryGetFloat(rawValue, out var value))
            return;

        if (value > 0f)
        {
            parts.Add(label + " up");
        }
        else if (value < 0f)
        {
            parts.Add(label + " down");
        }
    }

    private bool IsLampOn()
    {
        if (_deskLampType == null)
            return false;

        var lamp = GetStaticInstance(_deskLampType);
        if (lamp == null)
            return false;

        var light = GetFieldValue(lamp, "Light");
        if (light == null)
            return false;

        if (!ReflectionUtils.TryGetProperty(light.GetType(), light, "gameObject", out var lightObject) || lightObject == null)
            return false;

        return IsGameObjectActive(lightObject);
    }

    private string CombineProfileAndLamp(string profileText, string markText, string lampText)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(profileText))
            parts.Add(profileText);
        if (!string.IsNullOrWhiteSpace(markText))
            parts.Add(markText);
        if (!string.IsNullOrWhiteSpace(lampText))
            parts.Add(lampText);

        if (parts.Count == 0)
            return null;

        return string.Join(". ", parts);
    }

    private static bool TryGetFloat(object value, out float result)
    {
        result = 0f;
        if (value == null)
            return false;

        try
        {
            switch (value)
            {
                case float f:
                    result = f;
                    return true;
                case double d:
                    result = (float)d;
                    return true;
                case int i:
                    result = i;
                    return true;
                case long l:
                    result = l;
                    return true;
                case short s:
                    result = s;
                    return true;
                case byte b:
                    result = b;
                    return true;
                case decimal m:
                    result = (float)m;
                    return true;
                default:
                    result = Convert.ToSingle(value);
                    return true;
            }
        }
        catch
        {
            return false;
        }
    }

    private string BuildPhoneNewsText(object phone)
    {
        if (phone == null)
            return null;

        var textNews = GetFieldValue(phone, "TextNews");
        if (textNews == null)
            return null;

        var text = GetTextValue(textNews);
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    private string GetDialoguePromptText(object dialogue)
    {
        if (dialogue == null)
            return null;

        var textCurrent = GetFieldValue(dialogue, "TextCurrent");
        var text = GetTextValue(textCurrent);
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    private bool IsPromptShortcutPressed()
    {
        return GetKeyDown("BackQuote") || GetKeyDown("Backquote");
    }

    private bool GetKeyDown(string key)
    {
        if (_inputType == null || _keyCodeType == null || string.IsNullOrWhiteSpace(key))
            return false;

        var keyCode = GetKeyCode(key);
        if (keyCode == null)
            return false;

        try
        {
            var method = _inputType.GetMethod("GetKeyDown", BindingFlags.Public | BindingFlags.Static, null, new[] { _keyCodeType }, null);
            if (method == null)
                return false;

            var result = method.Invoke(null, new[] { keyCode });
            return result is bool pressed && pressed;
        }
        catch
        {
            return false;
        }
    }

    private object GetKeyCode(string key)
    {
        if (_keyCodeType == null || string.IsNullOrWhiteSpace(key))
            return null;

        if (_keyCodes.TryGetValue(key, out var cached))
            return cached;

        try
        {
            var value = Enum.Parse(_keyCodeType, key, ignoreCase: true);
            _keyCodes[key] = value;
            return value;
        }
        catch
        {
            return null;
        }
    }

    private string BuildLetterText(object letter)
    {
        if (letter == null)
            return null;

        var parts = new List<string>(3);
        AppendLetterPart(parts, GetFieldValue(letter, "TextLetterTop"));
        AppendLetterPart(parts, GetFieldValue(letter, "TextLetterBottom"));
        AppendLetterPart(parts, GetFieldValue(letter, "TextLetterFront"));

        return parts.Count == 0 ? null : string.Join(" ", parts);
    }

    private void AppendLetterPart(List<string> parts, object component)
    {
        if (component == null || !IsVisible(component))
            return;

        var text = GetTextValue(component);
        if (string.IsNullOrWhiteSpace(text))
            return;

        parts.Add(text.Trim());
    }

    private static bool IsPlaceholderValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        var trimmed = value.Trim();
        trimmed = trimmed.TrimEnd(':', ';', '.', '-', '—', '–');
        return string.Equals(trimmed, "name", StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, "age", StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, "position", StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, "situation", StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, "live", StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, "die", StringComparison.OrdinalIgnoreCase);
    }

    private string GetAssignedProfileField(object paperwork, string fieldName)
    {
        if (paperwork == null || string.IsNullOrWhiteSpace(fieldName))
            return null;

        var profile = GetFieldValue(paperwork, "AssignedProfile");
        if (profile == null)
            return null;

        var template = GetMemberValue(profile, "Template");
        if (template == null)
            return null;

        var basic = GetMemberValue(template, "profile_basic_data");
        if (basic == null)
            return null;

        var raw = GetMemberValue(basic, fieldName) as string;
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var cleaned = TextSanitizer.StripRichTextTags(raw);
        if (string.IsNullOrWhiteSpace(cleaned))
            return null;

        if (string.Equals(fieldName, "profile_name", StringComparison.Ordinal)
            && cleaned.IndexOf("[SPAWN_COUNTER]", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            var spawn = GetSpawnCounter();
            cleaned = cleaned.Replace("[SPAWN_COUNTER]", spawn ?? string.Empty);
        }

        return cleaned;
    }

    private static object GetMemberValue(object instance, string name)
    {
        if (instance == null || string.IsNullOrWhiteSpace(name))
            return null;

        var type = instance.GetType();
        try
        {
            var prop = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null)
                return prop.GetValue(instance);
        }
        catch
        {
            // Ignore.
        }

        try
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return field?.GetValue(instance);
        }
        catch
        {
            return null;
        }
    }

    private string GetSpawnCounter()
    {
        if (_saveManagerType == null)
            return null;

        try
        {
            var instanceField = _saveManagerType.GetField("instance", BindingFlags.Public | BindingFlags.Static);
            var instance = instanceField?.GetValue(null);
            if (instance == null)
                return null;

            var getState = _saveManagerType.GetMethod("GetCurrentPlayerState", BindingFlags.Instance | BindingFlags.Public);
            var state = getState?.Invoke(instance, null);
            if (state == null)
                return null;

            var method = state.GetType().GetMethod("GetSpawnCounter", BindingFlags.Instance | BindingFlags.Public);
            var value = method?.Invoke(state, null);
            return value?.ToString();
        }
        catch
        {
            return null;
        }
    }


    private bool ShouldAnnounceForGameObject(object gameObject)
    {
        if (_paperworkType == null || gameObject == null)
            return true;

        object paperwork = null;
        try
        {
            var method = gameObject.GetType().GetMethod(
                "GetComponentInParent",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(Type), typeof(bool) },
                null);

            if (method != null)
            {
                paperwork = method.Invoke(gameObject, new object[] { _paperworkType, true });
            }
            else
            {
                method = gameObject.GetType().GetMethod(
                    "GetComponentInParent",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(Type) },
                    null);

                if (method != null)
                {
                    paperwork = method.Invoke(gameObject, new object[] { _paperworkType });
                }
            }
        }
        catch
        {
            paperwork = null;
        }

        if (paperwork == null)
            return true;

        var statusField = _paperworkType.GetField("Status", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (statusField == null)
            return true;

        var statusValue = statusField.GetValue(paperwork);
        if (statusValue == null)
            return false;

        if (string.Equals(statusValue.ToString(), "FOCUS", StringComparison.OrdinalIgnoreCase))
            return true;

        return IsProfileNameObject(paperwork, gameObject);
    }

    private void AnnounceCurrentSelection()
    {
        if (_eventSystemType == null)
            return;

        var currentEventSystem = GetCurrentEventSystem();
        if (currentEventSystem == null)
        {
            _lastSelectedGameObject = null;
            return;
        }

        var current = GetCurrentSelectedGameObject(currentEventSystem);
        if (current == null)
        {
            _lastSelectedGameObject = null;
            return;
        }

        if (ReferenceEquals(current, _lastSelectedGameObject))
            return;

        if (!IsSelectableUsable(current))
            return;

        _lastSelectedGameObject = current;

        if (!IsGameObjectActive(current))
            return;

        if (!ShouldAnnounceForGameObject(current))
            return;

        if (TryAnnounceBarHoverFromVirtualCursor())
            return;

        if (TryAnnounceDialogChoice(current))
            return;

        AnnounceTextComponents(current, null, force: true);
        AnnounceHoveredObjectName(current);
    }

    private void AnnounceHoveredObjectName(object hovered)
    {
        var name = GetComponentOrParentName(hovered);
        if (string.IsNullOrWhiteSpace(name))
            return;

        AnnounceContent(name, priority: false);
    }

    private void AnnounceCurrentHover()
    {
        if (_eventSystemType == null)
            return;

        var currentEventSystem = GetCurrentEventSystem();
        if (currentEventSystem == null)
        {
            _lastHoveredGameObject = null;
            return;
        }

        var current = GetCurrentHoveredGameObject(currentEventSystem);
        if (current == null)
        {
            _lastHoveredGameObject = null;
            return;
        }

        if (ReferenceEquals(current, _lastHoveredGameObject))
            return;

        if (!IsSelectableUsable(current))
            return;

        _lastHoveredGameObject = current;

        if (!IsGameObjectActive(current))
            return;

        if (!ShouldAnnounceForGameObject(current))
            return;

        if (TryAnnounceBarHoverFromVirtualCursor())
            return;

        if (TryAnnounceDialogChoice(current))
            return;

        AnnounceTextComponents(current, null, force: true);
    }

    private void AnnounceMouseHover()
    {
        var nav = UiNavigationHandler.Instance;
        if (nav == null)
            return;

        if (TryAnnounceBarHoverFromVirtualCursor())
            return;

        if (TryAnnounceBarHoverFromMouse())
            return;

        if (nav.IsBarSceneActiveForAnnouncements)
            return;

        var hovered = GetCurrentHoveredGameObject(GetCurrentEventSystem());
        if (TryAnnounceDialogChoice(hovered))
            return;

        if (nav.TryGetMouseHoverText(out var text))
        {
            if (string.Equals(_lastMouseHoverText, text, StringComparison.Ordinal))
                return;

            _lastMouseHoverText = text;
            AnnounceContent(text, priority: false);
        }
    }

    private bool TryAnnounceBarHoverFromVirtualCursor()
    {
        var nav = UiNavigationHandler.Instance;
        if (nav == null || !nav.IsBarSceneActiveForAnnouncements)
            return false;

        if (!nav.TryGetBarHoverNameFromVirtualCursor(out var name))
            return false;

        if (string.Equals(_lastMouseHoverText, name, StringComparison.Ordinal))
            return true;

        _lastMouseHoverText = name;
        AnnounceContent(name, priority: false);
        return true;
    }

    private bool TryAnnounceBarHoverFromMouse()
    {
        var nav = UiNavigationHandler.Instance;
        if (nav == null || !nav.IsBarSceneActiveForAnnouncements)
            return false;

        if (!nav.TryGetBarHoverNameFromMouse(out var name))
            return false;

        if (string.Equals(_lastMouseHoverText, name, StringComparison.Ordinal))
            return true;

        _lastMouseHoverText = name;
        AnnounceContent(name, priority: false);
        return true;
    }

    private bool TryAnnounceDialogChoice(object gameObject)
    {
        if (gameObject == null || _selectableType == null)
            return false;

        var selectable = GetComponentInParent(gameObject, _selectableType);
        if (selectable == null)
            return false;

        var choice = GetComponentInParent(gameObject, "DialogueChoiceButton");
        if (choice == null)
            return false;

        var textChoice = GetFieldValue(choice, "TextChoice");
        if (textChoice == null)
            return false;

        var text = GetTextValue(textChoice);
        if (string.IsNullOrWhiteSpace(text))
            return false;

        AnnounceContent(text.Trim(), priority: true);
        return true;
    }

    private object GetComponentInParent(object gameObject, string typeName)
    {
        if (gameObject == null || string.IsNullOrWhiteSpace(typeName))
            return null;

        var type = TypeResolver.Get(typeName);
        if (type == null)
            return null;

        return GetComponentInParent(gameObject, type);
    }

    private object GetCurrentEventSystem()
    {
        if (_eventSystemType == null)
            return null;

        try
        {
            var currentProp = _eventSystemType.GetProperty("current", BindingFlags.Static | BindingFlags.Public);
            return currentProp?.GetValue(null);
        }
        catch
        {
            return null;
        }
    }

    private object GetCurrentSelectedGameObject(object eventSystem)
    {
        if (eventSystem == null || _eventSystemType == null)
            return null;

        try
        {
            var selectedProp = _eventSystemType.GetProperty("currentSelectedGameObject", BindingFlags.Instance | BindingFlags.Public);
            return selectedProp?.GetValue(eventSystem);
        }
        catch
        {
            return null;
        }
    }

    private object GetCurrentHoveredGameObject(object eventSystem)
    {
        if (eventSystem == null || _eventSystemType == null)
            return null;

        try
        {
            var inputModuleProp = _eventSystemType.GetProperty("currentInputModule", BindingFlags.Instance | BindingFlags.Public);
            var inputModule = inputModuleProp?.GetValue(eventSystem);
            if (inputModule == null)
                return null;

            var method = inputModule.GetType().GetMethod("GetLastPointerEventData", BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
                return null;

            var pointerData = method.Invoke(inputModule, new object[] { -1 });
            if (pointerData == null)
                return null;

            var hoverProp = pointerData.GetType().GetProperty("pointerEnter", BindingFlags.Instance | BindingFlags.Public);
            return hoverProp?.GetValue(pointerData);
        }
        catch
        {
            return null;
        }
    }

    private bool IsProfileNameObject(object paperwork, object gameObject)
    {
        var nameText = GetFieldValue(paperwork, "TextName");
        if (nameText == null)
            return false;

        if (!ReflectionUtils.TryGetProperty(nameText.GetType(), nameText, "gameObject", out var nameGameObject) || nameGameObject == null)
            return false;

        if (ReferenceEquals(gameObject, nameGameObject))
            return true;

        return IsChildOf(gameObject, nameGameObject);
    }

    private bool IsChildOf(object gameObject, object potentialParent)
    {
        if (gameObject == null || potentialParent == null)
            return false;

        if (!ReflectionUtils.TryGetProperty(gameObject.GetType(), gameObject, "transform", out var gameTransform) || gameTransform == null)
            return false;

        if (!ReflectionUtils.TryGetProperty(potentialParent.GetType(), potentialParent, "transform", out var parentTransform) || parentTransform == null)
            return false;

        var method = gameTransform.GetType().GetMethod("IsChildOf", BindingFlags.Instance | BindingFlags.Public);
        if (method == null)
            return false;

        try
        {
            var result = method.Invoke(gameTransform, new[] { parentTransform });
            return result is bool isChild && isChild;
        }
        catch
        {
            return false;
        }
    }

    private bool TryGetCurrentSceneName(out string sceneName)
    {
        sceneName = null;
        if (_elevatorManagerType == null)
            return true;

        var instance = GetStaticInstance("ElevatorManager");
        if (instance == null)
            return false;

        if (TryGetBoolField(_elevatorManagerType, instance, "bIsChangingScene", out var isChanging) && isChanging)
            return false;

        var sceneMethod = _elevatorManagerType.GetMethod("GetCurrentScene", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (sceneMethod == null)
            return true;

        object sceneObj;
        try
        {
            sceneObj = sceneMethod.Invoke(instance, null);
        }
        catch
        {
            return false;
        }

        if (sceneObj == null)
            return false;

        sceneName = sceneObj.ToString();
        return true;
    }

    private void AnnounceScreenByInstance(string typeName)
    {
        var instance = GetStaticInstance(typeName);
        if (instance == null)
            return;

        ProcessRoot(instance, IsRootActive, force => AnnounceTextComponents(instance, null, force));
    }

    private void AnnounceScreenByInstanceSelectionFirst(string typeName)
    {
        var instance = GetStaticInstance(typeName);
        if (instance == null)
            return;

        ProcessRoot(instance, IsRootActive, force =>
        {
            var state = _focusByRoot.GetOrCreateValue(instance);
            var prompt = GetConfirmPromptText(instance);
            var optionText = GetConfirmOptionText(instance);
            var promptChanged = !string.Equals(state.LastDialogPrompt, prompt, StringComparison.Ordinal);
            var optionChanged = !string.Equals(state.LastDialogOption, optionText, StringComparison.Ordinal);

            if (force || promptChanged || optionChanged)
            {
                var combined = BuildDialogAnnouncement(prompt, optionText);
                if (!string.IsNullOrWhiteSpace(combined))
                {
                    _screenreader?.SuppressHoverFor(2000);
                    AnnounceContent(combined, priority: true);
                }
                else if (force)
                {
                    AnnounceNonSelectableTextComponents(instance, null, force);
                }
            }

            state.LastDialogPrompt = prompt;
            state.LastDialogOption = optionText;
        });
    }

    private string BuildDialogAnnouncement(string prompt, string optionText)
    {
        var hasPrompt = !string.IsNullOrWhiteSpace(prompt);
        var hasOption = !string.IsNullOrWhiteSpace(optionText);
        if (!hasPrompt && !hasOption)
            return null;

        if (hasPrompt && hasOption)
        {
            var trimmedPrompt = prompt.Trim();
            var trimmedOption = optionText.Trim();
            if (trimmedPrompt.EndsWith("?") || trimmedPrompt.EndsWith(".") || trimmedPrompt.EndsWith("!"))
                return trimmedPrompt + " " + trimmedOption;

            return trimmedPrompt + ". " + trimmedOption;
        }

        return hasPrompt ? prompt.Trim() : optionText.Trim();
    }

    private string GetConfirmOptionText(object root)
    {
        if (root == null)
            return null;

        var selectable = GetConfirmPreferredSelectable(root) ?? GetFirstSelectableComponent(root);
        if (selectable == null)
            return null;

        return GetFirstTextInChildren(selectable) ?? GetTextValue(selectable);
    }

    private string GetConfirmPromptText(object instance)
    {
        if (instance == null)
            return null;

        var prompt = GetTextValue(GetFieldValue(instance, "TextConfirm"));
        if (!string.IsNullOrWhiteSpace(prompt))
            return prompt;

        prompt = GetTextValue(GetFieldValue(instance, "TextPrompt"));
        if (!string.IsNullOrWhiteSpace(prompt))
            return prompt;

        prompt = GetTextValue(GetFieldValue(instance, "TextCurrent"));
        if (!string.IsNullOrWhiteSpace(prompt))
            return prompt;

        return null;
    }

    private object GetConfirmPreferredSelectable(object instance)
    {
        if (instance == null)
            return null;

        var yes = GetFieldValue(instance, "ButtonYes");
        if (IsSelectableComponentActive(yes))
            return yes;

        var ok = GetFieldValue(instance, "ButtonOk");
        if (IsSelectableComponentActive(ok))
            return ok;

        var no = GetFieldValue(instance, "ButtonNo");
        if (IsSelectableComponentActive(no))
            return no;

        return null;
    }

    private bool IsSelectableComponentActive(object selectable)
    {
        if (selectable == null)
            return false;

        if (!ReflectionUtils.TryGetProperty(selectable.GetType(), selectable, "gameObject", out var gameObject) || gameObject == null)
            return true;

        return IsGameObjectActive(gameObject);
    }

    private object GetFirstSelectableComponent(object root)
    {
        var method = root.GetType().GetMethod(
            "GetComponentsInChildren",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(Type), typeof(bool) },
            null);

        Array components = null;
        if (method != null)
        {
            try
            {
                components = method.Invoke(root, new object[] { _selectableType, true }) as Array;
            }
            catch
            {
                components = null;
            }
        }

        if (components == null)
        {
            method = root.GetType().GetMethod(
                "GetComponentsInChildren",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(Type) },
                null);
            if (method == null)
                return null;

            try
            {
                components = method.Invoke(root, new object[] { _selectableType }) as Array;
            }
            catch
            {
                return null;
            }
        }

        if (components == null)
            return null;

        foreach (var component in components)
        {
            if (component == null)
                continue;

            if (!IsVisible(component))
                continue;

            if (ReflectionUtils.TryGetBoolProperty(component.GetType(), component, "interactable", out var interactable)
                && !interactable)
                continue;

            return component;
        }

        return null;
    }

    private string GetFirstTextInChildren(object root)
    {
        if (root == null)
            return null;

        var text = GetFirstTextOfType(root, _tmpTextType);
        if (!string.IsNullOrWhiteSpace(text))
            return text;

        return GetFirstTextOfType(root, _uiTextType);
    }

    private string GetFirstTextOfType(object root, Type textType)
    {
        if (root == null || textType == null)
            return null;

        var method = root.GetType().GetMethod(
            "GetComponentsInChildren",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(Type), typeof(bool) },
            null);

        Array components = null;
        if (method != null)
        {
            try
            {
                components = method.Invoke(root, new object[] { textType, true }) as Array;
            }
            catch
            {
                components = null;
            }
        }

        if (components == null)
        {
            method = root.GetType().GetMethod(
                "GetComponentsInChildren",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(Type) },
                null);
            if (method == null)
                return null;

            try
            {
                components = method.Invoke(root, new object[] { textType }) as Array;
            }
            catch
            {
                return null;
            }
        }

        if (components == null)
            return null;

        foreach (var component in components)
        {
            if (component == null || !IsVisible(component))
                continue;

            var value = GetTextValue(component);
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    private object GetStaticInstance(string typeName)
    {
        var type = TypeResolver.Get(typeName);
        if (type == null)
            return null;

        var field = type.GetField("instance", BindingFlags.Public | BindingFlags.Static)
                    ?? type.GetField("instance", BindingFlags.NonPublic | BindingFlags.Static);

        return field?.GetValue(null);
    }

    private object GetStaticInstance(Type type)
    {
        if (type == null)
            return null;

        var field = type.GetField("instance", BindingFlags.Public | BindingFlags.Static)
                    ?? type.GetField("instance", BindingFlags.NonPublic | BindingFlags.Static);

        return field?.GetValue(null);
    }

    private IEnumerable<object> FindObjectsOfType(string typeName)
    {
        var type = TypeResolver.Get(typeName);
        if (type == null || _findObjectsOfTypeMethod == null)
            yield break;

        Array objects;
        try
        {
            objects = _findObjectsOfTypeMethod.Invoke(null, new object[] { type }) as Array;
        }
        catch
        {
            yield break;
        }

        if (objects == null)
            yield break;

        foreach (var obj in objects)
        {
            yield return obj;
        }
    }

    private IEnumerable<object> FindObjectsOfType(Type type)
    {
        if (type == null || _findObjectsOfTypeMethod == null)
            yield break;

        Array objects;
        try
        {
            objects = _findObjectsOfTypeMethod.Invoke(null, new object[] { type }) as Array;
        }
        catch
        {
            yield break;
        }

        if (objects == null)
            yield break;

        foreach (var obj in objects)
            yield return obj;
    }

    private void AnnounceAllTextComponents(bool force, bool includeInactive, bool ignoreVisibility)
    {
        foreach (var textType in new[] { _tmpTextType, _uiTextType })
        {
            if (textType == null)
                continue;

            var components = includeInactive ? FindObjectsOfTypeAll(textType) : FindObjectsOfType(textType);
            foreach (var component in components)
            {
                if (component == null)
                    continue;

                AnnounceComponent(component, null, force, ignoreVisibility);
            }
        }
    }

    private void AnnounceTextComponents(object root, HashSet<string> excludedNames, bool force)
    {
        if (root == null)
            return;

        if (_tmpTextType != null)
            AnnounceTextComponentsOfType(root, _tmpTextType, excludedNames, force);

        if (_uiTextType != null)
            AnnounceTextComponentsOfType(root, _uiTextType, excludedNames, force);
    }

    private void AnnounceNonSelectableTextComponents(object root, HashSet<string> excludedNames, bool force)
    {
        if (root == null)
            return;

        if (_tmpTextType != null)
            AnnounceTextComponentsOfTypeFiltered(root, _tmpTextType, excludedNames, force);

        if (_uiTextType != null)
            AnnounceTextComponentsOfTypeFiltered(root, _uiTextType, excludedNames, force);
    }

    private void AnnounceTextComponentsOfTypeFiltered(object root, Type textType, HashSet<string> excludedNames, bool force)
    {
        var method = root.GetType().GetMethod(
            "GetComponentsInChildren",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(Type), typeof(bool) },
            null);

        Array components = null;

        if (method != null)
        {
            try
            {
                components = method.Invoke(root, new object[] { textType, true }) as Array;
            }
            catch
            {
                components = null;
            }
        }

        if (components == null)
        {
            method = root.GetType().GetMethod(
                "GetComponentsInChildren",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(Type) },
                null);

            if (method == null)
                return;

            try
            {
                components = method.Invoke(root, new object[] { textType }) as Array;
            }
            catch
            {
                return;
            }
        }

        if (components == null)
            return;

        foreach (var component in components)
        {
            if (component == null)
                continue;

            if (IsComponentUnderSelectable(component))
                continue;

            AnnounceComponent(component, excludedNames, force);
        }
    }

    private void AnnounceTextComponentsOfType(object root, Type textType, HashSet<string> excludedNames, bool force)
    {
        var method = root.GetType().GetMethod(
            "GetComponentsInChildren",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(Type), typeof(bool) },
            null);

        Array components = null;

        if (method != null)
        {
            try
            {
                components = method.Invoke(root, new object[] { textType, true }) as Array;
            }
            catch
            {
                components = null;
            }
        }

        if (components == null)
        {
            method = root.GetType().GetMethod(
                "GetComponentsInChildren",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(Type) },
                null);

            if (method == null)
                return;

            try
            {
                components = method.Invoke(root, new object[] { textType }) as Array;
            }
            catch
            {
                return;
            }
        }

        if (components == null)
            return;

        foreach (var component in components)
        {
            AnnounceComponent(component, excludedNames, force);
        }
    }

    private void AnnounceComponent(object component, HashSet<string> excludedNames, bool force)
    {
        AnnounceComponent(component, excludedNames, force, false);
    }

    private void AnnounceComponent(object component, HashSet<string> excludedNames, bool force, bool ignoreVisibility)
    {
        if (component == null)
            return;

        if (!IsComponentInActiveScene(component))
            return;

        if (excludedNames != null)
        {
            var name = GetObjectName(component);
            if (!string.IsNullOrWhiteSpace(name) && excludedNames.Contains(name))
                return;
        }

        var text = GetTextValue(component);
        if (string.IsNullOrWhiteSpace(text))
            return;

        if (IsPlaceholderValue(text))
            return;

        if (text.IndexOf("shop item template", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            var fallback = GetComponentOrParentName(component);
            if (string.IsNullOrWhiteSpace(fallback))
                return;

            text = fallback;
        }

        if (IsElevatorSceneActive() && IsMoneyNotificationText(text) && !IsMoneyNotificationComponent(component))
            return;

        if (IsElevatorSceneActive() && IsMoneyNotificationText(text))
            return;

        if (IsMoneyNotificationComponent(component) || (IsElevatorSceneActive() && IsMoneyNotificationText(text)))
        {
            if (_suppressMoneyNotifications)
                return;

            var normalized = TextSanitizer.StripRichTextTags(text)?.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                return;

            if (!IsVisible(component))
            {
                _moneyNotifyVisible = false;
                return;
            }

            var now = Environment.TickCount;
            var changed = !string.Equals(_lastMoneyNotifyText, normalized, StringComparison.Ordinal);
            if (!_moneyNotifyVisible || changed)
            {
                if (now - _lastMoneyNotifyTick > 500)
                {
                    _moneyNotifyVisible = true;
                    _lastMoneyNotifyText = normalized;
                    _lastMoneyNotifyTick = now;
                    AnnounceContent(normalized, priority: false);
                }
            }
            return;
        }

        if (IsElevatorSceneActive() && IsLikelyValueText(text))
            return;

        if (!ignoreVisibility && !IsVisible(component))
            return;

        var holder = _lastByComponent.GetOrCreateValue(component);
        if (!force && holder.Text == text)
            return;

        holder.Text = text;
        AnnounceContent(text, priority: false);
    }

    private bool IsMoneyNotificationComponent(object component)
    {
        if (_moneyNotificationType == null || component == null)
            return false;

        if (component.GetType() == _moneyNotificationType)
            return true;

        if (ReflectionUtils.TryGetProperty(component.GetType(), component, "gameObject", out var gameObject) && gameObject != null)
        {
            var method = gameObject.GetType().GetMethod(
                "GetComponentInParent",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(Type), typeof(bool) },
                null);

            if (method != null)
            {
                var parent = method.Invoke(gameObject, new object[] { _moneyNotificationType, true });
                if (parent != null)
                    return true;
            }

            method = gameObject.GetType().GetMethod(
                "GetComponentInParent",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(Type) },
                null);

            return method?.Invoke(gameObject, new object[] { _moneyNotificationType }) != null;
        }

        return false;
    }

    private bool IsComponentInActiveScene(object component)
    {
        if (component == null)
            return false;

        if (!TryGetCurrentSceneName(out var sceneName) || string.IsNullOrWhiteSpace(sceneName))
            return true;

        try
        {
            if (!ReflectionUtils.TryGetProperty(component.GetType(), component, "gameObject", out var gameObject) || gameObject == null)
                return true;

            var sceneProp = gameObject.GetType().GetProperty("scene", BindingFlags.Instance | BindingFlags.Public);
            var scene = sceneProp?.GetValue(gameObject);
            if (scene == null)
                return true;

            var nameProp = scene.GetType().GetProperty("name", BindingFlags.Instance | BindingFlags.Public);
            var name = nameProp?.GetValue(scene) as string;
            if (string.IsNullOrWhiteSpace(name))
                return true;

            return string.Equals(name, sceneName, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return true;
        }
    }

    private bool IsMoneyNotificationText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var trimmed = text.Trim();
        if (trimmed.StartsWith("+", StringComparison.Ordinal) || trimmed.StartsWith("-", StringComparison.Ordinal))
            trimmed = trimmed.Substring(1).TrimStart();

        if (trimmed.Length == 0)
            return false;

        foreach (var ch in trimmed)
        {
            if (!char.IsDigit(ch))
                return false;
        }

        return true;
    }

    private void AnnounceContent(string text, bool priority)
    {
        if (string.IsNullOrWhiteSpace(text) || _screenreader == null)
            return;

        _screenreader.SuppressHoverFor(500);
        if (priority)
            _screenreader.AnnouncePriority(text);
        else
            _screenreader.Announce(text);
    }

    private string GetComponentOrParentName(object component)
    {
        var name = SanitizeShopName(GetObjectName(component));
        if (!string.IsNullOrWhiteSpace(name))
            return name;

        if (!ReflectionUtils.TryGetProperty(component.GetType(), component, "gameObject", out var gameObject) || gameObject == null)
            return null;

        name = SanitizeShopName(GetObjectName(gameObject));
        if (!string.IsNullOrWhiteSpace(name))
            return name;

        if (!ReflectionUtils.TryGetProperty(gameObject.GetType(), gameObject, "transform", out var transform) || transform == null)
            return null;

        if (!ReflectionUtils.TryGetProperty(transform.GetType(), transform, "parent", out var parent) || parent == null)
            return null;

        name = SanitizeShopName(GetObjectName(parent));
        if (!string.IsNullOrWhiteSpace(name))
            return name;

        return null;
    }

    private static string SanitizeShopName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var cleaned = TextSanitizer.RemoveInsensitive(name, "(Clone)");
        cleaned = TextSanitizer.RemoveInsensitive(cleaned, "shop item template");
        cleaned = cleaned.Trim();

        return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
    }

    private static object GetFieldValue(object instance, string fieldName)
    {
        if (instance == null)
            return null;

        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return field?.GetValue(instance);
    }

    private static object GetStaticMemberValue(Type type, string name)
    {
        if (type == null || string.IsNullOrWhiteSpace(name))
            return null;

        var prop = type.GetProperty(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop != null)
        {
            try
            {
                return prop.GetValue(null);
            }
            catch
            {
                return null;
            }
        }

        var field = type.GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (field == null)
            return null;

        try
        {
            return field.GetValue(null);
        }
        catch
        {
            return null;
        }
    }

    private static object GetSprite(object renderer)
    {
        if (renderer == null)
            return null;

        var type = renderer.GetType();
        var prop = type.GetProperty("sprite", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop == null)
            return null;

        try
        {
            return prop.GetValue(renderer);
        }
        catch
        {
            return null;
        }
    }

    private bool IsVoicePlaying(object root)
    {
        if (root == null || _audioSourceType == null)
            return false;

        try
        {
            var source = GetComponentFromRoot(root, _audioSourceType);
            if (source == null)
                return false;

            var prop = source.GetType().GetProperty("isPlaying", BindingFlags.Instance | BindingFlags.Public);
            var value = prop?.GetValue(source);
            return value is bool playing && playing;
        }
        catch
        {
            return false;
        }
    }

    private object GetComponentFromRoot(object root, Type componentType)
    {
        try
        {
            var method = root.GetType().GetMethod("GetComponent", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(Type) }, null);
            if (method != null)
            {
                var direct = method.Invoke(root, new object[] { componentType });
                if (direct != null)
                    return direct;
            }

            if (ReflectionUtils.TryGetProperty(root.GetType(), root, "gameObject", out var gameObject) && gameObject != null)
            {
                method = gameObject.GetType().GetMethod("GetComponent", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(Type) }, null);
                return method?.Invoke(gameObject, new object[] { componentType });
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private string GetObjectName(object instance)
    {
        if (instance == null)
            return null;

        var type = instance.GetType();
        var prop = type.GetProperty("name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop == null)
            return null;

        try
        {
            return prop.GetValue(instance) as string;
        }
        catch
        {
            return null;
        }
    }

    private bool IsPaperworkFocused(object paperwork)
    {
        var type = paperwork.GetType();
        var statusField = type.GetField("Status", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (statusField == null)
            return false;

        var statusValue = statusField.GetValue(paperwork);
        if (statusValue == null)
            return false;

        return string.Equals(statusValue.ToString(), "FOCUS", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsPaperworkFocusedForAnnouncement(object paperwork)
    {
        if (!IsRootActive(paperwork))
            return false;

        return IsPaperworkFocused(paperwork);
    }

    private bool IsChaosGlobeFocused(object globe)
    {
        if (!IsRootActive(globe))
            return false;

        var status = GetFieldValue(globe, "Status");
        if (status == null)
            return false;

        return string.Equals(status.ToString(), "FOCUS", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsDialogueFocused(object instance)
    {
        if (!IsRootActive(instance))
            return false;

        var type = instance.GetType();
        var fadingIn = type.GetField("bIsFadingIn", BindingFlags.Instance | BindingFlags.NonPublic);
        if (fadingIn != null && fadingIn.GetValue(instance) is bool inVal && inVal)
            return false;

        var fadingOut = type.GetField("bIsFadingOut", BindingFlags.Instance | BindingFlags.NonPublic);
        if (fadingOut != null && fadingOut.GetValue(instance) is bool outVal && outVal)
            return false;

        return true;
    }

    private bool IsLetterFocused(object instance)
    {
        if (!IsRootActive(instance))
            return false;

        var type = instance.GetType();
        var openField = type.GetField("bIsOpen", BindingFlags.Instance | BindingFlags.NonPublic);
        if (openField != null && openField.GetValue(instance) is bool openVal)
            return openVal;

        return true;
    }

    private bool IsHudFocused(object instance)
    {
        if (!IsRootActive(instance))
            return false;

        var panelHud = GetFieldValue(instance, "PanelHUD");
        if (panelHud != null && !IsGameObjectActive(panelHud))
            return false;

        return true;
    }

    private bool IsPhoneFocused(object instance)
    {
        if (!IsRootActive(instance))
            return false;

        var type = instance.GetType();
        var focusedField = type.GetField("bIsFocused", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (focusedField != null && focusedField.GetValue(instance) is bool focused)
            return focused;

        return false;
    }

    private bool IsVoteCounterFocused(object instance)
    {
        if (!IsRootActive(instance))
            return false;

        var bookOpen = GetFieldValue(instance, "BookOpen");
        if (bookOpen != null)
            return IsGameObjectActive(bookOpen);

        return true;
    }

    private void ProcessRoot(object root, Func<object, bool> isFocused, Action<bool> announceAction)
    {
        var state = _focusByRoot.GetOrCreateValue(root);
        var focused = isFocused(root);
        if (focused)
        {
            if (!state.IsFocused)
            {
                state.IsFocused = true;
                announceAction(true);
            }
            else
            {
                announceAction(false);
            }
        }
        else
        {
            state.IsFocused = false;
        }
    }

    private bool IsRootActive(object instance)
    {
        if (instance == null)
            return false;

        if (!ReflectionUtils.TryGetProperty(instance.GetType(), instance, "gameObject", out var gameObject))
            return true;

        return IsGameObjectActive(gameObject);
    }

    private bool IsComponentUnderSelectable(object component)
    {
        if (component == null || _selectableType == null)
            return false;

        if (!ReflectionUtils.TryGetProperty(component.GetType(), component, "gameObject", out var gameObject))
            return false;

        if (gameObject == null)
            return false;

        return GetComponentInParent(gameObject, _selectableType) != null;
    }

    private bool IsSelectableUsable(object gameObject)
    {
        if (gameObject == null || _selectableType == null)
            return true;

        var selectable = GetComponentInParent(gameObject, _selectableType);
        if (selectable == null)
            return true;

        try
        {
            var method = selectable.GetType().GetMethod("IsInteractable", BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
                return true;

            var result = method.Invoke(selectable, null);
            return result is bool interactable && interactable;
        }
        catch
        {
            return true;
        }
    }

    private object GetComponentInParent(object gameObject, Type type)
    {
        if (gameObject == null || type == null)
            return null;

        try
        {
            var method = gameObject.GetType().GetMethod(
                "GetComponentInParent",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(Type), typeof(bool) },
                null);

            if (method != null)
                return method.Invoke(gameObject, new object[] { type, true });

            method = gameObject.GetType().GetMethod(
                "GetComponentInParent",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(Type) },
                null);

            return method?.Invoke(gameObject, new object[] { type });
        }
        catch
        {
            return null;
        }
    }

    private bool IsGameObjectActive(object gameObject)
    {
        if (gameObject == null)
            return false;

        var goType = gameObject.GetType();
        return !ReflectionUtils.TryGetBoolProperty(goType, gameObject, "activeInHierarchy", out var active) || active;
    }

    private string GetTextValue(object instance)
    {
        if (instance == null)
            return null;

        var textProp = instance.GetType().GetProperty("text", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (textProp != null)
            return TextSanitizer.StripRichTextTags(textProp.GetValue(instance) as string);

        var textField = instance.GetType().GetField("m_text", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (textField != null)
            return TextSanitizer.StripRichTextTags(textField.GetValue(instance) as string);

        return null;
    }

    private bool IsVisible(object instance)
    {
        var type = instance.GetType();

        if (ReflectionUtils.TryGetBoolProperty(type, instance, "isActiveAndEnabled", out var isActiveAndEnabled)
            && !isActiveAndEnabled)
            return false;

        if (ReflectionUtils.TryGetBoolProperty(type, instance, "enabled", out var enabled)
            && !enabled)
            return false;

        if (ReflectionUtils.TryGetProperty(type, instance, "gameObject", out var gameObject))
        {
            var goType = gameObject.GetType();
            if (ReflectionUtils.TryGetBoolProperty(goType, gameObject, "activeInHierarchy", out var activeInHierarchy)
                && !activeInHierarchy)
                return false;
        }

        if (ReflectionUtils.TryGetProperty(type, instance, "canvas", out var canvas))
        {
            var canvasType = canvas.GetType();
            if (ReflectionUtils.TryGetBoolProperty(canvasType, canvas, "enabled", out var canvasEnabled)
                && !canvasEnabled)
                return false;
        }

        if (IsHiddenByCanvasGroup(instance))
            return false;

        if (ReflectionUtils.TryGetProperty(type, instance, "canvasRenderer", out var canvasRenderer))
        {
            var crType = canvasRenderer.GetType();
            if (ReflectionUtils.TryGetBoolProperty(crType, canvasRenderer, "cull", out var cull)
                && cull)
                return false;

            var getAlpha = crType.GetMethod("GetAlpha", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (getAlpha != null)
            {
                try
                {
                    var alphaObj = getAlpha.Invoke(canvasRenderer, null);
                    if (alphaObj is float alpha && alpha <= 0.01f)
                        return false;
                }
                catch { }
            }
        }

        if (TryGetFloatProperty(type, instance, "alpha", out var tmpAlpha) && tmpAlpha <= 0.01f)
            return false;

        if (ReflectionUtils.TryGetProperty(type, instance, "color", out var color))
        {
            var colorType = color.GetType();
            var aField = colorType.GetField("a");
            if (aField != null)
            {
                var aObj = aField.GetValue(color);
                if (aObj is float aValue && aValue <= 0.01f)
                    return false;
            }
        }

        return true;
    }

    private bool IsHiddenByCanvasGroup(object instance)
    {
        if (_canvasGroupType == null)
            return false;

        var componentType = instance.GetType();
        var getComponents = componentType.GetMethod("GetComponentsInParent",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(Type), typeof(bool) },
            null);

        if (getComponents == null)
            return false;

        object groupsObj;
        try
        {
            groupsObj = getComponents.Invoke(instance, new object[] { _canvasGroupType, true });
        }
        catch
        {
            return false;
        }

        if (groupsObj is Array groups)
        {
            foreach (var group in groups)
            {
                if (group == null)
                    continue;

                var groupType = group.GetType();
                if (TryGetFloatProperty(groupType, group, "alpha", out var alpha) && alpha <= 0.01f)
                    return true;
            }
        }

        return false;
    }

    private static bool TryGetBoolField(Type type, object instance, string name, out bool value)
    {
        value = false;
        var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field == null)
            return false;

        try
        {
            var obj = field.GetValue(instance);
            if (obj is bool boolValue)
            {
                value = boolValue;
                return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static bool TryGetFloatProperty(Type type, object instance, string name, out float value)
    {
        value = 0f;
        if (!ReflectionUtils.TryGetProperty(type, instance, name, out var obj))
            return false;

        if (obj is float floatValue)
        {
            value = floatValue;
            return true;
        }

        return false;
    }

    private IEnumerable<object> FindObjectsOfTypeAll(Type type)
    {
        if (type == null || _findObjectsOfTypeAllMethod == null)
            yield break;

        Array objects;
        try
        {
            objects = _findObjectsOfTypeAllMethod.Invoke(null, new object[] { type }) as Array;
        }
        catch
        {
            yield break;
        }

        if (objects == null)
            yield break;

        foreach (var obj in objects)
            yield return obj;
    }

    private static bool IsIntroOrComicScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return false;

        return sceneName.IndexOf("Intro", StringComparison.OrdinalIgnoreCase) >= 0
            || sceneName.IndexOf("Comic", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsComicScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return false;

        return sceneName.IndexOf("Comic", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string FormatNumber(object value)
    {
        if (value == null)
            return null;

        try
        {
            if (value is float f)
                return Math.Round(f).ToString();
            if (value is double d)
                return Math.Round(d).ToString();
            if (value is decimal m)
                return Math.Round(m).ToString();
            return value.ToString();
        }
        catch
        {
            return null;
        }
    }

    private sealed class LastTextHolder
    {
        public string Text;
    }


    private sealed class FocusState
    {
        public bool IsFocused;
        public string LastProfileText;
        public string LastPhoneText;
        public string LastLampResult;
        public string LastChaosGlobeScore;
        public string LastDialogPrompt;
        public string LastDialogOption;
        public string LastLetterText;
    }

    private sealed class CoinState
    {
        public bool Initialized;
        public bool WasSpinning;
        public object LastSprite;
        public string LastResult;
    }

    private sealed class LampState
    {
        public bool Initialized;
        public bool IsOn;
    }
}

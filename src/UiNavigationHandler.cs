namespace Death_and_Access;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Runtime.CompilerServices;

public sealed class UiNavigationHandler
{
    private const int AxisRepeatMs = 150;
    private const float AxisDeadzone = 0.5f;
    private const int PointerStep = 24;
    private Type _inputType;
    private Type _keyCodeType;
    private Type _textType;
    private Type _tmpTextType;
    private Type _tmpTextMeshType;
    private Type _interactableType;
    private Type _inputManagerType;
    private Type _hudManagerType;
    private Type _cameraType;
    private Type _screenType;
    private Type _sceneManagerType;
    private Type _sceneType;
    private Type _vector3Type;
    private Type _vector2Type;
    private Type _physics2DType;
    private Type _paperworkType;
    private Type _paperworkManagerType;
    private Type _grimDeskType;
    private Type _grimDeskDrawerType;
    private Type _deskItemType;
    private Type _shopItemType;
    private Type _shopType;
    private Type _shopManagerType;
    private Type _elevatorType;
    private Type _eSceneType;
    private Type _faxMachineType;
    private Type _spinnerType;
    private Type _catToyType;
    private Type _deskLampType;
    private Type _chaosGlobeType;
    private Type _phoneType;
    private Type _instructionsType;
    private Type _decisionCoinType;
    private Type _radioType;
    private Type _cactusType;
    private Type _markerType;
    private Type _eraserType;
    private Type _salaryCoinType;
    private Type _dialogueScreenType;
    private Type _speechBubbleManagerType;
    private Type _speechBubbleType;
    private Type _markConfirmType;
    private Type _faxConfirmType;
    private Type _buyConfirmType;
    private Type _restartConfirmType;
    private Type _endDayConfirmType;
    private Type _comicEndConfirmType;
    private Type _elevatorButtonType;
    private MethodInfo _getRayIntersectionMethod;
    private MethodInfo _getMousePositionMethod;
    private Type _elevatorManagerType;
    private Type _calendarType;
    private Type _saveManagerType;
    private Type _collider2DType;
    private Type _collider3DType;
    private Type _rendererType;
    private Type _eventSystemType;
    private Type _pointerEventDataType;
    private Type _raycastResultType;
    private Type _selectableType;
    private Type _canvasType;
    private Type _rectTransformUtilityType;
    private Type _canvasGroupType;
    private Type _rectTransformType;
    private Type _sliderType;
    private Type _componentType;
    private Type _optionsManagerType;
    private Type _introControllerType;
    private Type _skipIntroType;
    private Type _galleryScreenType;
    private Type _demoEndScreenType;
    private Type _comicManagerType;
    private Type _unityObjectType;
    private Type _gameObjectType;
    private Type _transformType;
    private Type _mirrorType;
    private Type _mirrorNavigationButtonType;
    private Type _canvasScalerType;
    private Type _graphicRaycasterType;
    private Type _imageType;
    private Type _spriteType;
    private Type _texture2DType;
    private Type _colorType;
    private Type _rectType;
    private Type _renderModeType;
    private Type _cursorType;
    private Type _articyGlobalVariablesType;
    private MethodInfo _getKeyDownMethod;
    private MethodInfo _getKeyMethod;
    private MethodInfo _getAxisRawMethod;
    private MethodInfo _getAxisMethod;
    private MethodInfo _findObjectsOfTypeMethod;
    private object _virtualCursorCanvas;
    private object _virtualCursorImage;
    private object _virtualCursorRectTransform;
    private object _virtualCursorTexture;
    private object _virtualCursorSprite;
    private float _virtualCursorTextureWidth;
    private float _virtualCursorTextureHeight;
    private (float x, float y)? _lastRawMousePos;
    private bool _virtualCursorUsesFallback;
    private bool _cursorHiddenForVirtual;
    private (float x, float y)? _pendingShortcutScreenPos;
    private int _nextKeyTick;
    private bool _axisUpHeld;
    private bool _axisDownHeld;
    private bool _axisLeftHeld;
    private bool _axisRightHeld;
    private bool _submitHeld;
    private object _lastFocusedInteractable;
    private object _lastEventSelected;
    private int _pendingEventSystemSyncUntil;
    private readonly Dictionary<string, object> _keyCodes = new(StringComparer.OrdinalIgnoreCase);
    private ScreenreaderProvider _screenreader;
    private int _lastAxisTick;
    private float _cachedAxisX;
    private float _cachedAxisY;
    private int _lastSelectableCacheTick;
    private List<object> _cachedSelectables;
    private int _keyboardNavUntilTick;
    private int _keyboardFocusUntilTick;
    private bool _keyboardFocusActive;
    private bool _virtualCursorActive;
    private bool _readingRawMousePosition;
    private (float x, float y) _virtualCursorPos;
    private bool _virtualCursorMovedSinceLastSubmit;
    private string _lastHoverAnnouncementSignature;
    private bool _suppressMouseUiSyncFromKeyboard;
    private string _lastFocusedTargetKey;
    private string _activeSceneName;
    private string _elevatorSceneName;
    private readonly HashSet<string> _allowedSceneNames = new(StringComparer.OrdinalIgnoreCase);
    private bool _sceneChanging;

    public void Initialize(ScreenreaderProvider screenreader)
    {
        Instance = this;
        _screenreader = screenreader;
    }

    internal static UiNavigationHandler Instance { get; private set; }

    internal bool IsVirtualCursorActive => _virtualCursorActive;
    internal bool ShouldBypassVirtualMousePositionPatch => _readingRawMousePosition;

    internal bool IsBarSceneActiveForAnnouncements => IsBarSceneActive();

    internal bool TryGetBarHoverNameFromVirtualCursor(out string text)
    {
        text = null;
        EnsureTypes();

        if (!IsBarSceneActive())
            return false;

        if (!TryGetVirtualCursorPosition(out var x, out var y))
            return false;

        text = GetSceneObjectNameAtScreenPosition2D(x, y);
        return !string.IsNullOrWhiteSpace(text);
    }

    internal bool TryGetBarHoverNameFromMouse(out string text)
    {
        text = null;
        EnsureTypes();

        if (!IsBarSceneActive())
            return false;

        var lastHit = GetInputManagerLastHit();
        if (lastHit != null)
        {
            var go = GetInteractableGameObject(lastHit) ?? GetMemberValue(lastHit, "gameObject");
            var name = GetMemberValue(go, "name")?.ToString();
            var sanitized = SanitizeSceneObjectName(name);
            if (!string.IsNullOrWhiteSpace(sanitized))
            {
                text = sanitized;
                return true;
            }
        }

        var raw = GetRawMousePosition();
        if (raw == null)
            return false;

        text = GetSceneObjectNameAtScreenPosition2D(raw.Value.x, raw.Value.y);
        return !string.IsNullOrWhiteSpace(text);
    }


    internal void SubmitInteractableFromVirtual()
    {
        SubmitInteractable();
    }

    internal static void ClearInstance()
    {
        Instance = null;
    }

    internal bool TryGetVirtualCursorPosition(out float x, out float y)
    {
        if (!_virtualCursorActive)
        {
            x = 0f;
            y = 0f;
            return false;
        }

        x = _virtualCursorPos.x;
        y = _virtualCursorPos.y;
        return true;
    }

    internal bool TryGetMouseHoverText(out string text)
    {
        text = null;
        EnsureTypes();

        object interactable = GetInputManagerLastHit();
        if (interactable == null)
        {
            var pointer = GetMousePosition();
            if (pointer == null)
                return false;

            interactable = GetInteractableAtScreenPosition(pointer.Value.x, pointer.Value.y);
        }

        if (interactable == null && IsShopActive())
        {
            var pointer = GetMousePosition();
            if (pointer != null)
                interactable = GetShopItemAtScreenPosition(pointer.Value.x, pointer.Value.y);
        }

        if (interactable == null)
            return false;

        var hoverText = GetHoverText(interactable);
        if (string.IsNullOrWhiteSpace(hoverText))
        {
            var shopItem = ResolveShopItem(interactable);
            if (shopItem != null)
                hoverText = GetHoverText(shopItem);
        }

        text = BuildHoverText(interactable, hoverText);
        return !string.IsNullOrWhiteSpace(text);
    }

    private bool IsBarSceneActive()
    {
        return !string.IsNullOrWhiteSpace(_elevatorSceneName)
               && _elevatorSceneName.IndexOf("Bar", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private string GetSceneObjectNameAtScreenPosition2D(float x, float y)
    {
        if (!TryGetSceneObjectNameAtScreenPosition2D(x, y, out var name))
            return null;

        return name;
    }

    private bool TryGetSceneObjectNameAtScreenPosition2D(float x, float y, out string name)
    {
        name = null;

        if (_cameraType == null || _physics2DType == null || _vector3Type == null)
            return false;

        var camera = GetMainCamera() ?? GetAnyCamera();
        if (camera == null)
            return false;

        try
        {
            var screenVector = Activator.CreateInstance(_vector3Type, x, y, 0f);
            var screenPointToRay = _cameraType.GetMethod("ScreenPointToRay", BindingFlags.Instance | BindingFlags.Public, null, new[] { _vector3Type }, null);
            if (screenPointToRay == null)
                return false;

            var ray = screenPointToRay.Invoke(camera, new[] { screenVector });
            if (ray == null)
                return false;

            var rayType = ray.GetType();
            var getRayIntersection = _physics2DType.GetMethod("GetRayIntersection", BindingFlags.Public | BindingFlags.Static, null, new[] { rayType, typeof(float) }, null)
                                   ?? _physics2DType.GetMethod("GetRayIntersection", BindingFlags.Public | BindingFlags.Static, null, new[] { rayType }, null);
            if (getRayIntersection == null)
                return false;

            var args = getRayIntersection.GetParameters().Length == 2
                ? new object[] { ray, float.PositiveInfinity }
                : new object[] { ray };
            var hit = getRayIntersection.Invoke(null, args);
            if (hit == null)
                return false;

            var colliderProp = hit.GetType().GetProperty("collider", BindingFlags.Instance | BindingFlags.Public);
            var collider = colliderProp?.GetValue(hit);
            if (collider == null)
                return false;

            var gameObjectProp = collider.GetType().GetProperty("gameObject", BindingFlags.Instance | BindingFlags.Public);
            var gameObject = gameObjectProp?.GetValue(collider);
            if (gameObject == null)
                return false;

            var rawName = GetMemberValue(gameObject, "name")?.ToString();
            if (string.IsNullOrWhiteSpace(rawName))
                return false;

            name = SanitizeSceneObjectName(rawName);
            return !string.IsNullOrWhiteSpace(name);
        }
        catch
        {
            return false;
        }
    }

    private static string SanitizeSceneObjectName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var cleaned = TextSanitizer.RemoveInsensitive(name, "(Clone)");
        cleaned = TextSanitizer.RemoveInsensitive(cleaned, "SpeechBubble");
        cleaned = TextSanitizer.RemoveInsensitive(cleaned, "Marker");
        cleaned = cleaned.Replace("_", " ").Replace("-", " ").Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
    }

    public void Update()
    {
        EnsureTypes();
        UpdateSceneFocusLock();

        if (IsSceneChanging())
            return;

        SyncEventSystemSelectionIfNeeded();
        EnsureDialogSelection();
        EnsureSpeechBubbleSelection();
        EnsureMenuSelection();
        if (TryHandleComicArrowScroll())
            return;
        if (HandleOfficeShortcuts())
            return;

        SampleAxes(out var axisX, out var axisY);
        if (IsMouseClickDown())
        {
            _keyboardFocusActive = false;
            _keyboardFocusUntilTick = 0;
            _suppressMouseUiSyncFromKeyboard = false;
        }
        var direction = GetNavigationDirection(axisX, axisY);
        var submitInteractPressed = IsSubmitPressedForInteractables();
        if (submitInteractPressed && TryHandleIntroSkip())
            return;
        if (direction != NavigationDirection.None)
        {
            _keyboardNavUntilTick = Environment.TickCount + 500;
            _keyboardFocusUntilTick = Environment.TickCount + 1500;
            _keyboardFocusActive = true;
            _suppressMouseUiSyncFromKeyboard = true;
        }

        if (TryAdjustFocusedSlider(direction))
            return;

        if (IsDialogActive() || IsSpeechBubbleDialogActive())
        {
            SyncUnifiedCursorForUi(preferKeyboardSelection: false);
            if (direction != NavigationDirection.None)
            {
                _suppressMouseUiSyncFromKeyboard = true;
                MoveUnifiedCursorByDirection(direction);
            }

            if (submitInteractPressed)
            {
                if (TrySubmitDialogueContinueOnly())
                    return;
                SubmitInteractable();
            }

            return;
        }

        if (IsMenuActive() && !IsOfficeActive())
        {
            SyncUnifiedCursorForUi(preferKeyboardSelection: false);
            if (direction != NavigationDirection.None)
            {
                _suppressMouseUiSyncFromKeyboard = true;
                if (!TryMoveMenuCursorToNearestOption(direction))
                    MoveUnifiedCursorByDirection(direction);
            }
            if (submitInteractPressed)
                SubmitInteractable();
            return;
        }

        if (direction != NavigationDirection.None
            && !IsOfficeActive()
            && !IsDressingRoomActive()
            && !_virtualCursorActive
            && ShouldDeferToEventSystem()
            && IsPointerOverUi())
        {
            _pendingEventSystemSyncUntil = Environment.TickCount + 250;
            if (TryMoveEventSystemSelection(direction))
                return;
            ClearUiSelection();
        }

        UpdateVirtualCursorFocus(direction, submitInteractPressed, axisX, axisY);
    }

    private bool TryHandleIntroSkip()
    {
        if (!IsIntroOrComicScene())
            return false;

        if (!IsSkipIntroAvailable())
            return false;

        return TriggerSkipIntro();
    }

    private bool TryHandleComicArrowScroll()
    {
        if (!IsComicSceneActive())
            return false;

        if (IsDialogActive() || IsSpeechBubbleDialogActive() || IsMenuActive())
            return false;

        if (GetKeyDown("Return") || GetKeyDown("KeypadEnter"))
            return TriggerSkipIntro();

        var leftPressed = GetKey("LeftArrow");
        var rightPressed = GetKey("RightArrow");
        if (!leftPressed && !rightPressed)
            return false;

        if (IsComicScrollLocked())
            return true;

        if (_comicManagerType == null)
            return false;

        var manager = GetStaticInstance(_comicManagerType);
        if (manager == null)
            return false;

        var velocity = 0f;
        if (leftPressed)
            velocity -= 0.2f;
        if (rightPressed)
            velocity += 0.2f;

        if (Math.Abs(velocity) < 0.0001f)
            return true;

        try
        {
            var method = _comicManagerType.GetMethod("AddCameraVelocity", BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
                return false;

            method.Invoke(manager, new object[] { velocity });
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool IsIntroOrComicScene()
    {
        if (_elevatorManagerType == null)
            return false;

        var instance = GetStaticInstance(_elevatorManagerType);
        if (instance == null)
            return false;

        try
        {
            var method = _elevatorManagerType.GetMethod("GetCurrentScene", BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
                return false;

            var scene = method.Invoke(instance, null);
            if (scene == null)
                return false;

            var sceneName = scene.ToString();
            return sceneName.IndexOf("Intro", StringComparison.OrdinalIgnoreCase) >= 0
                || sceneName.IndexOf("Comic", StringComparison.OrdinalIgnoreCase) >= 0;
        }
        catch
        {
            return false;
        }
    }

    private bool IsComicScrollLocked()
    {
        if (_inputManagerType == null)
            return false;

        var instance = GetStaticInstance(_inputManagerType);
        if (instance == null)
            return false;

        try
        {
            var field = _inputManagerType.GetField("bLockUntilInputEnd", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return field?.GetValue(instance) is bool locked && locked;
        }
        catch
        {
            return false;
        }
    }

    private bool IsComicSceneActive()
    {
        if (!string.IsNullOrWhiteSpace(_activeSceneName)
            && _activeSceneName.IndexOf("Comic", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        return !string.IsNullOrWhiteSpace(_elevatorSceneName)
               && _elevatorSceneName.IndexOf("Comic", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private bool IsSkipIntroAvailable()
    {
        if (_skipIntroType == null)
            return false;

        try
        {
            foreach (var skip in FindSceneObjectsOfType(_skipIntroType))
            {
                if (skip == null)
                    continue;

                var go = GetMemberValue(skip, "gameObject");
                if (go != null && !IsGameObjectActive(go))
                    continue;

                if (IsSceneChanging())
                    return false;

                if (IsInstanceActive(_introControllerType))
                    return false;

                if (IsInstanceActive(_galleryScreenType))
                    return false;

                if (IsInstanceActive(_optionsManagerType))
                    return false;

                return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private bool IsInstanceActive(Type type)
    {
        if (type == null)
            return false;

        var instance = GetStaticInstance(type);
        if (instance == null)
            return false;

        var go = GetMemberValue(instance, "gameObject");
        return go != null && IsGameObjectActive(go);
    }

    private bool TriggerSkipIntro()
    {
        if (_comicManagerType == null)
            return false;

        var instance = GetStaticInstance(_comicManagerType);
        if (instance == null)
            return false;

        try
        {
            var method = _comicManagerType.GetMethod("EndComic", BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
                return false;

            method.Invoke(instance, null);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void EnsureDialogSelection()
    {
        if (!IsDialogActive())
            return;

        var roots = GetActiveDialogRoots();
        var current = ResolveUiFocusTarget(GetCurrentSelectedGameObject());
        if (current != null && IsUnderDialogRoot(GetInteractableGameObject(current) ?? current, roots))
            return;

        var preferred = GetPreferredDialogSelectable();
        if (preferred == null)
        {
            preferred = GetFirstSelectableUnderRoots(roots);
        }

        if (preferred == null)
        {
            var selectables = GetDialogSelectables(requireScreen: true);
            if (selectables.Count == 0)
                selectables = GetDialogSelectables(requireScreen: false);
            if (selectables.Count > 0)
                preferred = selectables[0];
        }

        if (preferred == null)
            return;

        SetUiSelected(preferred);
        _lastEventSelected = GetInteractableGameObject(preferred) ?? preferred;
        SetInteractableFocus(preferred);
    }

    private void EnsureMenuSelection()
    {
        if (!IsMenuActive())
            return;

        var roots = GetActiveMenuRoots();
        var current = ResolveUiFocusTarget(GetCurrentSelectedGameObject());
        if (current != null && IsUnderDialogRoot(GetInteractableGameObject(current) ?? current, roots))
            return;

        var preferred = GetFirstSelectableUnderRoots(roots);
        if (preferred == null)
        {
            var selectables = GetMenuSelectables(requireScreen: true);
            if (selectables.Count == 0)
                selectables = GetMenuSelectables(requireScreen: false);
            if (selectables.Count > 0)
                preferred = selectables[0];
        }

        if (preferred == null)
            return;

        SetUiSelected(preferred);
        _lastEventSelected = GetInteractableGameObject(preferred) ?? preferred;
        SetInteractableFocus(preferred);
    }

    private void EnsureSpeechBubbleSelection()
    {
        if (!IsSpeechBubbleDialogActive())
            return;

        var current = ResolveUiFocusTarget(GetCurrentSelectedGameObject());
        if (current != null)
        {
            var currentGo = GetInteractableGameObject(current) ?? current;
            var existing = GetSpeechBubbleSelectables(requireScreen: false);
            foreach (var selectable in existing)
            {
                var selectableGo = GetInteractableGameObject(selectable) ?? GetMemberValue(selectable, "gameObject") ?? selectable;
                if (selectableGo != null && ReferenceEquals(selectableGo, currentGo))
                    return;
            }
        }

        var preferred = default(object);
        var selectables = GetSpeechBubbleSelectables(requireScreen: true);
        if (selectables.Count == 0)
            selectables = GetSpeechBubbleSelectables(requireScreen: false);
        if (selectables.Count > 0)
            preferred = selectables[0];

        if (preferred == null)
            return;

        SetUiSelected(preferred);
        _lastEventSelected = GetInteractableGameObject(preferred) ?? preferred;
        SetInteractableFocus(preferred);
    }

    private object GetPreferredDialogSelectable()
    {
        var instance = GetStaticInstance(_markConfirmType)
                       ?? GetStaticInstance(_faxConfirmType)
                       ?? GetStaticInstance(_buyConfirmType)
                       ?? GetStaticInstance(_restartConfirmType)
                       ?? GetStaticInstance(_endDayConfirmType)
                       ?? GetStaticInstance(_comicEndConfirmType);
        if (instance == null)
            return null;

        var yes = GetMemberValue(instance, "ButtonYes");
        if (IsSelectableActive(yes))
            return yes;

        var ok = GetMemberValue(instance, "ButtonOk");
        if (IsSelectableActive(ok))
            return ok;

        var no = GetMemberValue(instance, "ButtonNo");
        if (IsSelectableActive(no))
            return no;

        return null;
    }

    private bool TrySubmitDialogueContinueOnly()
    {
        if (_dialogueScreenType == null)
            return false;

        var dialogue = GetStaticInstance(_dialogueScreenType);
        if (!IsDialogueScreenActive(dialogue))
            return false;

        var choiceList = GetMemberValue(dialogue, "ButtonChoiceList");
        if (choiceList is System.Collections.IEnumerable choices)
        {
            foreach (var entry in choices)
            {
                if (entry == null)
                    continue;

                var choiceButton = GetMemberValue(entry, "ButtonReference") ?? entry;
                if (IsSelectableActive(choiceButton))
                    return false;
            }
        }

        var continueWrapper = GetMemberValue(dialogue, "ButtonContinue");
        if (continueWrapper == null)
            return false;

        var continueButton = GetMemberValue(continueWrapper, "ButtonReference") ?? continueWrapper;
        if (!IsSelectableActive(continueButton))
            return false;

        return ActivateInteractableWithFocus(continueButton);
    }

    private object GetFirstSelectableUnderRoots(List<object> roots)
    {
        if (roots == null || roots.Count == 0 || _selectableType == null)
            return null;

        foreach (var root in roots)
        {
            if (root == null)
                continue;

            var selectable = GetFirstComponentInChildren(root, _selectableType);
            if (selectable != null && IsSelectableActive(selectable))
                return selectable;
        }

        return null;
    }

    private object GetFirstComponentInChildren(object root, Type componentType)
    {
        if (root == null || componentType == null)
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
                components = method.Invoke(root, new object[] { componentType, true }) as Array;
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
                components = method.Invoke(root, new object[] { componentType }) as Array;
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

            var go = GetInteractableGameObject(component) ?? GetMemberValue(component, "gameObject");
            if (go != null && !IsGameObjectActive(go))
                continue;

            return component;
        }

        return null;
    }

    private bool IsSelectableActive(object selectable)
    {
        if (selectable == null)
            return false;

        var go = GetInteractableGameObject(selectable) ?? GetMemberValue(selectable, "gameObject");
        return go == null || IsGameObjectActive(go);
    }

    private bool HandleOfficeShortcuts()
    {
        if (IsDialogActive() || IsSpeechBubbleDialogActive())
        {
            if (TryHandleConfirmDialogYesNoShortcut())
                return true;
            if (TryHandleDialogNumberShortcut())
                return true;
            return false;
        }

        if (IsMenuActive())
            return false;

        if (IsDressingRoomActive())
        {
            if (TryHandleDressingRoomShortcuts())
                return true;
            if (TryHandleDressingRoomNumberShortcut())
                return true;
            return false;
        }

        if (!IsOfficeActive())
            return HandleNonOfficeShortcuts();

        if (TryHandlePaperworkShortcut())
            return true;

        if (GetKeyDown("M"))
            return ActivateStaticInteractable(_markerType);
        if (GetKeyDown("E"))
            return ActivateStaticInteractable(_eraserType);
        if (GetKeyDown("S"))
            return ActivateStaticInteractable(_spinnerType);
        if (GetKeyDown("T"))
            return ActivateStaticInteractable(_catToyType);
        if (GetKeyDown("L"))
            return ActivateStaticInteractable(_deskLampType);
        if (GetKeyDown("G"))
            return ActivateStaticInteractable(_chaosGlobeType);
        if (GetKeyDown("N"))
            return ActivateStaticInteractable(_phoneType);
        if (GetKeyDown("I"))
            return ActivateStaticInteractable(_instructionsType);
        if (GetKeyDown("C"))
            return ActivateStaticInteractable(_decisionCoinType);
        if (GetKeyDown("B"))
            return ActivatePiggyBank();
        if (GetKeyDown("R"))
            return ActivateStaticInteractable(_radioType);
        if (GetKeyDown("P"))
            return ActivateStaticInteractable(_cactusType);
        if (GetKeyDown("F"))
            return ActivateStaticInteractable(_faxMachineType);

        if (GetKeyDown("LeftBracket"))
            return ActivateDrawer("Left");
        if (GetKeyDown("RightBracket"))
            return ActivateDrawer("Right");

        return false;
    }

    private bool HandleNonOfficeShortcuts()
    {
        if (IsElevatorActive())
        {
            if (TryHandleElevatorShortcuts())
                return true;
        }

        if (IsBarSceneActive())
        {
            if (TryHandleBarNumberShortcut())
                return true;
        }

        if (IsShopSceneActive())
        {
            if (TryHandleShopNumberShortcut())
                return true;
        }

        if (GetKeyDown("E"))
            return ActivateElevatorReturn();

        if (GetKeyDown("M"))
            return AnnounceMoney();

        return false;
    }

    private bool TryHandleElevatorShortcuts()
    {
        if (TryHandleElevatorNumberShortcut())
            return true;

        if (GetKeyDown("S"))
            return ActivateElevatorButtonByHover("mortimer", "emporium", "imporium", "shop");
        if (GetKeyDown("F"))
            return ActivateElevatorButtonByHover("fate", "office");
        if (GetKeyDown("G"))
            return ActivateElevatorButtonByHover("grim", "desk");
        if (GetKeyDown("D"))
            return ActivateElevatorButtonByHover("dressing");
        if (GetKeyDown("Q"))
            return ActivateElevatorButtonByHover("quarters");
        if (GetKeyDown("B"))
            return ActivateElevatorButtonByHover("cerberus", "den");

        return false;
    }

    private bool TryHandleElevatorNumberShortcut()
    {
        var index = GetDialogNumberIndex();
        if (index < 0)
            return false;

        var button = GetElevatorButtonByIndex(index);
        return ActivateInteractableWithFocus(button);
    }

    private bool TryHandleShopNumberShortcut()
    {
        if (!IsShopSceneActive())
            return false;

        var items = GetShopItemsOrdered();
        return TryHandleNumberRowPointer(items);
    }

    private bool TryHandleNumberRowPointer(List<object> targets)
    {
        var index = GetDialogNumberIndex();
        if (index < 0)
            return false;

        var ordered = OrderTargetsByScreenPosition(targets, requireOnScreen: true);
        if (ordered.Count == 0 || index >= ordered.Count)
            return false;

        var target = ordered[index];
        if (!TryMoveCursorToInteractable(target))
            return false;

        var now = Environment.TickCount;
        _keyboardNavUntilTick = now + 500;
        _keyboardFocusUntilTick = now + 1500;
        _keyboardFocusActive = true;

        SetInteractableFocus(target);
        return true;
    }

    private object GetElevatorButtonByIndex(int index)
    {
        if (index < 0)
            return null;

        var elevatorType = TypeResolver.Get("Elevator");
        if (elevatorType == null)
            return null;

        var instance = GetStaticInstance(elevatorType);
        if (instance == null)
            return null;

        try
        {
            var field = elevatorType.GetField("ButtonList", BindingFlags.Instance | BindingFlags.NonPublic);
            var list = field?.GetValue(instance) as System.Collections.IList;
            if (list == null || index >= list.Count)
                return null;

            return list[index];
        }
        catch
        {
            return null;
        }
    }

    private bool ActivateElevatorButtonByHover(params string[] tokens)
    {
        var button = FindElevatorButtonByHover(tokens);
        return ActivateInteractableWithFocus(button);
    }

    private object FindElevatorButtonByHover(params string[] tokens)
    {
        if (_elevatorButtonType == null)
            return null;

        if (tokens == null || tokens.Length == 0)
            return null;

        foreach (var token in tokens)
        {
            if (string.IsNullOrWhiteSpace(token))
                continue;

            foreach (var button in FindSceneObjectsOfType(_elevatorButtonType))
            {
                if (button == null)
                    continue;

                var hoverText = GetHoverTextValue(button);
                if (string.IsNullOrWhiteSpace(hoverText))
                    continue;

                if (hoverText.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    return button;
            }
        }

        return null;
    }

    private bool TryHandleDialogNumberShortcut()
    {
        var index = GetDialogNumberIndex();
        if (index < 0)
            return false;

        var selectable = GetDialogSelectableByIndex(index);
        if (selectable == null)
            return false;

        return TryActivateDialogSelectable(selectable);
    }

    private bool TryHandleConfirmDialogYesNoShortcut()
    {
        var instance = GetActiveConfirmDialogInstance();
        if (instance == null)
            return false;

        if (IsFaxIncompleteConfirmState(instance))
        {
            if (!GetKeyDown("Return") && !GetKeyDown("KeypadEnter"))
                return false;

            var ok = GetMemberValue(instance, "ButtonOk");
            return TryActivateDialogSelectable(ok);
        }

        if (GetKeyDown("Y"))
        {
            var yes = GetMemberValue(instance, "ButtonYes");
            if (!IsSelectableActive(yes))
                yes = GetMemberValue(instance, "ButtonOk");

            return TryActivateDialogSelectable(yes);
        }

        if (GetKeyDown("N"))
        {
            var no = GetMemberValue(instance, "ButtonNo");
            if (!IsSelectableActive(no))
                no = GetMemberValue(instance, "ButtonCancel");
            if (!IsSelectableActive(no))
                no = GetMemberValue(instance, "ButtonOk");

            return TryActivateDialogSelectable(no);
        }

        return false;
    }

    private bool IsFaxIncompleteConfirmState(object instance)
    {
        if (instance == null || _faxConfirmType == null || !ReferenceEquals(instance.GetType(), _faxConfirmType))
            return false;

        var yes = GetMemberValue(instance, "ButtonYes");
        var no = GetMemberValue(instance, "ButtonNo");
        var ok = GetMemberValue(instance, "ButtonOk");

        return IsSelectableActive(ok) && !IsSelectableActive(yes) && !IsSelectableActive(no);
    }

    private bool TryActivateDialogSelectable(object selectable)
    {
        if (!IsSelectableActive(selectable))
            return false;

        SetUiSelected(selectable);
        _lastEventSelected = GetInteractableGameObject(selectable) ?? selectable;
        SetInteractableFocus(selectable);
        if (TryInvokeUiClickResult(selectable))
            return true;

        SubmitInteractable();
        return true;
    }

    private bool TryHandleDressingRoomNumberShortcut()
    {
        var targets = GetSceneNumberRowTargets();
        return TryHandleNumberRowFocus(targets);
    }

    private bool TryHandleBarNumberShortcut()
    {
        if (IsDialogActive() || IsSpeechBubbleDialogActive())
            return TryHandleDialogNumberShortcut();

        var targets = GetSceneNumberRowTargets();
        return TryHandleNumberRowFocus(targets);
    }

    private bool TryHandleNumberRowFocus(List<object> targets)
    {
        var index = GetDialogNumberIndex();
        if (index < 0)
            return false;

        var ordered = OrderTargetsByScreenPosition(targets, requireOnScreen: true);
        if (ordered.Count == 0 || index >= ordered.Count)
            return false;

        SetInteractableFocus(ordered[index]);
        return true;
    }

    private object GetActiveConfirmDialogInstance()
    {
        var types = new[]
        {
            _markConfirmType,
            _faxConfirmType,
            _buyConfirmType,
            _restartConfirmType,
            _endDayConfirmType,
            _comicEndConfirmType
        };

        foreach (var type in types)
        {
            if (type == null)
                continue;

            var instance = GetStaticInstance(type);
            if (instance == null)
                continue;

            var go = GetMemberValue(instance, "gameObject");
            if (go != null && IsGameObjectActive(go))
                return instance;
        }

        return null;
    }

    private List<object> GetSceneNumberRowTargets()
    {
        var selectables = GetEligibleSelectables(requireScreen: true);
        if (selectables.Count > 0)
            return selectables;

        var targets = new List<object>();
        if (_interactableType == null)
            return targets;

        foreach (var interactable in FindSceneObjectsOfType(_interactableType))
        {
            if (interactable == null)
                continue;
            if (!IsInAllowedScene(interactable))
                continue;
            if (!IsInteractableActive(interactable))
                continue;

            targets.Add(interactable);
        }

        return targets;
    }

    private List<object> OrderTargetsByScreenPosition(IEnumerable<object> targets, bool requireOnScreen)
    {
        var ordered = new List<object>();
        if (targets == null)
            return ordered;

        var width = GetScreenDimension("width");
        var height = GetScreenDimension("height");
        var seen = new HashSet<object>();
        var withPos = new List<(object target, float x, float y)>();

        foreach (var target in targets)
        {
            if (target == null || !seen.Add(target))
                continue;
            if (ShouldExcludeFromNumberRow(target))
                continue;

            var pos = GetInteractableScreenPosition(target);
            if (pos == null)
                continue;

            if (requireOnScreen && width > 0 && height > 0)
            {
                if (pos.Value.x < 0 || pos.Value.x > width || pos.Value.y < 0 || pos.Value.y > height)
                    continue;
            }

            withPos.Add((target, pos.Value.x, pos.Value.y));
        }

        withPos.Sort((left, right) =>
        {
            var cmpY = right.y.CompareTo(left.y);
            if (cmpY != 0)
                return cmpY;
            return left.x.CompareTo(right.x);
        });

        foreach (var item in withPos)
            ordered.Add(item.target);

        return ordered;
    }

    private bool ShouldExcludeFromNumberRow(object target)
    {
        if (target == null)
            return true;

        if (_elevatorButtonType != null && _elevatorButtonType.IsInstanceOfType(target))
            return true;

        var name = GetGameObjectName(target);
        if (!string.IsNullOrWhiteSpace(name) && name.IndexOf("elevator", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        var hover = GetHoverText(target);
        if (!string.IsNullOrWhiteSpace(hover) && hover.IndexOf("elevator", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        return false;
    }

    private bool TryHandleDressingRoomShortcuts()
    {
        if (!IsDressingRoomActive())
            return false;

        var shiftHeld = IsShiftHeld();
        if (GetKeyDown("H"))
            return ActivateDressingRoomNavigation("Head", previous: shiftHeld);
        if (GetKeyDown("B"))
            return ActivateDressingRoomNavigation("Body", previous: shiftHeld);
        if (GetKeyDown("M"))
            return ActivateDressingRoomMirror();

        return false;
    }

    private bool ActivateDressingRoomMirror()
    {
        if (_mirrorType == null)
            return false;

        var mirror = GetStaticInstance(_mirrorType);
        return ActivateInteractableWithFocus(mirror);
    }

    private bool ActivateDressingRoomNavigation(string accessoryType, bool previous)
    {
        if (string.IsNullOrWhiteSpace(accessoryType) || _mirrorNavigationButtonType == null)
            return false;

        var desiredDirection = previous ? "Left" : "Right";

        foreach (var button in FindSceneObjectsOfType(_mirrorNavigationButtonType))
        {
            if (button == null)
                continue;

            var directionValue = GetMemberValue(button, "Direction") ?? GetMemberValue(button, "direction");
            var typeValue = GetMemberValue(button, "Type") ?? GetMemberValue(button, "type");

            var direction = directionValue?.ToString();
            var type = typeValue?.ToString();

            if (string.IsNullOrWhiteSpace(direction) || string.IsNullOrWhiteSpace(type))
                continue;

            if (direction.IndexOf(desiredDirection, StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            if (!string.Equals(type, accessoryType, StringComparison.OrdinalIgnoreCase))
                continue;

            return ActivateInteractableWithFocus(button);
        }

        return false;
    }

    private bool ActivateElevatorReturn()
    {
        var back = FindElevatorButtonInScene("Elevator")
                   ?? FindInteractableByHoverTextContains("elevator")
                   ?? FindInteractableByNameContains("elevator")
                   ?? FindInteractableByHoverTextContains("back")
                   ?? FindInteractableByNameContains("ButtonElevator");
        if (back == null)
            back = GetElevatorButtonForScene("Elevator");
        if (back == null)
            return false;

        var result = ActivateInteractableWithFocus(back);
        var interactResult = TryInvokeInteract(back);
        if (!interactResult)
            TryInvokeUiClick(back);
        return result || interactResult;
    }

    private int GetDialogNumberIndex()
    {
        for (var i = 1; i <= 9; i++)
        {
            if (GetKeyDown("Alpha" + i))
                return i - 1;
        }

        if (GetKeyDown("Alpha0"))
            return 9;

        return -1;
    }

    private List<object> GetShopItemsOrdered()
    {
        var items = new List<object>();
        if (_shopItemType == null)
            return items;

        try
        {
            if (_shopManagerType != null)
            {
                var manager = GetStaticInstance(_shopManagerType);
                if (manager != null)
                {
                    var field = _shopManagerType.GetField("SpawnedItems", BindingFlags.Instance | BindingFlags.NonPublic);
                    var list = field?.GetValue(manager) as System.Collections.IEnumerable;
                    if (list != null)
                    {
                        foreach (var item in list)
                        {
                            if (item == null)
                                continue;
                            if (!IsInAllowedScene(item))
                                continue;
                            var go = GetGameObjectFromComponent(item) ?? GetMemberValue(item, "gameObject");
                            if (go != null && !IsGameObjectActive(go))
                                continue;
                            items.Add(item);
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore and fall back.
        }

        if (items.Count == 0 && _shopType != null)
        {
            try
            {
                var instanceField = _shopType.GetField("instance", BindingFlags.Public | BindingFlags.Static);
                var instance = instanceField?.GetValue(null);
                var shelf = instance != null ? GetMemberValue(instance, "Shelf") : null;
                if (shelf != null)
                {
                    var method = shelf.GetType().GetMethod(
                        "GetComponentsInChildren",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null,
                        new[] { typeof(Type), typeof(bool) },
                        null);

                    var results = method?.Invoke(shelf, new object[] { _shopItemType, true }) as Array;
                    if (results != null)
                    {
                        foreach (var item in results)
                        {
                            if (item == null)
                                continue;
                            if (!IsInAllowedScene(item))
                                continue;
                            var go = GetGameObjectFromComponent(item) ?? GetMemberValue(item, "gameObject");
                            if (go != null && !IsGameObjectActive(go))
                                continue;
                            items.Add(item);
                        }
                    }
                }
            }
            catch
            {
                // Ignore and fall back.
            }
        }

        if (items.Count == 0)
        {
            foreach (var item in FindSceneObjectsOfType(_shopItemType))
            {
                if (item == null)
                    continue;

                if (!IsInAllowedScene(item))
                    continue;

                var go = GetGameObjectFromComponent(item) ?? GetMemberValue(item, "gameObject");
                if (go != null && !IsGameObjectActive(go))
                    continue;

                items.Add(item);
            }
        }

        items.Sort((left, right) =>
        {
            var leftSlot = GetShopItemSlot(left);
            var rightSlot = GetShopItemSlot(right);
            var cmpSlot = leftSlot.CompareTo(rightSlot);
            if (cmpSlot != 0)
                return cmpSlot;

            var leftPos = GetTransformPosition3(GetGameObjectFromComponent(left) ?? GetMemberValue(left, "gameObject"));
            var rightPos = GetTransformPosition3(GetGameObjectFromComponent(right) ?? GetMemberValue(right, "gameObject"));
            var leftX = leftPos?.x ?? 0f;
            var rightX = rightPos?.x ?? 0f;
            var cmp = leftX.CompareTo(rightX);
            if (cmp != 0)
                return cmp;
            var leftY = leftPos?.y ?? 0f;
            var rightY = rightPos?.y ?? 0f;
            return rightY.CompareTo(leftY);
        });

        return items;
    }

    private object GetDialogSelectableByIndex(int index)
    {
        if (index < 0)
            return null;

        if (IsSpeechBubbleDialogActive())
        {
            var list = GetSpeechBubbleSelectables(requireScreen: true);
            if (list.Count == 0)
                list = GetSpeechBubbleSelectables(requireScreen: false);

            var ordered = OrderTargetsByScreenPosition(list, requireOnScreen: true);
            if (ordered.Count == 0)
                ordered = OrderTargetsByScreenPosition(list, requireOnScreen: false);

            if (ordered.Count == 0 || index >= ordered.Count)
                return null;

            return ordered[index];
        }

        var roots = GetActiveDialogRoots();
        if (roots.Count == 0)
            return null;

        var order = new List<object>();
        foreach (var root in roots)
        {
            var selectable = GetFirstComponentInChildren(root, _selectableType);
            if (selectable == null)
                continue;

            var components = GetComponentsInChildren(root, _selectableType);
            if (components == null)
                continue;

            foreach (var component in components)
            {
                if (component == null || !IsSelectableActive(component))
                    continue;

                order.Add(component);
            }
        }

        if (order.Count == 0 || index >= order.Count)
            return null;

        return order[index];
    }

    private bool AnnounceMoney()
    {
        if (_hudManagerType == null)
            return false;

        var moneyValue = GetCurrentMoneyValue();
        if (!string.IsNullOrWhiteSpace(moneyValue))
        {
            _screenreader?.Announce(moneyValue);
            return true;
        }

        var instance = GetStaticInstance(_hudManagerType);
        if (instance == null)
            return false;

        var textMoney = GetMemberValue(instance, "TextMoney");
        if (textMoney == null)
            return false;

        var text = GetComponentText(textMoney, _tmpTextType) ?? GetComponentText(textMoney, _textType);
        if (string.IsNullOrWhiteSpace(text))
            return false;

        text = TextSanitizer.StripRichTextTags(text);
        var cleaned = ExtractMoneyValue(text);
        _screenreader?.Announce(string.IsNullOrWhiteSpace(cleaned) ? text.Trim() : cleaned);
        return true;
    }

    private string GetCurrentMoneyValue()
    {
        if (_articyGlobalVariablesType == null)
            return null;

        try
        {
            var defaultProp = _articyGlobalVariablesType.GetProperty("Default", BindingFlags.Public | BindingFlags.Static);
            var defaults = defaultProp?.GetValue(null);
            if (defaults == null)
                return null;

            var inventory = GetMemberValue(defaults, "inventory");
            if (inventory == null)
                return null;

            var money = GetMemberValue(inventory, "money");
            if (money == null)
                return null;

            return money.ToString();
        }
        catch
        {
            return null;
        }
    }

    private string ExtractMoneyValue(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var normalized = text.Replace("\r", "").Replace("\n", " ").Trim();
        var eq = normalized.LastIndexOf('=');
        if (eq >= 0 && eq + 1 < normalized.Length)
            return normalized.Substring(eq + 1).Trim();

        var digits = new string(normalized.Where(char.IsDigit).ToArray());
        return string.IsNullOrWhiteSpace(digits) ? null : digits;
    }


    private bool TryHandlePaperworkShortcut()
    {
        var ctrlHeld = GetKey("LeftControl") || GetKey("RightControl");
        var cmdHeld = GetKey("LeftCommand") || GetKey("RightCommand");
        var altHeld = GetKey("LeftAlt") || GetKey("RightAlt") || GetKey("LeftOption") || GetKey("RightOption");
        var spareHeld = ctrlHeld || cmdHeld;
        for (var i = 1; i <= 9; i++)
        {
            if (GetKeyDown("Alpha" + i))
            {
                if (spareHeld || altHeld)
                    return TryMarkPaperworkByIndex(i - 1, live: spareHeld && !altHeld);
                if (!TrySetPaperworkShortcutScreenPos(i - 1))
                    return false;
                var paperwork = GetPaperworkByIndex(i - 1);
                return ActivateInteractableWithFocus(paperwork);
            }
        }

        if (GetKeyDown("Alpha0"))
        {
            if (spareHeld || altHeld)
                return TryMarkPaperworkByIndex(9, live: spareHeld && !altHeld);
            if (!TrySetPaperworkShortcutScreenPos(9))
                return false;
            var paperwork = GetPaperworkByIndex(9);
            return ActivateInteractableWithFocus(paperwork);
        }

        return false;
    }

    private bool ActivateStaticInteractable(Type type)
    {
        if (type == null)
            return false;

        var instance = GetStaticInstance(type);
        return ActivateInteractableWithFocus(instance);
    }

    private bool ActivateDrawer(string side)
    {
        if (_grimDeskDrawerType == null || string.IsNullOrWhiteSpace(side))
            return false;

        var enumType = TypeResolver.Get("ELeftOrRight");
        if (enumType == null)
            return false;

        try
        {
            var value = Enum.Parse(enumType, side, ignoreCase: true);
            var drawer = GetDrawerByType(value) ?? GetGrimDeskDrawerByField(value);
            if (drawer == null)
                return false;

            if (!TrySetDrawerShortcutScreenPos(drawer))
            {
                return false;
            }
            return ActivateInteractableWithFocus(drawer);
        }
        catch
        {
            return false;
        }
    }

    private bool TryFindSelectableLinear(NavigationDirection direction, out object selectable)
    {
        selectable = null;
        if (_selectableType == null)
            return false;

        var strictCandidates = GetEligibleSelectables(requireScreen: true);
        var list = strictCandidates.Count > 0 ? strictCandidates : GetEligibleSelectables(requireScreen: false);
        if (list.Count == 0)
            return false;

        var current = GetCurrentSelectedGameObject();
        var currentSelectable = current != null ? GetComponentByType(current, _selectableType) : null;

        var ordered = new List<object>(list.Count);
        ordered.AddRange(list);
        ordered.Sort((left, right) =>
        {
            var leftGo = GetInteractableGameObject(left);
            var rightGo = GetInteractableGameObject(right);
            var leftKey = leftGo != null ? GetHierarchyOrderKey(leftGo) : null;
            var rightKey = rightGo != null ? GetHierarchyOrderKey(rightGo) : null;
            return string.Compare(leftKey ?? string.Empty, rightKey ?? string.Empty, StringComparison.Ordinal);
        });

        var index = currentSelectable != null ? ordered.IndexOf(currentSelectable) : 0;
        if (index < 0)
            index = 0;

        var forward = direction == NavigationDirection.Down || direction == NavigationDirection.Right;
        index = forward ? (index + 1) % ordered.Count : (index - 1 + ordered.Count) % ordered.Count;
        selectable = ordered[index];
        return selectable != null;
    }

    private bool ActivatePiggyBank()
    {
        var oinkBank = FindInteractableByHoverTextContains("oink bank")
                       ?? FindInteractableByNameContains("oink")
                       ?? FindInteractableByNameContains("bank")
                       ?? GetStaticInstance(_salaryCoinType);
        return ActivateInteractableWithFocus(oinkBank);
    }

    private object FindInteractableByNameContains(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || _interactableType == null)
            return null;

        foreach (var interactable in FindSceneObjectsOfType(_interactableType))
        {
            var name = GetGameObjectName(interactable);
            if (string.IsNullOrWhiteSpace(name))
                continue;

            if (name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                return interactable;
        }

        return null;
    }

    private object FindInteractableByHoverTextContains(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || _interactableType == null)
            return null;

        foreach (var interactable in FindSceneObjectsOfType(_interactableType))
        {
            var hoverText = GetHoverTextValue(interactable);
            if (string.IsNullOrWhiteSpace(hoverText))
                continue;

            if (hoverText.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                return interactable;
        }

        return null;
    }

    private string GetHoverTextValue(object interactable)
    {
        if (interactable == null)
            return null;

        try
        {
            var method = interactable.GetType().GetMethod("GetHoverText", BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
                return null;

            var result = method.Invoke(interactable, null) as string;
            return SanitizeHoverText(result);
        }
        catch
        {
            return null;
        }
    }

    private bool TryMarkPaperworkByIndex(int index, bool live)
    {
        if (!IsMarkerHeld())
            return false;

        var paperwork = GetPaperworkByIndex(index);
        if (paperwork == null)
            return false;

        var mark = GetMemberValue(paperwork, live ? "MarkLive" : "MarkDie");
        return TriggerInteractable(mark);
    }

    private bool IsMarkerHeld()
    {
        if (_markerType == null)
            return false;

        var instance = GetStaticInstance(_markerType);
        if (instance == null)
            return false;

        try
        {
            var method = _markerType.GetMethod("IsPickedUp", BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
                return false;

            var result = method.Invoke(instance, null);
            return result is bool held && held;
        }
        catch
        {
            return false;
        }
    }

    private object GetPaperworkByIndex(int index)
    {
        if (index < 0)
            return null;

        var list = GetPaperworkList();
        if (list == null || index >= list.Count)
            return null;

        return list[index];
    }

    private System.Collections.IList GetPaperworkList()
    {
        if (_paperworkManagerType == null)
            return null;

        var instance = GetStaticInstance(_paperworkManagerType);
        if (instance == null)
            return null;

        try
        {
            var field = _paperworkManagerType.GetField("PaperworkList", BindingFlags.Instance | BindingFlags.Public);
            return field?.GetValue(instance) as System.Collections.IList;
        }
        catch
        {
            return null;
        }
    }

    private bool ActivateInteractableWithFocus(object interactable)
    {
        if (interactable == null)
            return false;

        interactable = ResolveInteractableForFocus(interactable);
        if (interactable == null)
            return false;

        if (!CanActivateInteractableWithShortcut(interactable))
            return false;

        if (!TryMoveCursorToInteractable(interactable))
            return false;

        var now = Environment.TickCount;
        _keyboardNavUntilTick = now + 500;
        _keyboardFocusUntilTick = now + 1500;
        _keyboardFocusActive = true;

        SetInteractableFocus(interactable);
        if (TryErasePaperworkMark(interactable))
            return true;
        if (!TryInvokeInteract(interactable))
            TryInvokeUiClick(interactable);
        TryAnnounceDressingRoomSelection(interactable);
        return true;
    }

    private bool TryMoveCursorToInteractable(object interactable)
    {
        interactable = ResolveInteractableForFocus(interactable);
        if (interactable == null)
            return false;

        var screen = _pendingShortcutScreenPos ?? GetInteractableScreenPosition(interactable);
        _pendingShortcutScreenPos = null;
        if (screen == null)
            return false;

        _virtualCursorPos = screen.Value;
        _virtualCursorActive = true;
        EnsureVirtualCursorOverlay();
        SetSystemCursorVisible(false);
        UpdateVirtualCursorOverlayPosition(screen.Value.x, screen.Value.y);
        RefreshCursorSpriteIfNeeded();
        return true;
    }

    private bool TrySetPaperworkShortcutScreenPos(int index)
    {
        var pos = GetPaperworkShortcutScreenPos(index);
        if (pos == null)
            return false;

        _pendingShortcutScreenPos = pos;
        return true;
    }

    private (float x, float y)? GetPaperworkShortcutScreenPos(int index)
    {
        if (_grimDeskType == null || index < 0)
            return null;

        var desk = GetStaticInstance(_grimDeskType);
        if (desk == null)
            return null;

        var marker = GetMemberValue(desk, "PaperWorkSpawnMarker");
        if (marker == null)
            return null;

        var markerPos = GetTransformPosition3(marker);
        if (markerPos == null)
            return null;

        var offsetX = 1.0f * (index % 4);
        var offsetY = 0.3f * (index % 4) - 0.6f * (float)Math.Floor(index / 4.0);
        var offsetZ = (-index * 2f) - 3f;
        var world = (x: markerPos.Value.x + offsetX, y: markerPos.Value.y + offsetY, z: markerPos.Value.z + offsetZ);

        var screen = WorldToScreenPoint(world) ?? WorldToScreenPointWithCamera(world);
        if (screen != null)
            return screen;

        var width = GetScreenDimension("width");
        var height = GetScreenDimension("height");
        return ToScreenPosition((world.x, world.y), width, height);
    }

    private bool TrySetDrawerShortcutScreenPos(object drawer)
    {
        if (drawer == null)
            return false;

        var center = GetMemberValue(drawer, "Collider");
        var bounds = GetColliderBoundsCenter(center);
        if (bounds != null)
        {
            var screen = WorldToScreenPoint(bounds.Value) ?? WorldToScreenPointWithCamera((bounds.Value.x, bounds.Value.y, 0f));
            if (screen != null)
            {
                _pendingShortcutScreenPos = screen;
                return true;
            }
        }

        var pos = GetTransformPosition3(GetInteractableGameObject(drawer) ?? drawer);
        if (pos != null)
        {
            var screen = WorldToScreenPoint(pos.Value) ?? WorldToScreenPointWithCamera(pos.Value);
            if (screen != null)
            {
                _pendingShortcutScreenPos = screen;
                return true;
            }
        }

        var width = GetScreenDimension("width");
        var height = GetScreenDimension("height");
        if (width > 0 && height > 0)
        {
            var isLeft = string.Equals(drawer.GetType().GetProperty("Type")?.GetValue(drawer)?.ToString(), "Left", StringComparison.OrdinalIgnoreCase);
            var fallbackX = isLeft ? width * 0.25f : width * 0.75f;
            var fallbackY = height * 0.5f;
            _pendingShortcutScreenPos = (fallbackX, fallbackY);
            return true;
        }

        return false;
    }

    private object GetGrimDeskDrawerByField(object drawerTypeValue)
    {
        if (_grimDeskType == null || drawerTypeValue == null)
            return null;

        var desk = GetStaticInstance(_grimDeskType);
        if (desk == null)
            return null;

        var name = drawerTypeValue.ToString();
        if (string.Equals(name, "Left", StringComparison.OrdinalIgnoreCase))
            return GetMemberValue(desk, "DrawerLeft");
        if (string.Equals(name, "Right", StringComparison.OrdinalIgnoreCase))
            return GetMemberValue(desk, "DrawerRight");

        return null;
    }

    private bool CanActivateInteractableWithShortcut(object interactable)
    {
        if (interactable == null)
            return false;

        if (IsShopItem(interactable) && !IsShopItemOwned(interactable) && !IsShopSceneActive())
            return false;

        return true;
    }

    private bool TriggerInteractable(object interactable)
    {
        if (interactable == null)
            return false;

        if (!TryInvokeInteract(interactable))
            TryInvokeUiClick(interactable);
        return true;
    }

    private bool IsOfficeActive()
    {
        if (_grimDeskType == null)
            return false;

        var instance = GetStaticInstance(_grimDeskType);
        if (instance == null)
            return false;

        var gameObject = GetMemberValue(instance, "gameObject");
        return gameObject != null && IsGameObjectActive(gameObject);
    }

    private bool IsElevatorActive()
    {
        if (_elevatorManagerType == null)
            return false;

        var instance = GetStaticInstance(_elevatorManagerType);
        if (instance == null)
            return false;

        try
        {
            var method = _elevatorManagerType.GetMethod("GetCurrentScene", BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
                return false;

            var scene = method.Invoke(instance, null);
            return scene != null && string.Equals(scene.ToString(), "Elevator", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private bool IsDressingRoomActive()
    {
        if (_elevatorManagerType == null)
            return false;

        var instance = GetStaticInstance(_elevatorManagerType);
        if (instance == null)
            return false;

        try
        {
            var method = _elevatorManagerType.GetMethod("GetCurrentScene", BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
                return false;

            var scene = method.Invoke(instance, null);
            return scene != null && string.Equals(scene.ToString(), "DressingRoom", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private bool IsShopSceneActive()
    {
        if (_elevatorManagerType == null)
            return false;

        var instance = GetStaticInstance(_elevatorManagerType);
        if (instance == null)
            return false;

        try
        {
            var method = _elevatorManagerType.GetMethod("GetCurrentScene", BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
                return false;

            var scene = method.Invoke(instance, null);
            return scene != null && string.Equals(scene.ToString(), "Shop", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private object GetElevatorButtonForScene(string sceneName)
    {
        if (_elevatorType == null || _eSceneType == null || string.IsNullOrWhiteSpace(sceneName))
            return null;

        try
        {
            var instanceField = _elevatorType.GetField("instance", BindingFlags.Public | BindingFlags.Static);
            var instance = instanceField?.GetValue(null);
            if (instance == null)
                return null;

            object sceneValue;
            try
            {
                sceneValue = Enum.Parse(_eSceneType, sceneName);
            }
            catch
            {
                return null;
            }

            var method = _elevatorType.GetMethod("GetElevatorButtonBySceneType", BindingFlags.Instance | BindingFlags.Public);
            return method?.Invoke(instance, new[] { sceneValue });
        }
        catch
        {
            return null;
        }
    }

    private object FindElevatorButtonInScene(string sceneName)
    {
        if (_elevatorButtonType == null || _eSceneType == null || string.IsNullOrWhiteSpace(sceneName))
            return null;

        object sceneValue;
        try
        {
            sceneValue = Enum.Parse(_eSceneType, sceneName);
        }
        catch
        {
            return null;
        }

        foreach (var button in FindSceneObjectsOfType(_elevatorButtonType))
        {
            if (button == null)
                continue;

            if (!IsInAllowedScene(button))
                continue;

            var go = GetInteractableGameObject(button) ?? GetMemberValue(button, "gameObject");
            if (go != null && !IsGameObjectActive(go))
                continue;

            var dest = GetMemberValue(button, "DestinationScene");
            if (dest != null && dest.Equals(sceneValue))
                return button;
        }

        return null;
    }

    private bool IsDialogActive()
    {
        return GetActiveDialogRoots().Count > 0;
    }

    private bool IsSpeechBubbleDialogActive()
    {
        if (_speechBubbleManagerType == null)
            return false;

        var manager = GetStaticInstance(_speechBubbleManagerType);
        if (manager == null)
            return false;

        try
        {
            var method = _speechBubbleManagerType.GetMethod("IsBubbleSpeechActive", BindingFlags.Instance | BindingFlags.Public);
            if (method != null)
            {
                var activeObj = method.Invoke(manager, null);
                _ = activeObj;
            }

            var spawned = GetMemberValue(manager, "SpawnedBubbles") as System.Collections.IEnumerable;
            if (spawned == null)
                return false;

            foreach (var entry in spawned)
            {
                if (entry == null)
                    continue;

                if (IsSpeakerSpeechBubble(entry))
                    continue;

                var button = GetMemberValue(entry, "ButtonSpeechBubble") ?? entry;
                if (IsSelectableActive(button))
                    return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private bool IsMenuActive()
    {
        return GetActiveMenuRoots().Count > 0;
    }

    private List<object> GetActiveDialogRoots()
    {
        var roots = new List<object>();

        var dialogue = GetStaticInstance(_dialogueScreenType);
        if (IsDialogueScreenActive(dialogue))
        {
            var go = GetMemberValue(dialogue, "gameObject");
            if (go != null)
                roots.Add(go);
        }

        AddDialogRootIfActive(_markConfirmType, roots);
        AddDialogRootIfActive(_faxConfirmType, roots);
        AddDialogRootIfActive(_buyConfirmType, roots);
        AddDialogRootIfActive(_restartConfirmType, roots);
        AddDialogRootIfActive(_endDayConfirmType, roots);
        AddDialogRootIfActive(_comicEndConfirmType, roots);

        return roots;
    }

    private List<object> GetActiveMenuRoots()
    {
        var roots = new List<object>();
        AddMenuRootIfActive(_introControllerType, roots);
        AddMenuRootIfActive(_optionsManagerType, roots);
        AddMenuRootIfActive(_galleryScreenType, roots);
        AddMenuRootIfActive(_demoEndScreenType, roots);
        return roots;
    }

    private void AddMenuRootIfActive(Type type, List<object> roots)
    {
        if (type == null || roots == null)
            return;

        var instance = GetStaticInstance(type);
        if (instance == null)
            return;

        var go = GetMemberValue(instance, "gameObject");
        if (go != null && IsGameObjectActive(go))
            roots.Add(go);
    }

    private void AddDialogRootIfActive(Type type, List<object> roots)
    {
        if (type == null || roots == null)
            return;

        var instance = GetStaticInstance(type);
        if (instance == null)
            return;

        var go = GetMemberValue(instance, "gameObject");
        if (go != null && IsGameObjectActive(go))
            roots.Add(go);
    }

    private bool IsDialogueScreenActive(object instance)
    {
        if (instance == null)
            return false;

        var go = GetMemberValue(instance, "gameObject");
        if (go == null || !IsGameObjectActive(go))
            return false;

        try
        {
            var type = instance.GetType();
            var fadingIn = type.GetField("bIsFadingIn", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fadingIn != null && fadingIn.GetValue(instance) is bool inVal && inVal)
                return false;

            var fadingOut = type.GetField("bIsFadingOut", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fadingOut != null && fadingOut.GetValue(instance) is bool outVal && outVal)
                return false;
        }
        catch
        {
            return false;
        }

        return true;
    }

    private bool IsUnderDialogRoot(object gameObject, List<object> roots)
    {
        if (gameObject == null || roots == null || roots.Count == 0)
            return false;

        var transform = GetTransform(gameObject);
        if (transform == null)
            return false;

        foreach (var root in roots)
        {
            var rootTransform = GetTransform(root);
            if (rootTransform == null)
                continue;

            if (ReferenceEquals(rootTransform, transform))
                return true;

            if (IsTransformChildOf(transform, rootTransform))
                return true;
        }

        return false;
    }

    private void EnsureTypes()
    {
        _inputType ??= TypeResolver.Get("UnityEngine.Input");
        _keyCodeType ??= TypeResolver.Get("UnityEngine.KeyCode");
        _textType ??= TypeResolver.Get("UnityEngine.UI.Text");
        _tmpTextType ??= TypeResolver.Get("TMPro.TextMeshProUGUI");
        _tmpTextMeshType ??= TypeResolver.Get("TMPro.TextMeshPro");
        _interactableType ??= TypeResolver.Get("Interactable");
        _inputManagerType ??= TypeResolver.Get("InputManager");
        _hudManagerType ??= TypeResolver.Get("HUDManager");
        _cameraType ??= TypeResolver.Get("UnityEngine.Camera");
        _screenType ??= TypeResolver.Get("UnityEngine.Screen");
        _sceneManagerType ??= TypeResolver.Get("UnityEngine.SceneManagement.SceneManager");
        _sceneType ??= TypeResolver.Get("UnityEngine.SceneManagement.Scene");
        _vector3Type ??= TypeResolver.Get("UnityEngine.Vector3");
        _vector2Type ??= TypeResolver.Get("UnityEngine.Vector2");
        _physics2DType ??= TypeResolver.Get("UnityEngine.Physics2D");
        _paperworkType ??= TypeResolver.Get("Paperwork");
        _paperworkManagerType ??= TypeResolver.Get("PaperworkManager");
        _grimDeskType ??= TypeResolver.Get("GrimDesk");
        _grimDeskDrawerType ??= TypeResolver.Get("GrimDeskDrawer");
        _deskItemType ??= TypeResolver.Get("DeskItem");
        _shopItemType ??= TypeResolver.Get("ShopItem");
        _shopType ??= TypeResolver.Get("Shop");
        _shopManagerType ??= TypeResolver.Get("ShopManager");
        _elevatorType ??= TypeResolver.Get("Elevator");
        _eSceneType ??= TypeResolver.Get("EScene");
        _faxMachineType ??= TypeResolver.Get("FaxMachine");
        _spinnerType ??= TypeResolver.Get("Spinner");
        _catToyType ??= TypeResolver.Get("CatToy");
        _deskLampType ??= TypeResolver.Get("DeskLamp");
        _chaosGlobeType ??= TypeResolver.Get("ChaosGlobe");
        _phoneType ??= TypeResolver.Get("Phone");
        _instructionsType ??= TypeResolver.Get("LetterOfFate");
        _decisionCoinType ??= TypeResolver.Get("DecisionCoin");
        _radioType ??= TypeResolver.Get("Radio");
        _cactusType ??= TypeResolver.Get("Cactus");
        _markerType ??= TypeResolver.Get("MarkerOfDeath");
        _eraserType ??= TypeResolver.Get("Eraser");
        _salaryCoinType ??= TypeResolver.Get("SalaryCoin");
        _dialogueScreenType ??= TypeResolver.Get("DialogueScreen");
        _speechBubbleManagerType ??= TypeResolver.Get("SpeechBubbleManager");
        _speechBubbleType ??= TypeResolver.Get("SpeechBubble");
        _markConfirmType ??= TypeResolver.Get("MarkConfirm");
        _faxConfirmType ??= TypeResolver.Get("FaxConfirm");
        _buyConfirmType ??= TypeResolver.Get("BuyConfirm");
        _restartConfirmType ??= TypeResolver.Get("RestartConfirm");
        _endDayConfirmType ??= TypeResolver.Get("EndDayConfirm");
        _comicEndConfirmType ??= TypeResolver.Get("ComicEndConfirm");
        _elevatorButtonType ??= TypeResolver.Get("ElevatorButton");
        _elevatorManagerType ??= TypeResolver.Get("ElevatorManager");
        _calendarType ??= TypeResolver.Get("Calendar");
        _saveManagerType ??= TypeResolver.Get("SaveManager");
        _collider2DType ??= TypeResolver.Get("UnityEngine.Collider2D");
        _collider3DType ??= TypeResolver.Get("UnityEngine.Collider");
        _rendererType ??= TypeResolver.Get("UnityEngine.Renderer");
        _eventSystemType ??= TypeResolver.Get("UnityEngine.EventSystems.EventSystem");
        _pointerEventDataType ??= TypeResolver.Get("UnityEngine.EventSystems.PointerEventData");
        _raycastResultType ??= TypeResolver.Get("UnityEngine.EventSystems.RaycastResult");
        _selectableType ??= TypeResolver.Get("UnityEngine.UI.Selectable");
        _canvasType ??= TypeResolver.Get("UnityEngine.Canvas");
        _rectTransformUtilityType ??= TypeResolver.Get("UnityEngine.RectTransformUtility");
        _canvasGroupType ??= TypeResolver.Get("UnityEngine.CanvasGroup");
        _rectTransformType ??= TypeResolver.Get("UnityEngine.RectTransform");
        _sliderType ??= TypeResolver.Get("UnityEngine.UI.Slider");
        _componentType ??= TypeResolver.Get("UnityEngine.Component");
        _optionsManagerType ??= TypeResolver.Get("OptionsManager");
        _introControllerType ??= TypeResolver.Get("IntroController");
        _skipIntroType ??= TypeResolver.Get("SkipIntro");
        _galleryScreenType ??= TypeResolver.Get("GalleryScreen");
        _demoEndScreenType ??= TypeResolver.Get("DemoEndScreen");
        _articyGlobalVariablesType ??= TypeResolver.Get("Articy.Project_Of_Death.GlobalVariables.ArticyGlobalVariables");
        _comicManagerType ??= TypeResolver.Get("ComicManager");
        _unityObjectType ??= TypeResolver.Get("UnityEngine.Object");
        _gameObjectType ??= TypeResolver.Get("UnityEngine.GameObject");
        _transformType ??= TypeResolver.Get("UnityEngine.Transform");
        _mirrorType ??= TypeResolver.Get("Mirror");
        _mirrorNavigationButtonType ??= TypeResolver.Get("MirrorNavigationButton");
        _canvasScalerType ??= TypeResolver.Get("UnityEngine.UI.CanvasScaler");
        _graphicRaycasterType ??= TypeResolver.Get("UnityEngine.UI.GraphicRaycaster");
        _imageType ??= TypeResolver.Get("UnityEngine.UI.Image");
        _spriteType ??= TypeResolver.Get("UnityEngine.Sprite");
        _texture2DType ??= TypeResolver.Get("UnityEngine.Texture2D");
        _colorType ??= TypeResolver.Get("UnityEngine.Color");
        _rectType ??= TypeResolver.Get("UnityEngine.Rect");
        _renderModeType ??= TypeResolver.Get("UnityEngine.RenderMode");
        _cursorType ??= TypeResolver.Get("UnityEngine.Cursor");
        _findObjectsOfTypeMethod ??= _unityObjectType?.GetMethod(
            "FindObjectsOfType",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(Type) },
            null);
        _getRayIntersectionMethod ??= _physics2DType?.GetMethod("GetRayIntersection", BindingFlags.Public | BindingFlags.Static, null, new[] { TypeResolver.Get("UnityEngine.Ray"), typeof(float) }, null)
            ?? _physics2DType?.GetMethod("GetRayIntersection", BindingFlags.Public | BindingFlags.Static);
        _getMousePositionMethod ??= _inputType?.GetProperty("mousePosition", BindingFlags.Public | BindingFlags.Static)?.GetGetMethod();
        CacheInputMethods();
    }

    private void CacheInputMethods()
    {
        if (_inputType == null)
            return;

        _getKeyDownMethod ??= _inputType.GetMethod("GetKeyDown", BindingFlags.Public | BindingFlags.Static, null, new[] { _keyCodeType }, null);
        _getKeyMethod ??= _inputType.GetMethod("GetKey", BindingFlags.Public | BindingFlags.Static, null, new[] { _keyCodeType }, null);
        _getAxisRawMethod ??= _inputType.GetMethod("GetAxisRaw", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
        _getAxisMethod ??= _inputType.GetMethod("GetAxis", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);

    }

    private bool IsSceneChanging()
    {
        if (_elevatorManagerType == null)
            return false;

        var instance = GetStaticInstance(_elevatorManagerType);
        if (instance == null)
            return false;

        try
        {
            var field = _elevatorManagerType.GetField("bIsChangingScene", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
                return false;

            var value = field.GetValue(instance);
            return value is bool changing && changing;
        }
        catch
        {
            return false;
        }
    }

    private void UpdateSceneFocusLock()
    {
        if (IsSceneChanging())
        {
            if (!_sceneChanging)
            {
                _sceneChanging = true;
                ResetFocusForSceneChange();
            }

            return;
        }

        _sceneChanging = false;

        var activeName = TryGetActiveUnitySceneName(out var unityScene) ? unityScene : null;
        var elevatorName = TryGetElevatorSceneName(out var elevatorScene) ? elevatorScene : null;

        var changed = !string.Equals(activeName, _activeSceneName, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(elevatorName, _elevatorSceneName, StringComparison.OrdinalIgnoreCase);

        _activeSceneName = activeName;
        _elevatorSceneName = elevatorName;

        _allowedSceneNames.Clear();
        if (!string.IsNullOrWhiteSpace(_activeSceneName))
            _allowedSceneNames.Add(_activeSceneName);
        if (!string.IsNullOrWhiteSpace(_elevatorSceneName))
            _allowedSceneNames.Add(_elevatorSceneName);
        _allowedSceneNames.Add("DontDestroyOnLoad");

        if (changed)
            ResetFocusForSceneChange();
    }

    private void ResetFocusForSceneChange()
    {
        ClearInteractableFocus();
        ClearUiSelection();
        _lastEventSelected = null;
        _lastHoverAnnouncementSignature = null;
        _suppressMouseUiSyncFromKeyboard = false;
        _lastFocusedTargetKey = null;
        _keyboardFocusActive = false;
        _keyboardFocusUntilTick = 0;
        _keyboardNavUntilTick = 0;
        _pendingEventSystemSyncUntil = 0;
        _virtualCursorActive = false;
        _virtualCursorTexture = null;
        _virtualCursorSprite = null;
        _virtualCursorTextureWidth = 0f;
        _virtualCursorTextureHeight = 0f;
        _lastRawMousePos = null;
        _virtualCursorUsesFallback = false;
        _cursorHiddenForVirtual = false;
        _pendingShortcutScreenPos = null;
        VirtualMouseState.Reset();
        SetVirtualCursorOverlayVisible(false);
        SetSystemCursorVisible(true);
    }

    private bool IsPointerOverUi()
    {
        if (_eventSystemType == null || _pointerEventDataType == null || _raycastResultType == null || _vector2Type == null)
            return false;

        try
        {
            var currentProp = _eventSystemType.GetProperty("current", BindingFlags.Static | BindingFlags.Public);
            var eventSystem = currentProp?.GetValue(null);
            if (eventSystem == null)
                return false;

            var pointer = Activator.CreateInstance(_pointerEventDataType, eventSystem);
            if (pointer == null)
                return false;

            var screen = GetMousePosition();
            if (screen == null)
                return false;

            var ctor = _vector2Type.GetConstructor(new[] { typeof(float), typeof(float) });
            var vector = ctor?.Invoke(new object[] { screen.Value.x, screen.Value.y });
            var positionProp = _pointerEventDataType.GetProperty("position", BindingFlags.Instance | BindingFlags.Public);
            positionProp?.SetValue(pointer, vector);

            var listType = typeof(List<>).MakeGenericType(_raycastResultType);
            var results = Activator.CreateInstance(listType);
            if (results == null)
                return false;

            var raycastAll = _eventSystemType.GetMethod("RaycastAll", BindingFlags.Instance | BindingFlags.Public);
            raycastAll?.Invoke(eventSystem, new[] { pointer, results });

            if (results is not System.Collections.IEnumerable enumerable)
                return false;

            foreach (var item in enumerable)
            {
                if (item == null)
                    continue;

                var goProp = item.GetType().GetProperty("gameObject", BindingFlags.Instance | BindingFlags.Public);
                var go = goProp?.GetValue(item);
                if (go != null && IsInAllowedScene(go))
                    return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private object GetStaticInstance(Type type)
    {
        try
        {
            var field = type.GetField("instance", BindingFlags.Public | BindingFlags.Static)
                        ?? type.GetField("instance", BindingFlags.NonPublic | BindingFlags.Static);
            return field?.GetValue(null);
        }
        catch
        {
            return null;
        }
    }

    private void UpdateVirtualCursorFocus(NavigationDirection direction, bool submitPressed, float axisX, float axisY)
    {
        var moved = UpdatePointerFromInput(axisX, axisY, direction, out var sawArrowInput);
        SyncVirtualCursorToRawMouse();

        if (_lastFocusedInteractable != null && !IsInteractableActive(_lastFocusedInteractable))
        {
            ClearInteractableFocus();
        }

        if (sawArrowInput && !_virtualCursorActive)
            ActivateVirtualCursorFromRaw();

        if (_virtualCursorActive)
        {
            SetSystemCursorVisible(false);
            RefreshCursorSpriteIfNeeded();
        }
        else
        {
            SetSystemCursorVisible(true);
        }

        var mousePosition = GetMousePosition();
        if (mousePosition == null)
        {
            if (submitPressed)
            {
                SubmitInteractable();
                return;
            }

            if (_keyboardFocusActive)
                return;

            if (_lastFocusedInteractable != null && !IsInteractableInstance(_lastFocusedInteractable))
                return;

            ClearInteractableFocus();
            return;
        }

        var uiHit = GetUiRaycastGameObject();
        var hit = GetInteractableAtScreenPosition(mousePosition.Value.x, mousePosition.Value.y);
        if (hit == null && IsShopActive())
        {
            var shopHit = GetShopItemAtScreenPosition(mousePosition.Value.x, mousePosition.Value.y);
            if (shopHit != null)
                hit = shopHit;
        }
        if (hit == null)
        {
            if (uiHit != null)
            {
                var selectable = _selectableType != null ? GetComponentByType(uiHit, _selectableType) : null;
                var target = selectable ?? uiHit;
                if (!ReferenceEquals(target, _lastFocusedInteractable))
                    SetInteractableFocus(target);

                if (submitPressed)
                    SubmitInteractable();

                return;
            }

            if (submitPressed)
            {
                SubmitInteractable();
                return;
            }

            if (_keyboardFocusActive)
                return;

            if (_lastFocusedInteractable != null && !IsInteractableInstance(_lastFocusedInteractable))
                return;

            ClearInteractableFocus();
            return;
        }

        if (!ReferenceEquals(hit, _lastFocusedInteractable))
            SetInteractableFocus(hit);

        if (submitPressed)
            SubmitInteractable();
    }

    private bool UpdatePointerFromInput(float axisX, float axisY, NavigationDirection direction, out bool sawArrowInput)
    {
        var left = GetKey("LeftArrow");
        var right = GetKey("RightArrow");
        var up = GetKey("UpArrow");
        var down = GetKey("DownArrow");
        sawArrowInput = left || right || up || down
            || axisX < -AxisDeadzone || axisX > AxisDeadzone
            || axisY < -AxisDeadzone || axisY > AxisDeadzone;

        var movement = GetPointerMovementVector(axisX, axisY);
        if (movement.x == 0f && movement.y == 0f && direction != NavigationDirection.None)
        {
            movement = direction switch
            {
                NavigationDirection.Left => (-1f, 0f),
                NavigationDirection.Right => (1f, 0f),
                NavigationDirection.Up => (0f, 1f),
                NavigationDirection.Down => (0f, -1f),
                _ => movement
            };
        }

        if (movement.x == 0f && movement.y == 0f)
            return false;

        MoveVirtualCursor(movement.x, movement.y);
        _keyboardFocusActive = true;
        return true;
    }

    private void ActivateVirtualCursorFromRaw()
    {
        var raw = GetRawMousePosition();
        if (raw == null)
        {
            var width = Math.Max(GetScreenDimension("width"), 1);
            var height = Math.Max(GetScreenDimension("height"), 1);
            _virtualCursorPos = (width / 2f, height / 2f);
        }
        else
        {
            _virtualCursorPos = raw.Value;
        }
        _virtualCursorActive = true;
        EnsureVirtualCursorOverlay();
        UpdateVirtualCursorOverlayPosition(_virtualCursorPos.x, _virtualCursorPos.y);
    }

    private void SyncVirtualCursorToRawMouse()
    {
        if (_virtualCursorActive)
        {
            var raw = GetRawMousePosition();

            if (raw != null && _lastRawMousePos != null)
            {
                var dxRaw = raw.Value.x - _lastRawMousePos.Value.x;
                var dyRaw = raw.Value.y - _lastRawMousePos.Value.y;
                if (Math.Abs(dxRaw) > 0.5f || Math.Abs(dyRaw) > 0.5f)
                {
                    var screenWidth = Math.Max(GetScreenDimension("width"), 1);
                    var screenHeight = Math.Max(GetScreenDimension("height"), 1);
                    var targetX = Math.Max(0, Math.Min(raw.Value.x, screenWidth - 1));
                    var targetY = Math.Max(0, Math.Min(raw.Value.y, screenHeight - 1));
                    _virtualCursorPos = (targetX, targetY);
                    UpdateVirtualCursorOverlayPosition(targetX, targetY);
                    _keyboardNavUntilTick = 0;
                    _keyboardFocusActive = false;
                    _keyboardFocusUntilTick = 0;
                    _suppressMouseUiSyncFromKeyboard = false;
                }
            }

            var mouseDx = GetAxisRawAny("Mouse X", "MouseX");
            var mouseDy = GetAxisRawAny("Mouse Y", "MouseY");
            if (Math.Abs(mouseDx) < 0.0001f)
                mouseDx = GetAxisAny("Mouse X", "MouseX");
            if (Math.Abs(mouseDy) < 0.0001f)
                mouseDy = GetAxisAny("Mouse Y", "MouseY");

            if (Math.Abs(mouseDx) > 0.03f || Math.Abs(mouseDy) > 0.03f)
            {
                var screenWidth = Math.Max(GetScreenDimension("width"), 1);
                var screenHeight = Math.Max(GetScreenDimension("height"), 1);
                var targetX = _virtualCursorPos.x + (mouseDx * PointerStep);
                var targetY = _virtualCursorPos.y + (mouseDy * PointerStep);
                targetX = Math.Max(0, Math.Min(targetX, screenWidth - 1));
                targetY = Math.Max(0, Math.Min(targetY, screenHeight - 1));
                _virtualCursorPos = (targetX, targetY);
                UpdateVirtualCursorOverlayPosition(targetX, targetY);
                _keyboardNavUntilTick = 0;
                _keyboardFocusActive = false;
                _keyboardFocusUntilTick = 0;
                _suppressMouseUiSyncFromKeyboard = false;
            }

            if (raw != null)
                _lastRawMousePos = raw;

            return;
        }

        var rawPos = GetRawMousePosition();
        if (rawPos == null)
            return;

        _lastRawMousePos = rawPos;
    }

    private void SyncUnifiedCursorForUi(bool preferKeyboardSelection)
    {
        if (!_virtualCursorActive)
            ActivateVirtualCursorFromRaw();

        if (preferKeyboardSelection)
        {
            TrySyncUnifiedCursorToSelectedUi();
        }
        else
        {
            SyncVirtualCursorToRawMouse();
            SyncUiSelectionFromUnifiedCursor();
        }

        if (_virtualCursorActive)
        {
            SetSystemCursorVisible(false);
            EnsureVirtualCursorOverlay();
            SetVirtualCursorOverlayVisible(true);
            RefreshCursorSpriteIfNeeded();
        }
    }

    private void MoveUnifiedCursorByDirection(NavigationDirection direction)
    {
        var movement = direction switch
        {
            NavigationDirection.Left => (-1f, 0f),
            NavigationDirection.Right => (1f, 0f),
            NavigationDirection.Up => (0f, 1f),
            NavigationDirection.Down => (0f, -1f),
            _ => (0f, 0f)
        };

        if (movement == (0f, 0f))
            return;

        MoveVirtualCursor(movement.Item1, movement.Item2);
        SyncUiSelectionFromUnifiedCursor(forceFromKeyboard: true);
    }

    private bool TryMoveMenuCursorToNearestOption(NavigationDirection direction)
    {
        if (!IsMenuActive() || IsOfficeActive() || direction == NavigationDirection.None)
            return false;

        var selectables = GetMenuSelectables(requireScreen: true);
        if (selectables.Count == 0)
            selectables = GetMenuSelectables(requireScreen: false);
        if (selectables.Count == 0)
            return false;

        var origin = _virtualCursorActive ? _virtualCursorPos : GetMousePosition();
        if (origin == null)
        {
            var selected = ResolveUiFocusTarget(GetCurrentSelectedGameObject());
            if (selected != null)
                origin = GetInteractableScreenPosition(selected);
        }
        if (origin == null)
            return false;

        object best = null;
        var bestScore = float.PositiveInfinity;
        const float epsilon = 0.01f;

        foreach (var selectable in selectables)
        {
            if (selectable == null)
                continue;

            var pos = GetInteractableScreenPosition(selectable);
            if (pos == null)
                continue;

            var dx = pos.Value.x - origin.Value.x;
            var dy = pos.Value.y - origin.Value.y;

            var inDirection = direction switch
            {
                NavigationDirection.Left => dx < -epsilon,
                NavigationDirection.Right => dx > epsilon,
                NavigationDirection.Up => dy > epsilon,
                NavigationDirection.Down => dy < -epsilon,
                _ => false
            };
            if (!inDirection)
                continue;

            var score = (dx * dx) + (dy * dy);
            if (score >= bestScore)
                continue;

            bestScore = score;
            best = selectable;
        }

        if (best == null)
            return false;

        SetUiSelected(best);
        _lastEventSelected = GetInteractableGameObject(best) ?? best;
        SetInteractableFocus(best);
        TrySyncUnifiedCursorToSelectedUi();
        return true;
    }

    private void SyncUiSelectionFromUnifiedCursor(bool forceFromKeyboard = false)
    {
        if (!IsMenuActive() && !IsDialogActive() && !IsSpeechBubbleDialogActive())
            return;

        if (_suppressMouseUiSyncFromKeyboard && !forceFromKeyboard)
            return;

        var hovered = GetUiRaycastGameObject();
        if (hovered == null)
            return;

        var hoveredTarget = ResolveUiFocusTarget(hovered);
        if (hoveredTarget == null)
            return;

        var current = GetCurrentSelectedGameObject();
        var currentTarget = ResolveUiFocusTarget(current);
        if (currentTarget != null && IsSameUiTarget(currentTarget, hoveredTarget))
            return;

        SetUiSelected(hoveredTarget);
        SetInteractableFocus(hoveredTarget);
        _lastEventSelected = GetInteractableGameObject(hoveredTarget) ?? hoveredTarget;
    }

    private object ResolveUiFocusTarget(object uiObject)
    {
        if (uiObject == null)
            return null;

        if (_selectableType == null)
            return uiObject;

        var selectable = GetComponentByType(uiObject, _selectableType);
        if (selectable != null)
            return selectable;

        var gameObject = GetInteractableGameObject(uiObject) ?? GetGameObjectFromComponent(uiObject) ?? uiObject;
        var parentSelectable = GetComponentInParentByType(gameObject, _selectableType);
        return parentSelectable ?? uiObject;
    }

    private void TrySyncUnifiedCursorToSelectedUi()
    {
        if (!IsMenuActive() && !IsDialogActive() && !IsSpeechBubbleDialogActive())
            return;

        var selected = GetCurrentSelectedGameObject();
        if (selected == null)
            return;

        var selectable = _selectableType != null ? GetComponentByType(selected, _selectableType) : null;
        var target = selectable ?? selected;
        var screen = GetInteractableScreenPosition(target);
        if (screen == null)
            return;

        var screenWidth = Math.Max(GetScreenDimension("width"), 1);
        var screenHeight = Math.Max(GetScreenDimension("height"), 1);
        var x = Math.Max(0, Math.Min(screen.Value.x, screenWidth - 1));
        var y = Math.Max(0, Math.Min(screen.Value.y, screenHeight - 1));

        _virtualCursorPos = (x, y);
        _virtualCursorActive = true;
        EnsureVirtualCursorOverlay();
        UpdateVirtualCursorOverlayPosition(x, y);
    }

    private void DeactivateVirtualCursorForUi()
    {
        if (!_virtualCursorActive)
            return;

        _virtualCursorActive = false;
        SetVirtualCursorOverlayVisible(false);
        SetSystemCursorVisible(true);
    }

    private void MoveVirtualCursor(float dx, float dy)
    {
        if (dx == 0f && dy == 0f)
            return;

        if (!_virtualCursorActive)
        {
            var raw = GetRawMousePosition();
            if (raw == null)
            {
                var width = Math.Max(GetScreenDimension("width"), 1);
                var height = Math.Max(GetScreenDimension("height"), 1);
                _virtualCursorPos = (width / 2f, height / 2f);
            }
            else
            {
                _virtualCursorPos = raw.Value;
            }
            _virtualCursorActive = true;
            EnsureVirtualCursorOverlay();
            UpdateVirtualCursorOverlayPosition(_virtualCursorPos.x, _virtualCursorPos.y);
        }

        var screenWidth = Math.Max(GetScreenDimension("width"), 1);
        var screenHeight = Math.Max(GetScreenDimension("height"), 1);

        var targetX = _virtualCursorPos.x + (dx * PointerStep);
        var targetY = _virtualCursorPos.y + (dy * PointerStep);
        targetX = Math.Max(0, Math.Min(targetX, screenWidth - 1));
        targetY = Math.Max(0, Math.Min(targetY, screenHeight - 1));

        _virtualCursorPos = (targetX, targetY);
        _virtualCursorMovedSinceLastSubmit = true;
        UpdateVirtualCursorOverlayPosition(targetX, targetY);
        RefreshCursorSpriteIfNeeded();
    }

    private void EnsureVirtualCursorOverlay()
    {
        if (_virtualCursorCanvas != null)
        {
            SetVirtualCursorOverlayVisible(true);
            return;
        }

        if (_gameObjectType == null || _canvasType == null || _rectTransformType == null || _imageType == null || _vector2Type == null)
            return;

        try
        {
            var canvasGo = Activator.CreateInstance(_gameObjectType, "DAA_VirtualCursorCanvas");
            if (canvasGo == null)
                return;

            var addComponent = _gameObjectType.GetMethod("AddComponent", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(Type) }, null);
            if (addComponent == null)
                return;

            var canvas = addComponent.Invoke(canvasGo, new object[] { _canvasType });
            if (canvas != null)
            {
                var renderModeProp = _canvasType.GetProperty("renderMode", BindingFlags.Instance | BindingFlags.Public);
                if (renderModeProp != null && _renderModeType != null)
                {
                    var overlay = Enum.Parse(_renderModeType, "ScreenSpaceOverlay");
                    renderModeProp.SetValue(canvas, overlay);
                }

                var sortingOrderProp = _canvasType.GetProperty("sortingOrder", BindingFlags.Instance | BindingFlags.Public);
                sortingOrderProp?.SetValue(canvas, 5000);
            }

            if (_canvasScalerType != null)
                addComponent.Invoke(canvasGo, new object[] { _canvasScalerType });
            if (_graphicRaycasterType != null)
                addComponent.Invoke(canvasGo, new object[] { _graphicRaycasterType });

            var cursorGo = Activator.CreateInstance(_gameObjectType, "DAA_VirtualCursor");
            if (cursorGo == null)
                return;

            var cursorTransform = GetMemberValue(cursorGo, "transform");
            var canvasTransform = GetMemberValue(canvasGo, "transform");
            if (cursorTransform != null && canvasTransform != null && _transformType != null)
            {
                var setParent = _transformType.GetMethod("SetParent", BindingFlags.Instance | BindingFlags.Public, null, new[] { _transformType, typeof(bool) }, null);
                setParent?.Invoke(cursorTransform, new[] { canvasTransform, false });
            }

            _virtualCursorImage = addComponent.Invoke(cursorGo, new object[] { _imageType });
            _virtualCursorRectTransform = GetComponentByType(cursorGo, _rectTransformType);

            if (_virtualCursorImage != null)
            {
                var raycastProp = _imageType.GetProperty("raycastTarget", BindingFlags.Instance | BindingFlags.Public);
                raycastProp?.SetValue(_virtualCursorImage, false);

                var sprite = GetGameCursorSprite();
                if (sprite != null)
                {
                    var spriteProp = _imageType.GetProperty("sprite", BindingFlags.Instance | BindingFlags.Public);
                    spriteProp?.SetValue(_virtualCursorImage, sprite);
                }

                if (_colorType != null)
                {
                    var colorProp = _imageType.GetProperty("color", BindingFlags.Instance | BindingFlags.Public);
                    if (colorProp != null)
                    {
                        var color = CreateColor(1f, 1f, 1f, 1f);
                        if (color != null)
                            colorProp.SetValue(_virtualCursorImage, color);
                    }
                }
            }

            if (_virtualCursorRectTransform != null)
            {
                SetRectTransformVector2(_virtualCursorRectTransform, "anchorMin", 0f, 0f);
                SetRectTransformVector2(_virtualCursorRectTransform, "anchorMax", 0f, 0f);
                var hotspot = GetGameCursorHotspot();
                var size = GetVirtualCursorSize();
                var pivot = GetHotspotPivot(size.width, size.height, hotspot);
                var pivotX = pivot.x;
                var pivotY = pivot.y;
                SetRectTransformVector2(_virtualCursorRectTransform, "pivot", pivotX, pivotY);
                SetRectTransformVector2(_virtualCursorRectTransform, "sizeDelta", size.width, size.height);
            }

            _virtualCursorCanvas = canvasGo;
            SetVirtualCursorOverlayVisible(true);
            RefreshCursorSpriteIfNeeded();

            var dontDestroy = _unityObjectType?.GetMethod("DontDestroyOnLoad", BindingFlags.Public | BindingFlags.Static, null, new[] { _unityObjectType }, null);
            dontDestroy?.Invoke(null, new[] { canvasGo });
        }
        catch
        {
            _virtualCursorCanvas = null;
            _virtualCursorImage = null;
            _virtualCursorRectTransform = null;
        }
    }

    private void SetVirtualCursorOverlayVisible(bool visible)
    {
        if (_virtualCursorCanvas == null || _gameObjectType == null)
            return;

        try
        {
            var setActive = _gameObjectType.GetMethod("SetActive", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(bool) }, null);
            setActive?.Invoke(_virtualCursorCanvas, new object[] { visible });
        }
        catch
        {
            // Ignore.
        }
    }

    private void SetSystemCursorVisible(bool visible)
    {
        if (_cursorType == null)
            return;

        if (visible && !_cursorHiddenForVirtual)
            return;

        if (!visible && _cursorHiddenForVirtual)
            return;

        try
        {
            var prop = _cursorType.GetProperty("visible", BindingFlags.Public | BindingFlags.Static);
            prop?.SetValue(null, visible);
            _cursorHiddenForVirtual = !visible;
        }
        catch
        {
            // Ignore.
        }
    }

    private void UpdateVirtualCursorOverlayPosition(float x, float y)
    {
        if (_virtualCursorRectTransform == null)
            return;

        SetRectTransformVector2(_virtualCursorRectTransform, "anchoredPosition", x, y);
    }

    private object GetGameCursorSprite()
    {
        if (_virtualCursorSprite != null)
            return _virtualCursorSprite;

        if (_texture2DType == null || _spriteType == null || _rectType == null || _vector2Type == null)
            return null;

        try
        {
            var texture = GetGameCursorTexture();
            if (texture == null)
            {
                _virtualCursorUsesFallback = true;
                _virtualCursorSprite = CreateFallbackCursorSprite();
                return _virtualCursorSprite;
            }

            var width = GetTextureDimension(texture, "width");
            var height = GetTextureDimension(texture, "height");
            if (width <= 0 || height <= 0)
                return null;

            _virtualCursorTextureWidth = width;
            _virtualCursorTextureHeight = height;

            var rectCtor = _rectType.GetConstructor(new[] { typeof(float), typeof(float), typeof(float), typeof(float) });
            var rect = rectCtor?.Invoke(new object[] { 0f, 0f, width, height });
            if (rect == null)
                return null;

            var pivot = CreateVector2(0f, 0f);
            if (pivot == null)
                return null;

            var create = _spriteType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static, null, new[] { _texture2DType, _rectType, _vector2Type, typeof(float) }, null)
                        ?? _spriteType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static, null, new[] { _texture2DType, _rectType, _vector2Type }, null);
            if (create == null)
                return null;

            var args = create.GetParameters().Length == 4
                ? new object[] { texture, rect, pivot, 100f }
                : new object[] { texture, rect, pivot };

            _virtualCursorUsesFallback = false;
            _virtualCursorSprite = create.Invoke(null, args);
            return _virtualCursorSprite;
        }
        catch
        {
            return null;
        }
    }

    private void RefreshCursorSpriteIfNeeded()
    {
        if (_virtualCursorImage == null || _imageType == null)
            return;

        if (!_virtualCursorUsesFallback)
            return;

        var texture = GetGameCursorTexture();
        if (texture == null)
            return;

        _virtualCursorSprite = null;
        _virtualCursorTextureWidth = 0f;
        _virtualCursorTextureHeight = 0f;
        _virtualCursorUsesFallback = false;

        var sprite = GetGameCursorSprite();
        if (sprite == null)
            return;

        try
        {
            var spriteProp = _imageType.GetProperty("sprite", BindingFlags.Instance | BindingFlags.Public);
            spriteProp?.SetValue(_virtualCursorImage, sprite);

            var hotspot = GetGameCursorHotspot();
            var size = GetVirtualCursorSize();
            var pivot = GetHotspotPivot(size.width, size.height, hotspot);
            SetRectTransformVector2(_virtualCursorRectTransform, "pivot", pivot.x, pivot.y);
            SetRectTransformVector2(_virtualCursorRectTransform, "sizeDelta", size.width, size.height);
        }
        catch
        {
            // Ignore refresh failures.
        }
    }

    private object CreateFallbackCursorSprite()
    {
        if (_texture2DType == null || _spriteType == null || _rectType == null || _vector2Type == null || _colorType == null)
            return null;

        try
        {
            var ctor = _texture2DType.GetConstructor(new[] { typeof(int), typeof(int) });
            if (ctor == null)
                return null;

            var texture = ctor.Invoke(new object[] { 16, 16 });
            if (texture == null)
                return null;

            var color = CreateColor(1f, 1f, 1f, 1f);
            if (color == null)
                return null;

            var colors = Array.CreateInstance(_colorType, 16 * 16);
            for (var i = 0; i < colors.Length; i++)
                colors.SetValue(color, i);

            var setPixels = _texture2DType.GetMethod("SetPixels", BindingFlags.Instance | BindingFlags.Public, null, new[] { colors.GetType() }, null);
            setPixels?.Invoke(texture, new object[] { colors });

            var apply = _texture2DType.GetMethod("Apply", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
            apply?.Invoke(texture, null);

            var rectCtor = _rectType.GetConstructor(new[] { typeof(float), typeof(float), typeof(float), typeof(float) });
            var rect = rectCtor?.Invoke(new object[] { 0f, 0f, 16f, 16f });
            if (rect == null)
                return null;

            var pivot = CreateVector2(0f, 0f);
            if (pivot == null)
                return null;

            var create = _spriteType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static, null, new[] { _texture2DType, _rectType, _vector2Type, typeof(float) }, null)
                        ?? _spriteType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static, null, new[] { _texture2DType, _rectType, _vector2Type }, null);
            if (create == null)
                return null;

            var args = create.GetParameters().Length == 4
                ? new object[] { texture, rect, pivot, 100f }
                : new object[] { texture, rect, pivot };

            _virtualCursorTextureWidth = 16f;
            _virtualCursorTextureHeight = 16f;
            return create.Invoke(null, args);
        }
        catch
        {
            return null;
        }
    }

    private object GetGameCursorTexture()
    {
        if (_virtualCursorTexture != null)
            return _virtualCursorTexture;

        if (_inputManagerType == null)
            return null;

        var instance = GetStaticInstance(_inputManagerType);
        if (instance == null)
            return null;

        var texture = GetMemberValue(instance, "TextureCursor");
        if (texture == null)
            return null;

        _virtualCursorTexture = texture;
        return texture;
    }

    private (float width, float height) GetVirtualCursorSize()
    {
        if (_virtualCursorTextureWidth > 0f && _virtualCursorTextureHeight > 0f)
            return (_virtualCursorTextureWidth, _virtualCursorTextureHeight);

        var texture = GetGameCursorTexture();
        if (texture == null)
            return (16f, 16f);

        var width = GetTextureDimension(texture, "width");
        var height = GetTextureDimension(texture, "height");
        if (width > 0f)
            _virtualCursorTextureWidth = width;
        if (height > 0f)
            _virtualCursorTextureHeight = height;

        if (_virtualCursorTextureWidth <= 0f || _virtualCursorTextureHeight <= 0f)
            return (16f, 16f);

        return (_virtualCursorTextureWidth, _virtualCursorTextureHeight);
    }

    private (float x, float y) GetGameCursorHotspot()
    {
        if (_inputManagerType == null)
            return (0f, 0f);

        var instance = GetStaticInstance(_inputManagerType);
        if (instance == null)
            return (0f, 0f);

        var hotspot = GetMemberValue(instance, "CursorHotSpot");
        var pos = GetVector2FromValue(hotspot);
        return pos ?? (0f, 0f);
    }

    private (float x, float y) GetHotspotPivot(float width, float height, (float x, float y) hotspot)
    {
        if (width <= 0f || height <= 0f)
            return (0f, 1f);

        var pivotX = hotspot.x / width;
        var pivotY = 1f - (hotspot.y / height);

        if (pivotX < 0f) pivotX = 0f;
        if (pivotX > 1f) pivotX = 1f;
        if (pivotY < 0f) pivotY = 0f;
        if (pivotY > 1f) pivotY = 1f;

        return (pivotX, pivotY);
    }

    private float GetTextureDimension(object texture, string name)
    {
        if (texture == null)
            return 0f;

        try
        {
            var prop = texture.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (prop == null)
                return 0f;

            var value = prop.GetValue(texture);
            if (value is int intValue)
                return intValue;
            if (value is float floatValue)
                return floatValue;
        }
        catch
        {
            return 0f;
        }

        return 0f;
    }

    private object CreateVector2(float x, float y)
    {
        if (_vector2Type == null)
            return null;

        var ctor = _vector2Type.GetConstructor(new[] { typeof(float), typeof(float) });
        return ctor?.Invoke(new object[] { x, y });
    }

    private object CreateColor(float r, float g, float b, float a)
    {
        if (_colorType == null)
            return null;

        var color = Activator.CreateInstance(_colorType);
        var rField = _colorType.GetField("r", BindingFlags.Instance | BindingFlags.Public);
        var gField = _colorType.GetField("g", BindingFlags.Instance | BindingFlags.Public);
        var bField = _colorType.GetField("b", BindingFlags.Instance | BindingFlags.Public);
        var aField = _colorType.GetField("a", BindingFlags.Instance | BindingFlags.Public);
        rField?.SetValue(color, r);
        gField?.SetValue(color, g);
        bField?.SetValue(color, b);
        aField?.SetValue(color, a);
        return color;
    }

    private void SetRectTransformVector2(object rectTransform, string name, float x, float y)
    {
        if (rectTransform == null || _rectTransformType == null || _vector2Type == null)
            return;

        try
        {
            var prop = _rectTransformType.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (prop == null)
                return;

            var vector = CreateVector2(x, y);
            if (vector != null)
                prop.SetValue(rectTransform, vector);
        }
        catch
        {
            // Ignore.
        }
    }

    private (float x, float y) GetPointerMovementVector(float axisX, float axisY)
    {
        var dx = 0f;
        var dy = 0f;

        if (GetKey("LeftArrow") || axisX < -AxisDeadzone)
            dx = -1f;
        else if (GetKey("RightArrow") || axisX > AxisDeadzone)
            dx = 1f;

        if (GetKey("UpArrow") || axisY > AxisDeadzone)
            dy = 1f;
        else if (GetKey("DownArrow") || axisY < -AxisDeadzone)
            dy = -1f;

        return (dx, dy);
    }

    private object GetUiRaycastGameObject()
    {
        if (_eventSystemType == null || _pointerEventDataType == null || _raycastResultType == null || _vector2Type == null)
            return null;

        try
        {
            var currentProp = _eventSystemType.GetProperty("current", BindingFlags.Static | BindingFlags.Public);
            var eventSystem = currentProp?.GetValue(null);
            if (eventSystem == null)
                return null;

            var pointer = Activator.CreateInstance(_pointerEventDataType, eventSystem);
            if (pointer == null)
                return null;

            var screen = GetMousePosition();
            if (screen == null)
                return null;

            var ctor = _vector2Type.GetConstructor(new[] { typeof(float), typeof(float) });
            var vector = ctor?.Invoke(new object[] { screen.Value.x, screen.Value.y });
            var positionProp = _pointerEventDataType.GetProperty("position", BindingFlags.Instance | BindingFlags.Public);
            positionProp?.SetValue(pointer, vector);

            var listType = typeof(List<>).MakeGenericType(_raycastResultType);
            var results = Activator.CreateInstance(listType);
            if (results == null)
                return null;

            var raycastAll = _eventSystemType.GetMethod("RaycastAll", BindingFlags.Instance | BindingFlags.Public);
            raycastAll?.Invoke(eventSystem, new[] { pointer, results });

            if (results is not System.Collections.IEnumerable enumerable)
                return null;

            foreach (var item in enumerable)
            {
                if (item == null)
                    continue;

                var goProp = item.GetType().GetProperty("gameObject", BindingFlags.Instance | BindingFlags.Public);
                var go = goProp?.GetValue(item);
                if (go != null && IsInAllowedScene(go))
                    return go;
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private List<object> GetSelectableListCached()
    {
        var now = Environment.TickCount;
        if (_cachedSelectables != null && now - _lastSelectableCacheTick < 500)
            return _cachedSelectables;

        _lastSelectableCacheTick = now;
        _cachedSelectables = new List<object>();
        var width = GetScreenDimension("width");
        var height = GetScreenDimension("height");
        foreach (var item in FindSceneObjectsOfType(_selectableType))
        {
            if (item != null && IsSelectableEligible(item, width, height, requireScreen: false))
                _cachedSelectables.Add(item);
        }

        return _cachedSelectables;
    }


    private List<object> GetEligibleSelectables(bool requireScreen)
    {
        if (IsSpeechBubbleDialogActive())
            return GetSpeechBubbleSelectables(requireScreen);
        if (IsDialogActive())
            return GetDialogSelectables(requireScreen);
        if (IsMenuActive())
            return GetMenuSelectables(requireScreen);

        var width = GetScreenDimension("width");
        var height = GetScreenDimension("height");
        var list = GetSelectableListCached();
        if (list.Count == 0)
            return new List<object>();

        var eligible = new List<object>(list.Count);
        foreach (var item in list)
        {
            if (item != null && IsSelectableEligible(item, width, height, requireScreen))
                eligible.Add(item);
        }

        return eligible;
    }

    private List<object> GetDialogSelectables(bool requireScreen)
    {
        var width = GetScreenDimension("width");
        var height = GetScreenDimension("height");
        var list = GetSelectableListCached();
        if (list.Count == 0)
            return new List<object>();

        var roots = GetActiveDialogRoots();
        if (roots.Count == 0)
            return new List<object>();

        var eligible = new List<object>(list.Count);
        foreach (var item in list)
        {
            if (item == null || !IsSelectableEligible(item, width, height, requireScreen))
                continue;

            var go = GetInteractableGameObject(item) ?? item;
            if (go == null)
                continue;

            if (IsUnderDialogRoot(go, roots))
                eligible.Add(item);
        }

        return eligible;
    }

    private List<object> GetSpeechBubbleSelectables(bool requireScreen)
    {
        var list = new List<object>();
        if (_speechBubbleManagerType == null)
            return list;

        var manager = GetStaticInstance(_speechBubbleManagerType);
        if (manager == null)
            return list;

        var spawned = GetMemberValue(manager, "SpawnedBubbles") as System.Collections.IEnumerable;
        if (spawned == null)
            return list;

        var width = GetScreenDimension("width");
        var height = GetScreenDimension("height");

        foreach (var entry in spawned)
        {
            if (entry == null)
                continue;

            if (IsSpeakerSpeechBubble(entry))
                continue;

            var button = GetMemberValue(entry, "ButtonSpeechBubble") ?? entry;
            if (button == null || !IsSelectableActive(button))
                continue;

            if (IsSelectableEligible(button, width, height, requireScreen))
                list.Add(button);
        }

        return list;
    }

    private bool IsSpeakerSpeechBubble(object bubble)
    {
        if (bubble == null || _speechBubbleManagerType == null)
            return false;

        var manager = GetStaticInstance(_speechBubbleManagerType);
        if (manager == null)
            return false;

        try
        {
            var method = _speechBubbleManagerType.GetMethod("GetSpeakerBubble", BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
                return false;

            var speaker = method.Invoke(manager, null);
            return speaker != null && ReferenceEquals(speaker, bubble);
        }
        catch
        {
            return false;
        }
    }

    private string GetSpeechBubbleTextForInteractable(object interactable)
    {
        if (interactable == null || _speechBubbleType == null)
            return null;

        var gameObject = GetInteractableGameObject(interactable) ?? GetMemberValue(interactable, "gameObject");
        if (gameObject == null)
            return null;

        var bubble = GetComponentInParentByType(gameObject, _speechBubbleType)
                     ?? GetComponentByType(gameObject, _speechBubbleType);
        if (bubble != null)
        {
            var text = ReadSpeechBubbleText(bubble);
            if (!string.IsNullOrWhiteSpace(text))
                return text;
        }

        var childText = FindFirstUsefulTextInChildren(gameObject);
        if (!string.IsNullOrWhiteSpace(childText))
            return SanitizeHoverText(childText);

        return TryGetSpeechBubbleTextFromManager(interactable);
    }

    private string TryGetSpeechBubbleTextFromManager(object interactable)
    {
        if (_speechBubbleManagerType == null || interactable == null)
            return null;

        var manager = GetStaticInstance(_speechBubbleManagerType);
        if (manager == null)
            return null;

        var spawned = GetMemberValue(manager, "SpawnedBubbles") as System.Collections.IEnumerable;
        if (spawned == null)
            return null;

        foreach (var entry in spawned)
        {
            if (entry == null)
                continue;

            var button = GetMemberValue(entry, "ButtonSpeechBubble") ?? entry;
            if (!IsSameUiTarget(interactable, button))
                continue;

            var text = ReadSpeechBubbleText(entry);
            if (!string.IsNullOrWhiteSpace(text))
                return text;
        }

        return null;
    }

    private string ReadSpeechBubbleText(object bubble)
    {
        if (bubble == null)
            return null;

        var textObj = GetMemberValue(bubble, "TextSpeechBubble");
        if (textObj == null)
            return null;

        var textValue = ReadStringValue(GetMemberValue(textObj, "text")) ?? ReadStringValue(textObj);
        return SanitizeHoverText(textValue);
    }

    private bool IsSameUiTarget(object left, object right)
    {
        if (left == null || right == null)
            return false;

        if (ReferenceEquals(left, right))
            return true;

        var leftGo = GetInteractableGameObject(left) ?? GetMemberValue(left, "gameObject");
        var rightGo = GetInteractableGameObject(right) ?? GetMemberValue(right, "gameObject");
        if (leftGo == null || rightGo == null)
            return false;

        if (ReferenceEquals(leftGo, rightGo))
            return true;

        var leftTransform = GetTransform(leftGo);
        var rightTransform = GetTransform(rightGo);
        if (leftTransform != null && rightTransform != null)
        {
            if (IsTransformChildOf(leftTransform, rightTransform))
                return true;
            if (IsTransformChildOf(rightTransform, leftTransform))
                return true;
        }

        return false;
    }

    private List<object> GetMenuSelectables(bool requireScreen)
    {
        var width = GetScreenDimension("width");
        var height = GetScreenDimension("height");
        var list = GetSelectableListCached();
        if (list.Count == 0)
            return new List<object>();

        var roots = GetActiveMenuRoots();
        if (roots.Count == 0)
            return new List<object>();

        var eligible = new List<object>(list.Count);
        foreach (var item in list)
        {
            if (item == null || !IsSelectableEligible(item, width, height, requireScreen))
                continue;

            var go = GetInteractableGameObject(item) ?? item;
            if (go == null)
                continue;

            if (IsUnderDialogRoot(go, roots))
                eligible.Add(item);
        }

        return eligible;
    }

    private void SetUiSelected(object selectable)
    {
        if (selectable == null || _eventSystemType == null)
            return;

        try
        {
            var currentProp = _eventSystemType.GetProperty("current", BindingFlags.Static | BindingFlags.Public);
            var eventSystem = currentProp?.GetValue(null);
            if (eventSystem == null)
                return;

            var selectableComponent = selectable;
            if (_selectableType != null && !_selectableType.IsInstanceOfType(selectable))
            {
                var selectableFromGo = GetComponentByType(selectable, _selectableType);
                if (selectableFromGo != null)
                    selectableComponent = selectableFromGo;
            }

            var selectMethod = selectableComponent?.GetType().GetMethod("Select", BindingFlags.Instance | BindingFlags.Public);
            selectMethod?.Invoke(selectableComponent, null);

            var gameObject = GetInteractableGameObject(selectableComponent ?? selectable) ?? selectable;
            var setSelected = _eventSystemType.GetMethod("SetSelectedGameObject", BindingFlags.Instance | BindingFlags.Public);
            setSelected?.Invoke(eventSystem, new[] { gameObject, null });
            TrySyncUnifiedCursorToSelectedUi();
        }
        catch
        {
            // Ignore.
        }
    }

    private void ClearUiSelection()
    {
        if (_eventSystemType == null)
            return;

        try
        {
            var currentProp = _eventSystemType.GetProperty("current", BindingFlags.Static | BindingFlags.Public);
            var eventSystem = currentProp?.GetValue(null);
            if (eventSystem == null)
                return;

            var setSelected = _eventSystemType.GetMethod("SetSelectedGameObject", BindingFlags.Instance | BindingFlags.Public);
            setSelected?.Invoke(eventSystem, new object[] { null, null });
        }
        catch
        {
            // Ignore.
        }
    }

    private object GetCurrentSelectedGameObject()
    {
        if (_eventSystemType == null)
            return null;

        try
        {
            var currentProp = _eventSystemType.GetProperty("current", BindingFlags.Static | BindingFlags.Public);
            var eventSystem = currentProp?.GetValue(null);
            if (eventSystem == null)
                return null;

            var selectedProp = _eventSystemType.GetProperty("currentSelectedGameObject", BindingFlags.Instance | BindingFlags.Public);
            return selectedProp?.GetValue(eventSystem);
        }
        catch
        {
            return null;
        }
    }

    private bool IsSelectableEligible(object selectable, int width, int height, bool requireScreen)
    {
        if (selectable == null)
            return false;

        var gameObject = GetInteractableGameObject(selectable) ?? selectable;
        if (gameObject == null || !IsGameObjectActive(gameObject))
            return false;

        if (!IsCanvasGroupVisible(gameObject))
            return false;

        try
        {
            var method = selectable.GetType().GetMethod("IsInteractable", BindingFlags.Instance | BindingFlags.Public);
            if (method != null)
            {
                var result = method.Invoke(selectable, null);
                if (result is bool interactable && !interactable)
                    return false;
            }
        }
        catch
        {
            // Treat as interactable if query fails.
        }

        if (width > 0 && height > 0 && requireScreen)
        {
            var screen = GetUiScreenPosition(gameObject, width, height);
            if (screen != null)
            {
                if (screen.Value.x < 0 || screen.Value.y < 0 || screen.Value.x > width || screen.Value.y > height)
                    return false;

                if (!IsSelectableRaycastVisible(gameObject, screen.Value))
                    return false;
            }
        }

        return true;
    }

    private IEnumerable<object> GetDeskDrawersFromGrimDesk()
    {
        if (_grimDeskType == null)
            yield break;

        object instance;
        try
        {
            var instanceField = _grimDeskType.GetField("instance", BindingFlags.Public | BindingFlags.Static);
            instance = instanceField?.GetValue(null);
        }
        catch
        {
            yield break;
        }

        if (instance == null)
            yield break;

        var drawerLeft = GetFieldValueLocal(instance, "DrawerLeft");
        if (drawerLeft != null)
            yield return drawerLeft;

        var drawerRight = GetFieldValueLocal(instance, "DrawerRight");
        if (drawerRight != null)
            yield return drawerRight;
    }

    private static object GetFieldValueLocal(object instance, string name)
    {
        if (instance == null || string.IsNullOrWhiteSpace(name))
            return null;

        try
        {
            var field = instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return field?.GetValue(instance);
        }
        catch
        {
            return null;
        }
    }

    private bool IsCanvasGroupVisible(object gameObject)
    {
        if (gameObject == null || _canvasGroupType == null)
            return true;

        try
        {
            var method = gameObject.GetType().GetMethod("GetComponentsInParent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(Type), typeof(bool) }, null);
            if (method == null)
                return true;

            var groups = method.Invoke(gameObject, new object[] { _canvasGroupType, true }) as Array;
            if (groups == null || groups.Length == 0)
                return true;

            foreach (var group in groups)
            {
                if (group == null)
                    continue;

                var interactableProp = group.GetType().GetProperty("interactable", BindingFlags.Instance | BindingFlags.Public);
                var blocksProp = group.GetType().GetProperty("blocksRaycasts", BindingFlags.Instance | BindingFlags.Public);
                var alphaProp = group.GetType().GetProperty("alpha", BindingFlags.Instance | BindingFlags.Public);

                if (interactableProp?.GetValue(group) is bool interactable && !interactable)
                    return false;

                if (blocksProp?.GetValue(group) is bool blocks && !blocks)
                    return false;

                if (alphaProp?.GetValue(group) is float alpha && alpha <= 0.001f)
                    return false;
            }
        }
        catch
        {
            return true;
        }

        return true;
    }

    private bool IsSelectableRaycastVisible(object gameObject, (float x, float y) screen)
    {
        if (_eventSystemType == null || _pointerEventDataType == null || _raycastResultType == null || _vector2Type == null)
            return true;

        try
        {
            var currentProp = _eventSystemType.GetProperty("current", BindingFlags.Static | BindingFlags.Public);
            var eventSystem = currentProp?.GetValue(null);
            if (eventSystem == null)
                return true;

            var pointer = Activator.CreateInstance(_pointerEventDataType, eventSystem);
            if (pointer == null)
                return true;

            var ctor = _vector2Type.GetConstructor(new[] { typeof(float), typeof(float) });
            var vector = ctor?.Invoke(new object[] { screen.x, screen.y });
            var positionProp = _pointerEventDataType.GetProperty("position", BindingFlags.Instance | BindingFlags.Public);
            positionProp?.SetValue(pointer, vector);

            var listType = typeof(List<>).MakeGenericType(_raycastResultType);
            var results = Activator.CreateInstance(listType);
            if (results == null)
                return true;

            var raycastAll = _eventSystemType.GetMethod("RaycastAll", BindingFlags.Instance | BindingFlags.Public);
            raycastAll?.Invoke(eventSystem, new[] { pointer, results });

            if (results is not System.Collections.IEnumerable enumerable)
                return true;

            foreach (var item in enumerable)
            {
                if (item == null)
                    continue;

                var goProp = item.GetType().GetProperty("gameObject", BindingFlags.Instance | BindingFlags.Public);
                var hitGo = goProp?.GetValue(item);
                if (hitGo == null)
                    continue;

                if (ReferenceEquals(hitGo, gameObject))
                    return true;

                var hitTransform = GetTransform(hitGo);
                var targetTransform = GetTransform(gameObject);
                if (hitTransform != null && targetTransform != null && IsTransformChildOf(hitTransform, targetTransform))
                    return true;
            }
        }
        catch
        {
            return true;
        }

        return false;
    }

    

    private (float x, float y)? ToScreenPosition((float x, float y) position, int width, int height)
    {
        if (width <= 0 || height <= 0)
            return null;

        if (position.x >= 0 && position.y >= 0 && position.x <= width && position.y <= height)
            return position;

        var fallbackX = Clamp(position.x, 0f, width - 1);
        var fallbackY = Clamp(position.y, 0f, height - 1);
        return (fallbackX, fallbackY);
    }

    private (float x, float y)? TryProjectScreenPosition((float x, float y, float z) world)
    {
        var width = GetScreenDimension("width");
        var height = GetScreenDimension("height");

        var screen = WorldToScreenPoint(world) ?? WorldToScreenPointWithCamera(world);
        if (screen != null)
            return screen;

        return ToScreenPosition((world.x, world.y), width, height);
    }

    private (float x, float y)? GetUiScreenPosition(object gameObject, int width, int height)
    {
        if (gameObject == null)
            return null;

        var rectTransform = GetRectTransform(gameObject);
        if (rectTransform == null)
            return null;

        var world = GetRectTransformWorldPosition(rectTransform);
        if (world == null)
        {
            var anchored = GetRectTransformAnchoredPosition(rectTransform);
            if (anchored != null)
            {
                var overlay = IsScreenSpaceOverlay(rectTransform);
                if (overlay)
                    return (anchored.Value.x + (width / 2f), anchored.Value.y + (height / 2f));

                return anchored;
            }

            return null;
        }

        var camera = GetCanvasCamera(rectTransform);
        var screen = WorldToScreenPointWithUtility(camera, world.Value);
        if (screen != null)
            return screen;

        return ToScreenPosition(world.Value, width, height);
    }

    private (float x, float y)? GetRectTransformAnchoredPosition(object rectTransform)
    {
        if (rectTransform == null)
            return null;

        try
        {
            var anchored = GetVector2FromPropertyOrMethod(rectTransform, "anchoredPosition3D", "get_anchoredPosition3D")
                ?? GetVector2FromPropertyOrMethod(rectTransform, "anchoredPosition", "get_anchoredPosition")
                ?? GetRectTransformFieldVector2(rectTransform, "m_AnchoredPosition");
            if (anchored != null)
                return anchored;
        }
        catch
        {
            return GetRectTransformFieldVector2(rectTransform, "m_AnchoredPosition");
        }

        return GetRectTransformFieldVector2(rectTransform, "m_AnchoredPosition");
    }

    private (float x, float y)? GetRectTransformPivotOffset(object rectTransform)
    {
        try
        {
            var rectProp = rectTransform.GetType().GetProperty("rect", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var rect = rectProp?.GetValue(rectTransform);
            if (rect == null)
                return null;

            var widthProp = rect.GetType().GetProperty("width", BindingFlags.Instance | BindingFlags.Public);
            var heightProp = rect.GetType().GetProperty("height", BindingFlags.Instance | BindingFlags.Public);
            if (widthProp == null || heightProp == null)
                return null;

            var widthVal = widthProp.GetValue(rect);
            var heightVal = heightProp.GetValue(rect);
            if (widthVal is not float w || heightVal is not float h)
                return null;

            var pivotProp = rectTransform.GetType().GetProperty("pivot", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var pivot = pivotProp?.GetValue(rectTransform);
            if (pivot == null)
                return GetRectTransformFieldVector2(rectTransform, "m_Pivot");

            var xProp = pivot.GetType().GetProperty("x", BindingFlags.Instance | BindingFlags.Public);
            var yProp = pivot.GetType().GetProperty("y", BindingFlags.Instance | BindingFlags.Public);
            if (xProp == null || yProp == null)
                return null;

            var xVal = xProp.GetValue(pivot);
            var yVal = yProp.GetValue(pivot);
            if (xVal is not float px || yVal is not float py)
                return null;

            return (w * (px - 0.5f), h * (py - 0.5f));
        }
        catch
        {
            return null;
        }
    }

    private (float x, float y)? GetRectTransformFieldVector2(object rectTransform, string fieldName)
    {
        if (rectTransform == null || string.IsNullOrWhiteSpace(fieldName))
            return null;

        try
        {
            var field = rectTransform.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var value = field?.GetValue(rectTransform);
            if (value == null)
                return null;

            var xProp = value.GetType().GetProperty("x", BindingFlags.Instance | BindingFlags.Public);
            var yProp = value.GetType().GetProperty("y", BindingFlags.Instance | BindingFlags.Public);
            if (xProp == null || yProp == null)
                return null;

            var xVal = xProp.GetValue(value);
            var yVal = yProp.GetValue(value);
            if (xVal is float x && yVal is float y)
                return (x, y);
        }
        catch
        {
            return null;
        }

        return null;
    }

    private bool IsScreenSpaceOverlay(object rectTransform)
    {
        if (_canvasType == null || rectTransform == null)
            return false;

        try
        {
            var getComponents = rectTransform.GetType().GetMethod(
                "GetComponentsInParent",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(Type), typeof(bool) },
                null);
            if (getComponents == null)
                return false;

            var canvases = getComponents.Invoke(rectTransform, new object[] { _canvasType, true }) as Array;
            if (canvases == null || canvases.Length == 0)
                return false;

            var canvas = canvases.GetValue(0);
            if (canvas == null)
                return false;

            var renderModeProp = canvas.GetType().GetProperty("renderMode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var renderMode = renderModeProp?.GetValue(canvas);
            return renderMode != null && string.Equals(renderMode.ToString(), "ScreenSpaceOverlay", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private object GetCanvasCamera(object rectTransform)
    {
        if (_canvasType == null)
            return null;

        try
        {
            var getComponents = rectTransform.GetType().GetMethod(
                "GetComponentsInParent",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(Type), typeof(bool) },
                null);
            if (getComponents == null)
                return null;

            var canvases = getComponents.Invoke(rectTransform, new object[] { _canvasType, true }) as Array;
            if (canvases == null || canvases.Length == 0)
                return null;

            var canvas = canvases.GetValue(0);
            if (canvas == null)
                return null;

            var cameraProp = canvas.GetType().GetProperty("worldCamera", BindingFlags.Instance | BindingFlags.Public);
            return cameraProp?.GetValue(canvas);
        }
        catch
        {
            return null;
        }
    }

    private (float x, float y)? GetRectTransformWorldPosition(object rectTransform)
    {
        var worldPosition = GetTransformPosition(rectTransform);
        if (worldPosition != null)
            return worldPosition;

        try
        {
            var anchored = GetVector2FromPropertyOrMethod(rectTransform, "anchoredPosition3D", "get_anchoredPosition3D")
                ?? GetVector2FromPropertyOrMethod(rectTransform, "anchoredPosition", "get_anchoredPosition");
            if (anchored != null)
                return anchored;
        }
        catch
        {
            return null;
        }

        try
        {
            if (_vector3Type == null)
                return null;

            var method = rectTransform.GetType().GetMethod(
                "GetWorldCorners",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { _vector3Type.MakeArrayType() },
                null);
            if (method == null)
                return null;

            var corners = Array.CreateInstance(_vector3Type, 4);
            method.Invoke(rectTransform, new object[] { corners });

            if (corners.Length < 4)
                return null;

            var c0 = corners.GetValue(0);
            var c2 = corners.GetValue(2);
            if (c0 == null || c2 == null)
                return null;

            var x0 = c0.GetType().GetProperty("x", BindingFlags.Instance | BindingFlags.Public)?.GetValue(c0);
            var y0 = c0.GetType().GetProperty("y", BindingFlags.Instance | BindingFlags.Public)?.GetValue(c0);
            var x2 = c2.GetType().GetProperty("x", BindingFlags.Instance | BindingFlags.Public)?.GetValue(c2);
            var y2 = c2.GetType().GetProperty("y", BindingFlags.Instance | BindingFlags.Public)?.GetValue(c2);

            if (x0 is float fx0 && y0 is float fy0 && x2 is float fx2 && y2 is float fy2)
                return ((fx0 + fx2) * 0.5f, (fy0 + fy2) * 0.5f);
        }
        catch
        {
            return null;
        }

        return null;
    }

    private (float x, float y)? WorldToScreenPointWithUtility(object camera, (float x, float y) world)
    {
        if (_rectTransformUtilityType == null || _vector3Type == null)
            return null;

        try
        {
            if (_cameraType == null)
                return null;

            var method = _rectTransformUtilityType.GetMethod(
                "WorldToScreenPoint",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { _cameraType, _vector3Type },
                null);
            if (method == null)
                return null;

            var ctor = _vector3Type.GetConstructor(new[] { typeof(float), typeof(float), typeof(float) });
            var vector = ctor?.Invoke(new object[] { world.x, world.y, 0f });
            if (vector == null)
                return null;

            var result = method.Invoke(null, new[] { camera, vector });
            if (result == null)
                return null;

            var xProp = result.GetType().GetProperty("x", BindingFlags.Instance | BindingFlags.Public);
            var yProp = result.GetType().GetProperty("y", BindingFlags.Instance | BindingFlags.Public);
            if (xProp == null || yProp == null)
                return null;

            var xVal = xProp.GetValue(result);
            var yVal = yProp.GetValue(result);
            if (xVal is float sx && yVal is float sy)
                return (sx, sy);
        }
        catch
        {
            return null;
        }

        return null;
    }

    private IEnumerable<object> FindSceneObjectsOfType(Type type)
    {
        if (type == null || _findObjectsOfTypeMethod == null)
            yield break;

        Array result;
        try
        {
            result = _findObjectsOfTypeMethod.Invoke(null, new object[] { type }) as Array;
        }
        catch
        {
            yield break;
        }

        if (result == null)
            yield break;

        foreach (var item in result)
        {
            if (item == null)
                continue;

            if (!IsInAllowedScene(item))
                continue;

            var go = GetInteractableGameObject(item) ?? GetGameObjectFromComponent(item) ?? GetMemberValue(item, "gameObject");
            if (go != null && !IsGameObjectActive(go))
                continue;

            yield return item;
        }
    }

    private object GetComponentByType(object gameObject, Type type)
    {
        if (gameObject == null || type == null)
            return null;

        try
        {
            var method = gameObject.GetType().GetMethod("GetComponent", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(Type) }, null);
            return method?.Invoke(gameObject, new object[] { type });
        }
        catch
        {
            return null;
        }
    }

    private object GetComponentInParentByType(object gameObject, Type type)
    {
        if (gameObject == null || type == null)
            return null;

        var current = gameObject;
        for (var depth = 0; depth < 6 && current != null; depth++)
        {
            var component = GetComponentByType(current, type);
            if (component != null)
                return component;

            current = GetParentGameObject(current);
        }

        return null;
    }

    private (float x, float y)? GetColliderBoundsCenter(object collider)
    {
        if (collider == null)
            return null;

        try
        {
            var boundsProp = collider.GetType().GetProperty("bounds", BindingFlags.Instance | BindingFlags.Public);
            var bounds = boundsProp?.GetValue(collider);
            if (bounds == null)
                return null;

            var centerProp = bounds.GetType().GetProperty("center", BindingFlags.Instance | BindingFlags.Public);
            var center = centerProp?.GetValue(bounds);
            var centerPos = GetVector2FromValue(center);
            if (centerPos != null)
                return centerPos;
        }
        catch
        {
            return null;
        }

        return null;
    }

    

    private static float Clamp(float value, float min, float max)
    {
        if (value < min)
            return min;
        if (value > max)
            return max;
        return value;
    }

    private (float x, float y)? GetMousePosition()
    {
        if (_virtualCursorActive)
            return _virtualCursorPos;

        return GetRawMousePosition();
    }

    private bool TryGetElevatorSceneName(out string sceneName)
    {
        sceneName = null;

        if (_elevatorManagerType != null)
        {
            var instance = GetStaticInstance(_elevatorManagerType);
            if (instance != null)
            {
                var changing = GetMemberValue(instance, "bIsChangingScene");
                if (changing is bool changingScene && changingScene)
                    return false;

                var method = _elevatorManagerType.GetMethod("GetCurrentScene", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null)
                {
                    try
                    {
                        var sceneObj = method.Invoke(instance, null);
                        if (sceneObj != null)
                        {
                            sceneName = sceneObj.ToString();
                            return true;
                        }
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
        }

        return false;
    }

    private bool TryGetActiveUnitySceneName(out string sceneName)
    {
        sceneName = null;
        if (_sceneManagerType == null || _sceneType == null)
            return false;

        try
        {
            var method = _sceneManagerType.GetMethod("GetActiveScene", BindingFlags.Public | BindingFlags.Static);
            if (method == null)
                return false;

            var scene = method.Invoke(null, null);
            if (scene == null)
                return false;

            var nameProp = scene.GetType().GetProperty("name", BindingFlags.Instance | BindingFlags.Public);
            sceneName = nameProp?.GetValue(scene) as string;
            return !string.IsNullOrWhiteSpace(sceneName);
        }
        catch
        {
            return false;
        }
    }

    private bool IsInAllowedScene(object target)
    {
        if (target == null || _allowedSceneNames.Count == 0)
            return true;

        var go = GetInteractableGameObject(target) ?? GetGameObjectFromComponent(target) ?? GetMemberValue(target, "gameObject");
        if (go == null)
            return true;

        var sceneName = GetGameObjectSceneName(go);
        if (string.IsNullOrWhiteSpace(sceneName))
            return true;

        return _allowedSceneNames.Contains(sceneName);
    }

    private string GetGameObjectSceneName(object gameObject)
    {
        if (gameObject == null)
            return null;

        try
        {
            var sceneProp = gameObject.GetType().GetProperty("scene", BindingFlags.Instance | BindingFlags.Public);
            var scene = sceneProp?.GetValue(gameObject);
            if (scene == null)
                return null;

            var nameProp = scene.GetType().GetProperty("name", BindingFlags.Instance | BindingFlags.Public);
            return nameProp?.GetValue(scene) as string;
        }
        catch
        {
            return null;
        }
    }

    private (float x, float y)? GetRawMousePosition()
    {
        if (_inputType == null || _vector3Type == null)
            return null;

        try
        {
            _readingRawMousePosition = true;
            var prop = _inputType.GetProperty("mousePosition", BindingFlags.Public | BindingFlags.Static);
            var value = prop?.GetValue(null);
            if (value == null)
                return null;

            var xProp = value.GetType().GetProperty("x", BindingFlags.Instance | BindingFlags.Public);
            var yProp = value.GetType().GetProperty("y", BindingFlags.Instance | BindingFlags.Public);
            if (xProp == null || yProp == null)
                return null;

            var xVal = xProp.GetValue(value);
            var yVal = yProp.GetValue(value);
            if (xVal is float x && yVal is float y)
                return (x, y);
        }
        catch
        {
            return null;
        }
        finally
        {
            _readingRawMousePosition = false;
        }

        return null;
    }

    private int GetScreenDimension(string name)
    {
        if (_screenType == null)
            return 0;

        try
        {
            var prop = _screenType.GetProperty(name, BindingFlags.Public | BindingFlags.Static);
            var value = prop?.GetValue(null);
            return value is int intValue ? intValue : 0;
        }
        catch
        {
            return 0;
        }
    }

    private object GetInteractableAtScreenPosition(float x, float y)
    {
        if (_cameraType == null || _physics2DType == null || _vector3Type == null || _interactableType == null || _collider2DType == null)
            return null;

        var camera = GetMainCamera() ?? GetAnyCamera();
        if (camera == null)
            return null;

        try
        {
            var vector = Activator.CreateInstance(_vector3Type, x, y, 0f);
            var rayMethod = _cameraType.GetMethod("ScreenPointToRay", BindingFlags.Instance | BindingFlags.Public, null, new[] { _vector3Type }, null);
            if (rayMethod == null)
                return null;

            var ray = rayMethod.Invoke(camera, new[] { vector });
            if (ray == null)
                return null;

            var hitMethod = _physics2DType.GetMethod("GetRayIntersection", BindingFlags.Public | BindingFlags.Static, null, new[] { ray.GetType(), typeof(float) }, null)
                           ?? _physics2DType.GetMethod("GetRayIntersection", BindingFlags.Public | BindingFlags.Static);
            if (hitMethod == null)
                return null;

            object hit;
            if (hitMethod.GetParameters().Length >= 2)
            {
                hit = hitMethod.Invoke(null, new[] { ray, float.PositiveInfinity });
            }
            else
            {
                hit = hitMethod.Invoke(null, new[] { ray });
            }

            if (hit == null)
                return null;

            var colliderProp = hit.GetType().GetProperty("collider", BindingFlags.Instance | BindingFlags.Public);
            var collider = colliderProp?.GetValue(hit);
            if (collider == null)
                return null;

            var gameObjectProp = collider.GetType().GetProperty("gameObject", BindingFlags.Instance | BindingFlags.Public);
            var gameObject = gameObjectProp?.GetValue(collider);
            if (gameObject == null)
                return null;

            var getComponentInParent = gameObject.GetType().GetMethod("GetComponentInParent", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(Type) }, null);
            if (getComponentInParent == null)
                return null;

            var interactable = getComponentInParent.Invoke(gameObject, new object[] { _interactableType });
            if (interactable != null)
            {
                var resolved = ResolveInteractableForFocus(interactable);
                resolved = FilterDialogInteractable(resolved);
                return IsInAllowedScene(resolved) ? resolved : null;
            }

            if (_mirrorNavigationButtonType != null)
            {
                var mirrorNav = getComponentInParent.Invoke(gameObject, new object[] { _mirrorNavigationButtonType });
                if (mirrorNav != null && IsInAllowedScene(mirrorNav))
                    return mirrorNav;
            }

            if (_mirrorType != null)
            {
                var mirror = getComponentInParent.Invoke(gameObject, new object[] { _mirrorType });
                if (mirror != null && IsInAllowedScene(mirror))
                    return mirror;
            }

            var overlapPoint = _physics2DType.GetMethod("OverlapPoint", BindingFlags.Public | BindingFlags.Static, null, new[] { _vector2Type }, null);
            if (overlapPoint == null)
                return null;

            var point = Activator.CreateInstance(_vector2Type, x, y);
            var overlap = overlapPoint.Invoke(null, new[] { point });
            if (overlap == null)
                return null;

            var overlapGameObjectProp = overlap.GetType().GetProperty("gameObject", BindingFlags.Instance | BindingFlags.Public);
            var overlapGameObject = overlapGameObjectProp?.GetValue(overlap);
            if (overlapGameObject == null)
                return null;

            var overlapComponentInParent = overlapGameObject.GetType().GetMethod("GetComponentInParent", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(Type) }, null);
            if (overlapComponentInParent == null)
                return null;

            var overlapInteractable = overlapComponentInParent.Invoke(overlapGameObject, new object[] { _interactableType });
            var resolvedOverlap = ResolveInteractableForFocus(overlapInteractable);
            resolvedOverlap = FilterDialogInteractable(resolvedOverlap);
            if (IsInAllowedScene(resolvedOverlap))
                return resolvedOverlap;

            if (_mirrorNavigationButtonType != null)
            {
                var mirrorNav = overlapComponentInParent.Invoke(overlapGameObject, new object[] { _mirrorNavigationButtonType });
                if (mirrorNav != null && IsInAllowedScene(mirrorNav))
                    return mirrorNav;
            }

            if (_mirrorType != null)
            {
                var mirror = overlapComponentInParent.Invoke(overlapGameObject, new object[] { _mirrorType });
                if (mirror != null && IsInAllowedScene(mirror))
                    return mirror;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private object GetShopItemAtScreenPosition(float x, float y)
    {
        if (_cameraType == null || _physics2DType == null || _vector3Type == null || _vector2Type == null || _shopItemType == null || _collider2DType == null)
            return null;

        var camera = GetMainCamera() ?? GetAnyCamera();
        if (camera == null)
            return null;

        try
        {
            var screenVector = Activator.CreateInstance(_vector3Type, x, y, 0f);
            var screenToWorld = _cameraType.GetMethod("ScreenToWorldPoint", BindingFlags.Instance | BindingFlags.Public, null, new[] { _vector3Type }, null);
            if (screenToWorld == null)
                return null;

            var world = screenToWorld.Invoke(camera, new[] { screenVector });
            if (world == null)
                return null;

            var world2 = GetVector2FromValue(world);
            if (world2 == null)
                return null;

            var point = Activator.CreateInstance(_vector2Type, world2.Value.x, world2.Value.y);
            var overlapPoint = _physics2DType.GetMethod("OverlapPoint", BindingFlags.Public | BindingFlags.Static, null, new[] { _vector2Type }, null);
            if (overlapPoint == null)
                return null;

            var overlap = overlapPoint.Invoke(null, new[] { point });
            if (overlap == null)
                return null;

            var overlapGameObjectProp = overlap.GetType().GetProperty("gameObject", BindingFlags.Instance | BindingFlags.Public);
            var overlapGameObject = overlapGameObjectProp?.GetValue(overlap);
            if (overlapGameObject == null)
                return null;

            var getComponentInParent = overlapGameObject.GetType().GetMethod("GetComponentInParent", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(Type) }, null);
            if (getComponentInParent == null)
                return null;

            var shopItem = getComponentInParent.Invoke(overlapGameObject, new object[] { _shopItemType });
            return IsInAllowedScene(shopItem) ? shopItem : null;
        }
        catch
        {
            return null;
        }
    }

    private object GetGameObjectFromComponent(object component)
    {
        if (component == null)
            return null;

        try
        {
            var prop = component.GetType().GetProperty("gameObject", BindingFlags.Instance | BindingFlags.Public);
            return prop?.GetValue(component);
        }
        catch
        {
            return null;
        }
    }

    private Array GetComponentsByType(object gameObject, Type type)
    {
        if (gameObject == null || type == null)
            return null;

        try
        {
            var method = gameObject.GetType().GetMethod("GetComponents", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(Type) }, null);
            return method?.Invoke(gameObject, new object[] { type }) as Array;
        }
        catch
        {
            return null;
        }
    }

    private object FilterDialogInteractable(object interactable)
    {
        if (interactable == null)
            return null;

        if (IsDialogActive())
        {
            var roots = GetActiveDialogRoots();
            if (roots.Count == 0)
                return interactable;

            var go = GetInteractableGameObject(interactable) ?? GetMemberValue(interactable, "gameObject");
            if (go != null && IsUnderDialogRoot(go, roots))
                return interactable;

            return null;
        }

        if (IsMenuActive())
        {
            var roots = GetActiveMenuRoots();
            if (roots.Count == 0)
                return interactable;

            var go = GetInteractableGameObject(interactable) ?? GetMemberValue(interactable, "gameObject");
            if (go != null && IsUnderDialogRoot(go, roots))
                return interactable;

            return null;
        }

        return interactable;
    }

    private object ResolveInteractableForFocus(object interactable)
    {
        if (interactable == null)
            return null;

        var paperworkParent = GetPaperworkParent(interactable) ?? GetPaperworkFromHierarchy(interactable);
        if (paperworkParent != null)
        {
            if (!IsPaperworkFocused(paperworkParent))
                return paperworkParent;

            // Parent chosen; allow child focus at this level.
            return ReferenceEquals(interactable, paperworkParent) ? paperworkParent : interactable;
        }

        if (IsDeskItem(interactable))
        {
            if (!IsOfficeActive())
                return null;

            var deskGo = GetInteractableGameObject(interactable) ?? GetMemberValue(interactable, "gameObject");
            if (deskGo != null && !IsGameObjectActive(deskGo))
                return null;

            if (!IsDeskItemInDrawer(interactable))
                return interactable;

            if (TryGetDeskItemDrawerType(interactable, out var drawerType))
            {
                var drawer = GetDrawerByType(drawerType);
                if (drawer != null && IsDrawerOpen(drawer))
                {
                    // Parent chosen (drawer open) so child can be focused.
                    return interactable;
                }

                // Parent not chosen; focus the drawer instead if available.
                return drawer ?? null;
            }
        }

        return interactable;
    }

    private object GetPaperworkParent(object interactable)
    {
        if (interactable == null)
            return null;

        if (IsPaperwork(interactable))
            return interactable;

        if (!IsPaperworkMark(interactable))
            return null;

        try
        {
            var field = interactable.GetType().GetField("PaperworkParent", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return field?.GetValue(interactable);
        }
        catch
        {
            return null;
        }
    }

    private object GetPaperworkFromHierarchy(object interactable)
    {
        if (_paperworkType == null || interactable == null)
            return null;

        var current = GetInteractableGameObject(interactable) ?? interactable;
        while (current != null)
        {
            var component = GetComponentFromGameObject(current, _paperworkType);
            if (component != null)
                return component;

            current = GetParentGameObject(current);
        }

        return null;
    }

    private bool IsPaperwork(object interactable)
    {
        if (interactable == null)
            return false;

        if (_paperworkType != null && _paperworkType.IsInstanceOfType(interactable))
            return true;

        return string.Equals(interactable.GetType().Name, "Paperwork", StringComparison.Ordinal);
    }

    private bool IsPaperworkMark(object interactable)
    {
        if (interactable == null)
            return false;

        try
        {
            return string.Equals(interactable.GetType().Name, "PaperworkMark", StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    private bool IsPaperworkFocused(object paperwork)
    {
        if (_paperworkType == null || paperwork == null)
            return false;

        try
        {
            var statusField = _paperworkType.GetField("Status", BindingFlags.Instance | BindingFlags.Public);
            var statusValue = statusField?.GetValue(paperwork);
            return statusValue != null && string.Equals(statusValue.ToString(), "FOCUS", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private bool IsDeskItem(object interactable)
    {
        return _deskItemType != null && interactable != null && _deskItemType.IsInstanceOfType(interactable);
    }

    private bool TryGetDeskItemDrawerType(object item, out object drawerType)
    {
        drawerType = null;
        if (_deskItemType == null || item == null || !_deskItemType.IsInstanceOfType(item))
            return false;

        try
        {
            var statusField = _deskItemType.GetField("ItemStatus", BindingFlags.Instance | BindingFlags.Public);
            var status = statusField?.GetValue(item);
            if (status == null)
                return false;

            var drawerField = status.GetType().GetField("DrawerStatus", BindingFlags.Instance | BindingFlags.Public);
            drawerType = drawerField?.GetValue(status);
            if (drawerType == null)
                return false;

            return !string.Equals(drawerType.ToString(), "MAX", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private object GetDrawerByType(object drawerType)
    {
        if (_grimDeskDrawerType == null || drawerType == null)
            return null;

        try
        {
            var method = _grimDeskDrawerType.GetMethod("GetDrawerByType", BindingFlags.Public | BindingFlags.Static);
            if (method != null)
                return method.Invoke(null, new[] { drawerType });

            var name = drawerType.ToString();
            var fieldName = string.Equals(name, "Left", StringComparison.OrdinalIgnoreCase) ? "instanceLeft"
                : string.Equals(name, "Right", StringComparison.OrdinalIgnoreCase) ? "instanceRight"
                : null;
            if (fieldName == null)
                return null;

            var field = _grimDeskDrawerType.GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
            return field?.GetValue(null);
        }
        catch
        {
            return null;
        }
    }

    private bool IsDrawerOpen(object drawer)
    {
        if (drawer == null || _grimDeskDrawerType == null)
            return false;

        try
        {
            var isOpenMethod = _grimDeskDrawerType.GetMethod("IsOpen", BindingFlags.Instance | BindingFlags.Public);
            if (isOpenMethod == null)
                return false;

            var result = isOpenMethod.Invoke(drawer, null);
            return result is bool open && open;
        }
        catch
        {
            return false;
        }
    }

    private object GetMainCamera()
    {
        if (_cameraType == null)
            return null;

        try
        {
            var mainProp = _cameraType.GetProperty("main", BindingFlags.Public | BindingFlags.Static);
            return mainProp?.GetValue(null);
        }
        catch
        {
            return null;
        }
    }

    private object GetAnyCamera()
    {
        if (_cameraType == null)
            return null;

        try
        {
            var mainProp = _cameraType.GetProperty("main", BindingFlags.Public | BindingFlags.Static);
            var main = mainProp?.GetValue(null);
            if (main != null)
                return main;
        }
        catch
        {
            // Ignore.
        }

        try
        {
            var allProp = _cameraType.GetProperty("allCameras", BindingFlags.Public | BindingFlags.Static);
            var all = allProp?.GetValue(null) as Array;
            if (all != null && all.Length > 0)
                return all.GetValue(0);
        }
        catch
        {
            // Ignore.
        }

        try
        {
            var currentProp = _cameraType.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
            return currentProp?.GetValue(null);
        }
        catch
        {
            return null;
        }
    }

    private void ClearInteractableFocus()
    {
        if (_lastFocusedInteractable == null)
            return;

        CallInteractableMethod(_lastFocusedInteractable, "Unhover");
        _lastFocusedInteractable = null;
        UpdateInputManagerHover(null);
        UpdateHudHoverText(string.Empty);
    }


    private object GetTransform(object gameObject)
    {
        try
        {
            var prop = gameObject.GetType().GetProperty("transform", BindingFlags.Instance | BindingFlags.Public);
            return prop?.GetValue(gameObject);
        }
        catch
        {
            return null;
        }
    }

    private bool IsTransformChildOf(object transform, object potentialParent)
    {
        try
        {
            var method = transform.GetType().GetMethod("IsChildOf", BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
                return false;

            var result = method.Invoke(transform, new[] { potentialParent });
            return result is bool isChild && isChild;
        }
        catch
        {
            return false;
        }
    }

    private void SampleAxes(out float axisX, out float axisY)
    {
        var now = Environment.TickCount;
        if (now == _lastAxisTick)
        {
            axisX = _cachedAxisX;
            axisY = _cachedAxisY;
            return;
        }

        _cachedAxisX = GetAxisRawAny(
            "Horizontal",
            "DPadX",
            "DpadX",
            "DPadHorizontal",
            "DpadHorizontal",
            "DPad_X",
            "Dpad_X",
            "HatX",
            "JoyHatX");
        _cachedAxisY = GetAxisRawAny(
            "Vertical",
            "DPadY",
            "DpadY",
            "DPadVertical",
            "DpadVertical",
            "DPad_Y",
            "Dpad_Y",
            "HatY",
            "JoyHatY");
        if (IsOfficeActive() && (GetKey("W") || GetKey("A") || GetKey("S") || GetKey("D")))
        {
            _cachedAxisX = 0f;
            _cachedAxisY = 0f;
        }
        _lastAxisTick = now;
        axisX = _cachedAxisX;
        axisY = _cachedAxisY;
    }

    private NavigationDirection GetNavigationDirection(float axisX, float axisY)
    {
        if (_inputType == null || _keyCodeType == null)
            return NavigationDirection.None;

        var now = Environment.TickCount;
        if (now < _nextKeyTick)
            return NavigationDirection.None;

        var axisUp = axisY > AxisDeadzone;
        var axisDown = axisY < -AxisDeadzone;
        var axisLeft = axisX < -AxisDeadzone;
        var axisRight = axisX > AxisDeadzone;

        if (GetKeyDown("UpArrow") || (axisUp && !_axisUpHeld))
        {
            _nextKeyTick = now + AxisRepeatMs;
            return NavigationDirection.Up;
        }

        if (GetKeyDown("DownArrow") || (axisDown && !_axisDownHeld))
        {
            _nextKeyTick = now + AxisRepeatMs;
            return NavigationDirection.Down;
        }

        if (GetKeyDown("LeftArrow") || (axisLeft && !_axisLeftHeld))
        {
            _nextKeyTick = now + AxisRepeatMs;
            return NavigationDirection.Left;
        }

        if (GetKeyDown("RightArrow") || (axisRight && !_axisRightHeld))
        {
            _nextKeyTick = now + AxisRepeatMs;
            return NavigationDirection.Right;
        }

        _axisUpHeld = axisUp;
        _axisDownHeld = axisDown;
        _axisLeftHeld = axisLeft;
        _axisRightHeld = axisRight;
        if (!GetAnyKey("Return", "KeypadEnter") && !GetAnyJoystickButton(0) && !GetAnyJoystickButton(1))
            _submitHeld = false;

        return NavigationDirection.None;
    }


    private bool GetAnyKey(params string[] keyNames)
    {
        if (keyNames == null)
            return false;

        foreach (var name in keyNames)
        {
            if (!string.IsNullOrWhiteSpace(name) && GetKey(name))
                return true;
        }

        return false;
    }

    private bool GetAnyJoystickButton(int button)
    {
        if (GetKey($"JoystickButton{button}"))
            return true;

        for (var i = 1; i <= 16; i++)
        {
            if (GetKey($"Joystick{i}Button{button}"))
                return true;
        }

        return false;
    }

    private bool IsSubmitPressedForInteractables()
    {
        if (_inputType == null || _keyCodeType == null)
            return false;

        var submitHeld = GetAnyKey("Return", "KeypadEnter") || GetAnyJoystickButton(1) || GetAnyKey("JoystickButton0", "Joystick1Button0", "Joystick2Button0", "Joystick3Button0", "Joystick4Button0");
        if (submitHeld && !_submitHeld)
        {
            _submitHeld = true;
            return true;
        }

        return false;
    }


    private void SyncEventSystemSelectionIfNeeded()
    {
        if (_eventSystemType == null)
            return;

        var now = Environment.TickCount;
        if (_pendingEventSystemSyncUntil > 0 && now > _pendingEventSystemSyncUntil)
            _pendingEventSystemSyncUntil = 0;

        if (_pendingEventSystemSyncUntil == 0 && _lastEventSelected == null)
            return;

        var current = GetCurrentSelectedGameObject();
        if (current == null)
            return;

        var selectable = _selectableType != null ? GetComponentByType(current, _selectableType) : null;
        if (selectable == null)
            return;

        var width = GetScreenDimension("width");
        var height = GetScreenDimension("height");
        if (!IsSelectableEligible(selectable, width, height, requireScreen: true))
            return;

        if (ReferenceEquals(current, _lastEventSelected))
            return;

        _lastEventSelected = current;
        SetInteractableFocus(selectable);
    }

    private bool ShouldDeferToEventSystem()
    {
        if (_eventSystemType == null || _selectableType == null)
            return false;

        var current = GetCurrentSelectedGameObject();
        if (current == null)
            return false;

        var selectable = GetComponentByType(current, _selectableType);
        if (selectable == null)
            return false;

        var width = GetScreenDimension("width");
        var height = GetScreenDimension("height");
        return IsSelectableEligible(selectable, width, height, requireScreen: true);
    }

    private bool TryMoveEventSystemSelection(NavigationDirection direction)
    {
        if (_eventSystemType == null || _selectableType == null)
            return false;

        var current = GetCurrentSelectedGameObject();
        if (current == null)
            return false;

        var currentSelectable = GetComponentByType(current, _selectableType);
        if (currentSelectable == null)
            return false;

        var methodName = direction switch
        {
            NavigationDirection.Up => "FindSelectableOnUp",
            NavigationDirection.Down => "FindSelectableOnDown",
            NavigationDirection.Left => "FindSelectableOnLeft",
            NavigationDirection.Right => "FindSelectableOnRight",
            _ => null
        };

        if (string.IsNullOrWhiteSpace(methodName))
            return false;

        try
        {
            var method = currentSelectable.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
                return false;

            var next = method.Invoke(currentSelectable, null);
            if (next != null && !ReferenceEquals(next, currentSelectable))
            {
                var width = GetScreenDimension("width");
                var height = GetScreenDimension("height");
                if (IsSelectableEligible(next, width, height, requireScreen: true))
                {
                    SetUiSelected(next);
                    _lastEventSelected = GetInteractableGameObject(next) ?? next;
                    SetInteractableFocus(next);
                    return true;
                }
            }

            if (TryFindSelectableLinear(direction, out var fallback))
            {
                SetUiSelected(fallback);
                _lastEventSelected = GetInteractableGameObject(fallback) ?? fallback;
                SetInteractableFocus(fallback);
                return true;
            }

            return false;
        }
        catch
        {
            // Ignore UI navigation failures.
        }

        return false;
    }

    private bool GetKeyDown(string keyName)
    {
        if (_inputType == null || _keyCodeType == null || string.IsNullOrWhiteSpace(keyName))
            return false;

        if (!_keyCodes.TryGetValue(keyName, out var keyCode))
        {
            try
            {
                keyCode = Enum.Parse(_keyCodeType, keyName);
                _keyCodes[keyName] = keyCode;
            }
            catch
            {
                return false;
            }
        }

        try
        {
            if (_getKeyDownMethod == null)
                return false;

            var result = _getKeyDownMethod.Invoke(null, new[] { keyCode });
            return result is bool pressed && pressed;
        }
        catch
        {
            return false;
        }
    }

    private bool GetKey(string keyName)
    {
        if (_inputType == null || _keyCodeType == null || string.IsNullOrWhiteSpace(keyName))
            return false;

        if (!_keyCodes.TryGetValue(keyName, out var keyCode))
        {
            try
            {
                keyCode = Enum.Parse(_keyCodeType, keyName);
                _keyCodes[keyName] = keyCode;
            }
            catch
            {
                return false;
            }
        }

        try
        {
            if (_getKeyMethod == null)
                return false;

            var result = _getKeyMethod.Invoke(null, new[] { keyCode });
            return result is bool pressed && pressed;
        }
        catch
        {
            return false;
        }
    }

    private bool IsMouseClickDown()
    {
        return GetKeyDown("Mouse0") || GetKeyDown("Mouse1") || GetKeyDown("Mouse2");
    }

    private bool IsShiftHeld()
    {
        return GetKey("LeftShift") || GetKey("RightShift");
    }

    private float GetAxisRaw(string axisName)
    {
        try
        {
            if (_getAxisRawMethod == null)
                return 0f;

            var result = _getAxisRawMethod.Invoke(null, new object[] { axisName });
            return result is float value ? value : 0f;
        }
        catch
        {
            return 0f;
        }
    }

    private float GetAxis(string axisName)
    {
        try
        {
            if (_getAxisMethod == null)
                return 0f;

            var result = _getAxisMethod.Invoke(null, new object[] { axisName });
            return result is float value ? value : 0f;
        }
        catch
        {
            return 0f;
        }
    }

    private float GetAxisRawAny(params string[] axisNames)
    {
        if (axisNames == null || axisNames.Length == 0)
            return 0f;

        var best = 0f;
        foreach (var name in axisNames)
        {
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var value = GetAxisRaw(name);
            if (Math.Abs(value) > Math.Abs(best))
                best = value;
        }

        return best;
    }

    private float GetAxisAny(params string[] axisNames)
    {
        if (axisNames == null || axisNames.Length == 0)
            return 0f;

        var best = 0f;
        foreach (var name in axisNames)
        {
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var value = GetAxis(name);
            if (Math.Abs(value) > Math.Abs(best))
                best = value;
        }

        return best;
    }


    private void SubmitInteractable()
    {
        var uiObject = GetUiRaycastGameObject();
        if (uiObject != null)
        {
            var uiTarget = ResolveUiFocusTarget(uiObject) ?? uiObject;
            SetUiSelected(uiTarget);
            _lastFocusedInteractable = uiTarget;
            _lastEventSelected = GetInteractableGameObject(uiTarget) ?? uiTarget;

            if (TryInvokeUiClickResult(uiTarget))
            {
                _virtualCursorMovedSinceLastSubmit = false;
                return;
            }

            if (TryInvokeInteract(uiTarget))
            {
                _virtualCursorMovedSinceLastSubmit = false;
                return;
            }

            TryInvokeUiClick(uiTarget);
            _virtualCursorMovedSinceLastSubmit = false;
            return;
        }

        if (_virtualCursorActive)
        {
            var mousePosition = GetMousePosition();
            if (mousePosition != null)
            {
                var hit = GetInteractableAtScreenPosition(mousePosition.Value.x, mousePosition.Value.y);
                if (hit != null)
                    _lastFocusedInteractable = hit;
                else if (_virtualCursorMovedSinceLastSubmit)
                    return;
            }
            else if (_virtualCursorMovedSinceLastSubmit)
            {
                return;
            }
        }

        if (_lastFocusedInteractable == null)
        {
            _lastFocusedInteractable = GetInputManagerLastHit();
        }
        else if (_keyboardFocusActive)
        {
            // Keep keyboard focus; do not override with mouse hover.
        }

        if (_lastFocusedInteractable == null)
        {
            var selected = GetCurrentSelectedGameObject();
            if (selected != null)
                _lastFocusedInteractable = GetComponentByType(selected, _selectableType) ?? selected;
        }

        if (_lastFocusedInteractable == null)
        {
            var mousePosition = GetMousePosition();
            if (mousePosition != null)
                _lastFocusedInteractable = GetInteractableAtScreenPosition(mousePosition.Value.x, mousePosition.Value.y);
        }

        if (_lastFocusedInteractable == null)
            return;

        var paperworkParent = GetPaperworkParent(_lastFocusedInteractable) ?? GetPaperworkFromHierarchy(_lastFocusedInteractable);
        if (paperworkParent != null)
            _lastFocusedInteractable = paperworkParent;

        if (TryErasePaperworkMark(_lastFocusedInteractable))
            return;

        if (!TryInvokeInteract(_lastFocusedInteractable))
            TryInvokeUiClick(_lastFocusedInteractable);

        TryAnnounceDressingRoomSelection(_lastFocusedInteractable);
        _virtualCursorMovedSinceLastSubmit = false;

        // No extra focus logic; rely on the virtual cursor + raycast.
    }

    private bool TryErasePaperworkMark(object target)
    {
        if (!IsEraserHeld())
            return false;

        if (target == null)
            return false;

        if (IsPaperworkMark(target))
            return TryEraseMarkInstance(target);

        var paperwork = IsPaperwork(target)
            ? target
            : GetPaperworkParent(target) ?? GetPaperworkFromHierarchy(target);

        if (paperwork == null)
            return false;

        var status = GetMemberValue(paperwork, "MarkStatus");
        if (status == null)
            return false;

        var statusText = status.ToString();
        if (string.Equals(statusText, "Unmarked", StringComparison.OrdinalIgnoreCase))
            return false;

        object mark = null;
        if (string.Equals(statusText, "Live", StringComparison.OrdinalIgnoreCase))
            mark = GetMemberValue(paperwork, "MarkLive");
        else if (string.Equals(statusText, "Die", StringComparison.OrdinalIgnoreCase))
            mark = GetMemberValue(paperwork, "MarkDie");

        return TryEraseMarkInstance(mark);
    }

    private bool TryEraseMarkInstance(object mark)
    {
        if (mark == null)
            return false;

        try
        {
            var method = mark.GetType().GetMethod("EraseMark", BindingFlags.Instance | BindingFlags.Public);
            if (method != null)
            {
                method.Invoke(mark, null);
                return true;
            }
        }
        catch
        {
            return false;
        }

        return TryInvokeInteract(mark);
    }

    private bool IsEraserHeld()
    {
        if (_eraserType == null)
            return false;

        var instance = GetStaticInstance(_eraserType);
        if (instance == null)
            return false;

        var eraserGo = GetMemberValue(instance, "gameObject");
        if (eraserGo != null && !IsGameObjectActive(eraserGo))
            return false;

        try
        {
            var method = _eraserType.GetMethod("IsPickedUp", BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
                return false;

            var result = method.Invoke(instance, null);
            return result is bool held && held;
        }
        catch
        {
            return false;
        }
    }

    private bool TryAdjustFocusedSlider(NavigationDirection direction)
    {
        if (direction != NavigationDirection.Left && direction != NavigationDirection.Right)
            return false;

        if (TryAdjustOptionsSlider(direction))
            return true;

        var current = GetCurrentSelectedGameObject();
        if (current == null)
            current = _lastEventSelected ?? _lastFocusedInteractable ?? GetUiRaycastGameObject();
        if (current == null)
            return false;

        var currentSelectable = _selectableType != null ? GetComponentByType(current, _selectableType) : null;
        if (currentSelectable != null && !IsSelectableInteractable(currentSelectable))
            return false;

        var slider = FindSliderComponent(current);
        if (slider == null)
            return false;

        var min = 0f;
        var max = 1f;
        var value = 0f;
        var minObj = GetMemberValue(slider, "minValue");
        if (minObj is float minF) min = minF;
        else if (minObj is double minD) min = (float)minD;
        else if (minObj is int minI) min = minI;
        else if (minObj is long minL) min = minL;
        else if (minObj is decimal minM) min = (float)minM;
        var maxObj = GetMemberValue(slider, "maxValue");
        if (maxObj is float maxF) max = maxF;
        else if (maxObj is double maxD) max = (float)maxD;
        else if (maxObj is int maxI) max = maxI;
        else if (maxObj is long maxL) max = maxL;
        else if (maxObj is decimal maxM) max = (float)maxM;
        var valueObj = GetMemberValue(slider, "value");
        if (valueObj is float valueF) value = valueF;
        else if (valueObj is double valueD) value = (float)valueD;
        else if (valueObj is int valueI) value = valueI;
        else if (valueObj is long valueL) value = valueL;
        else if (valueObj is decimal valueM) value = (float)valueM;
        else value = min;
        var step = (max - min) * 0.05f;
        if (step <= 0f)
            step = 0.05f;

        value += direction == NavigationDirection.Right ? step : -step;
        value = Clamp(value, Math.Min(min, max), Math.Max(min, max));

        var wholeNumbersObj = GetMemberValue(slider, "wholeNumbers");
        if (wholeNumbersObj is bool wholeNumbers && wholeNumbers)
            value = (float)Math.Round(value);

        return SetFloatMember(slider, "value", value);
    }

    private bool TryAdjustOptionsSlider(NavigationDirection direction)
    {
        if (_optionsManagerType == null)
            return false;

        var instance = GetStaticInstance(_optionsManagerType);
        if (instance == null)
            return false;

        var instanceGo = GetMemberValue(instance, "gameObject");
        if (instanceGo != null && !IsGameObjectActive(instanceGo))
            return false;

        var current = GetCurrentSelectedGameObject() ?? _lastEventSelected ?? GetUiRaycastGameObject();
        if (current == null)
            return false;

        var slider = ResolveOptionsSlider(instance, current);
        if (slider == null)
            return false;

        var min = 0f;
        var max = 1f;
        var value = 0f;
        var minObj = GetMemberValue(slider, "minValue");
        if (minObj is float minF) min = minF;
        else if (minObj is double minD) min = (float)minD;
        else if (minObj is int minI) min = minI;
        else if (minObj is long minL) min = minL;
        else if (minObj is decimal minM) min = (float)minM;
        var maxObj = GetMemberValue(slider, "maxValue");
        if (maxObj is float maxF) max = maxF;
        else if (maxObj is double maxD) max = (float)maxD;
        else if (maxObj is int maxI) max = maxI;
        else if (maxObj is long maxL) max = maxL;
        else if (maxObj is decimal maxM) max = (float)maxM;
        var valueObj = GetMemberValue(slider, "value");
        if (valueObj is float valueF) value = valueF;
        else if (valueObj is double valueD) value = (float)valueD;
        else if (valueObj is int valueI) value = valueI;
        else if (valueObj is long valueL) value = valueL;
        else if (valueObj is decimal valueM) value = (float)valueM;
        else value = min;
        var step = (max - min) * 0.05f;
        if (step <= 0f)
            step = 0.05f;

        value += direction == NavigationDirection.Right ? step : -step;
        value = Clamp(value, Math.Min(min, max), Math.Max(min, max));

        var wholeNumbersObj = GetMemberValue(slider, "wholeNumbers");
        if (wholeNumbersObj is bool wholeNumbers && wholeNumbers)
            value = (float)Math.Round(value);

        if (!SetFloatMember(slider, "value", value))
            return false;

        try
        {
            var method = _optionsManagerType.GetMethod("OnSliderChanged", BindingFlags.Instance | BindingFlags.Public);
            method?.Invoke(instance, new object[] { value });
        }
        catch
        {
            // Ignore.
        }

        return true;
    }

    private object ResolveOptionsSlider(object optionsManager, object selected)
    {
        var sliderFields = new[] { "SliderVolumeMaster", "SliderVolumeGeneral", "SliderVolumeVoice", "SliderVolumeMusic" };
        foreach (var field in sliderFields)
        {
            var slider = GetMemberValue(optionsManager, field);
            if (slider == null)
                continue;

            var sliderGo = GetGameObjectFromComponent(slider) ?? GetMemberValue(slider, "gameObject");
            if (sliderGo == null)
                continue;

            var selectedGo = GetGameObjectFromComponent(selected) ?? selected;
            if (selectedGo == null)
                continue;

            var sliderTransform = GetTransform(sliderGo);
            var selectedTransform = GetTransform(selectedGo);
            if (sliderTransform == null || selectedTransform == null)
                continue;

            if (ReferenceEquals(sliderTransform, selectedTransform))
                return slider;

            if (IsTransformChildOf(selectedTransform, sliderTransform))
                return slider;
        }

        return null;
    }

    private object FindSliderComponent(object selected)
    {
        if (selected == null)
            return null;

        if (_sliderType != null && _sliderType.IsInstanceOfType(selected))
            return selected;

        var gameObject = GetInteractableGameObject(selected) ?? GetGameObjectFromComponent(selected) ?? selected;
        if (gameObject == null)
            return null;

        if (_sliderType != null)
        {
            var slider = GetComponentByType(gameObject, _sliderType);
            if (slider != null)
                return slider;
        }

        var parent = GetParentGameObject(gameObject);
        var depth = 0;
        while (parent != null && depth < 8)
        {
            if (_sliderType != null)
            {
                var slider = GetComponentByType(parent, _sliderType);
                if (slider != null)
                    return slider;
            }
            parent = GetParentGameObject(parent);
            depth++;
        }

        var sliderLike = FindSliderLikeComponentOn(gameObject);
        if (sliderLike != null)
            return sliderLike;

        parent = GetParentGameObject(gameObject);
        depth = 0;
        while (parent != null && depth < 8)
        {
            sliderLike = FindSliderLikeComponentOn(parent);
            if (sliderLike != null)
                return sliderLike;
            parent = GetParentGameObject(parent);
            depth++;
        }

        return null;
    }

    private object FindSliderLikeComponentOn(object gameObject)
    {
        if (gameObject == null || _componentType == null)
            return null;

        var components = GetComponentsByType(gameObject, _componentType);
        if (components == null)
            return null;

        foreach (var component in components)
        {
            if (component == null)
                continue;

            if (HasSliderMembers(component))
                return component;
        }

        return null;
    }

    private bool HasSliderMembers(object instance)
    {
        if (instance == null)
            return false;

        var type = instance.GetType();
        return type.GetProperty("value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != null
               && type.GetProperty("minValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != null
               && type.GetProperty("maxValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != null;
    }

    private bool TryInvokeInteract(object target)
    {
        try
        {
            var method = target.GetType().GetMethod("Interact", BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
                return false;

            method.Invoke(target, null);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void TryInvokeUiClick(object target)
    {
        if (target == null)
            return;

        var gameObject = GetInteractableGameObject(target) ?? target;
        if (gameObject == null)
            return;

        var button = GetComponentByName(gameObject, "UnityEngine.UI.Button");
        if (button == null)
            return;

        try
        {
            var onClickProp = button.GetType().GetProperty("onClick", BindingFlags.Instance | BindingFlags.Public);
            var onClick = onClickProp?.GetValue(button);
            var invoke = onClick?.GetType().GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public);
            invoke?.Invoke(onClick, null);
        }
        catch
        {
            // Ignore UI invoke failures.
        }
    }

    private bool TryInvokeUiClickResult(object target)
    {
        if (target == null)
            return false;

        var gameObject = GetInteractableGameObject(target) ?? target;
        if (gameObject == null)
            return false;

        var button = GetComponentByName(gameObject, "UnityEngine.UI.Button");
        if (button == null)
            return false;

        try
        {
            var onClickProp = button.GetType().GetProperty("onClick", BindingFlags.Instance | BindingFlags.Public);
            var onClick = onClickProp?.GetValue(button);
            var invoke = onClick?.GetType().GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public);
            invoke?.Invoke(onClick, null);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private string GetGameObjectName(object interactable)
    {
        var gameObject = GetInteractableGameObject(interactable);
        if (gameObject == null)
            return null;

        try
        {
            var prop = gameObject.GetType().GetProperty("name", BindingFlags.Instance | BindingFlags.Public);
            return prop?.GetValue(gameObject) as string;
        }
        catch
        {
            return null;
        }
    }

    private object GetInputManagerLastHit()
    {
        if (_inputManagerType == null)
            return null;

        try
        {
            var instanceField = _inputManagerType.GetField("instance", BindingFlags.Public | BindingFlags.Static);
            var instance = instanceField?.GetValue(null);
            if (instance == null)
                return null;

            var lastHitField = _inputManagerType.GetField("LastHitInteractable", BindingFlags.Public | BindingFlags.Instance);
            return lastHitField?.GetValue(instance);
        }
        catch
        {
            return null;
        }
    }

    private object GetInteractableGameObject(object interactable)
    {
        if (interactable == null)
            return null;

        if (_gameObjectType != null && _gameObjectType.IsInstanceOfType(interactable))
            return interactable;

        try
        {
            var prop = interactable.GetType().GetProperty("gameObject", BindingFlags.Instance | BindingFlags.Public);
            if (prop != null)
                return prop.GetValue(interactable);

            var getter = interactable.GetType().GetMethod("get_gameObject", BindingFlags.Instance | BindingFlags.Public);
            return getter?.Invoke(interactable, null);
        }
        catch
        {
            return null;
        }
    }

    private object GetComponentByName(object gameObject, string typeName)
    {
        try
        {
            var method = gameObject.GetType().GetMethod("GetComponent", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(Type) }, null);
            if (method == null)
                return null;

        var type = TypeResolver.Get(typeName);
        if (type == null)
            return null;

            return method.Invoke(gameObject, new object[] { type });
        }
        catch
        {
            return null;
        }
    }

    private (float x, float y)? GetInteractableScreenPosition(object interactable)
    {
        if (IsPaperwork(interactable))
        {
            var paperCollider = GetMemberValue(interactable, "ColliderPaperwork");
            var paperCenter = GetColliderBoundsCenter(paperCollider);
            if (paperCenter != null)
            {
                var projected = TryProjectScreenPosition((paperCenter.Value.x, paperCenter.Value.y, 0f));
                if (projected != null)
                    return projected;
            }

            var paperGo = GetInteractableGameObject(interactable);
            var paperPos = paperGo != null ? GetTransformPosition3(paperGo) : null;
            if (paperPos != null)
            {
                var projected = TryProjectScreenPosition(paperPos.Value);
                if (projected != null)
                    return projected;
            }

            foreach (var fieldName in new[] { "PositionDesktop", "OriginPosition", "PositionFocus" })
            {
                var fieldValue = GetMemberValue(interactable, fieldName);
                var vector3 = GetVector3FromValue(fieldValue);
                if (vector3 != null)
                {
                    var projected = TryProjectScreenPosition(vector3.Value);
                    if (projected != null)
                        return projected;
                }
            }
        }

        var gameObject = GetInteractableGameObject(interactable);
        var width = GetScreenDimension("width");
        var height = GetScreenDimension("height");
        if (gameObject != null)
        {
            var uiScreen = GetUiScreenPosition(gameObject, width, height);
            if (uiScreen != null)
                return uiScreen;
        }

        var position3 = gameObject != null ? GetTransformPosition3(gameObject) : null;
        if (position3 == null)
            position3 = GetTransformPosition3(interactable);
        if (position3 != null)
        {
            var projected = TryProjectScreenPosition(position3.Value);
            if (projected != null)
                return projected;
        }

        var position = gameObject != null ? GetTransformPosition(gameObject) : null;
        if (position == null)
            position = GetTransformPosition(interactable);

        if (position == null && IsDrawer(interactable))
            position = GetDrawerPosition(interactable);

        if (position == null && IsDrawer(interactable))
        {
            var drawerCollider = GetMemberValue(interactable, "Collider");
            var drawerCenter = GetColliderBoundsCenter(drawerCollider);
            if (drawerCenter != null)
                position = drawerCenter;
        }

        if (position == null)
            return null;

        var projected2d = TryProjectScreenPosition((position.Value.x, position.Value.y, 0f));
        if (projected2d != null)
            return projected2d;

        if (gameObject != null)
        {
            var uiFallback = GetUiScreenPosition(gameObject, width, height);
            if (uiFallback != null)
                return uiFallback;
        }

        if (width > 0 && height > 0)
            return ToScreenPosition(position.Value, width, height);

        return null;
    }

    private bool IsDrawer(object interactable)
    {
        return _grimDeskDrawerType != null && interactable != null && _grimDeskDrawerType.IsInstanceOfType(interactable);
    }

    private (float x, float y)? GetDrawerPosition(object interactable)
    {
        var gameObject = GetInteractableGameObject(interactable);
        if (gameObject == null)
            return null;

        return GetTransformPosition(gameObject);
    }


    private (float x, float y)? GetColliderBoundsCenterFromGameObject(object gameObject)
    {
        if (gameObject == null)
            return null;

        var colliders2D = GetComponentsInChildren(gameObject, _collider2DType);
        var center = GetBoundsCenterFromComponents(colliders2D);
        if (center != null)
            return center;

        var colliders3D = GetComponentsInChildren(gameObject, _collider3DType);
        return GetBoundsCenterFromComponents(colliders3D);
    }

    private bool IsDeskItemInDrawer(object item)
    {
        if (_deskItemType == null || item == null || !_deskItemType.IsInstanceOfType(item))
            return false;

        try
        {
            var method = _deskItemType.GetMethod("IsInDrawer", BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
                return false;

            var result = method.Invoke(item, null);
            var inDrawer = result is bool inDrawerValue && inDrawerValue;
            if (!inDrawer)
                return false;

            var world = GetTransformPosition(GetInteractableGameObject(item) ?? item);
            var localZ = GetLocalPositionZ(GetInteractableGameObject(item) ?? item);
            if (localZ.HasValue && localZ.Value > -0.1f)
                return false;

            if (world == null || _vector2Type == null)
                return inDrawer;

            var point = Activator.CreateInstance(_vector2Type, world.Value.x, world.Value.y);
            foreach (var drawer in GetDeskDrawersFromGrimDesk())
            {
                if (drawer == null)
                    continue;

                var collider = GetFieldValueLocal(drawer, "Collider");
                if (collider == null)
                    continue;

                var overlap = collider.GetType().GetMethod("OverlapPoint", BindingFlags.Instance | BindingFlags.Public, null, new[] { _vector2Type }, null);
                if (overlap == null)
                    continue;

                var hit = overlap.Invoke(collider, new[] { point });
                if (hit is bool hitValue && hitValue)
                    return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private float? GetLocalPositionZ(object gameObject)
    {
        try
        {
            var transform = GetTransform(gameObject);
            if (transform == null)
                return null;

            object value = null;
            var prop = transform.GetType().GetProperty("localPosition", BindingFlags.Instance | BindingFlags.Public);
            if (prop != null)
                value = prop.GetValue(transform);
            else
            {
                var getter = transform.GetType().GetMethod("get_localPosition", BindingFlags.Instance | BindingFlags.Public);
                value = getter?.Invoke(transform, null);
            }

            if (value == null)
                return null;

            var zProp = value.GetType().GetProperty("z", BindingFlags.Instance | BindingFlags.Public);
            if (zProp != null)
            {
                var zVal = zProp.GetValue(value);
                if (zVal is float z)
                    return z;
            }

            var zField = value.GetType().GetField("z", BindingFlags.Instance | BindingFlags.Public);
            if (zField != null)
            {
                var zVal = zField.GetValue(value);
                if (zVal is float z)
                    return z;
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private (float x, float y)? GetRendererBoundsCenterFromGameObject(object gameObject)
    {
        if (gameObject == null || _rendererType == null)
            return null;

        var renderers = GetComponentsInChildren(gameObject, _rendererType);
        return GetBoundsCenterFromComponents(renderers);
    }

    private (float x, float y)? GetBoundsCenterFromComponents(Array components)
    {
        if (components == null || components.Length == 0)
            return null;

        foreach (var component in components)
        {
            if (component == null)
                continue;

            try
            {
                var boundsProp = component.GetType().GetProperty("bounds", BindingFlags.Instance | BindingFlags.Public);
                var bounds = boundsProp?.GetValue(component);
                if (bounds == null)
                    continue;

                var centerProp = bounds.GetType().GetProperty("center", BindingFlags.Instance | BindingFlags.Public);
                var center = centerProp?.GetValue(bounds);
                var centerPos = GetVector2FromValue(center);
                if (centerPos != null)
                    return centerPos;
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    private Array GetComponentsInChildren(object gameObject, Type componentType)
    {
        if (gameObject == null || componentType == null)
            return null;

        try
        {
            var method = gameObject.GetType().GetMethod(
                "GetComponentsInChildren",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(Type), typeof(bool) },
                null);
            if (method != null)
                return method.Invoke(gameObject, new object[] { componentType, true }) as Array;
        }
        catch
        {
            // Ignore and fall back.
        }

        try
        {
            var method = gameObject.GetType().GetMethod(
                "GetComponentsInChildren",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(Type) },
                null);
            return method?.Invoke(gameObject, new object[] { componentType }) as Array;
        }
        catch
        {
            return null;
        }
    }

    private (float x, float y)? WorldToScreenPoint((float x, float y, float z) world)
    {
        if (_cameraType == null)
            return null;

        try
        {
            var camera = GetMainCamera() ?? GetAnyCamera();
            if (camera == null)
                return null;

            var method = _cameraType.GetMethod("WorldToScreenPoint", BindingFlags.Instance | BindingFlags.Public, null, new[] { _vector3Type }, null);
            if (method == null)
                return null;

            var ctor = _vector3Type?.GetConstructor(new[] { typeof(float), typeof(float), typeof(float) });
            var vector = ctor?.Invoke(new object[] { world.x, world.y, world.z });
            if (vector == null)
                return null;

            var result = method.Invoke(camera, new object[] { vector });
            if (result == null)
                return null;

            var xProp = result.GetType().GetProperty("x", BindingFlags.Instance | BindingFlags.Public);
            var yProp = result.GetType().GetProperty("y", BindingFlags.Instance | BindingFlags.Public);
            if (xProp == null || yProp == null)
                return null;

            var xVal = xProp.GetValue(result);
            var yVal = yProp.GetValue(result);
            if (xVal is float x && yVal is float y)
                return (x, y);
        }
        catch
        {
            return null;
        }

        return null;
    }

    private (float x, float y)? WorldToScreenPoint((float x, float y) world)
    {
        return WorldToScreenPoint((world.x, world.y, 0f));
    }

    private (float x, float y)? WorldToScreenPointWithCamera((float x, float y, float z) world)
    {
        var camera = GetMainCamera() ?? GetAnyCamera();
        if (camera == null)
            return null;

        try
        {
            var method = camera.GetType().GetMethod("WorldToScreenPoint", BindingFlags.Instance | BindingFlags.Public, null, new[] { _vector3Type ?? TypeResolver.Get("UnityEngine.Vector3") }, null);
            if (method == null)
                return null;

            var vector3Type = _vector3Type ?? TypeResolver.Get("UnityEngine.Vector3");
            if (vector3Type == null)
                return null;

            var ctor = vector3Type.GetConstructor(new[] { typeof(float), typeof(float), typeof(float) });
            if (ctor == null)
                return null;

            var vector = ctor.Invoke(new object[] { world.x, world.y, world.z });
            var result = method.Invoke(camera, new[] { vector });
            if (result == null)
                return null;

            var xProp = result.GetType().GetProperty("x", BindingFlags.Instance | BindingFlags.Public);
            var yProp = result.GetType().GetProperty("y", BindingFlags.Instance | BindingFlags.Public);
            if (xProp == null || yProp == null)
                return null;

            var xVal = xProp.GetValue(result);
            var yVal = yProp.GetValue(result);
            if (xVal is float x && yVal is float y)
                return (x, y);
        }
        catch
        {
            return null;
        }

        return null;
    }

    private void SetInteractableFocus(object interactable)
    {
        if (interactable == null)
            return;

        if (!IsInteractableInstance(interactable) && (IsMenuActive() || IsDialogActive() || IsSpeechBubbleDialogActive()))
            interactable = ResolveUiFocusTarget(interactable) ?? interactable;

        if (IsInteractableInstance(interactable) && !IsInteractableActive(interactable))
            return;

        var focusKey = GetFocusTargetKey(interactable);
        if (!string.IsNullOrWhiteSpace(focusKey)
            && string.Equals(focusKey, _lastFocusedTargetKey, StringComparison.Ordinal))
        {
            return;
        }

        if (_lastFocusedInteractable != null && IsSameUiTarget(_lastFocusedInteractable, interactable))
            return;

        if (_lastFocusedInteractable != null && !ReferenceEquals(_lastFocusedInteractable, interactable))
        {
            if (IsInteractableInstance(_lastFocusedInteractable))
                CallInteractableMethod(_lastFocusedInteractable, "Unhover");
        }

        _lastFocusedInteractable = interactable;
        _lastFocusedTargetKey = focusKey;
        _keyboardFocusActive = true;
        _keyboardFocusUntilTick = Environment.TickCount + 1500;
        if (IsInteractableInstance(interactable))
        {
            CallInteractableMethod(interactable, "Hover");
            UpdateInputManagerHover(interactable);
        }
        else
        {
            SetUiSelected(interactable);
        }
        var hoverText = GetHoverText(interactable);
        if (string.IsNullOrWhiteSpace(hoverText))
        {
            var shopItem = ResolveShopItem(interactable);
            if (shopItem != null)
                hoverText = GetHoverText(shopItem);
        }
        var speechBubbleText = GetSpeechBubbleTextForInteractable(interactable);
        if (!string.IsNullOrWhiteSpace(speechBubbleText))
            hoverText = speechBubbleText;
        UpdateHudHoverText(hoverText);
        AnnounceHoverText(hoverText, interactable);
    }

    private string GetFocusTargetKey(object interactable)
    {
        if (interactable == null)
            return null;

        var uiTarget = ResolveUiFocusTarget(interactable) ?? interactable;
        var gameObject = GetInteractableGameObject(uiTarget) ?? GetGameObjectFromComponent(uiTarget) ?? uiTarget;
        if (gameObject == null)
            return uiTarget.GetType().FullName;

        var key = GetHierarchyOrderKey(gameObject);
        if (!string.IsNullOrWhiteSpace(key))
            return key;

        var name = GetGameObjectName(gameObject);
        if (!string.IsNullOrWhiteSpace(name))
            return name;

        return uiTarget.GetType().FullName;
    }

    private bool IsInteractableInstance(object instance)
    {
        return instance != null && _interactableType != null && _interactableType.IsInstanceOfType(instance);
    }

    private bool IsInteractableActive(object interactable)
    {
        if (!IsInteractableInstance(interactable))
            return true;

        var go = GetInteractableGameObject(interactable) ?? GetMemberValue(interactable, "gameObject");
        return go == null || IsGameObjectActive(go);
    }

    private void CallInteractableMethod(object interactable, string methodName)
    {
        try
        {
            var method = interactable.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            method?.Invoke(interactable, null);
        }
        catch
        {
            // Ignore hover failures.
        }
    }

    private void UpdateInputManagerHover(object interactable)
    {
        if (_inputManagerType == null)
            return;

        try
        {
            var instanceField = _inputManagerType.GetField("instance", BindingFlags.Public | BindingFlags.Static);
            var instance = instanceField?.GetValue(null);
            if (instance == null)
                return;

            var lastHitField = _inputManagerType.GetField("LastHitInteractable", BindingFlags.Public | BindingFlags.Instance);
            lastHitField?.SetValue(instance, interactable);
        }
        catch
        {
            // Ignore.
        }
    }

    private void UpdateHudHoverText(string hoverText)
    {
        if (_hudManagerType == null)
            return;

        try
        {
            var instanceField = _hudManagerType.GetField("instance", BindingFlags.Public | BindingFlags.Static);
            var instance = instanceField?.GetValue(null);
            if (instance == null)
                return;

            var method = _hudManagerType.GetMethod("SetHoverText", BindingFlags.Instance | BindingFlags.Public);
            method?.Invoke(instance, new object[] { hoverText ?? string.Empty });
        }
        catch
        {
            // Ignore.
        }
    }

    private void AnnounceHoverText(string hoverText, object interactable)
    {
        if (_screenreader == null)
            return;

        if (IsMenuActive() || IsDialogActive() || IsSpeechBubbleDialogActive())
            return;

        var built = BuildHoverText(interactable, hoverText);
        if (string.IsNullOrWhiteSpace(built))
            return;

        var signature = BuildHoverAnnouncementSignature(interactable, built);
        if (!string.IsNullOrWhiteSpace(signature)
            && string.Equals(signature, _lastHoverAnnouncementSignature, StringComparison.Ordinal))
        {
            return;
        }

        if (_screenreader.ShouldSuppressHover() || _screenreader.IsBusy)
            return;

        _screenreader.Announce(built);
        _lastHoverAnnouncementSignature = signature;
        return;
    }

    private string BuildHoverAnnouncementSignature(object interactable, string builtText)
    {
        if (string.IsNullOrWhiteSpace(builtText))
            return null;

        if (IsMenuActive() || IsDialogActive() || IsSpeechBubbleDialogActive())
        {
            var selected = ResolveUiFocusTarget(GetCurrentSelectedGameObject());
            if (selected != null)
                interactable = selected;
            else
                interactable = ResolveUiFocusTarget(interactable) ?? interactable;
        }

        var normalizedText = NormalizeAnnouncementText(builtText);
        var gameObject = GetInteractableGameObject(interactable) ?? GetGameObjectFromComponent(interactable) ?? interactable;
        var key = gameObject != null ? GetHierarchyOrderKey(gameObject) : null;
        if (string.IsNullOrWhiteSpace(key))
            key = GetGameObjectName(gameObject) ?? interactable?.GetType().Name ?? "unknown";

        return $"{key}|{normalizedText}";
    }

    private static string NormalizeAnnouncementText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var trimmed = value.Trim();
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
                continue;
            }

            builder.Append(ch);
            lastWasSpace = false;
        }

        return builder.ToString();
    }

    private string BuildHoverText(object interactable, string hoverText)
    {
        var speechBubbleText = GetSpeechBubbleTextForInteractable(interactable);
        if (!string.IsNullOrWhiteSpace(speechBubbleText))
            return speechBubbleText;

        if (!string.IsNullOrWhiteSpace(hoverText) && IsMoneyNotificationText(hoverText))
            return null;

        var dressingLabel = GetDressingRoomInteractableLabel(interactable);
        if (!string.IsNullOrWhiteSpace(dressingLabel))
        {
            if (string.IsNullOrWhiteSpace(hoverText))
                hoverText = dressingLabel;
            else if (hoverText.IndexOf(dressingLabel, StringComparison.OrdinalIgnoreCase) < 0)
                hoverText = $"{dressingLabel}. {hoverText}";
        }

        hoverText = StripLeadingMoneySentence(hoverText);

        var shopItem = ResolveShopItem(interactable) ?? interactable;
        if (IsShopActive())
        {
            var shopHover = GetHudHoverShopText();
            if (string.IsNullOrWhiteSpace(shopHover))
                shopHover = hoverText;

            shopHover = SanitizeHoverText(shopHover);
            shopHover = StripLeadingMoneySentence(shopHover);
            if (!string.IsNullOrWhiteSpace(shopHover) && !LooksLikeValueText(shopHover))
            {
                var nameText = GetShopUiNameText();
                var priceText = GetShopUiPriceText();
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(nameText))
                    parts.Add(SanitizeHoverText(nameText));
                parts.Add(shopHover);
                if (!string.IsNullOrWhiteSpace(priceText))
                    parts.Add(priceText);
                return string.Join(". ", parts);
            }
        }
        else
        {
            var hudHover = GetHudHoverText();
            hudHover = SanitizeHoverText(hudHover);
            hudHover = StripLeadingMoneySentence(hudHover);
            if (!string.IsNullOrWhiteSpace(hudHover) && !LooksLikeValueText(hudHover))
                return hudHover;
        }

        var shopDescription = GetShopItemDescription(shopItem);
        var shopName = GetShopItemName(shopItem);
        var name = SanitizeHoverText(GetGameObjectName(interactable));
        hoverText = SanitizeHoverText(hoverText);
        shopDescription = SanitizeHoverText(shopDescription);
        if (string.IsNullOrWhiteSpace(shopName))
            shopName = GetFriendlyInteractableName(interactable);
        if (string.IsNullOrWhiteSpace(shopName) && shopItem != null)
            shopName = GetShopDisplayNameFromInstance();
        if (string.IsNullOrWhiteSpace(shopName) && IsShopActive())
            shopName = GetShopUiNameText();

        if (IsShopActive())
        {
            var hudShop = GetHudHoverShopText();
            if (!string.IsNullOrWhiteSpace(hudShop) && IsShopDescriptionCandidate(hudShop))
            {
                if (string.IsNullOrWhiteSpace(shopDescription) || IsLocalizationKey(shopDescription))
                    shopDescription = hudShop;
            }
        }

        if (string.IsNullOrWhiteSpace(shopDescription) && !string.IsNullOrWhiteSpace(hoverText))
        {
            var extracted = ExtractShopDescriptionFromHoverText(hoverText, shopName);
            if (!string.IsNullOrWhiteSpace(extracted))
                shopDescription = extracted;
        }

        if (!string.IsNullOrWhiteSpace(shopDescription))
        {
            if (string.IsNullOrWhiteSpace(hoverText))
                hoverText = shopDescription;
            else if (hoverText.IndexOf(shopDescription, StringComparison.OrdinalIgnoreCase) < 0)
                hoverText = $"{shopDescription}. {hoverText}";
        }

        if (!string.IsNullOrWhiteSpace(shopName))
        {
            if (string.IsNullOrWhiteSpace(hoverText))
                hoverText = shopName;
            else
                hoverText = $"{shopName}. {hoverText}";
            name = null;
        }

        var shopPrice = GetShopItemPriceText(shopItem);
        if (!string.IsNullOrWhiteSpace(shopPrice))
        {
            if (string.IsNullOrWhiteSpace(hoverText))
                hoverText = shopPrice;
            else
                hoverText = $"{hoverText}. {shopPrice}";
        }

        var calendarDay = GetCalendarDayText(interactable);
        if (!string.IsNullOrWhiteSpace(calendarDay))
        {
            if (string.IsNullOrWhiteSpace(hoverText))
                hoverText = calendarDay;
            else
                hoverText = $"{hoverText}. {calendarDay}";
        }

        if (string.IsNullOrWhiteSpace(hoverText))
            hoverText = GetFriendlyInteractableName(interactable) ?? name;

        if (IsShopActive() && !string.IsNullOrWhiteSpace(hoverText) && LooksLikeValueText(hoverText))
        {
            if (!string.IsNullOrWhiteSpace(shopName) || !string.IsNullOrWhiteSpace(shopDescription))
                hoverText = $"{shopName ?? shopDescription}";
            else
                hoverText = null;
        }

        if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(hoverText)
            && hoverText.IndexOf(name, StringComparison.OrdinalIgnoreCase) < 0)
        {
            hoverText = $"{name}. {hoverText}";
        }

        hoverText = StripLeadingMoneySentence(hoverText);

        return hoverText;
    }

    private string GetDressingRoomInteractableLabel(object interactable)
    {
        return null;
    }

    private void TryAnnounceDressingRoomSelection(object interactable)
    {
        if (_screenreader == null)
            return;

        if (interactable == null || _mirrorNavigationButtonType == null || !_mirrorNavigationButtonType.IsInstanceOfType(interactable))
            return;

        var typeValue = GetMemberValue(interactable, "Type") ?? GetMemberValue(interactable, "type");
        var typeText = typeValue?.ToString();
        if (string.IsNullOrWhiteSpace(typeText))
            return;

        var mirror = _mirrorType != null ? GetStaticInstance(_mirrorType) : null;
        if (mirror == null)
            return;

        object textObj = null;
        if (string.Equals(typeText, "Head", StringComparison.OrdinalIgnoreCase))
            textObj = GetMemberValue(mirror, "TextHead");
        else if (string.Equals(typeText, "Body", StringComparison.OrdinalIgnoreCase))
            textObj = GetMemberValue(mirror, "TextBody");

        if (textObj == null)
            return;

        string selection = null;
        try
        {
            var textProp = textObj.GetType().GetProperty("text", BindingFlags.Instance | BindingFlags.Public);
            selection = textProp?.GetValue(textObj) as string;
        }
        catch
        {
            selection = null;
        }

        selection = SanitizeHoverText(selection);
        if (string.IsNullOrWhiteSpace(selection))
            return;

        if (IsDialogActive())
            return;

        if (_screenreader.IsBusy)
            return;

        _screenreader.Announce(selection);
    }

    private string StripLeadingMoneySentence(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var trimmed = text.Trim();
        var sign = trimmed[0] == '+' || trimmed[0] == '-' ? 1 : 0;
        var idx = sign;
        while (idx < trimmed.Length && char.IsDigit(trimmed[idx]))
            idx++;

        if (idx == sign)
            return text;

        while (idx < trimmed.Length && char.IsWhiteSpace(trimmed[idx]))
            idx++;

        if (idx < trimmed.Length && trimmed[idx] == '.')
        {
            var remainder = trimmed.Substring(idx + 1).TrimStart();
            return string.IsNullOrWhiteSpace(remainder) ? null : remainder;
        }

        return text;
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

    private string GetHudHoverText()
    {
        if (_hudManagerType == null)
            return null;

        try
        {
            var instanceField = _hudManagerType.GetField("instance", BindingFlags.Public | BindingFlags.Static);
            var instance = instanceField?.GetValue(null);
            if (instance == null)
                return null;

            var textHoverField = _hudManagerType.GetField("TextHover", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var textHover = textHoverField?.GetValue(instance);
            var raw = ReadStringValue(GetMemberValue(textHover, "text"));
            return SanitizeHoverText(raw);
        }
        catch
        {
            return null;
        }
    }


    private string GetCalendarDayText(object interactable)
    {
        if (_calendarType == null || interactable == null)
            return null;

        if (!_calendarType.IsInstanceOfType(interactable))
            return null;

        var dayNumber = GetCurrentDayNumber();
        if (dayNumber <= 0)
            return null;

        return dayNumber.ToString();
    }

    private int GetCurrentDayNumber()
    {
        if (_saveManagerType == null)
            return 0;

        try
        {
            var instanceField = _saveManagerType.GetField("instance", BindingFlags.Public | BindingFlags.Static);
            var instance = instanceField?.GetValue(null);
            if (instance == null)
                return 0;

            var getState = _saveManagerType.GetMethod("GetCurrentPlayerState", BindingFlags.Instance | BindingFlags.Public);
            var state = getState?.Invoke(instance, null);
            if (state == null)
                return 0;

            var method = state.GetType().GetMethod(
                "GetCurrentDayNumberNotIndexThisHasOneAddedToIt",
                BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
                return 0;

            var value = method.Invoke(state, null);
            if (value is int day)
                return day;
        }
        catch
        {
            return 0;
        }

        return 0;
    }


    private string GetShopItemName(object interactable)
    {
        if (interactable == null)
            return null;

        if (!IsShopItem(interactable))
            return null;

        var itemData = GetMemberValue(interactable, "ItemData") ?? GetMemberValue(interactable, "itemData");
        var template = itemData != null ? GetMemberValue(itemData, "Template") ?? GetMemberValue(itemData, "template") : null;
        var itemDataObj = template != null ? GetMemberValue(template, "item_data") ?? GetMemberValue(template, "ItemData") : null;
        var source = itemDataObj ?? itemData;

        if (source != null)
        {
            foreach (var field in new[]
                     {
                         "item_name", "itemName", "ItemName",
                         "Unresolved_item_name", "LocaKey_item_name",
                         "name", "Name",
                         "display_name", "displayName", "DisplayName",
                         "item_title", "itemTitle", "ItemTitle",
                         "title", "Title",
                         "item_name_key", "itemNameKey", "name_key", "nameKey", "NameKey"
                     })
            {
                var value = ReadStringValue(GetMemberValue(source, field));
                if (!string.IsNullOrWhiteSpace(value) && IsShopNameCandidate(value))
                    return value;
            }
        }

        var uiName = GetShopUiNameText();
        if (!string.IsNullOrWhiteSpace(uiName) && IsShopNameCandidate(uiName))
            return uiName;

        var gameObject = GetInteractableGameObject(interactable) ?? GetMemberValue(interactable, "gameObject");
        var text = FindFirstUsefulTextInChildren(gameObject);
        if (string.IsNullOrWhiteSpace(text))
            text = FindNearestText(gameObject);
        if (string.IsNullOrWhiteSpace(text))
            text = FindSiblingLabelText(gameObject);
        text = SanitizeHoverText(text);
        if (!string.IsNullOrWhiteSpace(text) && IsShopNameCandidate(text))
            return text;

        return null;
    }

    private bool IsShopItemOwned(object interactable)
    {
        if (!IsShopItem(interactable))
            return true;

        try
        {
            var itemData = GetMemberValue(interactable, "ItemData");
            if (itemData == null)
                return false;

            var template = GetMemberValue(itemData, "Template");
            var itemDataObj = GetMemberValue(template, "item_data");
            var variable = GetMemberValue(itemDataObj, "item_variable");
            if (variable == null)
                return false;

            var method = variable.GetType().GetMethod("CallScript", BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
                return false;

            var result = method.Invoke(variable, null);
            return result is bool owned && owned;
        }
        catch
        {
            return false;
        }
    }

    private string GetShopItemPriceText(object interactable)
    {
        if (interactable == null)
            return null;

        if (!IsShopItem(interactable))
            return null;

        try
        {
            var method = interactable.GetType().GetMethod("GetPrice", BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
                throw new MissingMethodException();

            var result = method.Invoke(interactable, null);
            if (result is int price)
                return price.ToString();
            if (result is float f)
                return Math.Round(f).ToString();
            if (result is double d)
                return Math.Round(d).ToString();
        }
        catch
        {
            // Fall back to known price fields.
        }

        var itemData = GetMemberValue(interactable, "ItemData") ?? GetMemberValue(interactable, "itemData");
        var template = itemData != null ? GetMemberValue(itemData, "Template") ?? GetMemberValue(itemData, "template") : null;
        var itemDataObj = template != null ? GetMemberValue(template, "item_data") ?? GetMemberValue(template, "ItemData") : null;
        var source = itemDataObj ?? itemData ?? interactable;

        foreach (var field in new[]
                 {
                     "price", "Price", "item_price", "itemPrice", "ItemPrice",
                     "cost", "Cost", "item_cost", "itemCost", "ItemCost"
                 })
        {
            var value = GetMemberValue(source, field);
            if (value is int price)
                return price.ToString();
            if (value is long longPrice)
                return longPrice.ToString();
            if (value is float f)
                return Math.Round(f).ToString();
            if (value is double d)
                return Math.Round(d).ToString();
            if (value is decimal dec)
                return Math.Round(dec).ToString();

            var text = ReadStringValue(value);
            if (!string.IsNullOrWhiteSpace(text) && double.TryParse(text, out var parsed))
                return Math.Round(parsed).ToString();
        }

        var uiPrice = GetShopUiPriceText();
        if (!string.IsNullOrWhiteSpace(uiPrice))
            return uiPrice;

        return null;
    }

    private string GetShopItemDescription(object interactable)
    {
        if (interactable == null || !IsShopItem(interactable))
            return null;

        var itemData = GetMemberValue(interactable, "ItemData") ?? GetMemberValue(interactable, "itemData");
        var template = itemData != null ? GetMemberValue(itemData, "Template") ?? GetMemberValue(itemData, "template") : null;
        var itemDataObj = template != null ? GetMemberValue(template, "item_data") ?? GetMemberValue(template, "ItemData") : null;
        var source = itemDataObj ?? itemData;

        if (source != null)
        {
            foreach (var field in new[]
                     {
                         "item_description", "item_desc", "description", "desc", "itemDescription", "itemDesc",
                         "Unresolved_item_description", "LocaKey_item_description",
                         "item_description_key", "itemDescKey", "item_desc_key", "description_key", "desc_key"
                     })
            {
                var value = ReadStringValue(GetMemberValue(source, field));
                if (!string.IsNullOrWhiteSpace(value) && IsShopDescriptionCandidate(value) && !IsLocalizationKey(value))
                    return value;
            }

            foreach (var field in new[]
                     {
                         "item_flavour_text_first", "item_flavour_text_second", "item_flavour_text_third",
                         "Unresolved_item_flavour_text_first", "Unresolved_item_flavour_text_second", "Unresolved_item_flavour_text_third",
                         "LocaKey_item_flavour_text_first", "LocaKey_item_flavour_text_second", "LocaKey_item_flavour_text_third"
                     })
            {
                var value = ReadStringValue(GetMemberValue(source, field));
                if (!string.IsNullOrWhiteSpace(value) && IsShopDescriptionCandidate(value) && !IsLocalizationKey(value))
                    return value;
            }
        }

        var gameObject = GetInteractableGameObject(interactable) ?? GetMemberValue(interactable, "gameObject");
        var text = FindFirstUsefulTextInChildren(gameObject);
        if (string.IsNullOrWhiteSpace(text))
            text = FindNearestText(gameObject);
        if (string.IsNullOrWhiteSpace(text))
            text = FindSiblingLabelText(gameObject);
        if (!string.IsNullOrWhiteSpace(text) && IsShopDescriptionCandidate(text))
            return text;

        return null;
    }

    private string GetShopDisplayNameFromInstance()
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
            var text = ReadStringValue(GetMemberValue(textName, "text"));
            return SanitizeHoverText(text);
        }
        catch
        {
            return null;
        }
    }

    private string GetShopUiNameText()
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
            return SanitizeHoverText(ReadStringValue(GetMemberValue(textName, "text")));
        }
        catch
        {
            return null;
        }
    }

    private string GetShopUiPriceText()
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
            var raw = SanitizeHoverText(ReadStringValue(GetMemberValue(textPrice, "text")));
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            return raw.Trim();
        }
        catch
        {
            return null;
        }
    }

    private string GetHudHoverShopText()
    {
        if (_hudManagerType == null)
            return null;

        try
        {
            var instanceField = _hudManagerType.GetField("instance", BindingFlags.Public | BindingFlags.Static);
            var instance = instanceField?.GetValue(null);
            if (instance == null)
                return null;

            var textHoverShopField = _hudManagerType.GetField("TextHoverShop", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var textHoverShop = textHoverShopField?.GetValue(instance);
            var raw = ReadStringValue(GetMemberValue(textHoverShop, "text"));
            return SanitizeHoverText(raw);
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

            var go = GetMemberValue(instance, "gameObject");
            return go != null && IsGameObjectActive(go);
        }
        catch
        {
            return false;
        }
    }

    private string ReadStringValue(object value)
    {
        if (value == null)
            return null;

        if (value is string raw)
            return SanitizeHoverText(raw);

        var type = value.GetType();
        foreach (var name in new[]
                 {
                     "Value", "value", "Text", "text", "String", "string",
                     "LocalizedString", "localizedString",
                     "Key", "key", "m_key",
                     "Term", "term", "mTerm", "m_term"
                 })
        {
            var memberValue = GetMemberValue(value, name);
            if (memberValue is string memberText)
                return SanitizeHoverText(memberText);
        }

        try
        {
            var method = type.GetMethod("GetLocalizedString", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method != null)
            {
                var result = method.Invoke(value, null) as string;
                if (!string.IsNullOrWhiteSpace(result))
                    return SanitizeHoverText(result);
            }
        }
        catch
        {
            // Ignore localized getter failures.
        }

        try
        {
            var text = value.ToString();
            if (!string.IsNullOrWhiteSpace(text) && !string.Equals(text, type.FullName, StringComparison.Ordinal)
                && !string.Equals(text, type.Name, StringComparison.Ordinal))
            {
                return SanitizeHoverText(text);
            }
        }
        catch
        {
            // Ignore ToString failures.
        }

        return null;
    }

    private bool IsShopDescriptionCandidate(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var value = text.Trim();
        if (LooksLikeValueText(value))
            return false;

        return true;
    }

    private bool IsLocalizationKey(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var value = text.Trim();
        if (value.StartsWith("Ntt_", StringComparison.OrdinalIgnoreCase))
            return true;

        if (value.IndexOf(".item_data.item_description", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        if (value.IndexOf("item_data.item_description", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        return false;
    }

    private bool IsShopNameCandidate(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var value = text.Trim();
        if (LooksLikeValueText(value))
            return false;

        return true;
    }

    private bool IsShopDescriptionCandidateLoose(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var value = text.Trim();
        if (LooksLikeValueText(value))
            return false;

        return true;
    }

    private string ExtractShopDescriptionFromHoverText(string hoverText, string shopName)
    {
        if (string.IsNullOrWhiteSpace(hoverText))
            return null;

        var cleaned = SanitizeHoverText(hoverText);
        if (string.IsNullOrWhiteSpace(cleaned))
            return null;

        var normalized = cleaned.Replace("\r\n", ". ").Replace("\n", ". ").Replace("\r", ". ");
        var parts = normalized.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var raw in parts)
        {
            var sentence = raw?.Trim();
            if (string.IsNullOrWhiteSpace(sentence))
                continue;

            if (!string.IsNullOrWhiteSpace(shopName)
                && string.Equals(sentence, shopName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!IsShopDescriptionCandidateLoose(sentence))
                continue;

            return sentence;
        }

        return null;
    }

    private int GetShopItemSlot(object shopItem)
    {
        if (shopItem == null)
            return int.MaxValue;

        try
        {
            var itemData = GetMemberValue(shopItem, "ItemData") ?? GetMemberValue(shopItem, "itemData");
            var template = itemData != null ? GetMemberValue(itemData, "Template") ?? GetMemberValue(itemData, "template") : null;
            var itemDataObj = template != null ? GetMemberValue(template, "item_data") ?? GetMemberValue(template, "ItemData") : null;
            var source = itemDataObj ?? itemData;
            var slotValue = GetMemberValue(source, "item_slot_number") ?? GetMemberValue(source, "ItemSlotNumber");
            if (slotValue is int slot)
                return slot;
            if (slotValue is long longSlot)
                return (int)longSlot;
        }
        catch
        {
            return int.MaxValue;
        }

        return int.MaxValue;
    }

    private bool IsShopItem(object interactable)
    {
        return _shopItemType != null && interactable != null && _shopItemType.IsInstanceOfType(interactable);
    }

    private object ResolveShopItem(object interactable)
    {
        if (interactable == null)
            return null;

        if (IsShopItem(interactable))
            return interactable;

        if (_shopItemType == null)
            return null;

        var gameObject = GetInteractableGameObject(interactable) ?? GetMemberValue(interactable, "gameObject");
        if (gameObject == null)
            return null;

        try
        {
            var getComponentInParent = gameObject.GetType().GetMethod("GetComponentInParent", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(Type) }, null);
            if (getComponentInParent != null)
            {
                var parent = getComponentInParent.Invoke(gameObject, new object[] { _shopItemType });
                if (parent != null)
                    return parent;
            }

            var getComponentInChildren = gameObject.GetType().GetMethod("GetComponentInChildren", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(Type) }, null);
            if (getComponentInChildren != null)
            {
                var child = getComponentInChildren.Invoke(gameObject, new object[] { _shopItemType });
                if (child != null)
                    return child;
            }

            var getComponent = gameObject.GetType().GetMethod("GetComponent", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(Type) }, null);
            return getComponent?.Invoke(gameObject, new object[] { _shopItemType });
        }
        catch
        {
            return null;
        }
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

    private string GetFriendlyInteractableName(object interactable)
    {
        var gameObject = GetInteractableGameObject(interactable);
        if (gameObject == null)
            return null;

        var text = FindFirstUsefulTextInChildren(gameObject);
        if (string.IsNullOrWhiteSpace(text))
            text = FindNearestText(gameObject);
        if (string.IsNullOrWhiteSpace(text))
            text = FindSiblingLabelText(gameObject);

        return SanitizeHoverText(text);
    }

    private string FindFirstUsefulTextInChildren(object gameObject)
    {
        if (gameObject == null)
            return null;

        try
        {
            var method = gameObject.GetType().GetMethod("GetComponentsInChildren", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(Type) }, null);
            if (method == null)
                return null;

            foreach (var textType in GetTextTypes())
            {
                if (textType == null)
                    continue;

                var results = method.Invoke(gameObject, new object[] { textType }) as Array;
                if (results == null)
                    continue;

                foreach (var item in results)
                {
                    if (item == null)
                        continue;

                    var textProp = item.GetType().GetProperty("text", BindingFlags.Instance | BindingFlags.Public);
                    var value = textProp?.GetValue(item) as string;
                    value = SanitizeHoverText(value);
                    if (string.IsNullOrWhiteSpace(value))
                        continue;

                    if (LooksLikeValueText(value))
                        continue;

                    return value;
                }
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static string SanitizeHoverText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var cleaned = TextSanitizer.StripRichTextTags(value);
        cleaned = TextSanitizer.RemoveInsensitive(cleaned, "shop item template");
        cleaned = TextSanitizer.RemoveInsensitive(cleaned, "(clone)");
        cleaned = cleaned.Trim();

        return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
    }

    private object GetComponentFromGameObject(object gameObject, Type componentType)
    {
        if (gameObject == null || componentType == null)
            return null;

        try
        {
            var method = gameObject.GetType().GetMethod("GetComponent", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(Type) }, null);
            return method?.Invoke(gameObject, new object[] { componentType });
        }
        catch
        {
            return null;
        }
    }

    private string FindNearestText(object gameObject)
    {
        try
        {
            var current = gameObject;
            for (var depth = 0; depth < 3 && current != null; depth++)
            {
                var text = FindTextOnObject(current);
                if (!string.IsNullOrWhiteSpace(text))
                    return text;

                var parent = GetParentGameObject(current);
                current = parent;
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private string FindSiblingLabelText(object gameObject)
    {
        try
        {
            var parent = GetParentGameObject(gameObject);
            if (parent == null)
                return null;

            var method = parent.GetType().GetMethod("GetComponentsInChildren", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(Type) }, null);
            if (method == null)
                return null;

            foreach (var textType in GetTextTypes())
            {
                if (textType == null)
                    continue;

                var results = method.Invoke(parent, new object[] { textType }) as Array;
                if (results == null)
                    continue;

                foreach (var item in results)
                {
                    if (item == null)
                        continue;

                    var textProp = item.GetType().GetProperty("text", BindingFlags.Instance | BindingFlags.Public);
                    var value = textProp?.GetValue(item) as string;
                    if (string.IsNullOrWhiteSpace(value))
                        continue;

                    if (LooksLikeValueText(value))
                        continue;

                    return value;
                }
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private bool LooksLikeValueText(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.EndsWith("%", StringComparison.Ordinal))
            return true;

        var digits = 0;
        foreach (var ch in trimmed)
        {
            if (char.IsDigit(ch))
                digits++;
        }

        return digits > 0 && digits >= trimmed.Length / 2;
    }

    private string FindTextOnObject(object gameObject)
    {
        var tmp = GetComponentText(gameObject, _tmpTextType);
        if (!string.IsNullOrWhiteSpace(tmp))
            return tmp;

        var tmpMesh = GetComponentText(gameObject, _tmpTextMeshType);
        if (!string.IsNullOrWhiteSpace(tmpMesh))
            return tmpMesh;

        return GetComponentText(gameObject, _textType);
    }

    private IEnumerable<Type> GetTextTypes()
    {
        if (_tmpTextType != null)
            yield return _tmpTextType;
        if (_tmpTextMeshType != null)
            yield return _tmpTextMeshType;
        if (_textType != null)
            yield return _textType;
    }

    private string GetComponentText(object gameObject, Type textType)
    {
        if (gameObject == null || textType == null)
            return null;

        try
        {
            var method = gameObject.GetType().GetMethod("GetComponentInChildren", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(Type) }, null);
            if (method == null)
                return null;

            var component = method.Invoke(gameObject, new object[] { textType });
            if (component == null)
                return null;

            var textProp = component.GetType().GetProperty("text", BindingFlags.Instance | BindingFlags.Public);
            return textProp?.GetValue(component) as string;
        }
        catch
        {
            return null;
        }
    }

    private object GetParentGameObject(object gameObject)
    {
        try
        {
            var transformProp = gameObject.GetType().GetProperty("transform", BindingFlags.Instance | BindingFlags.Public);
            var transform = transformProp?.GetValue(gameObject);
            if (transform == null)
                return null;

            var parentProp = transform.GetType().GetProperty("parent", BindingFlags.Instance | BindingFlags.Public);
            var parent = parentProp?.GetValue(transform);
            if (parent == null)
                return null;

            var gameObjectProp = parent.GetType().GetProperty("gameObject", BindingFlags.Instance | BindingFlags.Public);
            return gameObjectProp?.GetValue(parent);
        }
        catch
        {
            return null;
        }
    }

    private string GetHoverText(object interactable)
    {
        try
        {
            var method = interactable.GetType().GetMethod("GetHoverText", BindingFlags.Instance | BindingFlags.Public);
            return method?.Invoke(interactable, null) as string;
        }
        catch
        {
            return null;
        }
    }


    private (float x, float y)? GetUiPosition(object gameObject)
    {
        var rectTransform = GetRectTransform(gameObject);
        if (rectTransform != null)
        {
            var rectPosition = GetRectTransformPosition(rectTransform);
            if (rectPosition != null)
                return rectPosition;
        }

        return GetTransformPosition(gameObject);
    }

    private object GetRectTransform(object gameObject)
    {
        try
        {
            var method = gameObject.GetType().GetMethod("GetComponent", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(Type) }, null);
            if (method == null)
                return null;

            if (_rectTransformType == null)
                return null;

            return method.Invoke(gameObject, new object[] { _rectTransformType });
        }
        catch
        {
            return null;
        }
    }

    private (float x, float y)? GetRectTransformPosition(object rectTransform)
    {
        try
        {
            var worldPosition = GetTransformPosition(rectTransform);
            if (worldPosition != null)
                return worldPosition;

            var anchored = GetVector2FromPropertyOrMethod(rectTransform, "anchoredPosition", "get_anchoredPosition");
            if (anchored != null)
                return anchored;
        }
        catch
        {
            return null;
        }

        return null;
    }

    private string GetHierarchyOrderKey(object gameObject)
    {
        try
        {
            var transformProp = gameObject.GetType().GetProperty("transform", BindingFlags.Instance | BindingFlags.Public);
            var transform = transformProp?.GetValue(gameObject);
            if (transform == null)
                return null;

            var indices = new List<int>();
            var current = transform;
            while (current != null)
            {
                var siblingIndexMethod = current.GetType().GetMethod("GetSiblingIndex", BindingFlags.Instance | BindingFlags.Public);
                if (siblingIndexMethod == null)
                    break;

                var indexValue = siblingIndexMethod.Invoke(current, null);
                if (indexValue is int index)
                    indices.Add(index);

                var parentProp = current.GetType().GetProperty("parent", BindingFlags.Instance | BindingFlags.Public);
                current = parentProp?.GetValue(current);
            }

            indices.Reverse();
            return string.Join("/", indices.ConvertAll(i => i.ToString("D4")));
        }
        catch
        {
            return null;
        }
    }

    private (float x, float y)? GetTransformPosition(object gameObject)
    {
        try
        {
            object transform = null;
            var transformProp = gameObject.GetType().GetProperty("transform", BindingFlags.Instance | BindingFlags.Public);
            if (transformProp != null)
                transform = transformProp.GetValue(gameObject);
            else
            {
                var transformGetter = gameObject.GetType().GetMethod("get_transform", BindingFlags.Instance | BindingFlags.Public);
                transform = transformGetter?.Invoke(gameObject, null);
            }

            if (transform == null)
                transform = gameObject;

            object position = null;
            var positionProp = transform.GetType().GetProperty("position", BindingFlags.Instance | BindingFlags.Public);
            if (positionProp != null)
                position = positionProp.GetValue(transform);
            else
            {
                var positionGetter = transform.GetType().GetMethod("get_position", BindingFlags.Instance | BindingFlags.Public);
                position = positionGetter?.Invoke(transform, null);
            }

            if (position == null)
            {
                var localProp = transform.GetType().GetProperty("localPosition", BindingFlags.Instance | BindingFlags.Public);
                if (localProp != null)
                    position = localProp.GetValue(transform);
                else
                {
                    var localGetter = transform.GetType().GetMethod("get_localPosition", BindingFlags.Instance | BindingFlags.Public);
                    position = localGetter?.Invoke(transform, null);
                }
            }

            if (position == null)
                return null;

            var pos = GetVector2FromValue(position);
            if (pos != null)
                return pos;
        }
        catch
        {
            return null;
        }

        return null;
    }

    private (float x, float y, float z)? GetTransformPosition3(object gameObject)
    {
        try
        {
            object transform = null;
            var transformProp = gameObject.GetType().GetProperty("transform", BindingFlags.Instance | BindingFlags.Public);
            if (transformProp != null)
                transform = transformProp.GetValue(gameObject);
            else
            {
                var transformGetter = gameObject.GetType().GetMethod("get_transform", BindingFlags.Instance | BindingFlags.Public);
                transform = transformGetter?.Invoke(gameObject, null);
            }

            if (transform == null)
                transform = gameObject;

            object position = null;
            var positionProp = transform.GetType().GetProperty("position", BindingFlags.Instance | BindingFlags.Public);
            if (positionProp != null)
                position = positionProp.GetValue(transform);
            else
            {
                var positionGetter = transform.GetType().GetMethod("get_position", BindingFlags.Instance | BindingFlags.Public);
                position = positionGetter?.Invoke(transform, null);
            }

            if (position == null)
            {
                var localProp = transform.GetType().GetProperty("localPosition", BindingFlags.Instance | BindingFlags.Public);
                if (localProp != null)
                    position = localProp.GetValue(transform);
                else
                {
                    var localGetter = transform.GetType().GetMethod("get_localPosition", BindingFlags.Instance | BindingFlags.Public);
                    position = localGetter?.Invoke(transform, null);
                }
            }

            if (position == null)
                return null;

            return GetVector3FromValue(position);
        }
        catch
        {
            return null;
        }
    }

    private static bool SetFloatMember(object instance, string name, float value)
    {
        if (instance == null || string.IsNullOrWhiteSpace(name))
            return false;

        var type = instance.GetType();
        try
        {
            var prop = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(instance, value);
                return true;
            }
        }
        catch
        {
            // Ignore.
        }

        try
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(instance, value);
                return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static bool IsSelectableInteractable(object selectable)
    {
        if (selectable == null)
            return false;

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

    private (float x, float y)? GetVector2FromPropertyOrMethod(object instance, string propertyName, string methodName)
    {
        if (instance == null || string.IsNullOrWhiteSpace(propertyName))
            return null;

        try
        {
            object value = null;
            var prop = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null)
                value = prop.GetValue(instance);
            else if (!string.IsNullOrWhiteSpace(methodName))
            {
                var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                value = method?.Invoke(instance, null);
            }

            return GetVector2FromValue(value);
        }
        catch
        {
            return null;
        }
    }

    private (float x, float y)? GetVector2FromValue(object value)
    {
        if (value == null)
            return null;

        try
        {
            var xProp = value.GetType().GetProperty("x", BindingFlags.Instance | BindingFlags.Public);
            var yProp = value.GetType().GetProperty("y", BindingFlags.Instance | BindingFlags.Public);
            if (xProp == null || yProp == null)
            {
                var xField = value.GetType().GetField("x", BindingFlags.Instance | BindingFlags.Public);
                var yField = value.GetType().GetField("y", BindingFlags.Instance | BindingFlags.Public);
                if (xField != null && yField != null)
                {
                    var xValField = xField.GetValue(value);
                    var yValField = yField.GetValue(value);
                    if (xValField is float xf && yValField is float yf)
                        return (xf, yf);
                }

                return null;
            }

            var xVal = xProp.GetValue(value);
            var yVal = yProp.GetValue(value);
            if (xVal is float x && yVal is float y)
                return (x, y);
        }
        catch
        {
            return null;
        }

        return null;
    }

    private (float x, float y, float z)? GetVector3FromValue(object value)
    {
        if (value == null)
            return null;

        try
        {
            var type = value.GetType();
            var xProp = type.GetProperty("x", BindingFlags.Instance | BindingFlags.Public);
            var yProp = type.GetProperty("y", BindingFlags.Instance | BindingFlags.Public);
            var zProp = type.GetProperty("z", BindingFlags.Instance | BindingFlags.Public);
            if (xProp != null && yProp != null && zProp != null)
            {
                var xVal = xProp.GetValue(value);
                var yVal = yProp.GetValue(value);
                var zVal = zProp.GetValue(value);
                if (xVal is float x && yVal is float y && zVal is float z)
                    return (x, y, z);
            }

            var xField = type.GetField("x", BindingFlags.Instance | BindingFlags.Public);
            var yField = type.GetField("y", BindingFlags.Instance | BindingFlags.Public);
            var zField = type.GetField("z", BindingFlags.Instance | BindingFlags.Public);
            if (xField != null && yField != null && zField != null)
            {
                var xVal = xField.GetValue(value);
                var yVal = yField.GetValue(value);
                var zVal = zField.GetValue(value);
                if (xVal is float x && yVal is float y && zVal is float z)
                    return (x, y, z);
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private bool IsGameObjectActive(object gameObject)
    {
        try
        {
            var prop = gameObject.GetType().GetProperty("activeInHierarchy", BindingFlags.Instance | BindingFlags.Public);
            var value = prop?.GetValue(gameObject);
            return value is bool active && active;
        }
        catch
        {
            return false;
        }
    }

private enum NavigationDirection
{
    None,
    Up,
    Down,
    Left,
    Right
}
}

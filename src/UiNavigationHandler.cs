namespace Death_and_Access;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

public sealed class UiNavigationHandler
{
    private const int AxisRepeatMs = 150;
    private const float AxisDeadzone = 0.5f;
    
    private Type _inputType;
    private Type _keyCodeType;
    private Type _textType;
    private Type _tmpTextType;
    private Type _interactableType;
    private Type _inputManagerType;
    private Type _hudManagerType;
    private Type _cameraType;
    private Type _screenType;
    private Type _vector3Type;
    private Type _vector2Type;
    private Type _physics2DType;
    private Type _paperworkType;
    private Type _paperworkManagerType;
    private Type _grimDeskType;
    private Type _grimDeskDrawerType;
    private Type _deskItemType;
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
    private MethodInfo _getKeyDownMethod;
    private MethodInfo _getKeyMethod;
    private MethodInfo _getAxisRawMethod;
    private MethodInfo _findObjectsOfTypeMethod;
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
    private bool _virtualCursorInitialized;
    private float _virtualCursorX;
    private float _virtualCursorY;
    private float _lastMouseX;
    private float _lastMouseY;
    private int _lastSelectableCacheTick;
    private List<object> _cachedSelectables;
    private object _pendingNavigationFocus;
    private int _keyboardNavUntilTick;
    private int _keyboardFocusUntilTick;
    private bool _keyboardFocusActive;
    private bool _ignoreMouseUntilMove;
    private int _raycastCacheTick;
    private Dictionary<object, (float x, float y)> _raycastScreenCache;
    private int _candidateCacheTick;
    private int _candidateCacheWidth;
    private int _candidateCacheHeight;
    private bool _candidateCacheHitAnyCollider;
    private List<(object target, (float x, float y) screen)> _candidateCache;

    internal static UiNavigationHandler Instance { get; private set; }

    internal static void ClearInstance()
    {
        Instance = null;
    }

    internal bool IsVirtualCursorActive => _virtualCursorInitialized && _keyboardFocusActive;

    internal bool TryGetVirtualCursorPosition(out float x, out float y)
    {
        if (!IsVirtualCursorActive)
        {
            x = 0f;
            y = 0f;
            return false;
        }

        x = _virtualCursorX;
        y = _virtualCursorY;
        return true;
    }

    public void Initialize(ScreenreaderProvider screenreader)
    {
        Instance = this;
        _screenreader = screenreader;
    }

    public void Update()
    {
        EnsureTypes();

        if (IsSceneChanging())
            return;

        SyncEventSystemSelectionIfNeeded();
        EnsureDialogSelection();
        EnsureMenuSelection();
        if (HandleOfficeShortcuts())
            return;

        SampleAxes(out var axisX, out var axisY);
        var direction = GetNavigationDirection(axisX, axisY);
        var submitInteractPressed = IsSubmitPressedForInteractables();
        if (submitInteractPressed && TryHandleIntroSkip())
            return;
        if (direction != NavigationDirection.None)
        {
            _keyboardNavUntilTick = Environment.TickCount + 500;
            _keyboardFocusUntilTick = Environment.TickCount + 1500;
            _keyboardFocusActive = true;
            _ignoreMouseUntilMove = true;
        }

        if (TryAdjustFocusedSlider(direction))
            return;

        if (IsMenuActive() && direction != NavigationDirection.None)
        {
            _pendingEventSystemSyncUntil = Environment.TickCount + 250;
            TryMoveEventSystemSelection(direction);
        }

        if (IsMenuActive())
        {
            if (submitInteractPressed)
                SubmitInteractable();
            return;
        }

        if (direction != NavigationDirection.None && ShouldDeferToEventSystem() && IsPointerOverUi())
        {
            _pendingEventSystemSyncUntil = Environment.TickCount + 250;
            if (TryMoveEventSystemSelection(direction))
                return;
            ClearUiSelection();
        }

        UpdateVirtualCursorFocus(direction, submitInteractPressed);
    }

    private bool TryHandleIntroSkip()
    {
        if (!IsIntroOrComicScene())
            return false;

        if (!IsSkipIntroAvailable())
            return false;

        return TriggerSkipIntro();
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
        var current = GetCurrentSelectedGameObject();
        if (current != null && IsUnderDialogRoot(current, roots))
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

        var position = GetInteractableScreenPosition(preferred);
        if (position != null)
        {
            _virtualCursorX = position.Value.x;
            _virtualCursorY = position.Value.y;
            _virtualCursorInitialized = true;
        }

        var now = Environment.TickCount;
        _keyboardNavUntilTick = now + 500;
        _keyboardFocusUntilTick = now + 1500;
        _keyboardFocusActive = true;
        _ignoreMouseUntilMove = true;
    }

    private void EnsureMenuSelection()
    {
        if (!IsMenuActive())
            return;

        var roots = GetActiveMenuRoots();
        var current = GetCurrentSelectedGameObject();
        if (current != null && IsUnderDialogRoot(current, roots))
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

        var position = GetInteractableScreenPosition(preferred);
        if (position != null)
        {
            _virtualCursorX = position.Value.x;
            _virtualCursorY = position.Value.y;
            _virtualCursorInitialized = true;
        }

        var now = Environment.TickCount;
        _keyboardNavUntilTick = now + 500;
        _keyboardFocusUntilTick = now + 1500;
        _keyboardFocusActive = true;
        _ignoreMouseUntilMove = true;
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
        if (IsDialogActive())
        {
            if (TryHandleDialogNumberShortcut())
                return true;
            return false;
        }

        if (IsMenuActive())
            return false;

        if (IsDressingRoomActive())
        {
            if (TryHandleDressingRoomNumberShortcut())
                return true;
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
        if (GetKeyDown("A"))
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

        if (GetKeyDown("L"))
            return TryMarkFocusedPaperwork(live: true);
        if (GetKeyDown("D"))
            return TryMarkFocusedPaperwork(live: false);

        return false;
    }

    private bool HandleNonOfficeShortcuts()
    {
        if (IsElevatorActive())
        {
            if (TryHandleElevatorShortcuts())
                return true;
        }

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

        SetUiSelected(selectable);
        _lastEventSelected = GetInteractableGameObject(selectable) ?? selectable;
        SetInteractableFocus(selectable);
        SubmitInteractable();
        return true;
    }

    private bool TryHandleDressingRoomNumberShortcut()
    {
        var index = GetDialogNumberIndex();
        if (index < 0)
            return false;

        var selectable = GetSceneSelectableByIndex(index);
        if (selectable == null)
            return false;

        SetUiSelected(selectable);
        _lastEventSelected = GetInteractableGameObject(selectable) ?? selectable;
        SetInteractableFocus(selectable);
        SubmitInteractable();
        return true;
    }

    private object GetSceneSelectableByIndex(int index)
    {
        if (index < 0)
            return null;

        var list = GetEligibleSelectables(requireScreen: true);
        if (list.Count == 0)
            list = GetEligibleSelectables(requireScreen: false);

        if (list.Count == 0 || index >= list.Count)
            return null;

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

        if (index >= ordered.Count)
            return null;

        return ordered[index];
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

    private object GetDialogSelectableByIndex(int index)
    {
        if (index < 0)
            return null;

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

    private bool TryFindSelectableNavigationTarget(NavigationDirection direction, out object selectable)
    {
        selectable = null;
        if (_selectableType == null)
            return false;

        object currentSelectable = null;
        var current = GetCurrentSelectedGameObject();
        if (current != null)
            currentSelectable = GetComponentByType(current, _selectableType);

        var strictCandidates = GetEligibleSelectables(requireScreen: true);
        var looseCandidates = strictCandidates.Count > 0 ? strictCandidates : GetEligibleSelectables(requireScreen: false);
        if (looseCandidates.Count == 0)
            return false;

        if (currentSelectable == null || !looseCandidates.Contains(currentSelectable))
            currentSelectable = looseCandidates[0];

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
            if (next == null)
                return false;

            if (ReferenceEquals(next, currentSelectable))
                return false;

            var strict = strictCandidates.Count > 0;
            if (!IsSelectableEligible(next, GetScreenDimension("width"), GetScreenDimension("height"), requireScreen: strict))
                return false;

            selectable = next;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool TryFindSelectableLinear(NavigationDirection direction, out object selectable)
    {
        selectable = null;
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

    private bool AnnounceMoney()
    {
        if (_hudManagerType == null)
            return false;

        var instance = GetStaticInstance(_hudManagerType);
        if (instance == null)
            return false;

        var textMoney = GetMemberValue(instance, "TextMoney");
        if (textMoney == null)
            return false;

        var text = GetComponentText(textMoney, _tmpTextType) ?? GetComponentText(textMoney, _textType);
        if (string.IsNullOrWhiteSpace(text))
            return false;

        _screenreader?.Announce(text.Trim());
        return true;
    }


    private bool TryHandlePaperworkShortcut()
    {
        for (var i = 1; i <= 9; i++)
        {
            if (GetKeyDown("Alpha" + i))
            {
                var paperwork = GetPaperworkByIndex(i - 1);
                return ActivateInteractableWithFocus(paperwork);
            }
        }

        if (GetKeyDown("Alpha0"))
        {
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
            var drawer = GetDrawerByType(value);
            return ActivateInteractableWithFocus(drawer);
        }
        catch
        {
            return false;
        }
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

    private bool TryMarkFocusedPaperwork(bool live)
    {
        if (!IsMarkerHeld())
            return false;

        var paperwork = GetFocusedPaperwork();
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

    private object GetFocusedPaperwork()
    {
        if (_paperworkType == null)
            return null;

        foreach (var paperwork in FindSceneObjectsOfType(_paperworkType))
        {
            if (IsPaperworkFocused(paperwork))
                return paperwork;
        }

        return null;
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

        var now = Environment.TickCount;
        _keyboardNavUntilTick = now + 500;
        _keyboardFocusUntilTick = now + 1500;
        _keyboardFocusActive = true;
        _ignoreMouseUntilMove = true;

        SetInteractableFocus(interactable);
        if (TryErasePaperworkMark(interactable))
            return true;
        if (!TryInvokeInteract(interactable))
            TryInvokeUiClick(interactable);
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

    private bool IsDialogActive()
    {
        return GetActiveDialogRoots().Count > 0;
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
        _interactableType ??= TypeResolver.Get("Interactable");
        _inputManagerType ??= TypeResolver.Get("InputManager");
        _hudManagerType ??= TypeResolver.Get("HUDManager");
        _cameraType ??= TypeResolver.Get("UnityEngine.Camera");
        _screenType ??= TypeResolver.Get("UnityEngine.Screen");
        _vector3Type ??= TypeResolver.Get("UnityEngine.Vector3");
        _vector2Type ??= TypeResolver.Get("UnityEngine.Vector2");
        _physics2DType ??= TypeResolver.Get("UnityEngine.Physics2D");
        _paperworkType ??= TypeResolver.Get("Paperwork");
        _paperworkManagerType ??= TypeResolver.Get("PaperworkManager");
        _grimDeskType ??= TypeResolver.Get("GrimDesk");
        _grimDeskDrawerType ??= TypeResolver.Get("GrimDeskDrawer");
        _deskItemType ??= TypeResolver.Get("DeskItem");
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
        _comicManagerType ??= TypeResolver.Get("ComicManager");
        _unityObjectType ??= TypeResolver.Get("UnityEngine.Object");
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

            var screen = GetMousePosition() ?? (_virtualCursorInitialized ? (_virtualCursorX, _virtualCursorY) : ((float x, float y)?)null);
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
                if (goProp?.GetValue(item) != null)
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

    private void UpdateVirtualCursorFocus(NavigationDirection direction, bool submitPressed)
    {
        UpdateVirtualCursorPosition(direction);

        if (_lastFocusedInteractable != null && !IsInteractableActive(_lastFocusedInteractable))
        {
            ClearInteractableFocus();
        }

        if (_pendingNavigationFocus != null)
        {
            var focus = _pendingNavigationFocus;
            _pendingNavigationFocus = null;
            SetInteractableFocus(focus);
            if (submitPressed)
                SubmitInteractable();
            return;
        }

        var hit = GetInteractableAtScreenPosition(_virtualCursorX, _virtualCursorY);
        if (hit == null)
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

        if (!ReferenceEquals(hit, _lastFocusedInteractable))
            SetInteractableFocus(hit);

        if (submitPressed)
            SubmitInteractable();
    }

    private void UpdateVirtualCursorPosition(NavigationDirection direction)
    {
        var allowMouse = Environment.TickCount > _keyboardNavUntilTick;
        var mouse = allowMouse ? GetMousePosition() : null;
        if (mouse.HasValue)
        {
            if (!_virtualCursorInitialized)
            {
                _virtualCursorX = mouse.Value.x;
                _virtualCursorY = mouse.Value.y;
                _virtualCursorInitialized = true;
                _keyboardFocusActive = false;
                _ignoreMouseUntilMove = false;
            }
            else if (_ignoreMouseUntilMove)
            {
                if (Math.Abs(mouse.Value.x - _lastMouseX) > 1f || Math.Abs(mouse.Value.y - _lastMouseY) > 1f)
                {
                    _virtualCursorX = mouse.Value.x;
                    _virtualCursorY = mouse.Value.y;
                    _keyboardFocusActive = false;
                    _ignoreMouseUntilMove = false;
                }
            }
            else if (direction == NavigationDirection.None && (Math.Abs(mouse.Value.x - _lastMouseX) > 1f || Math.Abs(mouse.Value.y - _lastMouseY) > 1f))
            {
                _virtualCursorX = mouse.Value.x;
                _virtualCursorY = mouse.Value.y;
                _keyboardFocusActive = false;
            }

            _lastMouseX = mouse.Value.x;
            _lastMouseY = mouse.Value.y;
        }

        if (!_virtualCursorInitialized)
        {
            if (direction != NavigationDirection.None)
            {
                var origin = GetNavigationOrigin();
                if (origin != null)
                {
                    _virtualCursorX = origin.Value.x;
                    _virtualCursorY = origin.Value.y;
                    _virtualCursorInitialized = true;
                }
            }
        }

        if (!_virtualCursorInitialized)
            return;

        if (direction != NavigationDirection.None)
        {
            var focus = TryMoveCursorToNearest(direction);
            if (focus != null)
                _pendingNavigationFocus = focus;
        }

        var width = GetScreenDimension("width");
        var height = GetScreenDimension("height");
        if (width > 0)
            _virtualCursorX = Clamp(_virtualCursorX, 0, width - 1);
        if (height > 0)
            _virtualCursorY = Clamp(_virtualCursorY, 0, height - 1);
    }

    private object TryMoveCursorToNearest(NavigationDirection direction)
    {
        if (direction == NavigationDirection.None)
            return null;

        var origin = GetNavigationOrigin();
        if (origin == null)
            return null;

        var currentResolved = _lastFocusedInteractable != null ? ResolveInteractableForFocus(_lastFocusedInteractable) : null;
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var width = GetScreenDimension("width");
        var height = GetScreenDimension("height");
        if (width <= 0 || height <= 0)
            return null;

        EnsureRaycastScreenCache(width, height);

        var start = GetPrimaryCursorOrigin(origin.Value, width, height, useMouse: false);
        if (!TryFindNearestInteractableInDirection(start, direction, width, height, currentResolved, seen, out var hit, out var hitScreen, out var anyCollider, out var candidateCount))
        {
            return null;
        }

        var resolvedHit = ResolveInteractableForFocus(hit) ?? hit;
        if (resolvedHit == null)
            return null;

        _virtualCursorX = hitScreen.x;
        _virtualCursorY = hitScreen.y;
        _virtualCursorInitialized = true;
        return resolvedHit;
    }

    private (float x, float y)? GetNavigationOrigin()
    {
        if (_lastFocusedInteractable != null)
        {
            var resolved = ResolveInteractableForFocus(_lastFocusedInteractable);
            var position = GetInteractableScreenPosition(resolved ?? _lastFocusedInteractable);
            if (position != null)
                return position;
        }

        var mouseHit = GetMouseHitInteractable();
        if (mouseHit != null)
        {
            var resolvedMouse = ResolveInteractableForFocus(mouseHit);
            var mousePosition = GetInteractableScreenPosition(resolvedMouse ?? mouseHit);
            if (mousePosition != null)
                return mousePosition;
        }

        if (_keyboardFocusActive)
        {
            if (_virtualCursorInitialized)
                return (_virtualCursorX, _virtualCursorY);

            var screenWidth = GetScreenDimension("width");
            var screenHeight = GetScreenDimension("height");
            if (screenWidth > 0 && screenHeight > 0)
                return (screenWidth / 2f, screenHeight / 2f);
        }

        var lastHit = GetInputManagerLastHit();
        if (lastHit != null)
        {
            var resolved = ResolveInteractableForFocus(lastHit);
            var position = GetInteractableScreenPosition(resolved ?? lastHit);
            if (position != null)
                return position;
        }

        if (_virtualCursorInitialized)
            return (_virtualCursorX, _virtualCursorY);

        var width = GetScreenDimension("width");
        var height = GetScreenDimension("height");
        if (width > 0 && height > 0)
            return (width / 2f, height / 2f);

        return null;
    }

    private static bool IsInDirection(NavigationDirection direction, float dx, float dy)
    {
        return direction switch
        {
            NavigationDirection.Up => dy > 0f,
            NavigationDirection.Down => dy < 0f,
            NavigationDirection.Left => dx < 0f,
            NavigationDirection.Right => dx > 0f,
            _ => false
        };
    }

    private static bool IsWithinScreen((float x, float y) screen, int width, int height)
    {
        if (width <= 0 || height <= 0)
            return true;

        return screen.x >= 0f && screen.y >= 0f && screen.x <= width && screen.y <= height;
    }

    private bool TryFindNearestInteractableInDirection(
        (float x, float y) origin,
        NavigationDirection direction,
        int width,
        int height,
        object currentResolved,
        HashSet<object> seen,
        out object hitInteractable,
        out (float x, float y) hitScreen,
        out bool hitAnyCollider,
        out int candidateCount)
    {
        hitInteractable = null;
        hitScreen = default;
        hitAnyCollider = false;
        candidateCount = 0;

        var candidates = GetCandidatePositions(width, height, ref hitAnyCollider);
        var sampleCandidates = GetRaycastSampleCandidates(width, height);
        if (sampleCandidates.Count > 0)
        {
            var seenTargets = new HashSet<object>(ReferenceEqualityComparer.Instance);
            foreach (var candidate in candidates)
                seenTargets.Add(candidate.target);

            foreach (var candidate in sampleCandidates)
            {
                if (candidate.target == null)
                    continue;

                if (seenTargets.Add(candidate.target))
                    candidates.Add(candidate);
            }
        }
        candidateCount = candidates.Count;
        if (candidates.Count == 0)
        {
            if (TryFindSelectableNavigationTarget(direction, out var selectableTarget))
            {
                hitInteractable = selectableTarget;
                hitScreen = origin;
                return true;
            }

            if (TryFindSelectableLinear(direction, out selectableTarget))
            {
                hitInteractable = selectableTarget;
                hitScreen = origin;
                return true;
            }

            return false;
        }

        var bestDistance = float.MaxValue;
        foreach (var candidate in candidates)
        {
            var resolved = ResolveInteractableForFocus(candidate.target) ?? candidate.target;
            if (resolved == null)
                continue;

            if (currentResolved != null && ReferenceEquals(resolved, currentResolved))
                continue;

            if (!seen.Add(resolved))
                continue;

            var dx = candidate.screen.x - origin.x;
            var dy = candidate.screen.y - origin.y;
            if (!IsInDirection(direction, dx, dy))
                continue;

            float dist;
            if (direction == NavigationDirection.Left || direction == NavigationDirection.Right)
            {
                var primary = Math.Abs(dx);
                var secondary = Math.Abs(dy);
                dist = (primary * primary) + (secondary * secondary * 4f);
            }
            else
            {
                var primary = Math.Abs(dy);
                var secondary = Math.Abs(dx);
                dist = (primary * primary) + (secondary * secondary * 4f);
            }
            if (dist < bestDistance)
            {
                bestDistance = dist;
                hitInteractable = resolved;
                hitScreen = candidate.screen;
            }
        }

        if (hitInteractable != null)
            return true;

        return TryFindDirectionalScan(origin, direction, width, height, currentResolved, seen, out hitInteractable, out hitScreen);
    }

    private bool TryFindDirectionalScan(
        (float x, float y) origin,
        NavigationDirection direction,
        int width,
        int height,
        object currentResolved,
        HashSet<object> seen,
        out object hitInteractable,
        out (float x, float y) hitScreen)
    {
        hitInteractable = null;
        hitScreen = default;

        var maxDistance = direction switch
        {
            NavigationDirection.Left => origin.x,
            NavigationDirection.Right => width - origin.x,
            NavigationDirection.Down => origin.y,
            NavigationDirection.Up => height - origin.y,
            _ => 0f
        };

        if (maxDistance <= 0f)
            return false;

        const int step = 24;
        var dir = direction switch
        {
            NavigationDirection.Left => (-1f, 0f),
            NavigationDirection.Right => (1f, 0f),
            NavigationDirection.Up => (0f, 1f),
            NavigationDirection.Down => (0f, -1f),
            _ => (0f, 0f)
        };

        for (var dist = step; dist <= maxDistance; dist += step)
        {
            var baseX = origin.x + (dir.Item1 * dist);
            var baseY = origin.y + (dir.Item2 * dist);

            if (direction == NavigationDirection.Left || direction == NavigationDirection.Right)
            {
                if (baseX < 0f || baseX > width)
                    continue;

                for (var y = 0f; y <= height; y += step)
                {
                    var hit = GetInteractableAtScreenPosition(baseX, y);
                    if (hit == null)
                        continue;

                    var resolved = ResolveInteractableForFocus(hit) ?? hit;
                    if (resolved == null)
                        continue;

                    if (currentResolved != null && ReferenceEquals(resolved, currentResolved))
                        continue;

                    if (!seen.Add(resolved))
                        continue;

                    hitInteractable = resolved;
                    hitScreen = (baseX, y);
                    return true;
                }
            }
            else
            {
                if (baseY < 0f || baseY > height)
                    continue;

                for (var x = 0f; x <= width; x += step)
                {
                    var hit = GetInteractableAtScreenPosition(x, baseY);
                    if (hit == null)
                        continue;

                    var resolved = ResolveInteractableForFocus(hit) ?? hit;
                    if (resolved == null)
                        continue;

                    if (currentResolved != null && ReferenceEquals(resolved, currentResolved))
                        continue;

                    if (!seen.Add(resolved))
                        continue;

                    hitInteractable = resolved;
                    hitScreen = (x, baseY);
                    return true;
                }
            }
        }

        return false;
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

            var screen = GetMousePosition() ?? (_virtualCursorInitialized ? (_virtualCursorX, _virtualCursorY) : ((float x, float y)?)null);
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
                if (go != null)
                    return go;
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private (float x, float y) GetPrimaryCursorOrigin((float x, float y) fallbackOrigin, int width, int height, bool useMouse)
    {
        if (useMouse && !_keyboardFocusActive)
        {
            var mouse = GetMousePosition();
            if (mouse.HasValue)
                return mouse.Value;
        }

        if (!_keyboardFocusActive)
        {
            var lastHit = GetInputManagerLastHit();
            if (lastHit != null)
            {
                var position = GetInteractableScreenPosition(lastHit);
                if (position != null)
                    return position.Value;
            }
        }

        if (_virtualCursorInitialized)
            return (_virtualCursorX, _virtualCursorY);

        if (width > 0 && height > 0)
            return (width / 2f, height / 2f);

        return fallbackOrigin;
    }

    private List<(object target, (float x, float y) screen)> GetCandidatePositions(int width, int height, ref bool hitAnyCollider)
    {
        if (IsDialogActive())
        {
            hitAnyCollider = false;
            return GetDialogSelectableCandidates(width, height);
        }

        var now = Environment.TickCount;
        if (_candidateCache != null
            && _candidateCacheWidth == width
            && _candidateCacheHeight == height
            && unchecked(now - _candidateCacheTick) < 250)
        {
            hitAnyCollider = _candidateCacheHitAnyCollider;
            return new List<(object target, (float x, float y) screen)>(_candidateCache);
        }

        var results = new List<(object target, (float x, float y) screen)>();
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);

        foreach (var collider in FindSceneObjectsOfType(_collider2DType))
            TryAddColliderCandidate(collider, width, height, ref hitAnyCollider, seen, results);

        foreach (var collider in FindSceneObjectsOfType(_collider3DType))
            TryAddColliderCandidate(collider, width, height, ref hitAnyCollider, seen, results);

        if (IsOfficeActive())
        {
            foreach (var deskItem in GetDeskItemsFromGrimDesk())
                TryAddInteractableCandidate(deskItem, width, height, seen, results);

            foreach (var drawer in GetDeskDrawersFromGrimDesk())
                TryAddInteractableCandidate(drawer, width, height, seen, results);

            foreach (var deskItem in FindSceneObjectsOfType(_deskItemType))
                TryAddInteractableCandidate(deskItem, width, height, seen, results);

            var faxInstance = _faxMachineType != null ? GetStaticInstance(_faxMachineType) : null;
            if (faxInstance != null)
                TryAddInteractableCandidate(faxInstance, width, height, seen, results);

            var spinnerInstance = _spinnerType != null ? GetStaticInstance(_spinnerType) : null;
            if (spinnerInstance != null)
                TryAddInteractableCandidate(spinnerInstance, width, height, seen, results);
        }

        foreach (var interactable in FindSceneObjectsOfType(_interactableType))
            TryAddInteractableCandidate(interactable, width, height, seen, results);

        foreach (var selectable in FindSceneObjectsOfType(_selectableType))
            TryAddSelectableCandidate(selectable, width, height, seen, results);

        _candidateCacheTick = now;
        _candidateCacheWidth = width;
        _candidateCacheHeight = height;
        _candidateCacheHitAnyCollider = hitAnyCollider;
        _candidateCache = results;

        return new List<(object target, (float x, float y) screen)>(results);
    }

    private List<(object target, (float x, float y) screen)> GetDialogSelectableCandidates(int width, int height)
    {
        var results = new List<(object target, (float x, float y) screen)>();
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach (var selectable in GetDialogSelectables(requireScreen: true))
            TryAddSelectableCandidate(selectable, width, height, seen, results);

        if (results.Count == 0)
        {
            foreach (var selectable in GetDialogSelectables(requireScreen: false))
                TryAddSelectableCandidate(selectable, width, height, seen, results);
        }

        return results;
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

    private List<(object target, (float x, float y) screen)> GetRaycastSampleCandidates(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return new List<(object target, (float x, float y) screen)>();

        const int step = 24;
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var results = new List<(object target, (float x, float y) screen)>();

        for (var y = step / 2; y < height; y += step)
        {
            for (var x = step / 2; x < width; x += step)
            {
                var hit = GetInteractableAtScreenPosition(x, y);
                if (hit == null)
                    continue;

                var resolved = ResolveInteractableForFocus(hit) ?? hit;
                if (resolved == null)
                    continue;

                if (!seen.Add(resolved))
                    continue;

                results.Add((resolved, (x, y)));
            }
        }

        return results;
    }


    private List<object> GetEligibleSelectables(bool requireScreen)
    {
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

    private void TryAddColliderCandidate(object collider, int width, int height, ref bool hitAnyCollider, HashSet<object> seen, List<(object target, (float x, float y) screen)> results)
    {
        if (collider == null || _interactableType == null)
            return;

        hitAnyCollider = true;

        if (!ReflectionUtils.TryGetProperty(collider.GetType(), collider, "gameObject", out var colliderGameObject) || colliderGameObject == null)
            return;

        var getComponentInParent = colliderGameObject.GetType().GetMethod("GetComponentInParent", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(Type) }, null);
        if (getComponentInParent == null)
            return;

        var interactable = getComponentInParent.Invoke(colliderGameObject, new object[] { _interactableType });
        if (interactable == null)
            return;

        if (!seen.Add(interactable))
            return;

        var position = GetColliderBoundsCenter(collider) ?? GetInteractableScreenPosition(interactable);
        if (position == null)
            return;

        var screen = ToScreenPosition(position.Value, width, height);
        if (screen == null)
            return;

        results.Add((interactable, screen.Value));
    }

    private void TryAddSelectableCandidate(object selectable, int width, int height, HashSet<object> seen, List<(object target, (float x, float y) screen)> results)
    {
        if (selectable == null || _selectableType == null)
            return;

        if (!seen.Add(selectable))
            return;

        if (!IsSelectableEligible(selectable, width, height, requireScreen: true))
            return;

        var gameObject = GetInteractableGameObject(selectable) ?? selectable;
        var screen = GetUiScreenPosition(gameObject, width, height);
        if (screen == null)
            return;

        results.Add((selectable, screen.Value));
    }

    private void TryAddInteractableCandidate(object interactable, int width, int height, HashSet<object> seen, List<(object target, (float x, float y) screen)> results)
    {
        if (interactable == null)
            return;

        var resolved = ResolveInteractableForFocus(interactable) ?? interactable;
        if (resolved == null)
            return;

        var position = GetInteractableScreenPosition(resolved);
        if (position == null && IsFaxMachine(resolved))
        {
            RefreshRaycastScreenCache(width, height);
            position = GetCachedRaycastScreenPosition(resolved) ?? GetInteractableScreenPosition(resolved);
        }
        if (position == null)
            return;

        if (!seen.Add(resolved))
            return;

        if (!IsWithinScreen(position.Value, width, height))
            return;

        results.Add((resolved, position.Value));
    }

    private bool IsFaxMachine(object interactable)
    {
        return _faxMachineType != null && interactable != null && _faxMachineType.IsInstanceOfType(interactable);
    }

    private void RefreshRaycastScreenCache(int width, int height)
    {
        _raycastCacheTick = 0;
        _raycastScreenCache = null;
        EnsureRaycastScreenCache(width, height);
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

    private IEnumerable<object> GetDeskItemsFromGrimDesk()
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

        object deskObjects;
        try
        {
            var field = _grimDeskType.GetField("DeskObjects", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            deskObjects = field?.GetValue(instance);
        }
        catch
        {
            yield break;
        }

        if (deskObjects is not System.Collections.IEnumerable enumerable)
            yield break;

        foreach (var item in enumerable)
        {
            if (item != null)
                yield return item;
        }
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
        var screen = WorldToScreenPoint(position);
        if (screen != null)
            return screen;

        if (position.x >= 0 && position.y >= 0 && position.x <= width && position.y <= height)
            return position;

        return null;
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
            yield return item;
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
        if (_inputType == null || _vector3Type == null)
            return null;

        try
        {
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

        var camera = GetMainCamera();
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
                return FilterDialogInteractable(resolved);
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
            return FilterDialogInteractable(resolvedOverlap);
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
        return _paperworkType != null && interactable != null && _paperworkType.IsInstanceOfType(interactable);
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


    private void SubmitInteractable()
    {
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
            return;

        if (TryErasePaperworkMark(_lastFocusedInteractable))
            return;

        if (!TryInvokeInteract(_lastFocusedInteractable))
            TryInvokeUiClick(_lastFocusedInteractable);

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

        var min = GetFloatMember(slider, "minValue", 0f);
        var max = GetFloatMember(slider, "maxValue", 1f);
        var value = GetFloatMember(slider, "value", min);
        var step = (max - min) * 0.05f;
        if (step <= 0f)
            step = 0.05f;

        value += direction == NavigationDirection.Right ? step : -step;
        value = Clamp(value, Math.Min(min, max), Math.Max(min, max));

        if (GetBoolMember(slider, "wholeNumbers", false))
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

        var min = GetFloatMember(slider, "minValue", 0f);
        var max = GetFloatMember(slider, "maxValue", 1f);
        var value = GetFloatMember(slider, "value", min);
        var step = (max - min) * 0.05f;
        if (step <= 0f)
            step = 0.05f;

        value += direction == NavigationDirection.Right ? step : -step;
        value = Clamp(value, Math.Min(min, max), Math.Max(min, max));

        if (GetBoolMember(slider, "wholeNumbers", false))
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

    private object GetMouseHitInteractable()
    {
        if (_cameraType == null || _physics2DType == null || _vector3Type == null || _interactableType == null)
            return null;

        var camera = GetMainCamera();
        if (camera == null)
            return null;

        if (_getMousePositionMethod == null || _getRayIntersectionMethod == null)
            return null;

        try
        {
            var mouse = _getMousePositionMethod.Invoke(null, null);
            if (mouse == null)
                return null;

            var xProp = mouse.GetType().GetProperty("x", BindingFlags.Instance | BindingFlags.Public);
            var yProp = mouse.GetType().GetProperty("y", BindingFlags.Instance | BindingFlags.Public);
            if (xProp == null || yProp == null)
                return null;

            var xVal = xProp.GetValue(mouse);
            var yVal = yProp.GetValue(mouse);
            if (xVal is not float x || yVal is not float y)
                return null;

            var vector = Activator.CreateInstance(_vector3Type, x, y, 0f);
            var rayMethod = _cameraType.GetMethod("ScreenPointToRay", BindingFlags.Instance | BindingFlags.Public, null, new[] { _vector3Type }, null);
            if (rayMethod == null)
                return null;

            var ray = rayMethod.Invoke(camera, new[] { vector });
            if (ray == null)
                return null;

            object hit;
            if (_getRayIntersectionMethod.GetParameters().Length >= 2)
            {
                hit = _getRayIntersectionMethod.Invoke(null, new[] { ray, float.PositiveInfinity });
            }
            else
            {
                hit = _getRayIntersectionMethod.Invoke(null, new[] { ray });
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
            return ResolveInteractableForFocus(interactable);
        }
        catch
        {
            return null;
        }
    }

    private object GetInteractableGameObject(object interactable)
    {
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
            var screen3 = WorldToScreenPoint(position3.Value);
            if (screen3 != null)
                return screen3;
        }

        var position = gameObject != null ? GetTransformPosition(gameObject) : null;

        if (position == null)
            position = GetTransformPosition(interactable);

        if (position == null && IsDeskItem(interactable))
            position = GetDeskItemStatusPosition(interactable, gameObject);

        if (position == null)
            position = GetDeskItemOriginPosition(interactable);

        if (position == null && IsDrawer(interactable))
            position = GetDrawerPosition(interactable);

        if (position == null && gameObject != null)
            position = GetColliderBoundsCenterFromGameObject(gameObject);

        if (position == null && gameObject != null)
            position = GetRendererBoundsCenterFromGameObject(gameObject);

        if (position == null)
            return null;

        var screen = WorldToScreenPoint(position.Value);
        if (screen != null)
            return screen;

        if (gameObject != null)
        {
            var uiFallback = GetUiScreenPosition(gameObject, width, height);
            if (uiFallback != null)
                return uiFallback;
        }

        var cached = GetCachedRaycastScreenPosition(interactable);
        if (cached != null)
            return cached;

        if (width > 0 && height > 0)
            return ToScreenPosition(position.Value, width, height);

        return null;
    }

    private void EnsureRaycastScreenCache(int width, int height)
    {
        var now = Environment.TickCount;
        if (_raycastScreenCache != null && unchecked(now - _raycastCacheTick) < 60000)
            return;

        _raycastCacheTick = now;
        _raycastScreenCache = new Dictionary<object, (float x, float y)>(ReferenceEqualityComparer.Instance);

        const int step = 24;
        for (var y = step / 2f; y < height; y += step)
        {
            for (var x = step / 2f; x < width; x += step)
            {
                var hit = GetInteractableAtScreenPosition(x, y);
                if (hit == null)
                    continue;

                var resolved = ResolveInteractableForFocus(hit) ?? hit;
                if (resolved == null)
                    continue;

                if (!_raycastScreenCache.ContainsKey(resolved))
                    _raycastScreenCache[resolved] = (x, y);
            }
        }
    }

    private (float x, float y)? GetCachedRaycastScreenPosition(object interactable)
    {
        if (_raycastScreenCache == null || interactable == null)
            return null;

        var resolved = ResolveInteractableForFocus(interactable) ?? interactable;
        if (resolved == null)
            return null;

        if (_raycastScreenCache.TryGetValue(resolved, out var pos))
            return pos;

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

    private (float x, float y)? GetDeskItemStatusPosition(object interactable, object gameObject)
    {
        if (_deskItemType == null || interactable == null || !_deskItemType.IsInstanceOfType(interactable))
            return null;

        try
        {
            var statusField = _deskItemType.GetField("ItemStatus", BindingFlags.Instance | BindingFlags.Public);
            var status = statusField?.GetValue(interactable);
            if (status == null)
                return null;

            var posField = status.GetType().GetField("Position", BindingFlags.Instance | BindingFlags.Public);
            var posValue = posField?.GetValue(status);
            var local = GetVector2FromValue(posValue);
            if (local == null)
                return null;

            var transform = gameObject != null ? GetTransform(gameObject) : null;
            if (transform != null && _vector3Type != null)
            {
                var method = transform.GetType().GetMethod("TransformPoint", BindingFlags.Instance | BindingFlags.Public, null, new[] { _vector3Type }, null);
                if (method != null)
                {
                    var ctor = _vector3Type.GetConstructor(new[] { typeof(float), typeof(float), typeof(float) });
                    var vector = ctor?.Invoke(new object[] { local.Value.x, local.Value.y, 0f });
                    var world = method.Invoke(transform, new[] { vector });
                    var worldPos = GetVector2FromValue(world);
                    if (worldPos != null)
                        return worldPos;
                }
            }

            return local;
        }
        catch
        {
            return null;
        }
    }

    private (float x, float y)? GetDeskItemOriginPosition(object interactable)
    {
        if (_deskItemType == null || interactable == null || !_deskItemType.IsInstanceOfType(interactable))
            return null;

        try
        {
            var field = _deskItemType.GetField("OriginPosition", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var value = field?.GetValue(interactable);
            return GetVector2FromValue(value);
        }
        catch
        {
            return null;
        }
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

    private void SetInteractableFocus(object interactable)
    {
        if (interactable == null)
            return;

        if (IsInteractableInstance(interactable) && !IsInteractableActive(interactable))
            return;

        if (_lastFocusedInteractable != null && !ReferenceEquals(_lastFocusedInteractable, interactable))
        {
            if (IsInteractableInstance(_lastFocusedInteractable))
                CallInteractableMethod(_lastFocusedInteractable, "Unhover");
        }

        _lastFocusedInteractable = interactable;
        _keyboardFocusActive = true;
        _keyboardFocusUntilTick = Environment.TickCount + 1500;
        var focusPosition = GetInteractableScreenPosition(interactable);
        if (focusPosition != null)
        {
            _virtualCursorX = focusPosition.Value.x;
            _virtualCursorY = focusPosition.Value.y;
            _virtualCursorInitialized = true;
        }
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
        UpdateHudHoverText(hoverText);
        AnnounceHoverText(hoverText, interactable);
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

        var shopName = GetShopItemName(interactable);
        var name = SanitizeHoverText(GetGameObjectName(interactable));
        hoverText = SanitizeHoverText(hoverText);

        if (!string.IsNullOrWhiteSpace(shopName))
        {
            if (string.IsNullOrWhiteSpace(hoverText))
                hoverText = shopName;
            else
                hoverText = $"{shopName}. {hoverText}";
            name = null;
        }

        var shopPrice = GetShopItemPriceText(interactable);
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

        if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(hoverText)
            && hoverText.IndexOf(name, StringComparison.OrdinalIgnoreCase) < 0)
        {
            hoverText = $"{name}. {hoverText}";
        }

        if (string.IsNullOrWhiteSpace(hoverText))
            return;

        if (IsDialogActive())
            return;

        if (_screenreader.ShouldSuppressHover() || _screenreader.IsBusy)
            return;

        _screenreader.Announce(hoverText);
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

        return "Day " + dayNumber;
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

        var type = interactable.GetType();
        if (!string.Equals(type.Name, "ShopItem", StringComparison.Ordinal))
            return null;

        var itemData = GetMemberValue(interactable, "ItemData");
        if (itemData == null)
            return null;

        var template = GetMemberValue(itemData, "Template");
        if (template == null)
            return null;

        var itemDataObj = GetMemberValue(template, "item_data");
        if (itemDataObj == null)
            return null;

        var itemName = GetMemberValue(itemDataObj, "item_name") as string;
        return SanitizeHoverText(itemName);
    }

    private string GetShopItemPriceText(object interactable)
    {
        if (interactable == null)
            return null;

        var type = interactable.GetType();
        if (!string.Equals(type.Name, "ShopItem", StringComparison.Ordinal))
            return null;

        try
        {
            var method = type.GetMethod("GetPrice", BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
                return null;

            var result = method.Invoke(interactable, null);
            if (result is int price)
                return "Price " + price;
            if (result is float f)
                return "Price " + Math.Round(f);
            if (result is double d)
                return "Price " + Math.Round(d);
        }
        catch
        {
            return null;
        }

        return null;
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

            foreach (var textType in new[] { _tmpTextType, _textType })
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

            foreach (var textType in new[] { _tmpTextType, _textType })
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

        return GetComponentText(gameObject, _textType);
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

    private static float GetFloatMember(object instance, string name, float fallback)
    {
        var value = GetMemberValue(instance, name);
        return value switch
        {
            float f => f,
            double d => (float)d,
            int i => i,
            long l => l,
            decimal m => (float)m,
            _ => fallback
        };
    }

    private static bool GetBoolMember(object instance, string name, bool fallback)
    {
        var value = GetMemberValue(instance, name);
        return value is bool flag ? flag : fallback;
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

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new();

        bool IEqualityComparer<object>.Equals(object x, object y) => ReferenceEquals(x, y);

        public int GetHashCode(object obj) => obj == null ? 0 : RuntimeHelpers.GetHashCode(obj);
    }

}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Characters.FirstPerson;

// Touch controls for phones and tablets, including the WebGL build opened in a mobile
// browser. Dragging on the left half of the screen walks, dragging on the right half looks
// around, and while a poem text box holds the player, buttons offer the same ways out as
// W, A, S and D. A Fly button switches to flying, with Up and Down buttons to rise and
// sink. It feeds the same input axes as the keyboard and mouse, so the player
// controller needs no changes. Desktop play is unaffected; to try it in the editor, use
// Window > General > Device Simulator.
[DefaultExecutionOrder(-100)] // set the axes before the player controller reads them
public class MobileControls : MonoBehaviour {
    const float StickRadius = 0.08f;           // drag distance for full speed, as a fraction of screen height
    const float DeadZone = 0.15f;
    const float LookDegreesPerScreen = 180f;    // turn from dragging across the full screen width
    const float FarClipPlane = 300f;            // the fog hides everything well before this
    const float HintSeconds = 8f;

    CrossPlatformInputManager.VirtualAxis moveX, moveY, lookX, lookY;
    int moveFinger = -1, lookFinger = -1;
    Vector2 moveStart, lookLast;
    RigidbodyFirstPersonController player;
    Canvas canvas;
    RectTransform stickBase, stickKnob;
    GameObject holdButtons, hint, flyToggle, climbButtons;
    Text flyLabel;
    bool rising, sinking;
    float hintUntil;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void StartOnMobile () {
        if (!Application.isMobilePlatform) return;
        // Must happen before any axes are registered, as they register with the active input.
        CrossPlatformInputManager.SwitchActiveInputMethod(CrossPlatformInputManager.ActiveInputMethod.Touch);
        // Phones render at high resolutions on modest GPUs, where multisampling costs the most.
        QualitySettings.antiAliasing = 0;
        var controls = new GameObject("Mobile Controls");
        DontDestroyOnLoad(controls);
        controls.AddComponent<MobileControls>();
    }

    void Awake () {
        moveX = Register("Horizontal");
        moveY = Register("Vertical");
        lookX = Register("Mouse X");
        lookY = Register("Mouse Y");
        BuildUI();
    }

    static CrossPlatformInputManager.VirtualAxis Register (string name) {
        if (CrossPlatformInputManager.AxisExists(name))
            CrossPlatformInputManager.UnRegisterVirtualAxis(name);
        var axis = new CrossPlatformInputManager.VirtualAxis(name);
        CrossPlatformInputManager.RegisterVirtualAxis(axis);
        return axis;
    }

    void Update () {
        if (player == null) {
            player = FindFirstObjectByType<RigidbodyFirstPersonController>();
            if (player != null) {
                Lighten(player.cam);
                // Time the hint from when the scene is playable, not from when it started loading.
                hintUntil = Time.time + HintSeconds;
            }
        }

        Vector2 move = Vector2.zero, look = Vector2.zero;
        foreach (var touch in Input.touches) {
            if (touch.phase == TouchPhase.Began) {
                // Touches that land on a button belong to the button.
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    continue;
                if (touch.position.x < Screen.width * 0.5f) {
                    if (moveFinger < 0) { moveFinger = touch.fingerId; moveStart = touch.position; }
                } else if (lookFinger < 0) {
                    lookFinger = touch.fingerId;
                    lookLast = touch.position;
                }
            }
            var ended = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
            if (touch.fingerId == moveFinger) {
                if (ended) { moveFinger = -1; continue; }
                move = Vector2.ClampMagnitude((touch.position - moveStart) / (Screen.height * StickRadius), 1f);
                if (move.magnitude < DeadZone) move = Vector2.zero;
            } else if (touch.fingerId == lookFinger) {
                if (ended) { lookFinger = -1; continue; }
                look = touch.position - lookLast;
                lookLast = touch.position;
            }
        }

        moveX.Update(move.x);
        moveY.Update(move.y);
        // Mouse look multiplies these by its sensitivity, so divide that back out.
        var sensitivityX = player != null ? player.mouseLook.XSensitivity : 1f;
        var sensitivityY = player != null ? player.mouseLook.YSensitivity : 1f;
        lookX.Update(look.x / Screen.width * LookDegreesPerScreen / sensitivityX);
        lookY.Update(look.y / Screen.width * LookDegreesPerScreen / sensitivityY);

        UpdateStick(move);
        holdButtons.SetActive(player != null && player.Held);
        var canFly = player != null && player.flySettings.Allowed;
        flyToggle.SetActive(canFly);
        if (canFly) flyLabel.text = player.Flying ? "Walk" : "Fly";
        var climbing = canFly && player.Flying && !player.Held;
        climbButtons.SetActive(climbing);
        // A button hidden mid-press never hears the finger lift.
        if (!climbing) rising = sinking = false;
        if (player != null) player.TouchClimb = (rising ? 1f : 0f) - (sinking ? 1f : 0f);
        if (hint.activeSelf && player != null && (Time.time > hintUntil || move != Vector2.zero))
            hint.SetActive(false);
        // Keep the UI sized by the screen's shorter side in either orientation.
        canvas.GetComponent<CanvasScaler>().matchWidthOrHeight = Screen.width > Screen.height ? 1f : 0f;
    }

    // The full-screen fog effect is the costliest thing on the camera; the scene's own fog remains.
    static void Lighten (Camera cam) {
        if (cam == null) return;
        var fog = cam.GetComponent("GlobalFog") as Behaviour;
        if (fog != null) fog.enabled = false;
        cam.farClipPlane = Mathf.Min(cam.farClipPlane, FarClipPlane);
    }

    void UpdateStick (Vector2 move) {
        var active = moveFinger >= 0;
        stickBase.gameObject.SetActive(active);
        if (!active) return;
        var radius = Screen.height * StickRadius;
        stickBase.position = moveStart;
        stickBase.sizeDelta = Vector2.one * (radius * 2f / canvas.scaleFactor);
        stickKnob.position = moveStart + move * radius;
        stickKnob.sizeDelta = Vector2.one * (radius * 0.8f / canvas.scaleFactor);
    }

    void BuildUI () {
        if (FindFirstObjectByType<EventSystem>() == null) {
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(events);
        }

        var canvasObject = new GameObject("Touch UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1080);

        // Joystick ring and knob, shown under the moving finger.
        stickBase = MakeImage("Stick", canvas.transform, Circle(true), new Color(1f, 1f, 1f, 0.35f));
        stickKnob = MakeImage("Knob", stickBase, Circle(false), new Color(1f, 1f, 1f, 0.5f));
        stickBase.gameObject.SetActive(false);

        // The ways out of a poem text box, matching W, S, A and D.
        var panel = MakePanel("Text Box Buttons", new Vector2(0.5f, 0f));
        holdButtons = panel.gameObject;
        MakeButton(panel, "Continue", new Vector2(0f, 290f), () => player.ReleaseHold(1f, 0f));
        MakeButton(panel, "Back away", new Vector2(0f, 50f), () => player.ReleaseHold(-1f, 0f));
        MakeButton(panel, "Turn left", new Vector2(-330f, 170f), () => player.ReleaseHold(0f, -1f));
        MakeButton(panel, "Turn right", new Vector2(330f, 170f), () => player.ReleaseHold(0f, 1f));
        holdButtons.SetActive(false);

        // Fly toggle in the top right corner, and Up and Down on the right edge while flying.
        var corner = MakePanel("Fly Toggle", new Vector2(1f, 1f));
        flyToggle = corner.gameObject;
        flyLabel = MakeButton(corner, "Fly", new Vector2(-180f, -90f), () => player.SetFlying(!player.Flying));
        flyToggle.SetActive(false);
        var edge = MakePanel("Climb Buttons", new Vector2(1f, 0.5f));
        climbButtons = edge.gameObject;
        MakeHoldButton(edge, "Up", new Vector2(-180f, 70f), held => rising = held);
        MakeHoldButton(edge, "Down", new Vector2(-180f, -70f), held => sinking = held);
        climbButtons.SetActive(false);

        var hintText = MakeText(canvas.transform, "Drag on the left to walk · drag on the right to look", 36);
        hintText.rectTransform.anchorMin = hintText.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        hintText.rectTransform.anchoredPosition = new Vector2(0f, 80f);
        hintText.rectTransform.sizeDelta = new Vector2(1000f, 80f);
        hint = hintText.gameObject;
    }

    // An empty holder for buttons, pinned to the given point of the screen.
    RectTransform MakePanel (string name, Vector2 anchor) {
        var panel = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        panel.SetParent(canvas.transform, false);
        panel.anchorMin = panel.anchorMax = panel.pivot = anchor;
        return panel;
    }

    static RectTransform MakeImage (string name, Transform parent, Sprite sprite, Color color) {
        var image = new GameObject(name, typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image.rectTransform;
    }

    static Text MakeText (Transform parent, string label, int size) {
        var text = new GameObject(label, typeof(Text)).GetComponent<Text>();
        text.transform.SetParent(parent, false);
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = label;
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    // Returns the button's label.
    static Text MakeButton (RectTransform parent, string label, Vector2 position, UnityEngine.Events.UnityAction onClick) {
        var image = MakeButtonImage(parent, label, position, typeof(Button));
        image.GetComponent<Button>().onClick.AddListener(onClick);
        return image.GetComponentInChildren<Text>();
    }

    // A button that reports when it's pressed (true) and let go (false), for holding down.
    static void MakeHoldButton (RectTransform parent, string label, Vector2 position, System.Action<bool> onHold) {
        var image = MakeButtonImage(parent, label, position, typeof(EventTrigger));
        var trigger = image.GetComponent<EventTrigger>();
        AddTrigger(trigger, EventTriggerType.PointerDown, () => onHold(true));
        AddTrigger(trigger, EventTriggerType.PointerUp, () => onHold(false));
    }

    static void AddTrigger (EventTrigger trigger, EventTriggerType type, System.Action action) {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(_ => action());
        trigger.triggers.Add(entry);
    }

    static Image MakeButtonImage (RectTransform parent, string label, Vector2 position, System.Type behaviour) {
        var image = new GameObject(label + " Button", typeof(Image), behaviour).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        image.color = new Color(0f, 0f, 0f, 0.55f);
        image.rectTransform.anchoredPosition = position;
        image.rectTransform.sizeDelta = new Vector2(300f, 110f);
        var text = MakeText(image.transform, label, 40);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.sizeDelta = Vector2.zero;
        return image;
    }

    // A filled disc or a soft ring, drawn at runtime so no texture asset is needed.
    static Sprite Circle (bool ring) {
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var centre = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                var d = Vector2.Distance(new Vector2(x, y), new Vector2(centre, centre)) / centre;
                var edge = Mathf.Clamp01((1f - d) * centre);  // anti-aliased rim
                var alpha = ring ? (d > 0.85f ? 1f : 0.2f) : 1f;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * edge));
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}

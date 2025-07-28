using TMPro;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class PasscodeWithMicrogestures : MonoBehaviour
{
    [SerializeField] private string secretPasscode;
    
    [SerializeField] private UnityEvent onPasscodeValid = new();
    
    [SerializeField] private UnityEvent onPasscodeInValid = new();
    
    [SerializeField] private UnityEvent onResetButtonPressed = new();

    [SerializeField] private TextMeshPro passcodeText;
    
    [SerializeField] private Material defaultMaterial;
    
    [SerializeField] private Material selectedMaterial;

    [Header("Button References")]
    [SerializeField] private GameObject[] numberButtons = new GameObject[12]; // 0-9, Reset, empty slot
    
    private OVRMicrogestureEventSource ovrMicrogestureEventSource;
    
    private readonly GameObject[,] buttons = new GameObject[4, 3];

    private int currentRow;
    private int currentCol;

    // Cache for performance
    private Dictionary<GameObject, Renderer> rendererCache = new Dictionary<GameObject, Renderer>();
    private System.Text.StringBuilder passcodeBuilder = new System.Text.StringBuilder();
    
    void Awake() 
    {
        PopulateButtons();
        CacheRenderers();
    }

   
    void Start()
    {
        passcodeText.text = string.Empty;
        ovrMicrogestureEventSource = GetComponent<OVRMicrogestureEventSource>();
        if (ovrMicrogestureEventSource != null)
        {
            ovrMicrogestureEventSource.GestureRecognizedEvent.AddListener(OnMicrogestureRecognized);
        }
        HighlightButton(currentRow, currentCol);
    }

    void OnMicrogestureRecognized(OVRHand.MicrogestureType microgestureType)
    {
        switch (microgestureType)
        {
            case OVRHand.MicrogestureType.SwipeForward:
                Move(-1, 0);
                break;
            case OVRHand.MicrogestureType.SwipeBackward:
                Move(1, 0);
                break;
            case OVRHand.MicrogestureType.SwipeLeft:
                Move(0, -1);
                break;
            case OVRHand.MicrogestureType.SwipeRight:
                Move(0, 1);
                break;
            case OVRHand.MicrogestureType.ThumbTap:
                HandleButtonPress();
                break;
        }
    }

    private void HandleButtonPress()
    {
        var gameObjectSelected = buttons[currentRow, currentCol];
        if (gameObjectSelected == null) return;

        var passcodeNumText = gameObjectSelected.GetComponentInChildren<TextMeshPro>();
        if (passcodeNumText == null) return;

        // reset executed
        if (passcodeNumText.text == "RESET")
        {
            ResetPasscode();
            return;
        }

        // clear it out if max
        if (passcodeBuilder.Length >= secretPasscode.Length)
        {
            ResetPasscode();
        }
        
        passcodeBuilder.Append(passcodeNumText.text);
        passcodeText.text = passcodeBuilder.ToString();

        // check passcode combination
        if (passcodeBuilder.Length >= secretPasscode.Length)
        {
            ValidatePasscode();
        }
    }

    private void ResetPasscode()
    {
        passcodeBuilder.Clear();
        passcodeText.text = string.Empty;
        onResetButtonPressed?.Invoke();
    }

    private void ValidatePasscode()
    {
        if (passcodeBuilder.ToString() == secretPasscode)
        {
            onPasscodeValid?.Invoke();
        }
        else
        {
            onPasscodeInValid?.Invoke();
        }
    }

    void Move(int rowDelta, int colDelta)
    {
        int newRow = Mathf.Clamp(currentRow + rowDelta, 0, 3);
        int newCol = Mathf.Clamp(currentCol + colDelta, 0, 2);

        // Skip invalid slot (like bottom-left "empty" space)
        if (buttons[newRow, newCol] == null)
            return;

        ResetHighlight(currentRow, currentCol);
        currentRow = newRow;
        currentCol = newCol;
        HighlightButton(currentRow, currentCol);
    }

    void HighlightButton(int row, int col)
    {
        var buttonObject = buttons[row, col];
        if (buttonObject != null && rendererCache.TryGetValue(buttonObject, out var rendererComponent))
        {
            rendererComponent.material = selectedMaterial;
        }
    }

    void ResetHighlight(int row, int col)
    {
        var buttonObject = buttons[row, col];
        if (buttonObject != null && rendererCache.TryGetValue(buttonObject, out var rendererComponent))
        {
            rendererComponent.material = defaultMaterial;
        }
    }

    private void CacheRenderers()
    {
        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                var buttonObject = buttons[row, col];
                if (buttonObject != null)
                {
                    var renderer = buttonObject.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        rendererCache[buttonObject] = renderer;
                    }
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (ovrMicrogestureEventSource != null)
        {
            ovrMicrogestureEventSource.GestureRecognizedEvent.RemoveListener(OnMicrogestureRecognized);
        }
        rendererCache.Clear();
    }
    
    private void PopulateButtons()
    {
        // Use assigned references instead of GameObject.Find for better performance
        if (numberButtons.Length >= 12)
        {
            // Assign from the serialized array
            buttons[0, 0] = numberButtons[0];  // NumberBox_1
            buttons[0, 1] = numberButtons[1];  // NumberBox_2
            buttons[0, 2] = numberButtons[2];  // NumberBox_3
            buttons[1, 0] = numberButtons[3];  // NumberBox_4
            buttons[1, 1] = numberButtons[4];  // NumberBox_5
            buttons[1, 2] = numberButtons[5];  // NumberBox_6
            buttons[2, 0] = numberButtons[6];  // NumberBox_7
            buttons[2, 1] = numberButtons[7];  // NumberBox_8
            buttons[2, 2] = numberButtons[8];  // NumberBox_9
            buttons[3, 0] = numberButtons[9];  // NumberBox_R
            buttons[3, 1] = numberButtons[10]; // NumberBox_0
            buttons[3, 2] = null; // empty slot
        }
        else
        {
            // Fallback to GameObject.Find if references aren't assigned (legacy support)
            Debug.LogWarning("Button references not properly assigned, falling back to GameObject.Find");
            buttons[0, 0] = GameObject.Find("NumberBox_1");
            buttons[0, 1] = GameObject.Find("NumberBox_2");
            buttons[0, 2] = GameObject.Find("NumberBox_3");
            buttons[1, 0] = GameObject.Find("NumberBox_4");
            buttons[1, 1] = GameObject.Find("NumberBox_5");
            buttons[1, 2] = GameObject.Find("NumberBox_6");
            buttons[2, 0] = GameObject.Find("NumberBox_7");
            buttons[2, 1] = GameObject.Find("NumberBox_8");
            buttons[2, 2] = GameObject.Find("NumberBox_9");
            buttons[3, 0] = GameObject.Find("NumberBox_R");
            buttons[3, 1] = GameObject.Find("NumberBox_0");
            buttons[3, 2] = null; // empty slot
        }
    }
}

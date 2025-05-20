using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class PasscodeWithMicrogestures : MonoBehaviour
{
    [SerializeField] private string secretPasscode;
    
    [SerializeField] private UnityEvent onPasscodeValid = new();
    
    [SerializeField] private UnityEvent onPasscodeInValid = new();
    
    [SerializeField] private UnityEvent onResetButtonPressed = new();

    [SerializeField] private TextMeshPro passcodeText;
    
    [SerializeField] private Material defaultMaterial;
    
    [SerializeField] private Material selectedMaterial;
    
    private OVRMicrogestureEventSource ovrMicrogestureEventSource;
    
    private readonly GameObject[,] buttons = new GameObject[4, 3];

    private int currentRow;
    private int currentCol;
    
    void Awake() => PopulateButtons();

   
    void Start()
    {
        passcodeText.text = string.Empty;
        ovrMicrogestureEventSource = GetComponent<OVRMicrogestureEventSource>();
        ovrMicrogestureEventSource.GestureRecognizedEvent.AddListener(OnMicrogestureRecognized);
        HighlightButton(currentRow, currentCol);
    }

    void OnMicrogestureRecognized(OVRHand.MicrogestureType microgestureType)
    {
        if (microgestureType == OVRHand.MicrogestureType.SwipeForward) Move(-1, 0);
        if (microgestureType == OVRHand.MicrogestureType.SwipeBackward) Move(1, 0);
        if (microgestureType == OVRHand.MicrogestureType.SwipeLeft) Move(0, -1);
        if (microgestureType == OVRHand.MicrogestureType.SwipeRight) Move(0, 1);
        if (microgestureType == OVRHand.MicrogestureType.ThumbTap)
        {
            var gameObjectSelected = buttons[currentRow, currentCol];
            var passcodeNumText = gameObjectSelected.GetComponentInChildren<TextMeshPro>();
        
            // reset executed
            if (passcodeNumText.text == "RESET")
            {
                passcodeText.text = string.Empty;
                onResetButtonPressed?.Invoke();
                return;
            }
        
            // clear it out if max
            if (passcodeText.text.Length >= secretPasscode.Length)
                passcodeText.text = string.Empty;
            
            passcodeText.text += passcodeNumText.text;

            // check passcode combination
            if (passcodeText.text.Length >= secretPasscode.Length)
            {
                if (passcodeText.text == secretPasscode)
                {
                    onPasscodeValid?.Invoke();
                }
                else
                {
                    onPasscodeInValid?.Invoke();
                }
            }
        }
    }

    void Move(int rowDelta, int colDelta)
    {
        int newRow = Mathf.Clamp(currentRow + rowDelta, 0, 3);
        int newCol = Mathf.Clamp(currentCol + colDelta, 0, 2);

        // Skip invalid slot (like bottom-left "empty" space)
        if (buttons[newRow, newCol] ==null)
            return;

        ResetHighlight(currentRow, currentCol);
        currentRow = newRow;
        currentCol = newCol;
        HighlightButton(currentRow, currentCol);
    }

    void HighlightButton(int row, int col)
    {
        var rendererComponent = buttons[row, col].GetComponent<Renderer>();
        if (rendererComponent != null)
            rendererComponent.material = selectedMaterial;
    }

    void ResetHighlight(int row, int col)
    {
        var rendererComponent = buttons[row, col].GetComponent<Renderer>();
        if (rendererComponent != null)
            rendererComponent.material = defaultMaterial;
    }

    private void OnDestroy()
    {
        ovrMicrogestureEventSource.GestureRecognizedEvent.RemoveListener(OnMicrogestureRecognized);
    }
    
    private void PopulateButtons()
    {
        // 4 rows, 3 columns
        // not the best way to handle this but this is just a demo ;)
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

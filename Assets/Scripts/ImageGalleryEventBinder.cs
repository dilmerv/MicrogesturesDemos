using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource))]
public class ImageGalleryEventBinder : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioClip imageChangeSound;
    [SerializeField] private AudioClip errorSound;
    [SerializeField] private float imageChangeVolume = 0.7f;
    [SerializeField] private float errorVolume = 0.8f;
    
    private AudioSource audioSource;
    private ImageGalleryUI imageGalleryUI;
    
    void Start()
    {
        // Get the required AudioSource component
        audioSource = GetComponent<AudioSource>();

        // Ensure AudioSource does not play on awake
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
        }
        
        // Get the ImageGalleryUI from the current component
        imageGalleryUI = GetComponent<ImageGalleryUI>();
        
        if (imageGalleryUI == null)
        {
            Debug.LogError("ImageGalleryEventBinder: No ImageGalleryUI component found on this GameObject!");
            return;
        }
        
        // Subscribe to events
        imageGalleryUI.OnImageChanged.AddListener(OnImageChanged);
        imageGalleryUI.OnLimitReached.AddListener(OnLimitReached);
        
        // Load default audio clips if not assigned
        if (imageChangeSound == null)
        {
            imageChangeSound = Resources.Load<AudioClip>("Audio/Success");
        }
        
        if (errorSound == null)
        {
            errorSound = Resources.Load<AudioClip>("Audio/Error");
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (imageGalleryUI != null)
        {
            imageGalleryUI.OnImageChanged.RemoveListener(OnImageChanged);
            imageGalleryUI.OnLimitReached.RemoveListener(OnLimitReached);
        }
    }
    
    private void OnImageChanged(int newImageIndex)
    {
        if (imageChangeSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(imageChangeSound, imageChangeVolume);
        }
    }
    
    private void OnLimitReached(string limitMessage)
    {
        if (errorSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(errorSound, errorVolume);
        }
    }
    
    // Public methods for manual control
    public void PlayImageChangeSound()
    {
        if (imageChangeSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(imageChangeSound, imageChangeVolume);
        }
    }
    
    public void PlayErrorSound()
    {
        if (errorSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(errorSound, errorVolume);
        }
    }
    
    // Public properties for runtime access
    public AudioClip ImageChangeSound
    {
        get => imageChangeSound;
        set => imageChangeSound = value;
    }
    
    public AudioClip ErrorSound
    {
        get => errorSound;
        set => errorSound = value;
    }
    
    public float ImageChangeVolume
    {
        get => imageChangeVolume;
        set => imageChangeVolume = Mathf.Clamp01(value);
    }
    
    public float ErrorVolume
    {
        get => errorVolume;
        set => errorVolume = Mathf.Clamp01(value);
    }
} 
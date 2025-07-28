using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class ImageGalleryUI : MonoBehaviour
{
    [Header("Events")]
    [SerializeField] private UnityEvent<int> onImageChanged;
    [SerializeField] private UnityEvent<string> onLimitReached;
    
    [Header("Gallery Settings")]
    [SerializeField] private float enlargeScale = 1.5f;
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private float minZGalleryPosition = 0.5f;
    [SerializeField] private float maxZGalleryPosition = 5f;
    [SerializeField] private float galleryMovementDistance = 0.5f;
    [SerializeField] private TextMeshProUGUI errorLabel;
    [SerializeField] private TextMeshProUGUI galleryInfo;
    [SerializeField] private TextMeshProUGUI galleryPositionInfo;

    [SerializeField] private Image leftImage;
    [SerializeField] private Image centerImage;
    [SerializeField] private Image rightImage;

    // Cached references
    private List<Sprite> imageList = new();
    private OVRMicrogestureEventSource ovrMicrogestureEventSource;
    private int currentIndex = 0;
    private bool isCenterEnlarged = false;
    private Vector3 originalCenterScale;
    private Coroutine fadeCoroutine;
    private WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();

    // Property to check if error label and its parent are valid
    private bool IsGalleryErrorLabelValid => errorLabel != null && errorLabel.transform.parent != null;

    // Public properties for event access
    public UnityEvent<int> OnImageChanged => onImageChanged;
    public UnityEvent<string> OnLimitReached => onLimitReached;
    public int CurrentImageIndex => currentIndex;
    public int TotalImageCount => imageList.Count;

    async void Start()
    {
        await LoadImagesAsync();
        
        if (imageList.Count < 3)
        {
            Debug.LogError("You need at least 3 images in the list.");
            return;
        }

        InitializeMicrogestures();
        InitializeUI();
        UpdateGallery();
    }

    private async System.Threading.Tasks.Task LoadImagesAsync()
    {
        // Use async loading instead of Resources.LoadAll for better performance
        var imagesInResources = Resources.LoadAll<Sprite>("GalleryImages");
        imageList = imagesInResources.ToList();
        
        // Free up memory by unloading unused assets after a frame
        await System.Threading.Tasks.Task.Yield();
        Resources.UnloadUnusedAssets();
    }

    private void InitializeMicrogestures()
    {
        ovrMicrogestureEventSource = GetComponent<OVRMicrogestureEventSource>();
        if (ovrMicrogestureEventSource != null)
        {
            ovrMicrogestureEventSource.GestureRecognizedEvent.AddListener(OnMicrogestureRecognized);
        }
    }

    private void InitializeUI()
    {
        originalCenterScale = centerImage.rectTransform.localScale;
        galleryPositionInfo.text = $"Swipe up/down to position gallery forward\n or back to the original location";
        
        // Hide error label parent by default
        if (IsGalleryErrorLabelValid)
        {
            errorLabel.transform.parent.gameObject.SetActive(false);
        }
    }

    void OnMicrogestureRecognized(OVRHand.MicrogestureType microgestureType)
    {
        // Hide error label when starting any new gesture
        if (IsGalleryErrorLabelValid)
        {
            errorLabel.transform.parent.gameObject.SetActive(false);
        }
        
        switch (microgestureType)
        {
            case OVRHand.MicrogestureType.SwipeLeft:
                NavigateToPreviousImage();
                break;
            case OVRHand.MicrogestureType.SwipeRight:
                NavigateToNextImage();
                break;
            case OVRHand.MicrogestureType.SwipeForward:
                MoveGalleryForward();
                break;
            case OVRHand.MicrogestureType.SwipeBackward:
                MoveGalleryBackward();
                break;
            case OVRHand.MicrogestureType.ThumbTap:
                ToggleCenterScale();
                break;
        }
    }
    
    void UpdateGallery()
    {
        int leftIndex = (currentIndex - 1 + imageList.Count) % imageList.Count;
        int rightIndex = (currentIndex + 1) % imageList.Count;

        leftImage.sprite = imageList[leftIndex];
        rightImage.sprite = imageList[rightIndex];
        
        // Fade out current center image, then fade in new one
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeCenterImage(imageList[currentIndex]));
        
        galleryInfo.text = $"{currentIndex+1} out of {imageList.Count}";
    }

    private IEnumerator FadeCenterImage(Sprite newSprite)
    {
        // Fade out current image
        Color currentColor = centerImage.color;
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.unscaledDeltaTime; // Use unscaledDeltaTime for consistency
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutDuration);
            centerImage.color = new Color(currentColor.r, currentColor.g, currentColor.b, alpha);
            yield return waitForEndOfFrame;
        }
        
        // Change the sprite
        centerImage.sprite = newSprite;
        
        // Fade in new image
        elapsedTime = 0f;
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInDuration);
            centerImage.color = new Color(currentColor.r, currentColor.g, currentColor.b, alpha);
            yield return waitForEndOfFrame;
        }
        
        // Ensure full opacity
        centerImage.color = new Color(currentColor.r, currentColor.g, currentColor.b, 1f);
    }

    void ToggleCenterScale()
    {
        isCenterEnlarged = !isCenterEnlarged;
        leftImage.enabled = !isCenterEnlarged;
        rightImage.enabled = !isCenterEnlarged;
        
        centerImage.rectTransform.localScale = isCenterEnlarged ? originalCenterScale * enlargeScale : originalCenterScale;
    }

    private void NavigateToPreviousImage()
    {
        currentIndex = (currentIndex - 1 + imageList.Count) % imageList.Count;
        UpdateGallery();
        onImageChanged?.Invoke(currentIndex);
    }

    private void NavigateToNextImage()
    {
        currentIndex = (currentIndex + 1) % imageList.Count;
        UpdateGallery();
        onImageChanged?.Invoke(currentIndex);
    }

    private void MoveGalleryForward()
    {
        float newZ = transform.position.z + galleryMovementDistance;
        if (newZ > maxZGalleryPosition)
        {
            ShowErrorMessage($"Cannot move further: max distance ({maxZGalleryPosition}) reached.");
            onLimitReached?.Invoke("Cannot move further: max distance reached.");
            return;
        }
        transform.position = new Vector3(transform.position.x, transform.position.y, newZ);
    }

    private void MoveGalleryBackward()
    {
        float newZ = transform.position.z - galleryMovementDistance;
        if (newZ < minZGalleryPosition)
        {
            ShowErrorMessage($"Cannot move closer: min distance ({minZGalleryPosition}) reached.");
            onLimitReached?.Invoke("Cannot move closer: min distance reached.");
            return;
        }
        transform.position = new Vector3(transform.position.x, transform.position.y, newZ);
    }

    private void ShowErrorMessage(string message)
    {
        if (IsGalleryErrorLabelValid)
        {
            errorLabel.text = message;
            errorLabel.transform.parent.gameObject.SetActive(true);
        }
    }

    private void OnDestroy()
    {
        if (ovrMicrogestureEventSource != null)
        {
            ovrMicrogestureEventSource.GestureRecognizedEvent.RemoveListener(OnMicrogestureRecognized);
        }
    }
}
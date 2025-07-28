using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class ImageGalleryUI : MonoBehaviour
{
    [SerializeField] private float enlargeScale = 1.5f;

    [SerializeField] private TextMeshProUGUI galleryInfo;
    [SerializeField] private TextMeshProUGUI galleryPositionInfo;

    [SerializeField] private Image leftImage;
    [SerializeField] private Image centerImage;
    [SerializeField] private Image rightImage;
    [SerializeField] private int bigIncrementsOf = 5;

    private List<Sprite> imageList = new();
    private OVRMicrogestureEventSource ovrMicrogestureEventSource;
    private int currentIndex = 0;
    private bool isCenterEnlarged = false;
    private Vector3 originalCenterScale;

    void Start()
    {
        imageList = Resources.LoadAll<Sprite>("GalleryImages")
            .ToList();
        
        if (imageList.Count < 3)
        {
            Debug.LogError("You need at least 3 images in the list.");
            return;
        }

        ovrMicrogestureEventSource = GetComponent<OVRMicrogestureEventSource>();
        ovrMicrogestureEventSource.GestureRecognizedEvent.AddListener(OnMicrogestureRecognized);
        
        originalCenterScale = centerImage.rectTransform.localScale;
        galleryPositionInfo.text = $"Swipe up/down to position gallery forward\n or back to the original location";
        UpdateGallery();
    }

    void OnMicrogestureRecognized(OVRHand.MicrogestureType microgestureType)
    {
        if (microgestureType == OVRHand.MicrogestureType.SwipeLeft)
        {
            NavigateToPreviousImage();
        }
        if (microgestureType == OVRHand.MicrogestureType.SwipeRight)
        {
            NavigateToNextImage();
        }
        if (microgestureType == OVRHand.MicrogestureType.SwipeForward)
        {
            MoveGalleryForward();
        }
        if (microgestureType == OVRHand.MicrogestureType.SwipeBackward)
        {
            MoveGalleryBackward();
        }
        if (microgestureType == OVRHand.MicrogestureType.ThumbTap)
        {
            ToggleCenterScale();
        }
    }
    
    void UpdateGallery()
    {
        int leftIndex = (currentIndex - 1 + imageList.Count) % imageList.Count;
        int rightIndex = (currentIndex + 1) % imageList.Count;

        leftImage.sprite = imageList[leftIndex];
        centerImage.sprite = imageList[currentIndex];
        rightImage.sprite = imageList[rightIndex];
        
        galleryInfo.text = $"{currentIndex+1} out of {imageList.Count}";
    }

    void ToggleCenterScale(bool reset = false)
    {
        isCenterEnlarged = !isCenterEnlarged;
        leftImage.enabled = !isCenterEnlarged;
        rightImage.enabled = !isCenterEnlarged;
        
        //galleryInfo.gameObject.SetActive(!isCenterEnlarged);
        centerImage.rectTransform.localScale = isCenterEnlarged ? originalCenterScale * enlargeScale : originalCenterScale;
    }

    private void NavigateToPreviousImage()
    {
        currentIndex = (currentIndex - 1 + imageList.Count) % imageList.Count;
        UpdateGallery();
    }

    private void NavigateToNextImage()
    {
        currentIndex = (currentIndex + 1) % imageList.Count;
        UpdateGallery();
    }

    private void MoveGalleryForward()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z + 0.5f);
    }

    private void MoveGalleryBackward()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z - 0.5f);
    }
}
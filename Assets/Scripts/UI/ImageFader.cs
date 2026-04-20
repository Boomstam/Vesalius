using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ImageFader : MonoBehaviour
{
    private int CurrentNumImages => images?.Length ?? 0;

    public Image[] images;
    
    public float fadeVal;

    private float lastFadeVal;

    private void Start()
    {
        if(images.Length < 3)
            return;
        
        if (images[2].sprite == null)
        {
            images[2].gameObject.SetActive(false);
            images = images.Take(2).ToArray();
        }
    }

    private void Update()
    {
        if (!Mathf.Approximately(lastFadeVal, fadeVal))
        {
            lastFadeVal = fadeVal;
            
            SetFadeVal(fadeVal);
        }
    }

    public void SetFadeVal(float fadeVal)
    {
        if(CurrentNumImages == 0)
        {
            Debug.LogWarning($"No images, can't fade");
            return;
        }
        if(CurrentNumImages == 1)
        {
            Debug.LogWarning($"Only 1 image, can't fade");
            return;
        }
        float percentagePerSource = 1f / (float)(CurrentNumImages - 1);

        int startSample = Mathf.FloorToInt(fadeVal / percentagePerSource);
        float remainder = fadeVal - (percentagePerSource * startSample);
        float remainderPercentage = remainder / percentagePerSource;
        
        for (int i = 0; i < CurrentNumImages; i++)
        {
            Image image = images[i];
            
            float alpha = 0;
            
            if (i == startSample)
                alpha = 1 - remainderPercentage;
            if (i == startSample + 1)
                alpha = remainderPercentage;
        
            Color currentColor = image.color;
            image.color = new Color(currentColor.r, currentColor.g, currentColor.b, alpha);
        }
    }
}

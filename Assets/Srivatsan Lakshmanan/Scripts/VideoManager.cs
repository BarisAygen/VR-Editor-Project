using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Video;
using UnityEngine.UI;

public class VideoManager : MonoBehaviour, IDragHandler, IPointerDownHandler
{

    public VideoPlayer player;
    public Image progress;
    
    void Update()
    {
        if (player.frameCount > 0)
            progress.fillAmount = (float)player.frame / (float)player.frameCount;
    }
    public void OnDrag(PointerEventData eventData)
    {
        TrySkip(eventData);
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        TrySkip(eventData);
    }
    private void SkipToPercent(float pct)
    {
        Debug.Log("3");
        var frame = player.frameCount * pct;
        player.frame = (long)frame;
        Debug.Log("4");
    }
    private void TrySkip(PointerEventData eventData)
    {
        Debug.Log("1");
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            progress.rectTransform, eventData.position, null, out localPoint))
        {
            Debug.Log("2");
            float pct = Mathf.InverseLerp(progress.rectTransform.rect.xMin, progress.rectTransform.rect.xMax, localPoint.x);
            SkipToPercent(pct);
        }
    }
    public void VideoPlayerPause()
    {
        if(player != null)
            player.Pause();
    }
    public void VideoPlayerPlay()
    {
        if(player != null)
            player.Play();  
    }
  
}


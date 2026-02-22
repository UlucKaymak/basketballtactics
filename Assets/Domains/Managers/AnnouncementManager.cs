using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;

public enum AnnouncementType
{
    Normal,
    Goal,
    Turn,
    Combat,
    Alert
}

public class AnnouncementManager : MonoBehaviour
{
    public static AnnouncementManager Instance;

    [Header("UI References")]
    public TextMeshProUGUI announcementText;
    public TextMeshProUGUI miniAnnouncementText;

    private Queue<AnnouncementRequest> announcementQueue = new Queue<AnnouncementRequest>();
    private bool isShowingAnnouncement = false;

    private struct AnnouncementRequest
    {
        public string message;
        public float duration;
        public AnnouncementType type;
        public Color color;
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (announcementText != null) 
        {
            announcementText.text = "";
            announcementText.alpha = 0;
        }
        if (miniAnnouncementText != null)
        {
            miniAnnouncementText.text = "";
            miniAnnouncementText.alpha = 0;
        }
    }

    public void SendAnnouncement(string message, float duration = 1.5f, AnnouncementType type = AnnouncementType.Normal, Color? color = null)
    {
        announcementQueue.Enqueue(new AnnouncementRequest 
        { 
            message = message, 
            duration = duration, 
            type = type,
            color = color ?? Color.white 
        });

        if (!isShowingAnnouncement)
        {
            StartCoroutine(ProcessQueue());
        }
    }

    public IEnumerator SendAnnouncementAndWait(string message, float duration = 1.5f, AnnouncementType type = AnnouncementType.Normal, Color? color = null)
    {
        AnnouncementRequest request = new AnnouncementRequest 
        { 
            message = message, 
            duration = duration, 
            type = type,
            color = color ?? Color.white 
        };
        
        // If we wait, we bypass the queue or rather, we wait for our turn in the queue?
        // Let's make it simple: if we are waiting, we wait until the queue is empty and then show this one.
        while (isShowingAnnouncement) yield return null;
        
        yield return StartCoroutine(ShowAnnouncementRoutine(request));
    }

    public void SendMiniAnnouncement(string message, Color? color = null)
    {
        if (miniAnnouncementText == null) return;
        
        miniAnnouncementText.DOKill();
        miniAnnouncementText.transform.DOKill();
        
        miniAnnouncementText.text = message;
        miniAnnouncementText.color = color ?? Color.white;
        miniAnnouncementText.alpha = 1f;
        
        miniAnnouncementText.transform.localScale = Vector3.one;
        miniAnnouncementText.transform.DOPunchScale(Vector3.one * 0.1f, 0.3f);
        
        // Mini announcements usually stay or fade out after a while
        miniAnnouncementText.DOFade(0, 0.5f).SetDelay(2.5f);
    }

    private IEnumerator ProcessQueue()
    {
        isShowingAnnouncement = true;

        while (announcementQueue.Count > 0)
        {
            AnnouncementRequest request = announcementQueue.Dequeue();
            yield return StartCoroutine(ShowAnnouncementRoutine(request));
        }

        isShowingAnnouncement = false;
    }

    private IEnumerator ShowAnnouncementRoutine(AnnouncementRequest request)
    {
        if (announcementText == null) yield break;

        announcementText.text = request.message;
        announcementText.color = request.color;
        announcementText.alpha = 0;
        announcementText.transform.localScale = Vector3.one * 0.5f;

        float punchAmount = 0.2f;
        if (request.type == AnnouncementType.Goal) punchAmount = 0.4f;

        Sequence seq = DOTween.Sequence();
        seq.Append(announcementText.DOFade(1f, 0.3f));
        seq.Join(announcementText.transform.DOScale(1f + punchAmount, 0.3f).SetEase(Ease.OutBack));
        
        if (request.type == AnnouncementType.Goal)
        {
            seq.Join(announcementText.transform.DOPunchRotation(new Vector3(0, 0, 10), 0.5f));
        }

        seq.AppendInterval(request.duration);
        
        seq.Append(announcementText.DOFade(0f, 0.3f));
        seq.Join(announcementText.transform.DOScale(0.8f, 0.3f));

        yield return seq.WaitForCompletion();
    }
}

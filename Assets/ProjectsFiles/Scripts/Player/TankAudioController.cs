using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public sealed class TankAudioController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TankShooter tankShooter;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource weaponSource;
    [SerializeField] private AudioSource feedbackSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip shotClip;
    [SerializeField] private AudioClip emptyWeaponClip;
    [SerializeField] private AudioClip hitConfirmClip;
    [SerializeField] private AudioClip killConfirmClip;
    [SerializeField] private AudioClip baseHitConfirmClip;
    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float shotVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float emptyWeaponVolume = 0.6f;
    [SerializeField, Range(0f, 1f)] private float hitConfirmVolume = 0.8f;
    [SerializeField, Range(0f, 1f)] private float killConfirmVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float baseHitConfirmVolume = 0.8f;
    private MatchManager subscribedMatch;
    private Coroutine matchSubscriptionRoutine;

    private TankShooter subscribedShooter;

    private void Awake()
    {
        if (tankShooter == null)
        {
            tankShooter = GetComponent<TankShooter>();
        }
    }

    private void OnEnable()
    {
        SubscribeToEvents();

        matchSubscriptionRoutine = StartCoroutine(
            SubscribeToMatchWhenReady());
    }

    private void OnDisable()
    {
        if (matchSubscriptionRoutine != null)
        {
            StopCoroutine(matchSubscriptionRoutine);
            matchSubscriptionRoutine = null;
        }

        UnsubscribeFromEvents();
        UnsubscribeFromMatch();
    }

    private void SubscribeToEvents()
    {
        // Remove any existing subscription before binding.
        UnsubscribeFromEvents();

        if (tankShooter == null)
            return;

        subscribedShooter = tankShooter;
        subscribedShooter.ShotFired += PlayShot;
        subscribedShooter.EmptyFireAttempted += PlayEmptyClick;
    }

    private void UnsubscribeFromEvents()
    {
        if (subscribedShooter != null)
        {
            subscribedShooter.ShotFired -= PlayShot;
            subscribedShooter.EmptyFireAttempted -= PlayEmptyClick;
        }

        subscribedShooter = null;
    }

    public void PlayShot()
    {
        PlaySound(weaponSource, shotClip, shotVolume);
    }

    public void PlayEmptyClick()
    {
        PlaySound(feedbackSource, emptyWeaponClip, emptyWeaponVolume);
    }

    public void PlayHitConfirm()
    {
        PlaySound(feedbackSource, hitConfirmClip, hitConfirmVolume);
    }

    public void PlayKillConfirm()
    {
        PlaySound(feedbackSource, killConfirmClip, killConfirmVolume);
    }
    public void PlayBaseHitConfirm()
    {
        PlaySound(
            feedbackSource,
            baseHitConfirmClip,
            baseHitConfirmVolume);
    }
    private void PlaySound(
        AudioSource source,
        AudioClip clip,
        float volume)
    {
        if (!isActiveAndEnabled)
            return;

        if (source == null || clip == null)
            return;

        if (!source.isActiveAndEnabled)
            return;

        source.PlayOneShot(clip, volume);
    }

    private IEnumerator SubscribeToMatchWhenReady()
    {
        UnsubscribeFromMatch();

        while (true)
        {
            MatchManager match = MatchManager.Instance;

            if (match != null && match.IsSpawned)
            {
                subscribedMatch = match;
                subscribedMatch.CombatConfirmed += OnCombatConfirmed;

                yield break;
            }

            yield return null;
        }
    }

    private void UnsubscribeFromMatch()
    {
        if (subscribedMatch != null)
        {
            subscribedMatch.CombatConfirmed -= OnCombatConfirmed;
        }

        subscribedMatch = null;
    }

    private void OnCombatConfirmed(CombatFeedback feedback)
    {
        if (tankShooter == null ||
            !tankShooter.IsSpawned ||
            !tankShooter.IsClient ||
            !tankShooter.IsOwner)
        {
            return;
        }

        if (!feedback.HasAttacker)
            return;

        if (feedback.AttackerId != tankShooter.OwnerClientId)
            return;

        if (feedback.IsBase)
        {
            PlayBaseHitConfirm();
        }
        else if (feedback.IsKill)
        {
            PlayKillConfirm();
        }
        else
        {
            PlayHitConfirm();
        }
    }
}
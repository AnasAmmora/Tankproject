using System.Collections;
using UnityEngine;

public class AutoReturnParticle : MonoBehaviour
{
    private ParticleSystem[] allSystems;
    private float lifetime;
    private Coroutine returnRoutine;

    private void Awake()
    {
        allSystems = GetComponentsInChildren<ParticleSystem>();

        float longest = 0f;
        foreach (ParticleSystem ps in allSystems)
        {
            var main = ps.main;
            float duration = main.duration + main.startLifetime.constantMax;
            if (duration > longest) longest = duration;
        }

        lifetime = longest > 0f ? longest : 2f; 
    }

    private void OnEnable()
    {
        foreach (ParticleSystem ps in allSystems)
        {
            ps.Clear(true);
            ps.Play(true);
        }

        returnRoutine = StartCoroutine(ReturnAfterDelay());
    }

    private void OnDisable()
    {
        if (returnRoutine != null)
        {
            StopCoroutine(returnRoutine);
            returnRoutine = null;
        }
    }

    private IEnumerator ReturnAfterDelay()
    {
        yield return new WaitForSeconds(lifetime);
        PoolManager.Release(gameObject);
    }
}
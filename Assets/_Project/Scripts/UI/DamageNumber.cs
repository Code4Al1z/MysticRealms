using UnityEngine;
using TMPro;
using System.Collections;

public class DamageNumber : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private float    riseSpeed      = 1.8f;
    [SerializeField] private float    lifetime       = 0.9f;
    [SerializeField] private float    heavyThreshold = 20f;
    [SerializeField] private float    heavyScale     = 1.4f;

    private DamageNumberSpawner spawner;

    public void Initialise(DamageNumberSpawner owner)
    {
        spawner = owner;
    }

    public void Show(float amount, Vector3 worldPosition, Color color)
    {
        transform.position   = worldPosition;
        transform.localScale = Vector3.one;
        gameObject.SetActive(true);

        if (label != null)
        {
            label.text  = Mathf.CeilToInt(amount).ToString();
            label.color = color;

            // Heavier hits get a brief scale pop
            if (amount >= heavyThreshold)
                transform.localScale = Vector3.one * heavyScale;
        }

        StopAllCoroutines();
        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        float   elapsed  = 0f;
        Vector3 startPos = transform.position;
        Color   startCol = label != null ? label.color : Color.white;

        // Slightly shrink heavy hits back to normal scale over the first third
        Vector3 startScale = transform.localScale;

        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lifetime;

            transform.position   = startPos + Vector3.up * (riseSpeed * elapsed);
            transform.localScale = Vector3.Lerp(startScale, Vector3.one, Mathf.Clamp01(t * 3f));

            if (label != null)
            {
                Color c = startCol;
                c.a         = Mathf.Lerp(1f, 0f, t);
                label.color = c;
            }

            yield return null;
        }

        gameObject.SetActive(false);
        if (spawner != null) 
            spawner.ReturnToPool(this);
    }
}

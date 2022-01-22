using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformCurve : MonoBehaviour
{
    public AnimationCurve yCurve;
    public AnimationCurve scaleCurve;
    public AnimationCurve opacityCurve;
    public AnimationCurve angleCurve;
    public float delay = 0f;
    float currentTime = 0.0f;
    Vector3 originScale;
    Vector3 originPosition;
    List<float> originOpacity = new List<float>();
    Quaternion originRotation;
    // Start is called before the first frame update
    void Start()
    {
        currentTime = -delay;
        originScale = transform.localScale;
        originPosition = transform.position;
        foreach (var renderer in GetComponentsInChildren<SpriteRenderer>())
        {
            originOpacity.Add(renderer.color.a);
        }
        originRotation = transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        if (currentTime < 0)
        {
            currentTime += Time.deltaTime;
            return;
        }
        if (scaleCurve.keys.Length > 0)
        {
            var scale = scaleCurve.Evaluate(currentTime);
            transform.localScale = originScale * scale;
        }
        if (yCurve.keys.Length > 0)
        {
            transform.position = originPosition + new Vector3(0, yCurve.Evaluate(currentTime), 0);
        }
        if (opacityCurve.keys.Length > 0)
        {
            var i = 0;
            foreach (var renderer in GetComponentsInChildren<SpriteRenderer>())
            {
                renderer.color = new Color(renderer.color.r, renderer.color.g, renderer.color.b, originOpacity[i] * opacityCurve.Evaluate(currentTime));
                i++;
            }
        }
        if (angleCurve.keys.Length > 0)
        {
            var angle = angleCurve.Evaluate(currentTime);
            transform.rotation = originRotation * Quaternion.Euler(0, 0, angle);
        }
        currentTime += Time.deltaTime;
    }
}

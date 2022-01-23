using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerBoard : MonoBehaviour
{
    RectTransform rectTransform;
    public TransformCurve thisCurve;
    public TransformCurve leaveCurve;
    public TransformCurve textCurve;
    Player player;
    string formatString;
    // Start is called before the first frame update
    void Start()
    {
        var tmp = textCurve.GetComponent<TMPro.TextMeshProUGUI>();
        formatString = tmp.text;
        rectTransform = GetComponent<RectTransform>();
        player = GameObject.FindObjectOfType<Player>();
        if (player.maxSelfCenterCount == 0)
        {
            gameObject.SetActive(false);
            return;
        }
        StartCoroutine(EnableTextCurve());
    }
    IEnumerator EnableTextCurve()
    {
        yield return new WaitForSeconds(thisCurve.yCurve.keys[thisCurve.yCurve.keys.Length - 1].time);
        textCurve.enabled = true;
    }

    // Update is called once per frame
    void Update()
    {
        var tmp = textCurve.GetComponent<TMPro.TextMeshProUGUI>();
        if (player.shouldSelfCenter)
        {
            tmp.text = "You are not the moving one\nRelease Z to cancel";
        }
        else
        {
            tmp.text = System.String.Format(formatString, player.maxSelfCenterCount);
        }
        if (player.maxSelfCenterCount == 0 && !player.shouldSelfCenter)
        {
            textCurve.enabled = false;
            thisCurve.enabled = false;
            leaveCurve.enabled = true;
        }
    }
}

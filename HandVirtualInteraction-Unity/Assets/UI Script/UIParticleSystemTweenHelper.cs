using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
 
public class UIParticleSystemTweenHelper : MonoBehaviour
{
    // Start is called before the first frame update
    [Range(0,1)]
    public float alpha = 0f;
    //
    private float lastAlpha = -1f;
    private List<ParticleSystemAlpha> colorList = null;
 
    void Start()
    {
        Init();
    }
 
    private void Init()
    {
        colorList = new List<ParticleSystemAlpha>();
        AddColorData();
    }
 
    public void ForceInit()
    {
        Init();
    }
    void AddColorData()
    {
        ParticleSystemAlpha psa = new ParticleSystemAlpha();
        if (psa.psr == null)
        {
            psa.psr = GetComponentsInChildren<ParticleSystemRenderer>(true);
            float[] tempAlpha_1 = new float[psa.psr.Length];
            float[] tempAlpha_2 = new float[psa.psr.Length];
            for (int i = 0; i < psa.psr.Length; i++)
            {
                if (psa.psr[i].gameObject != null || psa.psr[i].enabled)
                {
                    float temp_alpha = 0;
                    if (psa.psr[i].sharedMaterial && psa.psr[i].sharedMaterial.HasColor("_Color_Water"))
                    {
                        temp_alpha = psa.psr[i].sharedMaterial.GetColor("_Color_Water").a;
                        tempAlpha_2[i] = temp_alpha;
                    }
                    if (psa.psr[i].sharedMaterial && psa.psr[i].sharedMaterial.HasColor("_TintColor"))
                    {
                        temp_alpha = psa.psr[i].sharedMaterial.GetColor("_TintColor").a;
 
                    }
                    else if (psa.psr[i].sharedMaterial && psa.psr[i].sharedMaterial.HasColor("_Color"))
                    {
                        temp_alpha = psa.psr[i].sharedMaterial.GetColor("_Color").a;
                    }
                    if (temp_alpha >= 1)
                    {
                        temp_alpha = 1;
                    }
                    tempAlpha_1[i] = temp_alpha;
                }
            }
            if (psa.alphas_1 == null)
            {
                psa.alphas_1 = tempAlpha_1;
            }
            if (psa.alphas_2 == null)
            {
                psa.alphas_2 = tempAlpha_2;
            }
        }
        colorList.Add(psa);
    }
    private void OnEnable(){Update();}
    // Update is called once per frame
    void Update()
    {
        if (alpha <= 0 || colorList.Count == 0 || Mathf.Approximately(lastAlpha, alpha)) return;
        foreach (ParticleSystemAlpha psa in colorList)
        {
            ParticleSystemRenderer[] psr = psa.psr;
            float[] alphas_1 = psa.alphas_1;
            float[] alphas_2 = psa.alphas_2;
            for (int i = 0; i < psr.Length; i++)
            {
                if(psr[i].gameObject != null || psr[i].enabled)
                {
                    if (psa.psr[i].sharedMaterial && psa.psr[i].sharedMaterial.HasColor("_Color_Water"))
                    {
                        Color c = psr[i].sharedMaterial.GetColor("_Color_Water");
                        psr[i].sharedMaterial.SetColor("_Color_Water", new Color(c.r, c.g, c.b, alphas_2[i] * alpha));
                    }
                    if (psr[i].sharedMaterial && psr[i].sharedMaterial.HasColor("_TintColor"))
                    {
                        Color c = psr[i].sharedMaterial.GetColor("_TintColor");
                        psr[i].sharedMaterial.SetColor("_TintColor", new Color(c.r, c.g, c.b, alphas_1[i] * alpha));
                    }
                    else if (psr[i].sharedMaterial && psr[i].sharedMaterial.HasColor("_Color"))
                    {
                        Color c = psr[i].sharedMaterial.GetColor("_Color");
                        psr[i].sharedMaterial.SetColor("_Color", new Color(c.r, c.g, c.b, alphas_1[i] * alpha));
                    }
                }
            }
        }
        lastAlpha = alpha;
    }
    private void OnDestroy()
    {
        colorList.Clear();
        colorList = null;
    }
}
public class ParticleSystemAlpha
{
    public ParticleSystemRenderer[] psr = null;
    public float[] alphas_1 = null;
    public float[] alphas_2 = null;
}
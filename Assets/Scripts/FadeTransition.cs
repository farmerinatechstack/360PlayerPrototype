using UnityEngine;
using System;
using System.Collections;

public class FadeTransition : MonoBehaviour {
	public Material m;
	public float fadeTime = 2.5f;	// Time to fade
	public string sceneName;		// Name of scene to transition to

	// On start, the material always fades from black to transparent
	void Start () {
		m.color = new Color (m.color.r, m.color.g, m.color.b, 1.0f);
		StartCoroutine (FadeTo (0.0f, fadeTime));
	}

	IEnumerator FadeTo(float targetAlpha, float time) {
		float startAlpha = m.color.a;

		for (float t = 0.0f; t <= time; t += Time.deltaTime) {
			float fadeAlpha = Mathf.Lerp (startAlpha, targetAlpha, t / time);
			Color newColor = new Color(m.color.r, m.color.g, m.color.b, fadeAlpha);
			m.color = newColor;
			yield return null; 		// Wait for one frame
		}

		Color finalColor = new Color(m.color.r, m.color.g, m.color.b, targetAlpha);
		m.color = finalColor;		// Set the alpha to the target alpha
	}
}

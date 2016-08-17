using UnityEngine;
using System;

namespace VRAssets {
	// Encapsulates VR inputs.
	public class VRInput : MonoBehaviour {
		public event Action Back;		// Called on press down of back button
		public event Action OnDown; 	// Called on press down of Fire1
		public event Action OnUp; 		// Called on release of Fire1

		// Use this for initialization
		void Start () {
		
		}
		
		// Update is called once per frame
		void Update () {
			CheckInput();
		}

		private void CheckInput() {
			// Check for a press down of the fire button
			if (Input.GetButtonDown ("Fire1")) {
				if (OnDown != null)
					OnDown ();
			}

			if (Input.GetButtonUp ("Fire1")) {
				if (OnUp != null)
					OnUp ();
			} 

			if (Input.GetKeyDown(KeyCode.Escape)) {
				if (Back != null) 
					Back ();
			}
		}
	}
}
